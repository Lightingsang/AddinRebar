using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HPAutoCad.Aec.Model;
using HPAutoCad.Aec.Structural;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     The write half of the structural tools: <c>structural_tag_members</c> writes each decided mark where the member keeps it —
///     the block reference's MARK attribute, the existing mark TEXT edited in place, or a new middle-centred TEXT at the member's
///     centre on a tag layer created on demand; <c>structural_generate_member_schedule</c> can draw the schedule as an AutoCAD
///     Table. Both are two-phase (nothing opened for write before every item is validated) and never touch the member geometry.
/// </summary>
public static partial class StructuralWriteService
{
    public const string DefaultTagLayer = "S-ANNO-TEXT";
    public const double DefaultTagHeightMm = 200;
    public const string MarkAttributeTag = StructuralMember.MarkAttributeTag;
    private const short DefaultTagColor = 7;

    /// <summary>What phase 1 decided for one member: the block whose attribute takes the mark, the text edited in place, or a new text.</summary>
    private sealed record Plan(BlockReference? Block, DBText? Text);

    public static EditResult WriteTags(EditContext cx, CancellationToken ct, IReadOnlyList<MemberTag> tags, string? layerName, double textHeightMm, string? space, bool dryDecisionsOnly, IReadOnlyList<string> duplicates)
    {
        var result = new EditResult();
        var layer = string.IsNullOrWhiteSpace(layerName) ? DefaultTagLayer : layerName.Trim();
        var spaceId = cx.SpaceId(space, out var spaceError);
        if (spaceId.IsNull) throw new ArgumentException(spaceError!.Message);
        if (textHeightMm <= 0) throw new ArgumentException("textHeightMm must be > 0.");
        if (EditContext.RefuseSymbolName(layer, "layer") is { } badName) throw new ArgumentException(badName);
        if (cx.FindLayer(layer, out _) is { } existing)
        {
            if (existing.IsLocked) return EditResult.Refused(new ToolError(ToolErrorCode.LayerLocked, $"tag layer '{layer}' is locked; unlock it or pass another layer."));
            if (existing.IsFrozen) return EditResult.Refused(new ToolError(ToolErrorCode.LayerFrozen, $"tag layer '{layer}' is frozen; thaw it or pass another layer."));
            if (existing.IsOff) result.Warn($"tag layer '{layer}' is off: the marks exist but are not displayed until it is turned on.");
        }

        foreach (var d in duplicates) result.Warn($"existing mark {d} is carried by several members; kept as they are — renumber with overwrite: true.");

        // Phase 1: every write target is readable and writable — the member's MARK attribute, or the mark text to edit in place. Nothing is opened for write.
        var outcomes = new ItemOutcome[tags.Count];
        var plans = new Plan?[tags.Count];
        for (var i = 0; i < tags.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var tag = tags[i];
            if (tag.Kept) { outcomes[i] = new ItemOutcome(i, true, tag.Member.Handle, tag.Member.Kind, ["mark:" + tag.Mark, tag.Outcome]); continue; }
            if (tag.Member.MarkSource == StructuralMember.MarkFromText && tag.Member.MarkHandle is { } textHandle)
            {
                var entity = cx.OpenForEdit(textHandle, out var textError);
                if (entity is DBText text) plans[i] = new Plan(null, text);
                else outcomes[i] = new ItemOutcome(i, false, tag.Member.Handle, tag.Member.Kind, Error: textError ?? ToolError.ForHandle(ToolErrorCode.UnsupportedEntity, textHandle, $"mark {textHandle} is not a TEXT; it cannot be renumbered in place."));
                continue;
            }

            if (cx.OpenForEdit(tag.Member.Handle, out var error) is BlockReference block)
            {
                var errors = new List<ToolError>();
                var warnings = new List<string>();
                BlockService.ValidateAttributes(cx, block, Values(tag.Mark!), errors, warnings);
                if (errors.Count == 0 && warnings.Count == 0) plans[i] = new Plan(block, null);
                else if (errors.Count > 0 && errors[0].Code != ToolErrorCode.InvalidArgument) outcomes[i] = new ItemOutcome(i, false, tag.Member.Handle, "INSERT", Error: errors[0]);
                // a block without a MARK attribute gets a text tag like any other member
            }
            else if (error is { Code: not (ToolErrorCode.LayerLocked or ToolErrorCode.LayerFrozen) })
            {
                outcomes[i] = new ItemOutcome(i, false, tag.Member.Handle, tag.Member.Kind, Error: error);
            }
            // a locked member still gets a text beside it: only attribute writes need the member itself
        }

        if (dryDecisionsOnly)
        {
            result.Items = tags.Select((t, i) => outcomes[i] ?? new ItemOutcome(i, true, t.Member.Handle, t.Member.Kind, ["mark:" + t.Mark, t.Outcome, plans[i] is { Block: not null } ? "via:attribute" : plans[i] is { Text: not null } ? "via:text" : "via:newText"])).ToArray();
            result.Summary = Summary(tags, layer, false, []);
            return result;
        }

        if (outcomes.Any(o => o is { Ok: false }))
        {
            result.Settle(tags.Select((t, i) => outcomes[i] ?? new ItemOutcome(i, false, t.Member.Handle, t.Member.Kind)).ToArray());
            result.Summary = new { requested = tags.Count, tagged = 0, refused = true, reason = "atomic: a mark cannot be written for every member (see errors), nothing tagged" };
            return result;
        }

        var pending = Enumerable.Range(0, tags.Count).Where(i => outcomes[i] is null).ToArray();
        if (pending.Length == 0)
        {
            result.Settle(outcomes);
            result.Summary = Summary(tags, layer, false, []);
            return result;
        }

        // Phase 2: attributes and in-place texts first (no layer needed), then the space + layer for the new texts.
        var written = new List<object>();
        var layerId = ObjectId.Null;
        BlockTableRecord? target = null;
        var layerCreated = false;
        foreach (var i in pending)
        {
            ct.ThrowIfCancellationRequested();
            var tag = tags[i];
            if (plans[i] is { Block: { } block })
            {
                if (!cx.Upgrade(block, out var upgradeError)) throw new InvalidOperationException(upgradeError!.Message);
                BlockService.WriteAttributes(cx, block, Values(tag.Mark!));
                result.Modified(block.Handle.ToString());
                outcomes[i] = new ItemOutcome(i, true, block.Handle.ToString(), "INSERT", ["mark:" + tag.Mark, "attribute:" + MarkAttributeTag]);
                written.Add(new { handle = tag.Member.Handle, mark = tag.Mark, via = "attribute" });
                continue;
            }

            if (plans[i] is { Text: { } existingText })
            {
                if (!cx.Upgrade(existingText, out var upgradeError)) throw new InvalidOperationException(upgradeError!.Message);
                existingText.TextString = tag.Mark!;
                existingText.AdjustAlignment(cx.Db);
                var textHandle = existingText.Handle.ToString();
                result.Modified(textHandle);
                outcomes[i] = new ItemOutcome(i, true, textHandle, "TEXT", ["mark:" + tag.Mark, "was:" + tag.ExistingMark, "for:" + tag.Member.Handle]);
                written.Add(new { handle = tag.Member.Handle, mark = tag.Mark, via = "text", textHandle, was = tag.ExistingMark });
                continue;
            }

            target ??= (BlockTableRecord)cx.Tr.GetObject(spaceId, OpenMode.ForWrite);
            if (layerId.IsNull) layerId = EnsureLayer(cx, layer, out layerCreated);
            var at = new Point3d(cx.ToDrawing(tag.Member.CenterMm.X), cx.ToDrawing(tag.Member.CenterMm.Y), 0);
            var text = new DBText();
            text.SetDatabaseDefaults(cx.Db);
            text.TextString = tag.Mark!;
            text.Height = cx.ToDrawing(textHeightMm);
            text.Justify = AttachmentPoint.MiddleCenter;
            text.AlignmentPoint = at;
            text.LayerId = layerId;
            target.AppendEntity(text);
            cx.Tr.AddNewlyCreatedDBObject(text, true);
            text.AdjustAlignment(cx.Db);
            var handle = text.Handle.ToString();
            result.Created(handle);
            outcomes[i] = new ItemOutcome(i, true, handle, "TEXT", ["mark:" + tag.Mark, "for:" + tag.Member.Handle]);
            written.Add(new { handle = tag.Member.Handle, mark = tag.Mark, via = "newText", textHandle = handle });
        }

        result.Settle(outcomes);
        result.Summary = Summary(tags, layer, layerCreated, written);
        return result;
    }

    private static object Summary(IReadOnlyList<MemberTag> tags, string layer, bool layerCreated, List<object> written) => new
    {
        requested = tags.Count,
        assigned = tags.Count(t => t.Outcome == MemberTag.Assigned),
        overwritten = tags.Count(t => t.Outcome == MemberTag.Overwritten),
        keptExisting = tags.Count(t => t.Outcome == MemberTag.KeptExisting),
        keptForeign = tags.Count(t => t.Outcome == MemberTag.KeptForeign),
        byKind = tags.GroupBy(t => t.Member.Kind).ToDictionary(g => g.Key, g => g.Count()),
        layer,
        layerCreated,
        marks = tags.Select(t => new { handle = t.Member.Handle, kind = t.Member.Kind, mark = t.Mark, outcome = t.Outcome, existing = t.ExistingMark, markHandle = t.Member.MarkHandle }).ToArray(),
        written,
    };

    private static ScriptArgs Values(string mark) => new(System.Text.Json.JsonSerializer.SerializeToElement(new Dictionary<string, string> { [MarkAttributeTag] = mark }));

    private static ObjectId EnsureLayer(EditContext cx, string name, out bool created)
    {
        var table = (LayerTable)cx.Tr.GetObject(cx.Db.LayerTableId, OpenMode.ForRead);
        created = false;
        if (table.Has(name)) return table[name];
        table.UpgradeOpen();
        var record = new LayerTableRecord { Name = name, Color = Color.FromColorIndex(ColorMethod.ByAci, DefaultTagColor) };
        var id = table.Add(record);
        cx.Tr.AddNewlyCreatedDBObject(record, true);
        created = true;
        return id;
    }
}
