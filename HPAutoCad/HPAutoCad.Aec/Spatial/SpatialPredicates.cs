using HPAutoCad.Aec.Geometry;

namespace HPAutoCad.Aec.Spatial;

/// <summary>Outcome of one relation test: whether it holds, the distance where it means something, and the points that prove it.</summary>
public sealed record SpatialMatch(bool Holds, double? DistanceMm, IReadOnlyList<Pt> Points);

/// <summary>
///     Plan-view relation tests between two shapes with explicit tolerances. Boundaries are compared
///     segment by segment (curves arrive tessellated); "inside" tests use the even-odd rule on rings.
/// </summary>
public static class SpatialPredicates
{
    /// <summary>Shortest plan distance between the two shapes' geometry; 0 when they intersect or one lies inside the other.</summary>
    public static double DistanceXY(PlanShape a, PlanShape b, GeometryTolerance tol)
    {
        if (a.IsPoint && b.IsPoint) return a.Start.DistanceXY(b.Start);
        if (a.IsPoint) return PointToShape(a.Start, b, tol);
        if (b.IsPoint) return PointToShape(b.Start, a, tol);
        if (a.Bounds.IntersectsXY(b.Bounds, tol.PointEquality) && (AnyVertexInside(a, b, tol) || AnyVertexInside(b, a, tol))) return 0;

        var best = double.PositiveInfinity;
        foreach (var s in a.Segments)
        foreach (var t in b.Segments)
        {
            best = Math.Min(best, s.DistanceXY(t));
            if (best <= 0) return 0;
        }

        return best;
    }

    public static SpatialMatch Evaluate(PlanShape source, PlanShape target, SpatialRelation relation, GeometryTolerance tol, double? maxDistanceMm = null)
    {
        switch (relation)
        {
            case SpatialRelation.Intersects:
            {
                var points = IntersectionPoints(source, target, tol, proper: false);
                var holds = points.Count > 0 || AnyVertexInside(source, target, tol) || AnyVertexInside(target, source, tol) || PointTouch(source, target, tol);
                return new SpatialMatch(holds, holds ? 0 : null, points);
            }
            case SpatialRelation.Crosses:
            {
                var points = IntersectionPoints(source, target, tol, proper: true);
                return new SpatialMatch(points.Count > 0, points.Count > 0 ? 0 : null, points);
            }
            case SpatialRelation.Touches:
            {
                // Boundary contact only: no proper crossing, no containment, no shared collinear run (that is an overlap).
                var distance = DistanceXY(source, target, tol);
                var proper = IntersectionPoints(source, target, tol, proper: true).Count > 0;
                var contained = IsWithin(source, target, tol) || IsWithin(target, source, tol);
                var shared = CollinearOverlaps(source, target, tol).Count > 0;
                var holds = distance <= tol.EndpointConnection && !proper && !contained && !shared;
                return new SpatialMatch(holds, distance, holds ? IntersectionPoints(source, target, tol, proper: false) : []);
            }
            case SpatialRelation.Overlaps:
            {
                var runs = CollinearOverlaps(source, target, tol);
                if (runs.Count > 0) return new SpatialMatch(true, 0, runs);
                if (source.Closed && target.Closed)
                {
                    var partial = (AnyVertexInside(source, target, tol) || AnyVertexInside(target, source, tol) || IntersectionPoints(source, target, tol, true).Count > 0)
                                  && !IsWithin(source, target, tol) && !IsWithin(target, source, tol);
                    return new SpatialMatch(partial, partial ? 0 : null, []);
                }

                return new SpatialMatch(false, null, []);
            }
            case SpatialRelation.Within:
            case SpatialRelation.InsidePolygon:
            {
                var holds = target.Closed && IsWithin(source, target, tol);
                return new SpatialMatch(holds, null, []);
            }
            case SpatialRelation.Contains:
            {
                var holds = source.Closed && IsWithin(target, source, tol);
                return new SpatialMatch(holds, null, []);
            }
            case SpatialRelation.InsideBbox:
            {
                var holds = target.Bounds.ContainsXY(source.Bounds, tol.PointEquality);
                return new SpatialMatch(holds, null, []);
            }
            case SpatialRelation.DistanceTo:
            case SpatialRelation.Nearest:
            {
                var distance = DistanceXY(source, target, tol);
                var holds = maxDistanceMm is null || distance <= maxDistanceMm.Value;
                return new SpatialMatch(holds, distance, []);
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(relation), relation, "unknown spatial relation");
        }
    }

    /// <summary>
    ///     All of <paramref name="inner"/>'s vertices and segment midpoints inside the ring of <paramref name="outer"/> and no proper
    ///     boundary crossing — the midpoints catch a chord across a concave notch whose ends sit on the boundary.
    /// </summary>
    public static bool IsWithin(PlanShape inner, PlanShape outer, GeometryTolerance tol)
    {
        if (!outer.Closed || inner.Vertices.Count == 0) return false;
        if (!outer.Bounds.ContainsXY(inner.Bounds, tol.PointEquality)) return false;
        foreach (var v in inner.Vertices)
            if (!outer.ContainsPointXY(v, tol.PointEquality)) return false;
        foreach (var seg in inner.Segments)
            if (!outer.ContainsPointXY(seg.Mid, tol.PointEquality)) return false;
        return IntersectionPoints(inner, outer, tol, proper: true).Count == 0;
    }

    /// <summary>Crossing points of the two boundaries; <paramref name="proper"/> keeps only crossings strictly inside both segments.</summary>
    public static IReadOnlyList<Pt> IntersectionPoints(PlanShape a, PlanShape b, GeometryTolerance tol, bool proper)
    {
        if (!a.Bounds.IntersectsXY(b.Bounds, tol.PointEquality)) return [];
        var points = new List<Pt>();
        foreach (var s in a.Segments)
        {
            var sb = s.Bounds.Expand(tol.PointEquality);
            foreach (var t in b.Segments)
            {
                if (!sb.IntersectsXY(t.Bounds)) continue;
                if (proper)
                {
                    if (GeometryMath.CrossesProperlyXY(s, t, tol.PointEquality, out var p)) AddDistinct(points, p, tol.PointEquality);
                }
                else if (GeometryMath.IntersectXY(s, t, tol.PointEquality, out var p) != IntersectionKind.None)
                {
                    AddDistinct(points, p, tol.PointEquality);
                }
            }
        }

        return points;
    }

    /// <summary>Middle points of collinear runs longer than the collinearity tolerance shared by the two boundaries.</summary>
    public static IReadOnlyList<Pt> CollinearOverlaps(PlanShape a, PlanShape b, GeometryTolerance tol)
    {
        var points = new List<Pt>();
        if (!a.Bounds.IntersectsXY(b.Bounds, tol.Collinearity)) return points;
        foreach (var s in a.Segments)
        foreach (var t in b.Segments)
        {
            if (!GeometryMath.AreCollinearXY(s, t, tol.Collinearity, tol.ParallelAngle)) continue;
            if (!GeometryMath.CollinearOverlapXY(s, t, tol.Collinearity, out var from, out var to)) continue;
            if (from.DistanceXY(to) > tol.Collinearity) AddDistinct(points, Pt.Mid(from, to), tol.PointEquality);
        }

        return points;
    }

    private static bool AnyVertexInside(PlanShape candidate, PlanShape ring, GeometryTolerance tol)
    {
        if (!ring.Closed || !ring.Bounds.IntersectsXY(candidate.Bounds, tol.PointEquality)) return false;
        foreach (var v in candidate.Vertices)
            if (ring.ContainsPointXY(v, tol.PointEquality)) return true;
        return false;
    }

    private static bool PointTouch(PlanShape a, PlanShape b, GeometryTolerance tol) =>
        (a.IsPoint && b.DistanceToBoundaryXY(a.Start) <= tol.PointEquality) || (b.IsPoint && a.DistanceToBoundaryXY(b.Start) <= tol.PointEquality);

    private static double PointToShape(Pt p, PlanShape shape, GeometryTolerance tol) =>
        shape.ContainsPointXY(p, tol.PointEquality) ? 0 : shape.DistanceToBoundaryXY(p);

    private static void AddDistinct(List<Pt> points, Pt p, double tolerance)
    {
        foreach (var existing in points)
            if (existing.AlmostEqualsXY(p, tolerance)) return;
        points.Add(p);
    }
}
