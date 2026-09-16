using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Model;
using HPRebar.McpBridge.Core.Scripting;
using AcadException = Autodesk.AutoCAD.Runtime.Exception;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     Hatches behind <c>manage_hatches</c>: <c>create</c> from closed boundary entities, a polygon in mm, or a seed point
///     (the smallest closed entity around it), <c>update</c> pattern/scale/angle/properties, <c>delete</c> (hatches only), and
///     <c>detectBoundary</c> in the partner file. Every input is validated before the first write; a boundary that is not
///     closed is a <c>NOT_CLOSED</c> refusal, a pattern AutoCAD rejects aborts the run — a half-built or half-changed hatch is
///     never kept.
/// </summary>
public static partial class HatchService
{
    public static readonly IReadOnlyList<string> Ops = ["create", "update", "delete", "detectBoundary"];
    public static readonly IReadOnlyList<string> Styles = ["normal", "outer", "ignore"];
    public static readonly IReadOnlyList<string> UpdateKeys = ["pattern", "patternType", "scale", "angleDeg", "hatchStyle", "layer", "colorIndex", "color", "linetype", "lineweight", "visible"];
    private static readonly string[] ClosedTypes = ["LWPOLYLINE", "POLYLINE", "CIRCLE", "ELLIPSE", "SPLINE", "REGION"];

    public static EditResult Create(EditContext cx, Editor ed, CancellationToken ct, ScriptArgs args, string? space, int maxCandidates)
    {
        var target = cx.Space(space, out var spaceError) ?? throw new ArgumentException(spaceError!.Message);
        var result = new EditResult();
        var refusals = new List<ToolError>();

        // ---- boundary
        var loops = new List<Curve>();
        Point2dCollection? polygon = null;
        if (args.Has("boundaryHandles"))
        {
            var reader = new EntityShapeReader(cx.Db, cx.Tr, cx.Units, GeometryTolerance.Default);
            foreach (var boundary in EditContext.DistinctHandles(args.Strings("boundaryHandles"), result.Warnings))
            {
                var entity = HandleResolver.OpenEntity(cx.Db, cx.Tr, boundary, out var error);
                if (entity is null) { refusals.Add(error!); continue; }
                if (entity is not Curve curve) { refusals.Add(ToolError.ForHandle(ToolErrorCode.UnsupportedEntity, entity.Handle.ToString(), $"{entity.Handle} is a {entity.ObjectId.ObjectClass.DxfName}, not a curve.")); continue; }
                // Closed for AutoCAD, or closed within the point tolerance (ends meeting) — the same rule detectBoundary applies.
                if (!curve.Closed && reader.Read(curve, false).Shape is not { Closed: true }) { refusals.Add(ToolError.ForHandle(ToolErrorCode.NotClosed, curve.Handle.ToString(), $"{curve.ObjectId.ObjectClass.DxfName} {curve.Handle} is not closed; a hatch boundary must be.")); continue; }
                loops.Add(curve);
            }

            if (loops.Count == 0 && refusals.Count == 0) throw new ArgumentException("boundaryHandles is empty.");
        }
        else if (args.Has("points"))
        {
            var points = args.List("points");
            if (points.Count < 3) throw new ArgumentException("points needs at least 3 entries of {x, y} in millimetres.");
            polygon = [];
            for (var i = 0; i < points.Count; i++)
            {
                var p = cx.Point(points[i], out var error, $"points[{i}]") ?? throw new ArgumentException(error!.Message);
                polygon.Add(new Point2d(p.X, p.Y));
            }
        }
        else if (args.Has("seedPoint"))
        {
            var seed = cx.Point(args, "seedPoint", out var error) ?? throw new ArgumentException(error!.Message);
            var found = DetectBoundary(cx, ed, ct, seed, space, maxCandidates, 1);
            if (found.Items.Count == 0) throw new ArgumentException($"no closed entity contains seedPoint ({cx.ToMm(seed.X):0}, {cx.ToMm(seed.Y):0}) mm.");
            loops.Add((Curve)HandleResolver.OpenEntity(cx.Db, cx.Tr, (string)found.Items[0]["handle"]!, out _)!);
            result.Warn($"boundary from seedPoint: {found.Items[0]["type"]} {found.Items[0]["handle"]} ({found.Items[0]["areaMm2"]} mm²).");
        }
        else throw new ArgumentException("create needs boundaryHandles [] of closed curves, points [] (a polygon in mm) or seedPoint {x, y}.");

        // ---- everything else, before the hatch exists
        var pattern = string.IsNullOrWhiteSpace(args.Str("pattern")) ? "SOLID" : args.Str("pattern")!.Trim().ToUpperInvariant();
        var patternType = pattern == "SOLID" ? HatchPatternType.PreDefined : ParsePatternType(args.Str("patternType"));
        var style = ParseStyle(args.Str("hatchStyle"));
        if (pattern != "SOLID" && args.Double("scale", 1) <= 0) throw new ArgumentException("scale must be > 0.");
        var associative = args.Bool("associative") && loops.Count > 0;
        var layer = cx.LayerForCreate(args.Str("layer"), out var layerError, out var layerWarning);
        if (layer is null) refusals.Add(layerError!);
        cx.ValidateProperties(args, refusals, result.Warnings, skipLayer: true);
        if (refusals.Count > 0)
        {
            result.Settle([new ItemOutcome(0, false, null, "HATCH", Error: refusals[0])]);
            result.Errors.AddRange(refusals.Skip(1));
            result.Summary = new { refused = true, reason = $"{refusals.Count} refusal(s), nothing created" };
            return result;
        }

        // ---- build: database defaults first, then what the caller asked for (SetDatabaseDefaults resets layer/colour/linetype/lineweight)
        var hatch = new Hatch();
        hatch.SetDatabaseDefaults(cx.Db);
        hatch.LayerId = layer!.ObjectId;
        if (layerWarning is not null) result.Warn(layerWarning);
        cx.ApplyProperties(hatch, args, skipLayer: true);
        target.AppendEntity(hatch);
        cx.Tr.AddNewlyCreatedDBObject(hatch, true);
        try
        {
            hatch.SetHatchPattern(patternType, pattern);
            if (pattern != "SOLID")
            {
                hatch.PatternScale = args.Double("scale", 1);
                hatch.PatternAngle = args.Double("angleDeg") * Math.PI / 180;
            }

            hatch.HatchStyle = style;
            // Associativity must be set once the hatch is in the database and before the loops are appended by ObjectId:
            // AutoCAD then re-evaluates the hatch when a boundary entity changes (verified live: the hatch follows a moved boundary).
            hatch.Associative = associative;
            if (polygon is not null)
            {
                hatch.AppendLoop(HatchLoopTypes.Outermost, polygon, new DoubleCollection(new double[polygon.Count]));
            }
            else
            {
                for (var i = 0; i < loops.Count; i++)
                    hatch.AppendLoop(i == 0 ? HatchLoopTypes.Outermost : HatchLoopTypes.Default, new ObjectIdCollection([loops[i].ObjectId]));
            }

            hatch.EvaluateHatch(true);
        }
        catch (AcadException exception)
        {
            hatch.Erase();
            throw new ArgumentException($"AutoCAD refused the hatch ({exception.ErrorStatus}): check that the pattern '{pattern}' exists (acad.pat / acadiso.pat) and every boundary is a closed loop that does not cross itself. Nothing kept.");
        }

        var handle = hatch.Handle.ToString();
        result.Created(handle);
        result.Items = [new ItemOutcome(0, true, handle, "HATCH")];
        result.Summary = new { handle, pattern = hatch.PatternName, loops = hatch.NumberOfLoops, associative = hatch.Associative, areaMm2 = Area(cx, hatch) ?? BoundaryArea(cx, loops, polygon), layer = layer.Name, space = target.Name };
        return result;
    }

    public static EditResult Update(EditContext cx, string? handle, ScriptArgs set)
    {
        if (set.IsEmpty) throw new ArgumentException($"update needs handles [handle] and set {{{string.Join(", ", UpdateKeys)}}}.");
        var entity = cx.OpenForEdit(handle, out var error);
        if (entity is null) return EditResult.Refused(error!, type: "HATCH");
        if (entity is not Hatch hatch) return EditResult.Refused(NotAHatch(entity), entity.Handle.ToString(), "HATCH");
        var h = hatch.Handle.ToString();

        // Phase 1: every key must be valid or nothing changes.
        var result = new EditResult();
        var refusals = new List<ToolError>();
        foreach (var key in set.Keys)
            if (!UpdateKeys.Contains(key, StringComparer.OrdinalIgnoreCase)) refusals.Add(ToolError.ForHandle(ToolErrorCode.InvalidArgument, h, $"set.{key} is not a known hatch key (known: {string.Join(", ", UpdateKeys)})."));
        cx.ValidateProperties(set, refusals, result.Warnings);
        if (set.Has("scale") && set.Double("scale") <= 0) refusals.Add(ToolError.ForHandle(ToolErrorCode.InvalidArgument, h, "scale must be > 0."));
        var patternType = set.Has("patternType") ? ParsePatternType(set.Str("patternType")) : hatch.PatternType;
        var style = set.Has("hatchStyle") ? ParseStyle(set.Str("hatchStyle")) : hatch.HatchStyle;
        if (refusals.Count > 0)
        {
            result.Settle([new ItemOutcome(0, false, h, "HATCH", Error: refusals[0])]);
            result.Errors.AddRange(refusals.Skip(1));
            result.Summary = new { handle = h, refused = true, reason = $"{refusals.Count} refusal(s), nothing changed" };
            return result;
        }

        // Phase 2: apply; a pattern AutoCAD rejects aborts the run so the property changes above it are not kept either.
        if (!cx.Upgrade(hatch, out error)) return EditResult.Refused(error!, h, "HATCH");
        var changed = new List<string>(cx.ApplyProperties(hatch, set));
        try
        {
            if (set.Has("pattern"))
            {
                var pattern = set.Str("pattern")!.Trim().ToUpperInvariant();
                hatch.SetHatchPattern(pattern == "SOLID" ? HatchPatternType.PreDefined : patternType, pattern);
                changed.Add("pattern");
            }

            if (set.Has("scale")) { hatch.PatternScale = set.Double("scale"); changed.Add("scale"); }
            if (set.Has("angleDeg")) { hatch.PatternAngle = set.Double("angleDeg") * Math.PI / 180; changed.Add("angleDeg"); }
            if (set.Has("hatchStyle")) { hatch.HatchStyle = style; changed.Add("hatchStyle"); }
            if (changed.Count > 0) hatch.EvaluateHatch(true);
        }
        catch (AcadException exception)
        {
            throw new ArgumentException($"AutoCAD refused the hatch change ({exception.ErrorStatus}): pattern names must exist in acad.pat / acadiso.pat. Nothing kept.");
        }

        if (changed.Count > 0) result.Modified(h);
        result.Items = [new ItemOutcome(0, true, h, "HATCH", changed)];
        result.Summary = new { handle = h, changed, pattern = hatch.PatternName, areaMm2 = Area(cx, hatch) };
        return result;
    }

    public static EditResult Delete(EditContext cx, IReadOnlyList<string> handles, bool atomic)
    {
        if (handles.Count == 0) throw new ArgumentException("delete needs handles [] of hatches.");
        var result = new EditResult();
        handles = EditContext.DistinctHandles(handles, result.Warnings);
        var opened = new Hatch?[handles.Count];
        var errors = new ToolError?[handles.Count];
        for (var i = 0; i < handles.Count; i++)
        {
            var entity = cx.OpenForEdit(handles[i], out errors[i]);
            if (entity is Hatch hatch) opened[i] = hatch;
            else if (entity is not null) errors[i] = NotAHatch(entity);
        }

        if (atomic && opened.Any(h => h is null))
        {
            result.Settle(handles.Select((_, i) => new ItemOutcome(i, false, opened[i]?.Handle.ToString(), "HATCH", Error: errors[i])).ToArray());
            result.Summary = new { requested = handles.Count, deleted = 0, refused = true, reason = $"atomic: {opened.Count(h => h is null)} handle(s) are not editable hatches, nothing deleted" };
            return result;
        }

        var outcomes = new ItemOutcome[handles.Count];
        for (var i = 0; i < handles.Count; i++)
        {
            if (opened[i] is not { } hatch) { outcomes[i] = new ItemOutcome(i, false, null, "HATCH", Error: errors[i]); continue; }
            var h = hatch.Handle.ToString();
            if (!cx.Upgrade(hatch, out var upgradeError))
            {
                if (atomic) throw new InvalidOperationException($"items[{i}] ({h}): {upgradeError!.Message} Atomic batch aborted, nothing kept.");
                outcomes[i] = new ItemOutcome(i, false, h, "HATCH", Error: upgradeError);
                continue;
            }

            hatch.Erase();
            result.Deleted(h);
            outcomes[i] = new ItemOutcome(i, true, h, "HATCH");
        }

        result.Settle(outcomes);
        result.Summary = new { requested = handles.Count, deleted = result.DeletedCount };
        return result;
    }

    private static ToolError NotAHatch(Entity entity) =>
        ToolError.ForHandle(ToolErrorCode.UnsupportedEntity, entity.Handle.ToString(), $"{entity.Handle} is a {entity.ObjectId.ObjectClass.DxfName}, not a hatch.");
}
