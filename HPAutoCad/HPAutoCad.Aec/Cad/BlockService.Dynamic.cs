using Autodesk.AutoCAD.DatabaseServices;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Model;
using HPRebar.McpBridge.Core.Scripting;
using AcadException = Autodesk.AutoCAD.Runtime.Exception;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     The dynamic-block half of <see cref="BlockService"/>: <c>inspectDynamic</c> lists a reference's dynamic properties with
///     distances in mm, areas in mm² and angles in degrees; <c>setDynamic</c> validates every requested property first
///     (exists, not read-only) and writes them all with the same conversions — a value the property rejects aborts the run,
///     so a partially changed block is never committed.
/// </summary>
public static partial class BlockService
{
    /// <summary>A dynamic reference lists ~10 properties of ~150 B: this many per call stays under the 64 KB cap.</summary>
    public const int MaxDynamicHandles = 20;

    public static AnalysisResult<Dictionary<string, object?>> InspectDynamic(EditContext cx, IReadOnlyList<string> handles)
    {
        if (handles.Count == 0) throw new ArgumentException("inspectDynamic needs handles [] of block references.");
        var result = new AnalysisResult<Dictionary<string, object?>>();
        handles = CapHandles(handles, MaxDynamicHandles, result);
        var items = new List<Dictionary<string, object?>>();
        var reader = new EntityShapeReader(cx.Db, cx.Tr, cx.Units, GeometryTolerance.Default);
        foreach (var handle in handles)
        {
            var block = OpenBlock(cx, handle, result.Errors);
            if (block is null) continue;
            var properties = new List<object>();
            if (block.IsDynamicBlock)
                foreach (DynamicBlockReferenceProperty p in block.DynamicBlockReferencePropertyCollection)
                {
                    if (!p.Show) continue;
                    properties.Add(new
                    {
                        name = p.PropertyName,
                        value = Outbound(cx, p, p.Value),
                        unitsType = p.UnitsType.ToString(),
                        readOnly = p.ReadOnly,
                        description = string.IsNullOrEmpty(p.Description) ? null : p.Description,
                        allowedValues = p.GetAllowedValues() is { Length: > 0 } allowed ? allowed.Select(v => Outbound(cx, p, v)).ToArray() : null,
                    });
                }

            items.Add(new Dictionary<string, object?> { ["handle"] = block.Handle.ToString(), ["blockName"] = reader.EffectiveBlockName(block), ["isDynamic"] = block.IsDynamicBlock, ["properties"] = properties });
        }

        result.Items = items;
        result.Count = items.Count;
        result.Success = items.Count > 0 || result.Errors.Count == 0;
        result.Summary = new { requested = handles.Count, read = items.Count, dynamic = items.Count(i => (bool)i["isDynamic"]!) };
        return result;
    }

    public static EditResult SetDynamic(EditContext cx, string? handle, ScriptArgs properties)
    {
        if (properties.IsEmpty) throw new ArgumentException("setDynamic needs handles [handle] and properties {name: value} (distances in mm, angles in degrees).");
        var entity = cx.OpenForEdit(handle, out var error);
        if (entity is null) return EditResult.Refused(error!, type: "INSERT");
        if (entity is not BlockReference block) return EditResult.Refused(NotABlock(entity), entity.Handle.ToString(), "INSERT");
        var h = block.Handle.ToString();
        if (!block.IsDynamicBlock) return EditResult.Refused(ToolError.ForHandle(ToolErrorCode.UnsupportedEntity, h, $"{h} is not a dynamic block reference."), h, "INSERT");

        // Phase 1: every requested property must exist and be writable, or nothing is set.
        var result = new EditResult();
        var byName = new Dictionary<string, DynamicBlockReferenceProperty>(StringComparer.OrdinalIgnoreCase);
        foreach (DynamicBlockReferenceProperty p in block.DynamicBlockReferencePropertyCollection) byName[p.PropertyName] = p;
        var refusals = new List<ToolError>();
        foreach (var name in properties.Keys)
        {
            if (!byName.TryGetValue(name, out var p)) refusals.Add(ToolError.ForHandle(ToolErrorCode.InvalidArgument, h, $"{h} has no dynamic property '{name}' (properties: {string.Join(", ", byName.Keys)})."));
            else if (p.ReadOnly) refusals.Add(ToolError.ForHandle(ToolErrorCode.InvalidArgument, h, $"dynamic property '{p.PropertyName}' is read-only."));
        }

        if (refusals.Count > 0)
        {
            result.Settle([new ItemOutcome(0, false, h, "INSERT", Error: refusals[0])]);
            result.Errors.AddRange(refusals.Skip(1));
            result.Summary = new { handle = h, refused = true, reason = $"{refusals.Count} property refusal(s), nothing changed" };
            return result;
        }

        // Phase 2: write; a value the property rejects aborts the whole run (the bridge rolls back).
        if (!cx.Upgrade(block, out error)) return EditResult.Refused(error!, h, "INSERT");
        var changed = new List<string>();
        foreach (var name in properties.Keys)
        {
            var p = byName[name];
            try
            {
                p.Value = Inbound(cx, p, properties);
                changed.Add(p.PropertyName);
            }
            catch (Exception exception) when (exception is AcadException or InvalidCastException or FormatException or OverflowException)
            {
                throw new ArgumentException($"dynamic property '{p.PropertyName}' of {h} refused the value (type {p.Value?.GetType().Name}): {exception.Message} Nothing kept.");
            }
        }

        result.Modified(h);
        result.Items = [new ItemOutcome(0, true, h, "INSERT", changed)];
        result.Summary = new { handle = h, changed };
        return result;
    }

    private static object? Outbound(EditContext cx, DynamicBlockReferenceProperty p, object? value) => value switch
    {
        double d when p.UnitsType == DynamicBlockReferencePropertyUnitsType.Distance => Math.Round(cx.ToMm(d), 3),
        double d when p.UnitsType == DynamicBlockReferencePropertyUnitsType.Angular => Math.Round(d * 180 / Math.PI, 3),
        double d when p.UnitsType == DynamicBlockReferencePropertyUnitsType.Area => Math.Round(cx.ToMm(cx.ToMm(d)), 1),
        double d => Math.Round(d, 6),
        _ => value,
    };

    private static object Inbound(EditContext cx, DynamicBlockReferenceProperty p, ScriptArgs values)
    {
        var name = p.PropertyName;
        return p.Value switch
        {
            double when p.UnitsType == DynamicBlockReferencePropertyUnitsType.Distance => cx.ToDrawing(values.Double(name)),
            double when p.UnitsType == DynamicBlockReferencePropertyUnitsType.Angular => values.Double(name) * Math.PI / 180,
            double when p.UnitsType == DynamicBlockReferencePropertyUnitsType.Area => cx.ToDrawing(cx.ToDrawing(values.Double(name))),
            double => values.Double(name),
            short => (short)values.Int(name),
            int => values.Int(name),
            long => values.Long(name),
            bool => values.Bool(name),
            _ => values.Str(name) ?? "",
        };
    }
}
