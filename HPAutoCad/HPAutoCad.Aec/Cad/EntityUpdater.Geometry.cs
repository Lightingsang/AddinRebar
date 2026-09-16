using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HPAutoCad.Aec.Model;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     The geometry half of <see cref="EntityUpdater"/>: per-type <c>geometry</c> edits in millimetres (line ends, circle/arc
///     centre, radius and angles, polyline vertices re-written in place, positions of text/blocks/points), validated first and
///     applied second, plus the pivot the transforms <c>rotate</c> / <c>scaleBy</c> use when no <c>about</c> point is given.
/// </summary>
public sealed partial class EntityUpdater
{
    private static readonly string[] GeometryTypes = ["LINE", "LWPOLYLINE", "CIRCLE", "ARC", "TEXT", "MTEXT", "INSERT", "POINT"];

    private void ValidateGeometry(Entity entity, ScriptArgs g, List<ToolError> errors)
    {
        ToolError? error = null;
        switch (entity)
        {
            case Line:
                if (g.Has("start")) cx.Point(g, "start", out error);
                if (error is null && g.Has("end")) cx.Point(g, "end", out error);
                break;
            case Circle or Arc:
                if (g.Has("center")) cx.Point(g, "center", out error);
                if (error is null && g.Has("radiusMm") && g.Double("radiusMm") <= 0) error = ToolError.Argument("radiusMm must be > 0.");
                break;
            case Polyline:
                if (g.Has("points"))
                {
                    var points = g.List("points");
                    if (points.Count < 2) error = ToolError.Argument("geometry.points needs at least 2 entries of {x, y} in millimetres.");
                    for (var i = 0; error is null && i < points.Count; i++) cx.Point(points[i], out error, $"geometry.points[{i}]");
                }

                break;
            case DBText or MText or BlockReference or DBPoint:
                if (g.Has("position")) cx.Point(g, "position", out error);
                break;
            default:
                error = ToolError.ForHandle(ToolErrorCode.UnsupportedEntity, entity.Handle.ToString(), $"geometry edits cover {string.Join(", ", GeometryTypes)}; {entity.Handle} is a {Dxf(entity)} — use move/rotate/scaleBy.");
                break;
        }

        if (error is not null) errors.Add(error);
    }

    private void ApplyGeometry(Entity entity, ScriptArgs g, List<string> changed)
    {
        switch (entity)
        {
            case Line line:
                if (g.Has("start")) { line.StartPoint = cx.Point(g, "start", out _)!.Value; changed.Add("geometry.start"); }
                if (g.Has("end")) { line.EndPoint = cx.Point(g, "end", out _)!.Value; changed.Add("geometry.end"); }
                break;
            case Circle circle:
                if (g.Has("center")) { circle.Center = cx.Point(g, "center", out _)!.Value; changed.Add("geometry.center"); }
                if (g.Has("radiusMm")) { circle.Radius = cx.ToDrawing(g.Double("radiusMm")); changed.Add("geometry.radiusMm"); }
                break;
            case Arc arc:
                if (g.Has("center")) { arc.Center = cx.Point(g, "center", out _)!.Value; changed.Add("geometry.center"); }
                if (g.Has("radiusMm")) { arc.Radius = cx.ToDrawing(g.Double("radiusMm")); changed.Add("geometry.radiusMm"); }
                if (g.Has("startAngleDeg")) { arc.StartAngle = g.Double("startAngleDeg") * Math.PI / 180; changed.Add("geometry.startAngleDeg"); }
                if (g.Has("endAngleDeg")) { arc.EndAngle = g.Double("endAngleDeg") * Math.PI / 180; changed.Add("geometry.endAngleDeg"); }
                break;
            case Polyline pl:
                ApplyPolyline(pl, g, changed);
                break;
            case DBText t:
                if (g.Has("position")) { t.Position = cx.Point(g, "position", out _)!.Value; changed.Add("geometry.position"); }
                break;
            case MText m:
                if (g.Has("position")) { m.Location = cx.Point(g, "position", out _)!.Value; changed.Add("geometry.position"); }
                break;
            case BlockReference b:
                if (g.Has("position")) { b.Position = cx.Point(g, "position", out _)!.Value; changed.Add("geometry.position"); }
                break;
            case DBPoint p:
                if (g.Has("position")) { p.Position = cx.Point(g, "position", out _)!.Value; changed.Add("geometry.position"); }
                break;
        }
    }

    private void ApplyPolyline(Polyline pl, ScriptArgs g, List<string> changed)
    {
        if (g.Has("points"))
        {
            var points = g.List("points");
            var parsed = points.Select(p => (P: cx.Point(p, out _)!.Value, Bulge: p.Double("bulge"))).Select(x => (P: new Point2d(x.P.X, x.P.Y), x.Bulge)).ToList();
            // Overwrite in place, then grow or shrink — the polyline never drops to zero vertices on the way.
            var keep = Math.Min(pl.NumberOfVertices, parsed.Count);
            for (var i = 0; i < keep; i++)
            {
                pl.SetPointAt(i, parsed[i].P);
                pl.SetBulgeAt(i, parsed[i].Bulge);
            }

            for (var i = keep; i < parsed.Count; i++) pl.AddVertexAt(i, parsed[i].P, parsed[i].Bulge, 0, 0);
            for (var i = pl.NumberOfVertices - 1; i >= parsed.Count; i--) pl.RemoveVertexAt(i);
            changed.Add("geometry.points");
        }

        if (g.Has("closed"))
        {
            pl.Closed = g.Bool("closed");
            changed.Add("geometry.closed");
        }
    }

    /// <summary>The transform pivot must be parseable when given, or derivable from the extents when not.</summary>
    private void ValidatePivot(Entity entity, ScriptArgs transform, string key, List<ToolError> errors)
    {
        if (transform.Has("about"))
        {
            if (cx.Point(transform, "about", out var error) is null) errors.Add(error!);
        }
        else if (Pivot(entity) is null)
        {
            errors.Add(ToolError.Argument($"{key} needs an about {{x, y}} point for this entity (it has no extents to pivot on)."));
        }
    }

    private Point3d PivotOf(Entity entity, ScriptArgs transform) => transform.Has("about") ? cx.Point(transform, "about", out _)!.Value : Pivot(entity)!.Value;

    /// <summary>A natural pivot when none is given: the centre of the entity's extents.</summary>
    private static Point3d? Pivot(Entity entity)
    {
        try
        {
            var box = entity.GeometricExtents;
            return box.MinPoint + (box.MaxPoint - box.MinPoint) / 2;
        }
        catch (Autodesk.AutoCAD.Runtime.Exception)
        {
            return null;
        }
    }
}
