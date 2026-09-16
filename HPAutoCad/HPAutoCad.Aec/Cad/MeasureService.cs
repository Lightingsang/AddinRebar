using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Model;
using HPAutoCad.Aec.Spatial;
using HPRebar.McpBridge.Core.Scripting;
using AcadException = Autodesk.AutoCAD.Runtime.Exception;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     Measurements in millimetres over handles and/or points. Exact AutoCAD curve maths where it exists
///     (length, area, closest point, intersections between two curves); the tessellated engine for
///     everything else (blocks, text, mixed pairs, distance between shapes).
/// </summary>
public static class MeasureService
{
    public static readonly IReadOnlyList<string> Measures =
        ["length", "totalLength", "area", "perimeter", "centroid", "boundingBox", "angle", "distance", "closestPoint", "intersections"];

    public static AnalysisResult<object> Measure(Database db, Transaction tr, ScriptUnits units, CancellationToken ct,
        string measure, IReadOnlyList<string> handles, IReadOnlyList<Pt> pointsMm, GeometryTolerance tol)
    {
        var result = new AnalysisResult<object>();
        var key = Measures.FirstOrDefault(m => string.Equals(m, measure, StringComparison.OrdinalIgnoreCase))
                  ?? throw new ArgumentException($"measure must be one of {string.Join(", ", Measures)}.");
        var reader = new EntityShapeReader(db, tr, units, tol);

        var entities = new List<(Entity Entity, AecEntityRecord Record)>();
        foreach (var handle in handles)
        {
            ct.ThrowIfCancellationRequested();
            var entity = HandleResolver.OpenEntity(db, tr, handle, out var error);
            if (entity is null) result.Fail(error!);
            else entities.Add((entity, reader.Read(entity, false)));
        }

        // A handle that did not resolve already produced a typed error; a measure that needs every handle must not hide it behind a generic message.
        if (result.Errors.Count > 0 && entities.Count < handles.Count && key is "distance" or "intersections" or "angle")
        {
            result.Success = false;
            return result;
        }

        var items = new List<object>();
        switch (key)
        {
            case "length":
            case "totalLength":
            case "perimeter":
            {
                double total = 0;
                foreach (var (entity, record) in entities)
                {
                    var length = ExactLength(entity, units) ?? record.LengthMm;
                    if (length is null || (key == "perimeter" && record.Shape is { Closed: false }))
                    {
                        result.Fail(ToolError.ForHandle(ToolErrorCode.NoGeometry, record.Handle, $"{record.Type} {record.Handle} has no {(key == "perimeter" ? "closed outline" : "length")}."));
                        continue;
                    }

                    total += length.Value;
                    items.Add(new { handle = record.Handle, type = record.Type, layer = record.Layer, lengthMm = Math.Round(length.Value, 2) });
                }

                result.Summary = new { totalLengthMm = Math.Round(total, 2), measured = items.Count };
                break;
            }
            case "area":
            {
                double total = 0;
                foreach (var (entity, record) in entities)
                {
                    var area = ExactArea(entity, units) ?? record.AreaMm2;
                    if (area is null)
                    {
                        result.Fail(ToolError.ForHandle(ToolErrorCode.NotClosed, record.Handle, $"{record.Type} {record.Handle} is not a closed area."));
                        continue;
                    }

                    total += area.Value;
                    items.Add(new { handle = record.Handle, type = record.Type, layer = record.Layer, areaMm2 = Math.Round(area.Value, 1), areaM2 = Math.Round(area.Value / 1e6, 4) });
                }

                result.Summary = new { totalAreaMm2 = Math.Round(total, 1), totalAreaM2 = Math.Round(total / 1e6, 4), measured = items.Count };
                break;
            }
            case "centroid":
            {
                foreach (var (_, record) in entities)
                {
                    if (record.Shape is null)
                    {
                        result.Fail(ToolError.ForHandle(ToolErrorCode.NoGeometry, record.Handle, $"{record.Type} {record.Handle}: {record.GeometryNote}"));
                        continue;
                    }

                    items.Add(new { handle = record.Handle, type = record.Type, centroidMm = record.Shape.Centroid.Rounded(), isAreaCentroid = record.Shape.Closed, note = record.Shape.Closed ? null : "open geometry: centre of its bounding box" });
                }

                if (pointsMm.Count >= 3) items.Add(new { points = pointsMm.Count, centroidMm = GeometryMath.CentroidXY(pointsMm).Rounded(), areaMm2 = Math.Round(GeometryMath.AreaXY(pointsMm), 1) });
                break;
            }
            case "boundingBox":
            {
                var union = Box.Empty;
                foreach (var (_, record) in entities)
                {
                    if (record.BoundsMm is not { } box)
                    {
                        result.Fail(ToolError.ForHandle(ToolErrorCode.NoGeometry, record.Handle, $"{record.Type} {record.Handle}: {record.GeometryNote}"));
                        continue;
                    }

                    union = union.Union(box);
                    items.Add(new { handle = record.Handle, type = record.Type, boundsMm = box, widthMm = Math.Round(box.Width, 1), heightMm = Math.Round(box.Height, 1) });
                }

                if (pointsMm.Count > 0) union = union.Union(Box.Of(pointsMm));
                result.Summary = union.IsEmpty ? null : new { boundsMm = union.Rounded(), widthMm = Math.Round(union.Width, 1), heightMm = Math.Round(union.Height, 1), centerMm = union.Center.Rounded() };
                break;
            }
            case "angle":
            {
                if (pointsMm.Count >= 2)
                {
                    var seg = new Seg(pointsMm[0], pointsMm[1]);
                    items.Add(new { from = pointsMm[0], to = pointsMm[1], directionDegrees = Math.Round(GeometryMath.DirectionDegreesXY(seg), 3) });
                    if (pointsMm.Count >= 3) items.Add(new { vertex = pointsMm[1], angleDegrees = Math.Round(GeometryMath.AngleBetweenXY(pointsMm[0] - pointsMm[1], pointsMm[2] - pointsMm[1]), 3) });
                }

                foreach (var (_, record) in entities)
                {
                    var seg = MainSegment(record);
                    if (seg is null)
                    {
                        result.Fail(ToolError.ForHandle(ToolErrorCode.NoGeometry, record.Handle, $"{record.Type} {record.Handle} has no direction (needs a line or a two-point shape)."));
                        continue;
                    }

                    items.Add(new { handle = record.Handle, type = record.Type, directionDegrees = Math.Round(GeometryMath.DirectionDegreesXY(seg.Value), 3) });
                }

                if (entities.Count == 2 && MainSegment(entities[0].Record) is { } a && MainSegment(entities[1].Record) is { } b)
                    result.Summary = new { angleBetweenDegrees = Math.Round(GeometryMath.AngleBetweenXY(a.DirectionXY, b.DirectionXY), 3), parallel = GeometryMath.AreParallelXY(a, b, tol.ParallelAngle), perpendicular = GeometryMath.ArePerpendicularXY(a, b, tol.ParallelAngle) };
                break;
            }
            case "distance":
            {
                if (pointsMm.Count >= 2 && entities.Count == 0)
                    result.Summary = new { distanceMm = Math.Round(pointsMm[0].Distance(pointsMm[1]), 3), distanceXYMm = Math.Round(pointsMm[0].DistanceXY(pointsMm[1]), 3) };
                else if (entities.Count >= 2)
                {
                    var (ea, ra) = entities[0];
                    var (eb, rb) = entities[1];
                    if (ra.Shape is null || rb.Shape is null) throw new ArgumentException("Both entities need readable geometry for a distance.");
                    var distance = SpatialPredicates.DistanceXY(ra.Shape, rb.Shape, tol);
                    result.Summary = new { sourceHandle = ra.Handle, targetHandle = rb.Handle, distanceMm = Math.Round(distance, 3), intersects = distance <= tol.PointEquality };
                }
                else if (entities.Count == 1 && pointsMm.Count >= 1)
                {
                    var (entity, record) = entities[0];
                    var (point, distance) = ClosestPoint(entity, record, pointsMm[0], units, tol);
                    result.Summary = new { handle = record.Handle, pointMm = pointsMm[0], closestPointMm = point?.Rounded(), distanceMm = Math.Round(distance, 3) };
                }
                else throw new ArgumentException("distance needs two points, two handles, or one handle and one point.");
                break;
            }
            case "closestPoint":
            {
                if (pointsMm.Count == 0) throw new ArgumentException("closestPoint needs at least one point in points[].");
                foreach (var (entity, record) in entities)
                {
                    var (point, distance) = ClosestPoint(entity, record, pointsMm[0], units, tol);
                    if (point is null) result.Fail(ToolError.ForHandle(ToolErrorCode.NoGeometry, record.Handle, $"{record.Type} {record.Handle}: {record.GeometryNote}"));
                    else items.Add(new { handle = record.Handle, type = record.Type, closestPointMm = point.Value.Rounded(), distanceMm = Math.Round(distance, 3) });
                }

                break;
            }
            case "intersections":
            {
                if (entities.Count < 2) throw new ArgumentException("intersections needs two handles.");
                var (ea, ra) = entities[0];
                var (eb, rb) = entities[1];
                var points = Intersections(ea, eb, ra, rb, units, tol);
                items.AddRange(points.Select(p => (object)new { pointMm = p.Rounded() }));
                result.Summary = new { sourceHandle = ra.Handle, targetHandle = rb.Handle, intersections = points.Count, exact = ea is Curve && eb is Curve };
                break;
            }
        }

        result.Items = items;
        result.Count = items.Count;
        // Measures that produce only a summary (distance, intersections) throw on bad input, so an error list with no item means nothing was measured.
        result.Success = result.Errors.Count == 0 || items.Count > 0;
        return result;
    }

    private static Seg? MainSegment(AecEntityRecord record) =>
        record.Shape is { Vertices.Count: >= 2, Closed: false } s ? new Seg(s.Start, s.End) : null;

    private static double? ExactLength(Entity entity, ScriptUnits units)
    {
        if (entity is not Curve curve) return null;
        try { return units.ToMm(Math.Abs(curve.GetDistanceAtParameter(curve.EndParam) - curve.GetDistanceAtParameter(curve.StartParam))); }
        catch (AcadException) { return null; }
    }

    private static double? ExactArea(Entity entity, ScriptUnits units)
    {
        try
        {
            var areaDu = entity switch
            {
                Curve { Closed: true } curve => curve.Area,
                Hatch hatch => hatch.Area,
                Region region => region.Area,
                _ => (double?)null,
            };
            return areaDu is null ? null : areaDu.Value * units.MmPerUnit * units.MmPerUnit;
        }
        catch (AcadException)
        {
            return null;
        }
    }

    private static (Pt? Point, double Distance) ClosestPoint(Entity entity, AecEntityRecord record, Pt pointMm, ScriptUnits units, GeometryTolerance tol)
    {
        if (entity is Curve curve)
        {
            try
            {
                var du = new Point3d(units.ToDrawing(pointMm.X), units.ToDrawing(pointMm.Y), units.ToDrawing(pointMm.Z));
                var closest = curve.GetClosestPointTo(du, false);
                var p = new Pt(units.ToMm(closest.X), units.ToMm(closest.Y), units.ToMm(closest.Z));
                return (p, p.DistanceXY(pointMm));
            }
            catch (AcadException)
            {
                // fall through to the tessellated shape
            }
        }

        if (record.Shape is null) return (null, double.PositiveInfinity);
        Pt? best = null;
        var bestDistance = double.PositiveInfinity;
        foreach (var seg in record.Shape.Segments)
        {
            var c = seg.ClosestPointXY(pointMm);
            var d = c.DistanceXY(pointMm);
            if (d < bestDistance) (best, bestDistance) = (c, d);
        }

        if (best is null && record.Shape.IsPoint) (best, bestDistance) = (record.Shape.Start, record.Shape.Start.DistanceXY(pointMm));
        return (best, bestDistance);
    }

    private static IReadOnlyList<Pt> Intersections(Entity a, Entity b, AecEntityRecord ra, AecEntityRecord rb, ScriptUnits units, GeometryTolerance tol)
    {
        if (a is Curve ca && b is Curve cb)
        {
            try
            {
                using var points = new Point3dCollection();
                ca.IntersectWith(cb, Intersect.OnBothOperands, points, IntPtr.Zero, IntPtr.Zero);
                return points.Cast<Point3d>().Select(p => new Pt(units.ToMm(p.X), units.ToMm(p.Y), units.ToMm(p.Z))).ToArray();
            }
            catch (AcadException)
            {
                // some curve pairs (splines in some states) refuse; the tessellated answer is still useful
            }
        }

        if (ra.Shape is null || rb.Shape is null) return [];
        return SpatialPredicates.IntersectionPoints(ra.Shape, rb.Shape, tol, proper: false);
    }
}
