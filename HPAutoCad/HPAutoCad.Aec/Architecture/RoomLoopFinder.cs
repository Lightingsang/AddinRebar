using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Spatial;

namespace HPAutoCad.Aec.Architecture;

/// <summary>One wall piece with the entity it came from.</summary>
public sealed record WallSegment(Seg Seg, string Handle);

/// <summary>A bounded face of the wall graph: its ring (counter-clockwise), the walls along it, area and perimeter.</summary>
public sealed record RoomLoop(IReadOnlyList<Pt> Ring, IReadOnlyList<string> Handles, double AreaMm2, double PerimeterMm);

/// <summary>A wall end that reaches nothing after gap closing and opening bridging, with the nearest other wall when one is within reach.</summary>
public sealed record OpenEnd(Pt PointMm, string Handle, double? NearestGapMm, string? NearestHandle);

/// <summary>An endpoint moved onto another wall because the gap was within the closing tolerance.</summary>
public sealed record ClosedGap(Pt FromMm, Pt ToMm, double GapMm, string Handle, string OtherHandle);

/// <summary>A doorway: two wall ends facing each other along one line (or two jamb lines facing each other) bridged by a virtual wall so the rooms on either side stay apart.</summary>
public sealed record Opening(Pt FromMm, Pt ToMm, double WidthMm, string Handle, string OtherHandle);

/// <summary>What the loop finder decides: rooms, the free ends left, the gaps it closed, the openings it bridged, and the counts of what it dropped.</summary>
public sealed record LoopOutcome(IReadOnlyList<RoomLoop> Loops, IReadOnlyList<OpenEnd> OpenEnds, IReadOnlyList<ClosedGap> ClosedGaps, IReadOnlyList<Opening> Openings,
    int Segments, int Nodes, int Cavities, int TinyFaces, int Nested, int Overshoots);

/// <summary>The sizes that decide what a gap, an opening and a wall cavity are; every number is overridable per call.</summary>
public sealed record LoopSettings(double GapMm, double MaxGapMm, double MinOpeningMm, double MaxOpeningMm, double MinAreaMm2, double MinWidthMm, double MaxWallThicknessMm)
{
    public static LoopSettings Default(GeometryTolerance tol) => new(tol.RoomGap, RoomLoopFinder.DefaultMaxGapMm, RoomLoopFinder.DefaultMinOpeningMm, RoomLoopFinder.DefaultMaxOpeningMm,
        RoomLoopFinder.DefaultMinAreaMm2, RoomLoopFinder.DefaultMinWidthMm, RoomLoopFinder.DefaultMaxWallThicknessMm);
}

/// <summary>
///     Rooms as the bounded faces of the wall drawing: wall pieces are cut at every crossing and T-junction (collinear overlaps
///     become one edge), endpoints within the gap tolerance are moved onto the wall they miss, doorways — two wall ends facing each
///     other along one line, or two short jamb lines facing each other across the wall — are bridged by virtual walls, dangling
///     pieces are peeled off (a wall overshooting the corner it crosses is not an open end), and the remaining planar graph is walked
///     face by face (leftmost turn, so a bounded face comes out counter-clockwise). Faces too small to be a room or too thin to be
///     anything but the cavity between the two lines of a wall are dropped and counted, and so is the outer line of a double-line
///     wall (a face whose inner loop hugs it within the wall thickness). An island (a column, a shaft) inside a room is its own face:
///     the room's area stays gross. Pure.
/// </summary>
public static class RoomLoopFinder
{
    /// <summary>Faces below this are closets, shafts or drafting artefacts, not rooms, unless the caller lowers it.</summary>
    public const double DefaultMinAreaMm2 = 500_000;

    /// <summary>A face whose mean width (area over half its perimeter) is below this is the cavity between the two lines of a wall (≤ 400 mm walls); 1 m closets stay rooms.</summary>
    public const double DefaultMinWidthMm = 450;

    /// <summary>A free wall end whose nearest other wall is farther than this is a missing wall, not a gap.</summary>
    public const double DefaultMaxGapMm = 300;

    /// <summary>Facing wall ends closer than this are a gap to report; from here up to <see cref="DefaultMaxOpeningMm"/> they are a doorway.</summary>
    public const double DefaultMinOpeningMm = 600;

    /// <summary>The widest doorway bridged; wider, the wall is simply missing.</summary>
    public const double DefaultMaxOpeningMm = 2500;

    /// <summary>The thickness of a wall: the outer line of a double-line wall runs this close to the inner lines, and a wall overshooting a corner by no more than this is not an open end.</summary>
    public const double DefaultMaxWallThicknessMm = 500;

    /// <summary>Segment pairs are checked against each other: beyond this many pieces the caller narrows the filter.</summary>
    public const int MaxSegments = 20_000;

    public static LoopOutcome Find(IReadOnlyList<WallSegment> walls, GeometryTolerance tol, LoopSettings settings, CancellationToken ct)
    {
        var pieces = walls.Where(w => w.Seg.LengthXY > tol.TinySegment).Select(w => new Piece(w.Seg.A, w.Seg.B, w.Handle)).ToList();
        if (pieces.Count > MaxSegments) throw new ArgumentException($"{pieces.Count} wall segments; the maximum is {MaxSegments} — narrow the filter (layers, space) or split the plan.");
        var closed = CloseGaps(pieces, tol, settings.GapMm, ct);
        var openings = OpeningBridger.Bridge(pieces, tol, settings, ct);
        var graph = WallGraph.Build(pieces, tol, ct);
        var openEnds = graph.PeelDangling(pieces, tol, settings.MaxGapMm, settings.MaxWallThicknessMm, ct, out var overshoots);
        var loops = new List<RoomLoop>();
        var cavities = 0;
        var tiny = 0;
        foreach (var face in graph.Faces(ct))
        {
            var ring = Simplify(face.Ring, tol);
            if (ring.Count < 3) continue;
            var area = GeometryMath.SignedAreaXY(ring);
            if (area <= 0) continue; // the outer face, or a face traced around a hole
            var perimeter = Perimeter(ring);
            if (area < settings.MinAreaMm2) { tiny++; continue; }
            if (perimeter > 0 && area / (perimeter / 2) < settings.MinWidthMm) { cavities++; continue; }
            loops.Add(new RoomLoop(ring, face.Handles.Where(h => !OpeningBridger.IsVirtual(h)).ToArray(), area, perimeter));
        }

        var nested = OuterWallLines(loops, tol, settings.MaxWallThicknessMm, ct);
        loops = loops.Where(l => !nested.Contains(l)).OrderByDescending(l => Box.Of(l.Ring).Max.Y).ThenBy(l => Box.Of(l.Ring).Min.X).ThenBy(l => l.Handles.FirstOrDefault() ?? "", StringComparer.Ordinal).ToList();
        return new LoopOutcome(loops, openEnds, closed, openings, pieces.Count(p => !p.Virtual), graph.NodeCount, cavities, tiny, nested.Count, overshoots);
    }

    /// <summary>
    ///     The outer line of a double-line wall: a loop whose every vertex lies within the wall thickness of the boundary of some loop
    ///     it contains — the outer line runs parallel to the inner lines all the way round. A hall with a riser in one corner, or a room
    ///     with a closet carved into it, has far corners no inner loop comes near, so it stays a room.
    /// </summary>
    private static HashSet<RoomLoop> OuterWallLines(List<RoomLoop> loops, GeometryTolerance tol, double maxWallThicknessMm, CancellationToken ct)
    {
        var index = new SpatialIndex<RoomLoop>();
        foreach (var l in loops) index.Insert(Box.Of(l.Ring), l);
        var outlines = loops.ToDictionary(l => l, l => new PlanShape(l.Ring, true));
        var nested = new HashSet<RoomLoop>();
        foreach (var outer in loops)
        {
            ct.ThrowIfCancellationRequested();
            var outerBox = Box.Of(outer.Ring);
            var contained = index.Query(outerBox, tol.PointEquality)
                .Where(inner => !ReferenceEquals(inner, outer) && outerBox.ContainsXY(Box.Of(inner.Ring), tol.PointEquality) && outlines[outer].ContainsPointXY(Room.InsidePoint(outlines[inner], tol), tol.PointEquality))
                .Select(inner => outlines[inner]).ToList();
            if (contained.Count > 0 && outer.Ring.All(v => contained.Any(inner => inner.DistanceToBoundaryXY(v) <= maxWallThicknessMm))) nested.Add(outer);
        }

        return nested;
    }

    /// <summary>An endpoint that touches nothing but sits within the gap of another wall is moved onto it (its nearest endpoint first, else its closest point).</summary>
    private static List<ClosedGap> CloseGaps(List<Piece> pieces, GeometryTolerance tol, double gapMm, CancellationToken ct)
    {
        var closed = new List<ClosedGap>();
        if (gapMm <= 0) return closed;
        var index = new SpatialIndex<Piece>();
        foreach (var p in pieces) index.Insert(Box.Of(p.A, p.B), p);
        foreach (var piece in pieces.OrderBy(p => p.Handle, StringComparer.Ordinal))
            for (var end = 0; end < 2; end++)
            {
                ct.ThrowIfCancellationRequested();
                var at = end == 0 ? piece.A : piece.B;
                Piece? best = null;
                var bestDistance = double.PositiveInfinity;
                Pt bestPoint = at;
                var touching = false;
                foreach (var other in index.Query(Box.Of(at, at), gapMm))
                {
                    if (ReferenceEquals(other, piece)) continue;
                    var seg = new Seg(other.A, other.B);
                    var onOther = seg.DistanceXY(at);
                    if (onOther <= tol.PointEquality) { touching = true; break; }
                    // an endpoint of the other wall within reach wins over its interior at the same distance: corners close as corners
                    var toEnd = Math.Min(at.DistanceXY(other.A), at.DistanceXY(other.B));
                    var candidate = toEnd <= gapMm && toEnd <= onOther + tol.PointEquality ? (toEnd, at.DistanceXY(other.A) <= at.DistanceXY(other.B) ? other.A : other.B) : (onOther, seg.ClosestPointXY(at));
                    if (candidate.Item1 < bestDistance) (best, bestDistance, bestPoint) = (other, candidate.Item1, candidate.Item2);
                }

                if (touching || best is null || bestDistance > gapMm) continue;
                if (end == 0) piece.A = bestPoint; else piece.B = bestPoint;
                closed.Add(new ClosedGap(at.Rounded(), bestPoint.Rounded(), Math.Round(bestDistance, 2), piece.Handle, best.Handle));
            }

        return closed;
    }

    /// <summary>Consecutive duplicates and collinear midpoints removed so a rectangle is four vertices whatever it was cut into.</summary>
    private static List<Pt> Simplify(IReadOnlyList<Pt> ring, GeometryTolerance tol)
    {
        var points = new List<Pt>();
        foreach (var p in ring)
            if (points.Count == 0 || !points[^1].AlmostEqualsXY(p, tol.PointEquality)) points.Add(p);
        if (points.Count > 1 && points[0].AlmostEqualsXY(points[^1], tol.PointEquality)) points.RemoveAt(points.Count - 1);
        var changed = true;
        while (changed && points.Count > 3)
        {
            changed = false;
            for (var i = 0; i < points.Count; i++)
            {
                var prev = points[(i + points.Count - 1) % points.Count];
                var next = points[(i + 1) % points.Count];
                if (GeometryMath.DistanceToLineXY(new Seg(prev, next), points[i]) <= tol.Collinearity && new Seg(prev, next).ClosestParameterXY(points[i]) is > 0 and < 1)
                {
                    points.RemoveAt(i);
                    changed = true;
                    break;
                }
            }
        }

        return points;
    }

    private static double Perimeter(IReadOnlyList<Pt> ring)
    {
        var sum = 0.0;
        for (var i = 0; i < ring.Count; i++) sum += ring[i].DistanceXY(ring[(i + 1) % ring.Count]);
        return sum;
    }

    /// <summary>A wall piece whose ends may be moved by gap closing; a <see cref="Virtual"/> piece bridges an opening and never counts as a wall.</summary>
    internal sealed class Piece(Pt a, Pt b, string handle, bool isVirtual = false)
    {
        public Pt A { get; set; } = a;
        public Pt B { get; set; } = b;
        public string Handle { get; } = handle;
        public bool Virtual { get; } = isVirtual;
    }
}
