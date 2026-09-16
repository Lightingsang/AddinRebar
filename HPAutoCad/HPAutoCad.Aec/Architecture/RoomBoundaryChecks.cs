using System.Globalization;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Issues;
using HPAutoCad.Aec.Spatial;

namespace HPAutoCad.Aec.Architecture;

/// <summary>Issue types the room boundary check reports (category <c>architecture</c>).</summary>
public static class RoomIssueType
{
    public const string OpenBoundary = "open_boundary";
    public const string BoundaryGap = "boundary_gap";
    public const string BoundaryGapClosed = "boundary_gap_closed";
    public const string OpeningAssumed = "opening_assumed";
    public const string RoomOverlap = "room_overlap";
    public const string RoomInsideRoom = "room_inside_room";
    public const string DuplicateRoom = "duplicate_room";
    public const string UnlabelledRoom = "unlabelled_room";

    public const string Category = "architecture";
    public const string Prefix = "ARC";

    public static readonly IReadOnlyList<string> All = [OpenBoundary, BoundaryGap, BoundaryGapClosed, OpeningAssumed, RoomOverlap, RoomInsideRoom, DuplicateRoom, UnlabelledRoom];
}

/// <summary>
///     What keeps a plan from reading as rooms: wall ends that reach nothing (one <c>boundary_gap</c> per gap while another wall is
///     within reach, <c>open_boundary</c> beyond), gaps the loop finder closed and doorways it assumed (info, so the drafter knows),
///     explicit room outlines that overlap in their interiors, sit inside one another, or duplicate each other, rooms with no label. Pure.
/// </summary>
public static class RoomBoundaryChecks
{
    public static IReadOnlyList<AuditIssue> Check(LoopOutcome loops, IReadOnlyList<Room> rooms, GeometryTolerance tol, double maxGapMm, CancellationToken ct)
    {
        var issues = new List<AuditIssue>();
        foreach (var g in loops.ClosedGaps)
            issues.Add(Issue(RoomIssueType.BoundaryGapClosed, IssueSeverity.Info, [g.Handle, g.OtherHandle], g.FromMm, g.GapMm,
                $"Wall {g.Handle} stopped {Mm(g.GapMm)} mm short of wall {g.OtherHandle}; the gap was closed for room detection.",
                "Snap the wall end to the other wall (update_entities_batch geometry) so the drawing itself is closed."));
        foreach (var o in loops.Openings)
            issues.Add(Issue(RoomIssueType.OpeningAssumed, IssueSeverity.Info, [o.Handle, o.OtherHandle], Pt.Mid(o.FromMm, o.ToMm), o.WidthMm,
                $"A {Mm(o.WidthMm)} mm doorway between walls {o.Handle} and {o.OtherHandle} was assumed; the rooms on either side are kept apart.",
                "Nothing to do when it is a door; if it is a missing wall, draw it (a gap wider than maxOpeningMm is reported as open)."));

        // Two free ends facing each other are one gap, not two issues naming each other.
        var reported = new HashSet<(string, string)>();
        foreach (var end in loops.OpenEnds)
        {
            ct.ThrowIfCancellationRequested();
            if (end.NearestGapMm is { } gap && gap <= maxGapMm)
            {
                var pair = string.CompareOrdinal(end.Handle, end.NearestHandle) < 0 ? (end.Handle, end.NearestHandle!) : (end.NearestHandle!, end.Handle);
                var facing = loops.OpenEnds.FirstOrDefault(o => !ReferenceEquals(o, end) && o.Handle == end.NearestHandle && o.NearestHandle == end.Handle && o.PointMm.DistanceXY(end.PointMm) <= maxGapMm + tol.PointEquality);
                if (facing is not null && !reported.Add(pair)) continue;
                var at = facing is null ? end.PointMm : Pt.Mid(end.PointMm, facing.PointMm);
                issues.Add(Issue(RoomIssueType.BoundaryGap, IssueSeverity.Warning, [end.Handle, end.NearestHandle!], at, gap,
                    $"Wall {end.Handle} ends {Mm(gap)} mm from wall {end.NearestHandle} at ({Mm(at.X)}, {Mm(at.Y)}); the room behind it is open.",
                    "Extend the wall to meet the other (update_entities_batch geometry) or pass tolerance.roomGap to close gaps this size."));
            }
            else
                issues.Add(Issue(RoomIssueType.OpenBoundary, IssueSeverity.Critical, [end.Handle], end.PointMm, end.NearestGapMm,
                    $"Wall {end.Handle} ends free at ({Mm(end.PointMm.X)}, {Mm(end.PointMm.Y)}): no wall within {Mm(maxGapMm)} mm — no room closes here.",
                    "Draw the missing wall, or check the wall layer classification (classify_aec_entities) and maxOpeningMm when this is a wide doorway."));
        }

        var outlines = rooms.Where(r => r.Source == Room.FromOutline).ToList();
        var index = new SpatialIndex<Room>();
        foreach (var r in outlines) index.Insert(r.Outline.Bounds, r);
        var seen = new HashSet<(string, string)>();
        foreach (var a in outlines)
        {
            ct.ThrowIfCancellationRequested();
            foreach (var b in index.Query(a.Outline.Bounds, tol.PointEquality))
            {
                if (ReferenceEquals(a, b) || !seen.Add(string.CompareOrdinal(a.Id, b.Id) < 0 ? (a.Id, b.Id) : (b.Id, a.Id))) continue;
                if (SameRing(a.Outline, b.Outline, tol))
                    issues.Add(Issue(RoomIssueType.DuplicateRoom, IssueSeverity.Warning, [.. a.Handles, .. b.Handles], a.CentroidMm, a.AreaMm2,
                        $"Room outlines {a.Id} and {b.Id} are the same outline drawn twice ({a.AreaM2} m²).", "Erase one of them; a duplicated outline doubles the area schedule."));
                else if (SpatialPredicates.IsWithin(a.Outline, b.Outline, tol) || SpatialPredicates.IsWithin(b.Outline, a.Outline, tol))
                {
                    var (inner, outer) = a.AreaMm2 <= b.AreaMm2 ? (a, b) : (b, a);
                    issues.Add(Issue(RoomIssueType.RoomInsideRoom, IssueSeverity.Info, [.. inner.Handles, .. outer.Handles], inner.CentroidMm, inner.AreaMm2,
                        $"Room outline {inner.Id} ({inner.AreaM2} m²) lies inside {outer.Id} ({outer.AreaM2} m²); both are scheduled.", "Keep it when it is a booth inside a hall; otherwise the outer one is a zone, not a room."));
                }
                else if (Interpenetrate(a.Outline, b.Outline, tol))
                    issues.Add(Issue(RoomIssueType.RoomOverlap, IssueSeverity.Warning, [.. a.Handles, .. b.Handles], Overlap(a, b), null,
                        $"Room outlines {a.Id} and {b.Id} overlap.", "Room outlines must tile the floor; trim them to the shared wall."));
            }
        }

        foreach (var r in rooms.Where(r => r.Name is null && r.Number is null))
            issues.Add(Issue(RoomIssueType.UnlabelledRoom, IssueSeverity.Info, r.Handles, r.LabelPointMm, r.AreaMm2,
                $"Room {r.Id} ({r.AreaM2} m²) has no name or number text inside it.", "Tag it (arch_create_room_tags) or add the room text."));

        return AuditIssue.Ordered(issues).Select((i, n) => i with { IssueId = $"{RoomIssueType.Prefix}-{n + 1:000}" }).ToArray();
    }

    /// <summary>Interiors overlap: a proper crossing or a vertex strictly inside the other — a shared wall edge or corner is how outlines tile.</summary>
    private static bool Interpenetrate(PlanShape a, PlanShape b, GeometryTolerance tol) =>
        SpatialPredicates.IntersectionPoints(a, b, tol, proper: true).Count > 0
        || a.Vertices.Any(v => b.ContainsPointXY(v, tol.PointEquality) && b.DistanceToBoundaryXY(v) > tol.PointEquality)
        || b.Vertices.Any(v => a.ContainsPointXY(v, tol.PointEquality) && a.DistanceToBoundaryXY(v) > tol.PointEquality);

    private static bool SameRing(PlanShape a, PlanShape b, GeometryTolerance tol) =>
        a.Vertices.Count == b.Vertices.Count && Math.Abs(a.AreaMm2 - b.AreaMm2) <= tol.PointEquality * a.LengthMm
        && a.Vertices.All(p => b.Vertices.Any(q => q.AlmostEqualsXY(p, tol.PointEquality)));

    private static Pt Overlap(Room a, Room b)
    {
        var box = Box.Of(new Pt(Math.Max(a.Outline.Bounds.Min.X, b.Outline.Bounds.Min.X), Math.Max(a.Outline.Bounds.Min.Y, b.Outline.Bounds.Min.Y)),
            new Pt(Math.Min(a.Outline.Bounds.Max.X, b.Outline.Bounds.Max.X), Math.Min(a.Outline.Bounds.Max.Y, b.Outline.Bounds.Max.Y)));
        return box.IsEmpty ? a.CentroidMm : box.Center;
    }

    private static string Mm(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);

    private static AuditIssue Issue(string type, string severity, IReadOnlyList<string> handles, Pt at, double? valueMm, string description, string action) =>
        new("", RoomIssueType.Category, type, severity, handles, at.Rounded(), valueMm, description, action);
}
