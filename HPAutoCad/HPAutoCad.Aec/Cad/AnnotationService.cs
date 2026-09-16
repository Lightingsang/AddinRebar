using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HPAutoCad.Aec.Model;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     Text, mtext, dimensions (linear/aligned/angular/radial/diameter) and multileaders behind <c>manage_annotations</c>:
///     <c>create</c> builds one annotation from typed fields in mm/degrees, <c>update</c> / <c>batchUpdate</c> reuse the entity
///     updater, <c>delete</c> erases annotation entities only — geometry can never be erased through this tool.
/// </summary>
public static class AnnotationService
{
    public static readonly IReadOnlyList<string> Ops = ["create", "update", "delete", "batchUpdate"];
    public static readonly IReadOnlyList<string> Types = ["text", "mtext", "dimension", "mleader"];
    public static readonly IReadOnlyList<string> DimensionKinds = ["linear", "aligned", "angular", "radial", "diameter"];
    private static readonly HashSet<string> AnnotationDxf = new(StringComparer.OrdinalIgnoreCase) { "TEXT", "MTEXT", "DIMENSION", "LEADER", "MULTILEADER" };

    public static EditResult Create(EditContext cx, ScriptArgs item, string? space)
    {
        var target = cx.Space(space, out var spaceError) ?? throw new ArgumentException(spaceError!.Message);
        var factory = new EntityFactory(cx);
        var warnings = new List<string>();
        var type = (item.Str("type") ?? "").Trim().ToLowerInvariant();
        var built = type switch
        {
            "text" or "mtext" => factory.Create(item),
            "dimension" => Dimension(cx, factory, item, warnings),
            "mleader" => MLeader(cx, factory, item, warnings),
            _ => new EntityFactory.Built(null, ToolError.Argument($"type must be one of {string.Join(", ", Types)}."), []),
        };
        if (built.Entity is null) return EditResult.Refused(built.Error!);
        var result = new EditResult();
        foreach (var w in warnings.Concat(built.Warnings)) result.Warn(w);

        target.AppendEntity(built.Entity);
        cx.Tr.AddNewlyCreatedDBObject(built.Entity, true);
        var handle = built.Entity.Handle.ToString();
        result.Created(handle);
        result.Items = [new ItemOutcome(0, true, handle, built.Entity.ObjectId.ObjectClass.DxfName)];
        result.Summary = new { handle, type = built.Entity.GetType().Name, measurement = built.Entity is Dimension d ? Measurement(cx, d) : null, space = target.Name };
        return result;
    }

    public static EditResult Update(EditContext cx, string? handle, ScriptArgs set, CancellationToken ct)
    {
        if (set.IsEmpty) throw new ArgumentException($"update needs handle and set {{…}} (keys: {string.Join(", ", EntityUpdater.Keys)}).");
        return BatchEditService.UpdateBatch(cx, [], [handle ?? ""], set, atomic: true, ct);
    }

    public static EditResult Delete(EditContext cx, IReadOnlyList<string> handles, bool atomic, CancellationToken ct)
    {
        if (handles.Count == 0) throw new ArgumentException("delete needs handles [] of annotation entities.");
        var result = new EditResult();
        handles = EditContext.DistinctHandles(handles, result.Warnings);
        var opened = new Entity?[handles.Count];
        var errors = new ToolError?[handles.Count];
        for (var i = 0; i < handles.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var entity = cx.OpenForEdit(handles[i], out errors[i]);
            if (entity is not null && !AnnotationDxf.Contains(entity.ObjectId.ObjectClass.DxfName ?? ""))
            {
                errors[i] = ToolError.ForHandle(ToolErrorCode.UnsupportedEntity, entity.Handle.ToString(), $"{entity.Handle} is a {entity.ObjectId.ObjectClass.DxfName}, not an annotation; manage_annotations never erases geometry.");
                entity = null;
            }

            opened[i] = entity;
        }

        if (atomic && opened.Any(e => e is null))
        {
            result.Settle(handles.Select((_, i) => new ItemOutcome(i, false, null, Error: errors[i])).ToArray());
            result.Summary = new { requested = handles.Count, deleted = 0, refused = true, reason = $"atomic: {opened.Count(e => e is null)} handle(s) cannot be erased, nothing deleted" };
            return result;
        }

        var outcomes = new ItemOutcome[handles.Count];
        for (var i = 0; i < handles.Count; i++)
        {
            if (opened[i] is not { } entity)
            {
                outcomes[i] = new ItemOutcome(i, false, null, Error: errors[i]);
                continue;
            }

            var h = entity.Handle.ToString();
            var type = entity.ObjectId.ObjectClass.DxfName;
            if (!cx.Upgrade(entity, out var upgradeError))
            {
                if (atomic) throw new InvalidOperationException($"items[{i}] ({h}): {upgradeError!.Message} Atomic batch aborted, nothing kept.");
                outcomes[i] = new ItemOutcome(i, false, h, type, Error: upgradeError);
                continue;
            }

            entity.Erase();
            result.Deleted(h);
            outcomes[i] = new ItemOutcome(i, true, h, type);
        }

        result.Settle(outcomes);
        result.Summary = new { requested = handles.Count, deleted = result.DeletedCount };
        return result;
    }

    /// <summary>The named dimension style, or the drawing's current one; <see cref="ObjectId.Null"/> with an error when it does not exist.</summary>
    public static ObjectId DimStyle(EditContext cx, string? name, out ToolError? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(name)) return cx.Db.Dimstyle;
        var styles = (DimStyleTable)cx.Tr.GetObject(cx.Db.DimStyleTableId, OpenMode.ForRead);
        if (styles.Has(name)) return styles[name];
        error = ToolError.Argument($"dimension style '{name}' does not exist in this drawing.");
        return ObjectId.Null;
    }

    private static EntityFactory.Built Dimension(EditContext cx, EntityFactory factory, ScriptArgs item, List<string> warnings)
    {
        var kind = (item.Str("kind") ?? "linear").Trim().ToLowerInvariant();
        if (kind is "linear" or "rotated" or "aligned") return factory.Create(item);
        var styleId = DimStyle(cx, item.Str("dimStyle"), out var error);
        if (styleId.IsNull) return new EntityFactory.Built(null, error, warnings);
        Dimension? dimension;
        switch (kind)
        {
            case "radial":
            {
                var c = cx.Point(item, "center", out error);
                var chord = c is null ? null : cx.Point(item, "chordPoint", out error);
                if (c is null || chord is null) return new EntityFactory.Built(null, error, warnings);
                dimension = new RadialDimension(c.Value, chord.Value, cx.ToDrawing(item.Double("leaderLengthMm")), "", styleId);
                break;
            }
            case "diameter":
            {
                var chord = cx.Point(item, "chordPoint", out error);
                var far = chord is null ? null : cx.Point(item, "farChordPoint", out error);
                if (chord is null || far is null) return new EntityFactory.Built(null, error, warnings);
                dimension = new DiametricDimension(chord.Value, far.Value, cx.ToDrawing(item.Double("leaderLengthMm")), "", styleId);
                break;
            }
            case "angular":
            {
                var v = cx.Point(item, "vertex", out error);
                var p1 = v is null ? null : cx.Point(item, "p1", out error);
                var p2 = p1 is null ? null : cx.Point(item, "p2", out error);
                var arc = p2 is null ? null : cx.Point(item, "arcPoint", out error);
                if (v is null || p1 is null || p2 is null || arc is null) return new EntityFactory.Built(null, error ?? ToolError.Argument("angular needs vertex, p1, p2 and arcPoint."), warnings);
                dimension = new Point3AngularDimension(v.Value, p1.Value, p2.Value, arc.Value, "", styleId);
                break;
            }
            default:
                return new EntityFactory.Built(null, ToolError.Argument($"dimension kind must be one of {string.Join(", ", DimensionKinds)}."), warnings);
        }

        if (item.Has("textOverride")) dimension.DimensionText = item.Str("textOverride") ?? "";
        return factory.Finish(dimension, item, warnings);
    }

    private static EntityFactory.Built MLeader(EditContext cx, EntityFactory factory, ScriptArgs item, List<string> warnings)
    {
        var text = item.Str("text");
        var arrow = cx.Point(item, "arrowPoint", out var error);
        var landing = arrow is null ? null : cx.Point(item, "landingPoint", out error);
        if (string.IsNullOrEmpty(text) || arrow is null || landing is null)
            return new EntityFactory.Built(null, error ?? ToolError.Argument("mleader needs text, arrowPoint and landingPoint (mm); optional heightMm."), warnings);

        return factory.Finish(BuildMLeader(cx, text, arrow.Value, landing.Value, item.Double("heightMm")), item, warnings);
    }

    /// <summary>A multileader with MText content: one leader line from the arrow to the landing, text at the landing. Database defaults first.</summary>
    internal static MLeader BuildMLeader(EditContext cx, string text, Point3d arrow, Point3d landing, double heightMm)
    {
        var leader = new MLeader();
        leader.SetDatabaseDefaults(cx.Db);
        leader.ContentType = ContentType.MTextContent;
        var mtext = new MText();
        mtext.SetDatabaseDefaults(cx.Db);
        mtext.Contents = text;
        mtext.Location = landing;
        if (heightMm > 0) mtext.TextHeight = cx.ToDrawing(heightMm);
        leader.MText = mtext;
        var leaderIndex = leader.AddLeader();
        var lineIndex = leader.AddLeaderLine(leaderIndex);
        leader.AddFirstVertex(lineIndex, arrow);
        leader.AddLastVertex(lineIndex, landing);
        return leader;
    }

    private static object Measurement(EditContext cx, Dimension d) => d switch
    {
        Point3AngularDimension or LineAngularDimension2 => new { angleDeg = Math.Round(d.Measurement * 180 / Math.PI, 3) },
        _ => new { lengthMm = Math.Round(cx.ToMm(d.Measurement), 2) },
    };
}
