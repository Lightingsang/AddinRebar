using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HPAutoCad.Aec.Model;
using HPRebar.McpBridge.Core.Scripting;
using AcadException = Autodesk.AutoCAD.Runtime.Exception;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     External references behind <c>manage_xrefs</c>: <c>list</c> / <c>resolveStatus</c> read the block table's xref
///     records (name, saved path, found path, status, overlay/attach, unloaded, reference count, nesting); <c>attach</c>
///     reads a DWG from disk and inserts one reference; <c>detach</c> / <c>reload</c> / <c>unload</c> act by name; <c>bind</c>
///     is allowed only for resolved, loaded xrefs (the "safe" half of BIND). Every op names what it changed.
/// </summary>
public static class XrefService
{
    public static readonly IReadOnlyList<string> Ops = ["list", "attach", "detach", "reload", "unload", "bind", "resolveStatus"];

    public static AnalysisResult<Dictionary<string, object?>> List(EditContext cx, string? namePattern)
    {
        var result = new AnalysisResult<Dictionary<string, object?>>();
        var items = Records(cx).Where(r => string.IsNullOrWhiteSpace(namePattern) || EntityFilter.WildcardMatch(namePattern, r.Name)).Select(r => Describe(cx, r)).ToArray();
        result.Items = items;
        result.Count = items.Length;
        result.Summary = new
        {
            xrefs = items.Length,
            byStatus = items.GroupBy(i => (string)i["status"]!).ToDictionary(g => g.Key, g => g.Count()),
            unresolved = items.Where(i => (string)i["status"]! is not ("Resolved" or "Unloaded")).Select(i => (string)i["name"]!).ToArray(),
        };
        return result;
    }

    /// <summary>Re-resolves every xref (finds files again) and reports the statuses afterwards.</summary>
    public static AnalysisResult<Dictionary<string, object?>> ResolveStatus(EditContext cx)
    {
        try { cx.Db.ResolveXrefs(true, false); }
        catch (AcadException exception) { return List(cx, null).Warn($"ResolveXrefs reported {exception.ErrorStatus}; statuses below are as AutoCAD left them."); }
        return List(cx, null);
    }

    public static EditResult Attach(EditContext cx, ScriptArgs args, string? space)
    {
        var path = (args.Str("path") ?? "").Trim();
        if (XrefPathPolicy.Refuse(path, cx.Db.Filename) is { } refusal) throw new ArgumentException(refusal);
        if (!File.Exists(path)) throw new ArgumentException($"attach: file not found — {path}");
        var name = string.IsNullOrWhiteSpace(args.Str("name")) ? Path.GetFileNameWithoutExtension(path) : args.Str("name")!.Trim();
        try { SymbolUtilityServices.ValidateSymbolName(name, false); }
        catch (AcadException) { throw new ArgumentException($"name '{name}' is not a valid block name (AutoCAD refuses < > / \\ \" : ; ? * | , = ` characters)."); }
        var result = new EditResult();
        var target = cx.Space(space, out var spaceError) ?? throw new ArgumentException(spaceError!.Message);
        var position = cx.Point(args, "position", out var pointError) ?? throw new ArgumentException(pointError!.Message);
        var scale = args.Double("scale", 1);
        if (scale <= 0) throw new ArgumentException("scale must be > 0.");
        var layer = cx.LayerForCreate(args.Str("layer"), out var layerError, out var layerWarning);
        if (layer is null) return EditResult.Refused(layerError!, type: "INSERT");
        if (layerWarning is not null) result.Warn(layerWarning);
        if (XrefPathPolicy.IsUnc(path)) result.Warn("network path: AutoCAD resolves it synchronously; an unreachable share blocks the drawing for the share's timeout.");
        var table = (BlockTable)cx.Tr.GetObject(cx.Db.BlockTableId, OpenMode.ForRead);
        if (table.Has(name)) throw new ArgumentException($"a block or xref named '{name}' already exists; pass another name.");

        ObjectId definitionId;
        try
        {
            definitionId = args.Bool("overlay") ? cx.Db.OverlayXref(path, name) : cx.Db.AttachXref(path, name);
        }
        catch (AcadException exception)
        {
            throw new ArgumentException($"AutoCAD could not attach '{path}' ({exception.ErrorStatus}): the file must be a readable DWG that does not reference this drawing.");
        }

        if (definitionId.IsNull) throw new InvalidOperationException($"AutoCAD returned no definition for '{path}'.");
        var reference = new BlockReference(position, definitionId) { ScaleFactors = new Scale3d(scale), Rotation = args.Double("rotationDeg") * Math.PI / 180, LayerId = layer.ObjectId };
        target.AppendEntity(reference);
        cx.Tr.AddNewlyCreatedDBObject(reference, true);
        var handle = reference.Handle.ToString();
        result.Created(handle);
        result.Items = [new ItemOutcome(0, true, handle, "INSERT")];
        var record = (BlockTableRecord)cx.Tr.GetObject(definitionId, OpenMode.ForRead);
        result.Summary = new { name, path, overlay = args.Bool("overlay"), handle, status = record.XrefStatus.ToString(), space = target.Name };
        return result;
    }

    /// <summary>All or nothing: every name must be an xref before the first detach; a nested xref AutoCAD refuses aborts the run.</summary>
    public static EditResult Detach(EditContext cx, IReadOnlyList<string> names)
    {
        var result = new EditResult();
        var records = Resolve(cx, names, result).ToArray();
        if (result.Errors.Count > 0)
        {
            result.Summary = new { requested = names.Count, detached = 0, refused = true, reason = $"{result.Errors.Count} name(s) are not xrefs, nothing detached" };
            return result;
        }

        var detached = new List<string>();
        foreach (var record in records)
        {
            var references = record.GetBlockReferenceIds(true, false).Cast<ObjectId>().Select(id => id.Handle.ToString()).ToArray();
            try { cx.Db.DetachXref(record.ObjectId); }
            catch (AcadException exception) { throw new ArgumentException($"detach '{record.Name}' failed ({exception.ErrorStatus}): an xref nested in another block definition cannot be detached — detach the parent or bind first. Nothing kept."); }
            foreach (var h in references) result.Deleted(h);
            detached.Add(record.Name);
        }

        result.Summary = new { requested = names.Count, detached, referencesErased = result.DeletedCount };
        return result;
    }

    public static EditResult Reload(EditContext cx, IReadOnlyList<string> names) => Act(cx, names, "reload", ids => cx.Db.ReloadXrefs(ids));

    public static EditResult Unload(EditContext cx, IReadOnlyList<string> names) => Act(cx, names, "unload", ids => cx.Db.UnloadXrefs(ids));

    /// <summary>Binds only xrefs that are resolved and loaded; <paramref name="insertBind"/> merges names (INSERT style) instead of prefixing them.</summary>
    public static EditResult Bind(EditContext cx, IReadOnlyList<string> names, bool insertBind)
    {
        var result = new EditResult();
        var records = Resolve(cx, names, result).ToArray();
        foreach (var r in records)
            if (r.XrefStatus != XrefStatus.Resolved || r.IsUnloaded)
                result.Fail(new ToolError(ToolErrorCode.InvalidArgument, $"bind refused for '{r.Name}': status {r.XrefStatus}{(r.IsUnloaded ? " (unloaded)" : "")} — only resolved, loaded xrefs are bound (run resolveStatus / reload first)."));
        if (result.Errors.Count > 0) return result; // all or nothing: a refused xref refuses the bind
        var ids = new ObjectIdCollection(records.Select(r => r.ObjectId).ToArray());
        try { cx.Db.BindXrefs(ids, insertBind); }
        catch (AcadException exception) { throw new InvalidOperationException($"BindXrefs failed ({exception.ErrorStatus}); nothing kept."); }
        result.ModifiedCount = records.Length; // block table records, not entities: names go in the summary, never in affectedHandles
        result.Summary = new { bound = records.Select(r => r.Name).ToArray(), insertBind };
        return result;
    }

    private static EditResult Act(EditContext cx, IReadOnlyList<string> names, string op, Action<ObjectIdCollection> action)
    {
        var result = new EditResult();
        var records = Resolve(cx, names, result).ToArray();
        if (records.Length == 0) { result.Success = false; return result; }
        if (result.Errors.Count > 0) { result.Summary = new { op, refused = true, reason = $"{result.Errors.Count} name(s) are not xrefs, nothing done" }; return result; }
        var ids = new ObjectIdCollection(records.Select(r => r.ObjectId).ToArray());
        try { action(ids); }
        catch (AcadException exception) { throw new InvalidOperationException($"{op} failed ({exception.ErrorStatus}); nothing kept."); }
        result.ModifiedCount = records.Length; // block table records, not entities: names go in the summary, never in affectedHandles
        result.Summary = new { op, xrefs = records.Select(r => new { name = r.Name, status = ((BlockTableRecord)cx.Tr.GetObject(r.ObjectId, OpenMode.ForRead)).XrefStatus.ToString() }).ToArray() };
        return result;
    }

    private static IEnumerable<BlockTableRecord> Resolve(EditContext cx, IReadOnlyList<string> names, EditResult result)
    {
        if (names.Count == 0) throw new ArgumentException("names [] of xrefs is required (manage_xrefs list shows them).");
        var table = (BlockTable)cx.Tr.GetObject(cx.Db.BlockTableId, OpenMode.ForRead);
        foreach (var name in names)
        {
            if (!table.Has(name)) { result.Fail(new ToolError(ToolErrorCode.InvalidArgument, $"no block named '{name}'.")); continue; }
            var record = (BlockTableRecord)cx.Tr.GetObject(table[name], OpenMode.ForRead);
            if (!record.IsFromExternalReference) { result.Fail(new ToolError(ToolErrorCode.InvalidArgument, $"'{name}' is a local block, not an xref.")); continue; }
            yield return record;
        }
    }

    private static IEnumerable<BlockTableRecord> Records(EditContext cx)
    {
        var table = (BlockTable)cx.Tr.GetObject(cx.Db.BlockTableId, OpenMode.ForRead);
        foreach (var id in table)
        {
            var record = (BlockTableRecord)cx.Tr.GetObject(id, OpenMode.ForRead);
            if (record.IsFromExternalReference) yield return record;
        }
    }

    private static Dictionary<string, object?> Describe(EditContext cx, BlockTableRecord record)
    {
        var references = record.GetBlockReferenceIds(true, false);
        var nestedIn = new List<string>();
        foreach (ObjectId id in references)
        {
            var owner = (BlockTableRecord)cx.Tr.GetObject(cx.Tr.GetObject(id, OpenMode.ForRead).OwnerId, OpenMode.ForRead);
            if (!owner.IsLayout && !nestedIn.Contains(owner.Name)) nestedIn.Add(owner.Name);
        }

        string? found = null;
        try { found = HostApplicationServices.Current.FindFile(record.PathName, cx.Db, FindFileHint.XRefDrawing); }
        catch (AcadException) { /* not found: status says so */ }
        return new Dictionary<string, object?>
        {
            ["name"] = record.Name,
            ["path"] = record.PathName,
            ["foundPath"] = string.IsNullOrEmpty(found) ? null : found,
            ["found"] = !string.IsNullOrEmpty(found),
            ["status"] = record.XrefStatus.ToString(),
            ["loaded"] = !record.IsUnloaded && record.XrefStatus == XrefStatus.Resolved,
            ["overlay"] = record.IsFromOverlayReference,
            ["referenceCount"] = references.Count,
            ["nestedIn"] = nestedIn.Count == 0 ? null : nestedIn,
        };
    }
}
