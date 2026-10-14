using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.GraphicsInterface;
using HPAutoCad.Aec.Model;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     Phase 2 of the conversion: writes a <see cref="VnConvertPlan"/>. A plan with a mapping error, or (atomic) a failed entity, writes
///     nothing; otherwise every planned text is written and flagged with <see cref="VnTextMarker"/>, and the planned styles are remapped
///     after all texts went through.
/// </summary>
public static class VnTextWriteService
{
    /// <summary>Outcomes listed in the envelope (errors first); the summary counts the rest so a whole drawing stays under the 64 KB cap.</summary>
    public const int MaxListedItems = 100;

    /// <summary>Block definitions / kept styles named in the summary.</summary>
    public const int MaxListedBlocks = 20;

    /// <summary>Handles kept in <c>affectedHandles</c>: a whole drawing can convert thousands of texts, the envelope must stay under 64 KB.</summary>
    public const int MaxAffectedHandles = 500;

    public static EditResult Write(EditContext cx, VnScanResult scan, VnConvertPlan plan, bool remapStyles, bool atomic, CancellationToken ct)
    {
        var result = new EditResult();
        if (plan.MappingErrors.Count > 0)
        {
            result.Settle([]);
            foreach (var error in plan.MappingErrors) result.Fail(error);
            result.Summary = new { refused = true, reason = "a legacy font has no Unicode face; nothing written", targets = scan.Targets.Count };
            return result;
        }

        var failures = plan.Failed.Select(f => new ItemOutcome(f.Index, false, f.Target.Handle, f.Target.Type, Error: f.Error)).ToList();
        if (atomic && failures.Count > 0)
        {
            result.Settle(Listed(failures));
            result.Summary = new { refused = true, reason = "atomic: an entity cannot be edited; nothing written", targets = scan.Targets.Count };
            return result;
        }

        var outcomes = new List<ItemOutcome>(failures);
        if (plan.Convert.Count > 0) VnTextMarker.EnsureRegistered(cx.Db, cx.Tr);
        foreach (var target in plan.Convert)
        {
            ct.ThrowIfCancellationRequested();
            var entity = (Entity)cx.Tr.GetObject(target.Id, OpenMode.ForWrite);
            Apply(entity, target);
            VnTextMarker.Mark(entity);
            result.Modified(target.Handle);
            outcomes.Add(new ItemOutcome(scan.Targets.IndexOf(target), true, target.Handle, target.Type, target.Fields.Select(f => f.Name).Distinct().ToArray()));
        }

        var remapped = remapStyles ? RemapStyles(cx, plan.Remaps, result) : [];
        foreach (var kept in plan.Kept.Take(MaxListedBlocks))
            result.Warn($"style '{kept.Style.Name}' ({kept.Style.FontLabel}) kept: {kept.LegacyLeft} legacy text(s) under it are not converted in this run (outside the selection, in an excluded block or table, on a locked layer, or with a field), so its selected texts were left as well — convert them together, or pass remapStyles: false.");
        result.Settle(Listed(outcomes));
        result.Summary = Summary(scan, plan, remapped, remapStyles);
        TrimAffected(result, MaxAffectedHandles);
        return result;
    }

    /// <summary>Keeps the first <paramref name="max"/> affected handles (style handles come last, so they are kept first) and says how many were dropped.</summary>
    internal static void TrimAffected(EditResult result, int max)
    {
        var total = result.AffectedHandles.Count;
        if (total <= max) return;
        var styles = result.AffectedHandles.Skip(total - Math.Min(total, max / 10)).ToArray();
        result.AffectedHandles.RemoveRange(max - styles.Length, total - (max - styles.Length));
        result.AffectedHandles.AddRange(styles);
        result.Warn($"affectedHandles lists {result.AffectedHandles.Count} of {total} modified handles (the text styles last); modifiedCount is the full count.");
    }

    private static void Apply(Entity entity, VnTextTarget target)
    {
        foreach (var field in target.Fields)
        {
            switch (entity, field.Name)
            {
                case (MText mtext, "contents"): mtext.Contents = field.Converted; break;
                case (AttributeDefinition definition, "prompt"): definition.Prompt = field.Converted; break;
                case (DBText text, "text"): text.TextString = field.Converted; break;
                case (Dimension dimension, "dimensionText"): dimension.DimensionText = field.Converted; break;
                case (MLeader leader, "mtext"):
                    using (var mtext = leader.MText)
                    {
                        mtext.Contents = field.Converted;
                        leader.MText = mtext;
                    }

                    break;
                case (Table table, "cell"): table.Cells[field.Row, field.Column].TextString = field.Converted; break;
                default: throw new InvalidOperationException($"{target.Type} {target.Handle}: no writer for field '{field.Name}'.");
            }
        }

        if (entity is Dimension changed) changed.RecomputeDimensionBlock(true);
    }

    private static List<object> RemapStyles(EditContext cx, IReadOnlyList<VnStyleRemap> remaps, EditResult result)
    {
        var remapped = new List<object>();
        foreach (var remap in remaps)
        {
            var record = (TextStyleTableRecord)cx.Tr.GetObject(remap.Style.Id, OpenMode.ForWrite);
            var old = record.Font;
            // Only the face is set: AutoCAD resolves it to the installed file (times.ttf, or times_1.ttf where Windows keeps a copy).
            // Writing FileName after Font clears the face again. Charset 0 = ANSI: the text is real Unicode, which a TrueType face draws anyway.
            record.BigFontFileName = "";
            record.Font = new FontDescriptor(remap.TargetFace!, old.Bold, old.Italic, 0, 0);
            result.Modified(remap.Style.Handle);
            remapped.Add(new { style = remap.Style.Name, from = remap.Style.FontLabel, to = record.Font.TypeFace, file = record.FileName });
        }

        return remapped;
    }

    /// <summary>Errors first, then the first successes, up to <see cref="MaxListedItems"/>, in input order.</summary>
    private static ItemOutcome[] Listed(IEnumerable<ItemOutcome> outcomes)
    {
        var all = outcomes.ToArray();
        return all.Where(o => !o.Ok).Concat(all.Where(o => o.Ok)).Take(MaxListedItems).OrderBy(o => o.Index).ToArray();
    }

    private static object Summary(VnScanResult scan, VnConvertPlan plan, List<object> remapped, bool remapStyles) => new
    {
        examined = scan.Examined,
        converted = plan.Convert.Count,
        failed = plan.Failed.Count,
        held = plan.Held.Count,
        byType = plan.Convert.GroupBy(t => t.Type).ToDictionary(g => g.Key, g => g.Count()),
        blockDefinitions = plan.Convert.Where(t => t.InBlockDefinition).GroupBy(t => t.Owner).OrderByDescending(g => g.Count()).Take(MaxListedBlocks).ToDictionary(g => g.Key, g => g.Count()),
        stylesRemapped = remapped,
        stylesKept = plan.Kept.Take(MaxListedBlocks).Select(k => new { style = k.Style.Name, font = k.Style.FontLabel, legacyLeft = k.LegacyLeft }),
        remapStyles,
        skipped = scan.Skipped,
        listed = Math.Min(plan.Convert.Count + plan.Failed.Count, MaxListedItems),
        truncated = plan.Convert.Count + plan.Failed.Count > MaxListedItems,
    };
}
