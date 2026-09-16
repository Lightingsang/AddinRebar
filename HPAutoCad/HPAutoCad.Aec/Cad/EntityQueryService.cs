using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Model;
using HPRebar.McpBridge.Core.Scripting;
using AcadException = Autodesk.AutoCAD.Runtime.Exception;

namespace HPAutoCad.Aec.Cad;

/// <summary>What a query produced before paging/projection: the records kept, how many matched, and what went wrong.</summary>
public sealed class EntityQueryResult
{
    public List<AecEntityRecord> Records { get; } = [];

    /// <summary>Entities matching the filter (before offset/limit); equals the candidates examined when not truncated.</summary>
    public int Count { get; set; }

    /// <summary>The candidate cap stopped the scan: <see cref="Count"/> is a lower bound.</summary>
    public bool Truncated { get; set; }

    public List<ToolError> Errors { get; } = [];

    public List<string> Warnings { get; } = [];
}

/// <summary>
///     Selects entities for every AEC tool. Broad phase = AutoCAD's own selection engine
///     (<see cref="SelectionFilter"/>: type, layer, layout, block name, linetype — wildcards included), or
///     the handle list when one is given; narrow phase = the criteria the engine cannot express (text,
///     visibility, non-ACI colours), applied while paging so only the returned page is read in full.
///     Order is by handle, so paging is stable across calls.
/// </summary>
public static class EntityQueryService
{
    public const int DefaultMaxCandidates = 5000;

    public static EntityQueryResult Query(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct,
        EntityFilter filter, GeometryTolerance tol, int limit, int offset, bool detail, int maxCandidates = DefaultMaxCandidates)
    {
        var result = new EntityQueryResult();
        var reader = new EntityShapeReader(db, tr, units, tol);

        IEnumerable<ObjectId> candidates;
        if (filter.Handles.Count > 0) candidates = ResolveHandles(db, filter.Handles, result);
        else if (!TryBuildSelection(db, ed, filter, result, out var selected)) return result;
        else candidates = selected;

        var examined = 0;
        var matched = 0;
        foreach (var id in candidates.OrderBy(id => id.Handle.Value))
        {
            ct.ThrowIfCancellationRequested();
            if (examined++ >= maxCandidates)
            {
                result.Truncated = true;
                result.Warnings.Add($"Stopped after {maxCandidates} candidates; narrow the filter (types/layers/space) or raise maxCandidates.");
                break;
            }

            Entity? entity;
            try
            {
                entity = tr.GetObject(id, OpenMode.ForRead, false) as Entity;
            }
            catch (AcadException exception)
            {
                result.Errors.Add(ToolError.ForHandle(ToolErrorCode.InvalidHandle, id.Handle.ToString(), $"Entity {id.Handle} could not be opened: {exception.ErrorStatus}."));
                continue;
            }

            if (entity is null || !PostFilter(entity, filter, reader)) continue;
            matched++;
            if (matched <= offset) continue;
            if (result.Records.Count >= limit) continue; // keep counting for `count`, stop reading geometry
            result.Records.Add(reader.Read(entity, detail));
        }

        result.Count = matched;
        return result;
    }

    /// <summary>Handles → ids; every bad handle becomes an error entry and the rest still run.</summary>
    public static List<ObjectId> ResolveHandles(Database db, IReadOnlyList<string> handles, EntityQueryResult result)
    {
        var ids = new List<ObjectId>(handles.Count);
        foreach (var handle in handles)
        {
            var id = HandleResolver.Resolve(db, handle, out var error);
            if (error is not null) result.Errors.Add(error);
            else ids.Add(id);
        }

        return ids;
    }

    private static bool TryBuildSelection(Database db, Editor ed, EntityFilter filter, EntityQueryResult result, out ObjectId[] ids)
    {
        ids = [];
        var values = new List<TypedValue>();
        if (filter.Types.Count > 0) values.Add(new TypedValue((int)DxfCode.Start, string.Join(",", filter.Types)));
        if (filter.Layers.Count > 0) values.Add(new TypedValue((int)DxfCode.LayerName, string.Join(",", filter.Layers)));
        if (filter.Linetypes.Count > 0) values.Add(new TypedValue((int)DxfCode.LinetypeName, string.Join(",", filter.Linetypes)));
        // Block names are matched after the selection on the *effective* name (a dynamic block reference's DXF name is an anonymous *U12),
        // so the engine only narrows to block references here.
        if (filter.BlockNames.Count > 0 && filter.Types.Count == 0) values.Add(new TypedValue((int)DxfCode.Start, "INSERT"));

        var space = filter.Space.Trim();
        switch (space.ToLowerInvariant())
        {
            case "model":
                values.Add(new TypedValue((int)DxfCode.LayoutName, "Model"));
                break;
            case "current":
                values.Add(new TypedValue((int)DxfCode.LayoutName, LayoutManager.Current.CurrentLayout));
                break;
            case "all":
            case "":
                break;
            default:
                if (!LayoutExists(db, space))
                {
                    result.Warnings.Add($"space '{space}' is not a layout of this drawing (use model, current, all, or a layout name).");
                    return true;
                }

                values.Add(new TypedValue((int)DxfCode.LayoutName, space));
                break;
        }

        PromptSelectionResult selection;
        try
        {
            selection = values.Count == 0 ? ed.SelectAll() : ed.SelectAll(new SelectionFilter(values.ToArray()));
        }
        catch (AcadException exception)
        {
            result.Errors.Add(ToolError.Argument($"AutoCAD rejected the selection filter ({exception.ErrorStatus}); check the type/layer patterns."));
            return false;
        }

        if (selection.Status != PromptStatus.OK || selection.Value is null) return true; // nothing matched — a valid, empty answer
        ids = selection.Value.GetObjectIds();
        return true;
    }

    private static bool PostFilter(Entity entity, EntityFilter filter, EntityShapeReader reader)
    {
        if (filter.Colors.Count > 0 && !MatchesColor(entity, filter.Colors)) return false;
        if (filter.VisibleOnly && !reader.IsVisible(entity)) return false;
        if (filter.BlockNames.Count > 0 && (entity is not BlockReference block || !EntityFilter.Matches(filter.BlockNames, reader.EffectiveBlockName(block)))) return false;
        if (filter.TextContains is { } needle && !TextOf(entity, reader).Contains(needle, StringComparison.OrdinalIgnoreCase)) return false;
        // The handle path skipped the selection engine, so its criteria are applied here too.
        if (filter.Handles.Count > 0)
        {
            var type = entity.ObjectId.ObjectClass.DxfName ?? entity.ObjectId.ObjectClass.Name;
            if (!EntityFilter.Matches(filter.Types, type) || !EntityFilter.Matches(filter.Layers, entity.Layer) || !EntityFilter.Matches(filter.Linetypes, entity.Linetype)) return false;
            if (!filter.Space.Equals("all", StringComparison.OrdinalIgnoreCase) && !SpaceMatches(reader.SpaceOf(entity), filter.Space)) return false;
        }

        return true;
    }

    private static bool LayoutExists(Database db, string name)
    {
        try
        {
            using var tr = db.TransactionManager.StartOpenCloseTransaction();
            var layouts = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
            foreach (var entry in layouts)
                if (string.Equals(entry.Key, name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
        catch (AcadException)
        {
            return true; // cannot tell: let the selection engine answer
        }
    }

    private static bool SpaceMatches(string actual, string wanted) => wanted.ToLowerInvariant() switch
    {
        "model" => actual == "Model",
        "current" => string.Equals(actual, LayoutManager.Current.CurrentLayout, StringComparison.OrdinalIgnoreCase) || (actual == "Model" && LayoutManager.Current.CurrentLayout == "Model"),
        _ => string.Equals(actual, wanted, StringComparison.OrdinalIgnoreCase),
    };

    private static bool MatchesColor(Entity entity, IReadOnlyList<string> wanted)
    {
        var color = entity.Color;
        var actual = color.IsByLayer ? "ByLayer" : color.IsByBlock ? "ByBlock" : color.IsByAci ? color.ColorIndex.ToString() : color.ColorNameForDisplay;
        return wanted.Any(w => string.Equals(w, actual, StringComparison.OrdinalIgnoreCase));
    }

    private static string TextOf(Entity entity, EntityShapeReader reader) => entity switch
    {
        DBText t => t.TextString,
        MText m => m.Text,
        Dimension d => d.DimensionText ?? string.Empty,
        BlockReference b => reader.EffectiveBlockName(b) + " " + string.Join(" ", reader.AttributeValues(b)),
        _ => string.Empty,
    };

    /// <summary>
    ///     The JSON the AI receives per entity: the summary keys by default, everything in detail mode, or exactly the
    ///     <paramref name="properties"/> asked for (handle always). Unknown property names are ignored by the caller's warning.
    /// </summary>
    public static Dictionary<string, object?> Project(AecEntityRecord record, IReadOnlySet<string>? properties, bool detail)
    {
        var item = new Dictionary<string, object?>(StringComparer.Ordinal) { ["handle"] = record.Handle, ["type"] = record.Type, ["layer"] = record.Layer };
        bool Wants(string key) => properties is null ? detail || DefaultKeys.Contains(key) : properties.Contains(key);

        if (Wants("space")) item["space"] = record.Space;
        if (Wants("bounds")) item["boundsMm"] = record.BoundsMm;
        if (Wants("color")) item["color"] = record.Color;
        if (Wants("linetype")) item["linetype"] = record.Linetype;
        if (Wants("lineweight")) item["lineweight"] = record.Lineweight;
        if (Wants("visible")) item["visible"] = record.Visible;
        if (Wants("length") && record.LengthMm is not null) item["lengthMm"] = record.LengthMm;
        if (Wants("area") && record.AreaMm2 is not null) item["areaMm2"] = record.AreaMm2;
        if (Wants("text") && record.Text is not null) item["text"] = record.Text;
        if (Wants("block") && record.BlockName is not null) item["blockName"] = record.BlockName;
        if (Wants("attributes") && record.Attributes is not null) item["attributes"] = record.Attributes;
        if (Wants("position") && record.PositionMm is not null) item["positionMm"] = record.PositionMm;
        if (Wants("geometry")) item["geometry"] = record.Geometry ?? record.ToGeometry();
        if (record.GeometryNote is not null && (detail || Wants("geometry"))) item["geometryNote"] = record.GeometryNote;
        return item;
    }

    public static readonly HashSet<string> DefaultKeys = new(StringComparer.Ordinal) { "space", "bounds", "text", "block", "length" };

    public static readonly IReadOnlyList<string> PropertyNames =
        ["space", "bounds", "color", "linetype", "lineweight", "visible", "length", "area", "text", "block", "attributes", "position", "geometry"];
}
