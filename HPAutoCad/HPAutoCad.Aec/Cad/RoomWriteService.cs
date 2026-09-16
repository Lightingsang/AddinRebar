using System.Text.Json;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HPAutoCad.Aec.Architecture;
using HPAutoCad.Aec.Model;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     The write half of the architecture tools: <c>arch_create_room_tags</c> puts one MTEXT (or one block reference with attributes)
///     per room at the room's inside point, rendered from a format with placeholders; <c>arch_auto_dimension_plan</c> draws the
///     planned aligned dimensions. Two-phase: nothing is opened for write before every tag / dimension is validated.
/// </summary>
public static partial class RoomWriteService
{
    public const string DefaultTagLayer = "A-ANNO-ROOM";
    public const string DefaultFormat = "{name}\\P{areaM2} m²";
    public const double DefaultTagHeightMm = 250;
    private const short DefaultTagColor = 7;
    public static readonly IReadOnlyList<string> Placeholders = ["id", "name", "number", "department", "areaM2", "areaMm2", "perimeterMm"];

    /// <summary>One room's rendered tag and, for a block tag, the attribute values.</summary>
    private sealed record TagPlan(Room Room, string Text, Dictionary<string, string>? Attributes);

    public static EditResult WriteTags(EditContext cx, CancellationToken ct, IReadOnlyList<Room> rooms, string? format, string? layerName, double textHeightMm, string? space, string? blockName, ScriptArgs attributeFormats, bool dryDecisionsOnly)
    {
        var result = new EditResult();
        var layer = string.IsNullOrWhiteSpace(layerName) ? DefaultTagLayer : layerName.Trim();
        var spaceId = cx.SpaceId(space, out var spaceError);
        if (spaceId.IsNull) throw new ArgumentException(spaceError!.Message);
        if (textHeightMm <= 0) throw new ArgumentException("textHeightMm must be > 0.");
        if (cx.CheckAnnotationLayer(layer, "tag", out var layerWarning) is { } layerError) return EditResult.Refused(layerError);
        if (layerWarning is not null) result.Warn(layerWarning);
        var template = string.IsNullOrWhiteSpace(format) ? DefaultFormat : format;
        ObjectId definitionId = ObjectId.Null;
        if (!string.IsNullOrWhiteSpace(blockName))
        {
            var table = (BlockTable)cx.Tr.GetObject(cx.Db.BlockTableId, OpenMode.ForRead);
            if (!table.Has(blockName)) throw new ArgumentException($"blockName '{blockName}' is not a defined block (manage_blocks_attributes listDefinitions shows them).");
            definitionId = table[blockName];
            var record = (BlockTableRecord)cx.Tr.GetObject(definitionId, OpenMode.ForRead);
            if (record.IsLayout || record.IsAnonymous || record.IsFromExternalReference) throw new ArgumentException($"blockName '{blockName}' is a layout, anonymous or xref block; a tag block is a plain definition.");
            // every attribute the caller wants to fill must exist on the definition: a typo would insert every block with default values
            var tags = BlockService.AttributeTags(cx.Tr, definitionId);
            var unknown = attributeFormats.Keys.Where(k => !tags.Contains(k, StringComparer.OrdinalIgnoreCase)).ToList();
            if (unknown.Count > 0) throw new ArgumentException($"attributes: block '{blockName}' has no attribute {string.Join(", ", unknown)} (its tags: {(tags.Count == 0 ? "none" : string.Join(", ", tags))}).");
        }

        // Phase 1: every tag renders (unknown placeholders are the caller's error) — nothing opened for write.
        var plans = new List<TagPlan>();
        foreach (var room in rooms)
        {
            ct.ThrowIfCancellationRequested();
            var attributes = definitionId.IsNull ? null : attributeFormats.Keys.ToDictionary(k => k, k => Render(attributeFormats.Str(k) ?? "", room), StringComparer.OrdinalIgnoreCase);
            plans.Add(new TagPlan(room, definitionId.IsNull ? Render(template, room) : string.Join(" | ", attributes!.Select(kv => $"{kv.Key}={kv.Value}")), attributes));
        }

        if (dryDecisionsOnly)
        {
            result.Items = plans.Select((p, i) => new ItemOutcome(i, true, null, definitionId.IsNull ? "MTEXT" : "INSERT", ["room:" + p.Room.Id, "text:" + p.Text.Replace("\\P", " / ")])).ToArray();
            result.Summary = Summary(plans, layer, false, definitionId.IsNull ? null : blockName, null);
            return result;
        }

        // Phase 2.
        var target = (BlockTableRecord)cx.Tr.GetObject(spaceId, OpenMode.ForWrite);
        var layerId = cx.EnsureLayer(layer, DefaultTagColor, out var layerCreated);
        var outcomes = new ItemOutcome[plans.Count];
        var written = new List<object>();
        for (var i = 0; i < plans.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var plan = plans[i];
            var at = new Point3d(cx.ToDrawing(plan.Room.LabelPointMm.X), cx.ToDrawing(plan.Room.LabelPointMm.Y), 0);
            Entity entity;
            if (definitionId.IsNull)
            {
                var text = new MText();
                text.SetDatabaseDefaults(cx.Db);
                text.Contents = plan.Text;
                text.TextHeight = cx.ToDrawing(textHeightMm);
                text.Attachment = AttachmentPoint.MiddleCenter;
                text.Location = at;
                text.LayerId = layerId;
                entity = text;
                target.AppendEntity(entity);
                cx.Tr.AddNewlyCreatedDBObject(entity, true);
            }
            else
            {
                var block = new BlockReference(at, definitionId);
                block.SetDatabaseDefaults(cx.Db);
                block.LayerId = layerId;
                entity = block;
                target.AppendEntity(entity);
                cx.Tr.AddNewlyCreatedDBObject(entity, true);
                var unmatched = BlockService.AppendAttributes(cx.Tr, block, definitionId, new ScriptArgs(JsonSerializer.SerializeToElement(plan.Attributes!)));
                foreach (var w in unmatched) result.WarnItem(i, w);
            }

            var handle = entity.Handle.ToString();
            result.Created(handle);
            outcomes[i] = new ItemOutcome(i, true, handle, definitionId.IsNull ? "MTEXT" : "INSERT", ["room:" + plan.Room.Id]);
            written.Add(new { room = plan.Room.Id, handle, text = plan.Text });
        }

        result.Settle(outcomes);
        result.Summary = Summary(plans, layer, layerCreated, definitionId.IsNull ? null : blockName, written);
        return result;
    }

    /// <summary>The format with every {placeholder} replaced (MTEXT line breaks are \P); a placeholder the tool does not know is the caller's error.</summary>
    public static string Render(string format, Room room)
    {
        var rendered = Regex.Replace(format, @"\{(\w+)\}", m => m.Groups[1].Value.ToLowerInvariant() switch
        {
            "id" => room.Id,
            "name" => room.Name ?? "",
            "number" => room.Number ?? "",
            "department" => room.Department ?? "",
            "aream2" => room.AreaM2.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
            "areamm2" => Math.Round(room.AreaMm2).ToString(System.Globalization.CultureInfo.InvariantCulture),
            "perimetermm" => Math.Round(room.PerimeterMm).ToString(System.Globalization.CultureInfo.InvariantCulture),
            _ => throw new ArgumentException($"format: '{{{m.Groups[1].Value}}}' is not a placeholder (known: {string.Join(", ", Placeholders.Select(p => "{" + p + "}"))})."),
        });
        // an empty name leaves an empty first line: drop blank lines rather than tag "\P24.5 m²"
        var lines = rendered.Split("\\P").Select(l => l.Trim()).Where(l => l.Length > 0);
        return string.Join("\\P", lines) is { Length: > 0 } text ? text : room.Id;
    }

    /// <summary>Before the write the plan is listed (tags); after it only what was written (the items carry the rest), so a page of 120 tags is never echoed twice.</summary>
    private static object Summary(List<TagPlan> plans, string layer, bool layerCreated, string? blockName, List<object>? written) => new
    {
        requested = plans.Count,
        tagged = written?.Count ?? 0,
        layer,
        layerCreated,
        blockName,
        tags = written is null ? plans.Select(p => new { room = p.Room.Id, name = p.Room.Name, number = p.Room.Number, text = p.Text, atMm = p.Room.LabelPointMm.Rounded() }).ToArray() : null,
        written,
    };
}
