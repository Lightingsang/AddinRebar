using System.Diagnostics;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPAutoCad.Aec.Cad;
using HPAutoCad.Aec.Model;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec;

/// <summary>
///     The write tools. Every one runs inside the bridge's <c>auto</c> transaction — <c>dryRun</c> on the request rolls
///     it back — and returns the edit envelope for write ops or the analysis envelope for the read ops that live under
///     the same tool (list/find/read/inspect/detect/status). A request that cannot start throws <see cref="ArgumentException"/>.
/// </summary>
public static partial class AecTools
{
    public static EditResult CreateEntitiesBatch(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        IReadOnlyList<ScriptArgs> items, string? space, bool atomic)
    {
        var watch = Stopwatch.StartNew();
        var result = BatchEditService.CreateBatch(new EditContext(db, tr, units), items, space, atomic, ct);
        log($"create_entities_batch: {items.Count} item(s) → {result.CreatedCount} created, {result.Errors.Count} error(s), atomic={atomic}, {watch.ElapsedMilliseconds} ms");
        return result;
    }

    public static EditResult UpdateEntitiesBatch(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        IReadOnlyList<ScriptArgs> items, IReadOnlyList<string> handles, ScriptArgs set, bool atomic)
    {
        var watch = Stopwatch.StartNew();
        var result = BatchEditService.UpdateBatch(new EditContext(db, tr, units), items, handles, set, atomic, ct);
        log($"update_entities_batch: {Math.Max(items.Count, handles.Count)} item(s) → {result.ModifiedCount} modified, {result.Errors.Count} error(s), atomic={atomic}, {watch.ElapsedMilliseconds} ms");
        return result;
    }

    public static object ManageBlocksAttributes(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        string? op, string? namePattern, bool includeAnonymous, ScriptArgs filterArgs, IReadOnlyList<string> handles, IReadOnlyList<ScriptArgs> items, ScriptArgs attributes, ScriptArgs properties,
        ScriptArgs insert, string? space, bool atomic, int limit, int offset, int maxCandidates)
    {
        var watch = Stopwatch.StartNew();
        var cx = new EditContext(db, tr, units);
        limit = Math.Clamp(limit, 1, MaxLimit);
        offset = Math.Max(0, offset);
        maxCandidates = Math.Clamp(maxCandidates, 1, MaxCandidatesCeiling);
        object result = Op(op, BlockService.Ops) switch
        {
            "listdefinitions" => BlockService.ListDefinitions(cx, ct, namePattern, includeAnonymous, limit, offset),
            "findreferences" => FindReferences(cx, ed, ct, filterArgs, handles, limit, offset, maxCandidates),
            "insert" => BlockService.Insert(cx, insert, space),
            "readattributes" => BlockService.ReadAttributes(cx, handles),
            "writeattributes" => BlockService.WriteAttributesOp(cx, [], handles, attributes, atomic, ct),
            "batchupdateattributes" => BlockService.WriteAttributesOp(cx, items, ReferencesFor(cx, ed, ct, filterArgs, handles, items, maxCandidates), attributes, atomic, ct),
            "inspectdynamic" => BlockService.InspectDynamic(cx, handles),
            "setdynamic" => BlockService.SetDynamic(cx, handles.FirstOrDefault(), properties),
            _ => throw new UnreachableException(),
        };
        log($"manage_blocks_attributes {op}: {Describe(result)}, {watch.ElapsedMilliseconds} ms");
        return result;
    }

    public static object ManageAnnotations(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        string? op, ScriptArgs annotation, IReadOnlyList<string> handles, IReadOnlyList<ScriptArgs> items, ScriptArgs set, string? space, bool atomic)
    {
        var watch = Stopwatch.StartNew();
        var cx = new EditContext(db, tr, units);
        object result = Op(op, AnnotationService.Ops) switch
        {
            "create" => AnnotationService.Create(cx, annotation, space),
            "update" => AnnotationService.Update(cx, handles.FirstOrDefault(), set, ct),
            "delete" => AnnotationService.Delete(cx, handles, atomic, ct),
            "batchupdate" => BatchEditService.UpdateBatch(cx, items, handles, set, atomic, ct),
            _ => throw new UnreachableException(),
        };
        log($"manage_annotations {op}: {Describe(result)}, {watch.ElapsedMilliseconds} ms");
        return result;
    }

    public static object ManageHatches(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        string? op, ScriptArgs hatch, IReadOnlyList<string> handles, ScriptArgs set, ScriptArgs seedPoint, string? space, bool atomic, int limit, int maxCandidates)
    {
        var watch = Stopwatch.StartNew();
        var cx = new EditContext(db, tr, units);
        limit = Math.Clamp(limit, 1, MaxLimit);
        maxCandidates = Math.Clamp(maxCandidates, 1, MaxCandidatesCeiling);
        object result = Op(op, HatchService.Ops) switch
        {
            "create" => HatchService.Create(cx, ed, ct, hatch, space, maxCandidates),
            "update" => HatchService.Update(cx, handles.FirstOrDefault(), set),
            "delete" => HatchService.Delete(cx, handles, atomic),
            "detectboundary" => HatchService.DetectBoundary(cx, ed, ct, cx.Point(seedPoint, out var error, "seedPoint") ?? throw new ArgumentException(error!.Message), space, maxCandidates, limit),
            _ => throw new UnreachableException(),
        };
        log($"manage_hatches {op}: {Describe(result)}, {watch.ElapsedMilliseconds} ms");
        return result;
    }

    public static object ManageXrefs(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        string? op, IReadOnlyList<string> names, string? namePattern, ScriptArgs attach, bool insertBind, string? space)
    {
        var watch = Stopwatch.StartNew();
        var cx = new EditContext(db, tr, units);
        object result = Op(op, XrefService.Ops) switch
        {
            "list" => XrefService.List(cx, namePattern),
            "resolvestatus" => XrefService.ResolveStatus(cx),
            "attach" => XrefService.Attach(cx, attach, space),
            "detach" => XrefService.Detach(cx, names),
            "reload" => XrefService.Reload(cx, names),
            "unload" => XrefService.Unload(cx, names),
            "bind" => XrefService.Bind(cx, names, insertBind),
            _ => throw new UnreachableException(),
        };
        log($"manage_xrefs {op}: {Describe(result)}, {watch.ElapsedMilliseconds} ms");
        return result;
    }

    // ---------------------------------------------------------------- helpers

    private static string Op(string? op, IReadOnlyList<string> ops)
    {
        var key = (op ?? "").Trim().ToLowerInvariant();
        if (!ops.Any(o => o.Equals(key, StringComparison.OrdinalIgnoreCase))) throw new ArgumentException($"op must be one of {string.Join(", ", ops)}.");
        return key;
    }

    private static AnalysisResult<Dictionary<string, object?>> FindReferences(EditContext cx, Editor ed, CancellationToken ct, ScriptArgs filterArgs, IReadOnlyList<string> handles, int limit, int offset, int maxCandidates)
    {
        var result = new AnalysisResult<Dictionary<string, object?>>();
        var filter = ResolveFilter(filterArgs, handles, result);
        var found = BlockService.FindReferences(cx, ed, ct, filter, limit, offset, maxCandidates);
        found.Warnings.InsertRange(0, result.Warnings);
        return found;
    }

    /// <summary>batchUpdateAttributes without items: the block references a filter selects (blockNames/layers/handles) all get the shared attributes.</summary>
    private static IReadOnlyList<string> ReferencesFor(EditContext cx, Editor ed, CancellationToken ct, ScriptArgs filterArgs, IReadOnlyList<string> handles, IReadOnlyList<ScriptArgs> items, int maxCandidates)
    {
        if (items.Count > 0) return [];
        if (handles.Count > 0) return handles;
        var probe = new AnalysisResult<Dictionary<string, object?>>();
        var filter = ResolveFilter(filterArgs, handles, probe);
        if (filter.IsEmpty) throw new ArgumentException("batchUpdateAttributes needs items [{handle, attributes}], handles [] + attributes, or filter {blockNames/layers…} + attributes.");
        var found = BlockService.FindReferences(cx, ed, ct, filter, BatchEditService.MaxBatchItems, 0, maxCandidates);
        if (found.Truncated) throw new ArgumentException($"the filter selects more than {BatchEditService.MaxBatchItems} block references; narrow it (blockNames/layers/space).");
        return found.Items.Select(i => (string)i["handle"]!).ToArray();
    }

    private static string Describe(object result) => result switch
    {
        EditResult e => $"success={e.Success} created={e.CreatedCount} modified={e.ModifiedCount} deleted={e.DeletedCount} errors={e.Errors.Count}",
        AnalysisResult<Dictionary<string, object?>> a => $"success={a.Success} count={a.Count} returned={a.Items.Count} errors={a.Errors.Count}",
        _ => result.GetType().Name,
    };
}
