using HPAutoCad.Aec.Classification;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Spatial;

namespace HPAutoCad.Aec.Coordination;

/// <summary>An MEP route (open chain) or a host it may pass through (a wall or beam line, a beam / slab / wall outline), and the space it lives in.</summary>
public sealed record OpeningSubject(string Handle, string AecType, string Layer, PlanShape Shape, string Space = "Model");

/// <summary>One opening to request: where the route passes through the host, how big, and how it is oriented (along the host).</summary>
public sealed record OpeningRequest(string Id, string RouteHandle, string RouteType, string HostHandle, string HostType, Pt CenterMm, double WidthMm, double HeightMm, double AngleDeg)
{
    public string Label => $"{Id}: {RouteType} {RouteHandle} through {HostType} {HostHandle} ({WidthMm:0}×{HeightMm:0})";
}

/// <summary>The sizes an opening request is made of: the route's nominal size per kind plus a margin all round.</summary>
public sealed record OpeningSizes(double PipeMm, double DuctMm, double TrayMm, double MarginMm)
{
    public static readonly OpeningSizes Default = new(OpeningPlanner.DefaultPipeMm, OpeningPlanner.DefaultDuctMm, OpeningPlanner.DefaultTrayMm, OpeningPlanner.DefaultMarginMm);

    public double Of(string routeType) => routeType switch
    {
        AecType.Duct => DuctMm,
        AecType.CableTray => TrayMm,
        _ => PipeMm,
    };
}

/// <summary>The requests planned and what was left out: passes longer than the chord cap (a run inside a wall cavity, a chord across a slab).</summary>
public sealed record OpeningPlan(IReadOnlyList<OpeningRequest> Requests, int LongChordsSkipped);

/// <summary>
///     Where MEP routes pass through hosts. A host line (single-line wall or beam): every point where the route crosses from one side
///     to the other — a route ending on the line, or touching it and turning back, does not pass. A host outline (beam, wall, column
///     drawn double-line): the boundary crossings sorted along the route; the stretch between two consecutive crossings that runs inside
///     the outline is a pass, opened at the middle of the route inside — a route starting on the near face and leaving through the far
///     face passes, a route ending inside, touching a face or running along it does not; a pass longer than the chord cap is not an
///     opening through a member and is counted instead. The opening is the route's nominal size plus the margin, square, turned along the
///     host at the crossing. Pure.
/// </summary>
public static class OpeningPlanner
{
    public const double DefaultPipeMm = 150;
    public const double DefaultDuctMm = 400;
    public const double DefaultTrayMm = 300;
    public const double DefaultMarginMm = 50;

    /// <summary>The longest stretch inside an outline that still reads as passing through a member (two thick walls); longer chords are runs inside the outline.</summary>
    public const double DefaultMaxChordMm = 1000;

    /// <summary>Requests per call: each is a rectangle + a leader in the drawing and ~250 B in the answer.</summary>
    public const int MaxRequests = 100;

    public static OpeningPlan Plan(IReadOnlyList<OpeningSubject> routes, IReadOnlyList<OpeningSubject> hosts, OpeningSizes sizes, double maxChordMm, GeometryTolerance tol, CancellationToken ct)
    {
        var index = new SpatialIndex<int>();
        for (var j = 0; j < hosts.Count; j++) index.Insert(hosts[j].Shape.Bounds, j);
        var requests = new List<OpeningRequest>();
        var longChords = 0;
        foreach (var route in routes.OrderBy(r => r.Handle.Length).ThenBy(r => r.Handle, StringComparer.Ordinal))
        {
            ct.ThrowIfCancellationRequested();
            if (route.Shape.Closed || route.Shape.Vertices.Count < 2) continue;
            var size = sizes.Of(route.AecType) + 2 * sizes.MarginMm;
            foreach (var j in index.Query(route.Shape.Bounds, tol.PointEquality).OrderBy(j => j))
            {
                var host = hosts[j];
                if (host.Handle == route.Handle || !string.Equals(host.Space, route.Space, StringComparison.OrdinalIgnoreCase)) continue;
                var points = SpatialPredicates.IntersectionPoints(route.Shape, host.Shape, tol, proper: false);
                if (points.Count == 0) continue;
                var passes = host.Shape.Closed ? Passes(route.Shape, host.Shape, points, maxChordMm, tol, ref longChords) : Crossings(route.Shape, host.Shape, points, tol);
                foreach (var (centre, angle) in passes)
                    requests.Add(new OpeningRequest("", route.Handle, route.AecType, host.Handle, host.AecType, centre.Rounded(), size, size, Math.Round(angle, 2)));
            }
        }

        var ordered = requests.OrderBy(r => r.RouteHandle.Length).ThenBy(r => r.RouteHandle, StringComparer.Ordinal).ThenBy(r => r.HostHandle.Length).ThenBy(r => r.HostHandle, StringComparer.Ordinal).ThenBy(r => r.CenterMm.X).ThenBy(r => r.CenterMm.Y)
            .Select((r, n) => r with { Id = $"OPN-{n + 1:000}" }).ToArray();
        return new OpeningPlan(ordered, longChords);
    }

    /// <summary>A route through an outline: consecutive boundary stations whose stretch runs strictly inside are passes, opened at the route's middle point inside.</summary>
    private static IEnumerable<(Pt Centre, double Angle)> Passes(PlanShape route, PlanShape host, IReadOnlyList<Pt> points, double maxChordMm, GeometryTolerance tol, ref int longChords)
    {
        var passes = new List<(Pt, double)>();
        var stations = points.Select(p => (p, s: Station(route, p))).OrderBy(x => x.s).ToList();
        for (var k = 0; k + 1 < stations.Count; k++)
        {
            var (entry, s0) = stations[k];
            var s1 = stations[k + 1].s;
            if (s1 - s0 <= tol.PointEquality) continue; // the same crossing seen twice, or a vertex resting on the boundary
            var mid = PointAt(route, (s0 + s1) / 2);
            if (!host.ContainsPointXY(mid, tol.PointEquality) || host.DistanceToBoundaryXY(mid) <= tol.PointEquality) continue; // outside, or running along a face
            if (s1 - s0 > maxChordMm) { longChords++; continue; }
            passes.Add((mid, HostAngle(host, entry)));
        }

        return passes;
    }

    /// <summary>A route across a host line: the crossings where the route continues on the other side — not its own ends, not a touch-and-turn.</summary>
    private static IEnumerable<(Pt Centre, double Angle)> Crossings(PlanShape route, PlanShape host, IReadOnlyList<Pt> points, GeometryTolerance tol)
    {
        var length = route.Segments.Sum(s => s.LengthXY);
        var step = Math.Max(tol.EndpointConnection, 2 * tol.PointEquality);
        foreach (var p in points)
        {
            var s = Station(route, p);
            if (s - step < 0 || s + step > length) continue;
            var edge = host.Segments.MinBy(h => h.DistanceXY(p));
            if (Side(edge, PointAt(route, s - step)) * Side(edge, PointAt(route, s + step)) < 0) yield return (p, HostAngle(host, p));
        }
    }

    private static int Side(Seg edge, Pt p) => Math.Sign((edge.B.X - edge.A.X) * (p.Y - edge.A.Y) - (edge.B.Y - edge.A.Y) * (p.X - edge.A.X));

    /// <summary>Distance along the route to the point (the segment it lies on, plus its offset).</summary>
    private static double Station(PlanShape route, Pt p)
    {
        var run = 0.0;
        var best = (Station: 0.0, Distance: double.PositiveInfinity);
        foreach (var s in route.Segments)
        {
            var d = s.DistanceXY(p);
            if (d < best.Distance) best = (run + s.ClosestParameterXY(p) * s.LengthXY, d);
            run += s.LengthXY;
        }

        return best.Station;
    }

    /// <summary>The point of the route at a distance along it (clamped to its ends).</summary>
    private static Pt PointAt(PlanShape route, double station)
    {
        var run = 0.0;
        foreach (var s in route.Segments)
        {
            if (station <= run + s.LengthXY) return s.LengthXY <= 0 ? s.A : Pt.Lerp(s.A, s.B, Math.Clamp((station - run) / s.LengthXY, 0, 1));
            run += s.LengthXY;
        }

        return route.End;
    }

    /// <summary>The direction of the host's segment nearest the point, so the opening lies along the wall or beam.</summary>
    private static double HostAngle(PlanShape host, Pt p) => GeometryMath.DirectionDegreesXY(host.Segments.MinBy(s => s.DistanceXY(p))) % 180;
}
