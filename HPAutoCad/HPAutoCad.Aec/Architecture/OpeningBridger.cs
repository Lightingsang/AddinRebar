using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Spatial;

namespace HPAutoCad.Aec.Architecture;

/// <summary>
///     Doorways in the wall drawing, bridged by virtual walls before the faces are walked so the rooms on either side stay apart:
///     (1) two free wall ends facing each other along one line, between <see cref="LoopSettings.MinOpeningMm"/> and
///     <see cref="LoopSettings.MaxOpeningMm"/> apart (single-line walls); (2) two jamb lines — short pieces spanning the two long
///     lines of a double-line wall — facing each other across the same wall at that distance (the two bridges fence the doorway
///     cell, which is then dropped as tiny). Openings are inferred from the walls alone; door objects are not needed. Pure.
/// </summary>
internal static class OpeningBridger
{
    private const string VirtualPrefix = "opening:";

    /// <summary>The long lines of a wall a jamb may span must be at least this long — twice the wall thickness keeps column outlines from pairing as jambs.</summary>
    private const double WallLineFactor = 2;

    public static bool IsVirtual(string handle) => handle.StartsWith(VirtualPrefix, StringComparison.Ordinal);

    public static List<Opening> Bridge(List<RoomLoopFinder.Piece> pieces, GeometryTolerance tol, LoopSettings s, CancellationToken ct)
    {
        var openings = new List<Opening>();
        if (s.MaxOpeningMm <= 0 || s.MinOpeningMm > s.MaxOpeningMm) return openings;
        var index = new SpatialIndex<RoomLoopFinder.Piece>();
        foreach (var p in pieces) index.Insert(Box.Of(p.A, p.B), p);

        // (1) free ends facing each other along one line
        var tips = new List<(RoomLoopFinder.Piece Piece, Pt At, Pt Direction)>();
        foreach (var piece in pieces)
        {
            ct.ThrowIfCancellationRequested();
            if (IsFree(index, piece, piece.A, tol)) tips.Add((piece, piece.A, (piece.A - piece.B).NormalizedXY()));
            if (IsFree(index, piece, piece.B, tol)) tips.Add((piece, piece.B, (piece.B - piece.A).NormalizedXY()));
        }

        var used = new HashSet<(string, Pt)>();
        foreach (var (piece, at, direction) in tips.OrderBy(t => t.Piece.Handle, StringComparer.Ordinal).ThenBy(t => t.At.X).ThenBy(t => t.At.Y))
        {
            ct.ThrowIfCancellationRequested();
            if (used.Contains((piece.Handle, at))) continue;
            var line = new Seg(at - direction, at);
            var partner = tips.Where(t => !ReferenceEquals(t.Piece, piece) && !used.Contains((t.Piece.Handle, t.At)))
                .Select(t => (t, d: (t.At - at).DotXY(direction)))
                .Where(x => x.d >= s.MinOpeningMm && x.d <= s.MaxOpeningMm && GeometryMath.DistanceToLineXY(line, x.t.At) <= tol.RoomGap && x.t.Direction.DotXY(direction) < 0)
                .OrderBy(x => x.d).Select(x => x.t).FirstOrDefault();
            if (partner.Piece is null) continue;
            AddBridge(pieces, at, partner.At, piece.Handle, partner.Piece.Handle, openings);
            used.Add((piece.Handle, at));
            used.Add((partner.Piece.Handle, partner.At));
        }

        // (2) jambs of double-line walls facing each other
        var minWallLine = s.MaxWallThicknessMm * WallLineFactor;
        var jambs = pieces.Where(p => !p.Virtual && new Seg(p.A, p.B).LengthXY <= s.MaxWallThicknessMm)
            .Select(p => (Piece: p, Lines: WallLinesOf(index, p, tol, minWallLine)))
            .Where(j => j.Lines is not null).ToList();
        var paired = new HashSet<RoomLoopFinder.Piece>();
        foreach (var (jamb, lines) in jambs.OrderBy(j => j.Piece.Handle, StringComparer.Ordinal))
        {
            ct.ThrowIfCancellationRequested();
            if (paired.Contains(jamb)) continue;
            var axis = new Seg(jamb.A, jamb.B);
            var across = (lines!.Value.First.DirectionXY);
            var partner = jambs.Where(k => !ReferenceEquals(k.Piece, jamb) && !paired.Contains(k.Piece) && GeometryMath.AreParallelXY(axis, new Seg(k.Piece.A, k.Piece.B), tol.ParallelAngle))
                .Where(k => GeometryMath.AreCollinearXY(lines.Value.First, k.Lines!.Value.First, tol.RoomGap, tol.ParallelAngle) && GeometryMath.AreCollinearXY(lines.Value.Second, k.Lines.Value.Second, tol.RoomGap, tol.ParallelAngle)
                            || GeometryMath.AreCollinearXY(lines.Value.First, k.Lines!.Value.Second, tol.RoomGap, tol.ParallelAngle) && GeometryMath.AreCollinearXY(lines.Value.Second, k.Lines.Value.First, tol.RoomGap, tol.ParallelAngle))
                .Select(k => (k, d: Math.Abs((k.Piece.A - jamb.A).DotXY(across))))
                .Where(x => x.d >= s.MinOpeningMm && x.d <= s.MaxOpeningMm && NothingBetween(index, jamb, x.k.Piece, tol))
                .OrderBy(x => x.d).Select(x => x.k.Piece).FirstOrDefault();
            if (partner is null) continue;
            var (pa, pb) = jamb.A.DistanceXY(partner.A) <= jamb.A.DistanceXY(partner.B) ? (partner.A, partner.B) : (partner.B, partner.A);
            AddBridge(pieces, jamb.A, pa, jamb.Handle, partner.Handle, openings);
            AddBridge(pieces, jamb.B, pb, jamb.Handle, partner.Handle, null);
            paired.Add(jamb);
            paired.Add(partner);
        }

        return openings;
    }

    private static void AddBridge(List<RoomLoopFinder.Piece> pieces, Pt from, Pt to, string handle, string otherHandle, List<Opening>? openings)
    {
        pieces.Add(new RoomLoopFinder.Piece(from, to, $"{VirtualPrefix}{handle}:{otherHandle}", isVirtual: true));
        openings?.Add(new Opening(from.Rounded(), to.Rounded(), Math.Round(from.DistanceXY(to), 1), handle, otherHandle));
    }

    private static bool IsFree(SpatialIndex<RoomLoopFinder.Piece> index, RoomLoopFinder.Piece piece, Pt at, GeometryTolerance tol) =>
        !index.Query(Box.Of(at, at), tol.PointEquality).Any(o => !ReferenceEquals(o, piece) && new Seg(o.A, o.B).DistanceXY(at) <= tol.PointEquality);

    /// <summary>The two long, parallel wall lines a jamb spans (one at each end, both at least <paramref name="minLength"/>), or null when it is not a jamb.</summary>
    private static (Seg First, Seg Second)? WallLinesOf(SpatialIndex<RoomLoopFinder.Piece> index, RoomLoopFinder.Piece jamb, GeometryTolerance tol, double minLength)
    {
        var axis = new Seg(jamb.A, jamb.B);
        Seg? LineAt(Pt end) => index.Query(Box.Of(end, end), tol.PointEquality)
            .Where(o => !ReferenceEquals(o, jamb) && !o.Virtual && new Seg(o.A, o.B).DistanceXY(end) <= tol.PointEquality)
            .Select(o => new Seg(o.A, o.B))
            .Where(l => l.LengthXY >= minLength && GeometryMath.ArePerpendicularXY(l, axis, tol.ParallelAngle))
            .OrderByDescending(l => l.LengthXY).Select(l => (Seg?)l).FirstOrDefault();
        var first = LineAt(jamb.A);
        var second = LineAt(jamb.B);
        return first is { } f && second is { } se && GeometryMath.AreParallelXY(f, se, tol.ParallelAngle) ? (f, se) : null;
    }

    /// <summary>No wall crosses the doorway between two jambs (a third jamb in between means another opening, not this one).</summary>
    private static bool NothingBetween(SpatialIndex<RoomLoopFinder.Piece> index, RoomLoopFinder.Piece a, RoomLoopFinder.Piece b, GeometryTolerance tol)
    {
        var mid = new Seg(Pt.Mid(a.A, a.B), Pt.Mid(b.A, b.B));
        return !index.Query(Box.Of(mid.A, mid.B), tol.PointEquality).Any(o => !ReferenceEquals(o, a) && !ReferenceEquals(o, b) && !o.Virtual
            && GeometryMath.IntersectXY(mid, new Seg(o.A, o.B), tol.PointEquality, out _) != IntersectionKind.None);
    }
}
