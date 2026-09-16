using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Issues;
using HPAutoCad.Aec.Relationships;
using HPAutoCad.Aec.Spatial;

namespace HPAutoCad.Aec.Structural;

/// <summary>Issue types the structural checks report (category <c>structural</c>).</summary>
public static class StructuralIssueType
{
    public const string UnsupportedEnd = "unsupported_end";
    public const string GapToSupport = "gap_to_support";
    public const string BeamWithoutSupports = "beam_without_supports";
    public const string ColumnOffGrid = "column_off_grid";
    public const string ColumnNoGrid = "column_no_grid";
    public const string OpeningThroughColumn = "opening_through_column";
    public const string OpeningNearColumn = "opening_near_column";
    public const string OpeningOutsideHost = "opening_outside_host";

    public const string Category = "structural";
    public const string ConnectivityPrefix = "STR-CON";
    public const string AlignmentPrefix = "STR-ALN";
    public const string OpeningPrefix = "STR-OPN";
}

/// <summary>
///     Geometry-only structural checks on classified members: beam ends reaching a support, column centres on grid
///     intersections, openings clear of columns and inside a host. Every issue is located and measured; none says anything
///     about capacity. Pure — unit-tested on synthetic members and grids.
/// </summary>
public static class StructuralChecks
{
    /// <summary>A gap this many times the connection tolerance is still "close but not landed"; beyond it the end is unsupported.</summary>
    public const double LooseGapFactor = 3;

    /// <summary>Columns are matched to the nearest grid intersection within this radius; farther, the column has no grid.</summary>
    public const double GridSearchRadiusMm = 2000;

    /// <summary>Default clearance an opening should keep from a column face.</summary>
    public const double DefaultOpeningClearanceMm = 300;

    /// <summary>A column centre this far from its grid intersection is still "on grid" by default.</summary>
    public const double DefaultAlignmentToleranceMm = 25;

    // ---------------------------------------------------------------- connectivity

    public static IReadOnlyList<AuditIssue> Connectivity(IReadOnlyList<StructuralMember> members, GeometryTolerance tol, CancellationToken ct)
    {
        var supports = members.Where(m => MemberKind.Supports.Contains(m.Kind)).ToList();
        var index = new SpatialIndex<StructuralMember>();
        foreach (var s in supports) index.Insert(s.BoundsMm, s);
        var reach = tol.EndpointConnection * LooseGapFactor;
        var issues = new List<AuditIssue>();
        foreach (var beam in members.Where(m => m.Kind == MemberKind.Beam && m.Axis is not null))
        {
            ct.ThrowIfCancellationRequested();
            var ends = new[] { beam.Axis!.Value.A, beam.Axis.Value.B };
            var unsupported = 0;
            foreach (var end in ends)
            {
                var (best, gap) = NearestSupport(index, beam, end, reach, tol);
                if (best is not null && gap <= tol.EndpointConnection) continue;
                if (best is not null)
                {
                    issues.Add(Issue(StructuralIssueType.GapToSupport, IssueSeverity.Warning, [beam.Handle, best.Handle], end, gap,
                        $"Beam {beam.Handle} ends {gap:0.#} mm short of {best.Kind} {best.Handle} (tolerance {tol.EndpointConnection} mm).",
                        $"Extend the beam end to the {best.Kind} face (update_entities_batch geometry) or accept the gap by raising tolerance.endpointConnection."));
                    continue;
                }

                unsupported++;
                issues.Add(Issue(StructuralIssueType.UnsupportedEnd, IssueSeverity.Critical, [beam.Handle], end, null,
                    $"Beam {beam.Handle} end at ({end.X:0}, {end.Y:0}) reaches no column, wall or beam within {reach:0} mm.",
                    "Draw the support, extend the beam to one, or classify the support layer (classify_aec_entities ruleSet)."));
            }

            if (unsupported == 2)
                issues.Add(Issue(StructuralIssueType.BeamWithoutSupports, IssueSeverity.Critical, [beam.Handle], beam.CenterMm, beam.LengthMm,
                    $"Beam {beam.Handle} ({beam.LengthMm:0} mm) has no support at either end.", "Check the beam's layer and the supports around it; a free beam is usually a drafting leftover."));
        }

        return Number(issues, StructuralIssueType.ConnectivityPrefix);
    }

    /// <summary>The support closest to a beam end (boundary distance, 0 when the end lies inside/on it), excluding the beam itself and any parallel beam passing within the landing tolerance of the end — that is the same beam drawn twice, snapped or not.</summary>
    private static (StructuralMember? Support, double Gap) NearestSupport(SpatialIndex<StructuralMember> index, StructuralMember beam, Pt end, double reach, GeometryTolerance tol)
    {
        StructuralMember? best = null;
        var bestGap = double.PositiveInfinity;
        var area = Box.Of(new Pt(end.X - reach, end.Y - reach), new Pt(end.X + reach, end.Y + reach));
        foreach (var s in index.Query(area, 0))
        {
            if (s.Handle == beam.Handle) continue;
            if (s.Kind == MemberKind.Beam && s.Axis is { } axis && beam.Axis is { } mine && GeometryMath.AreParallelXY(axis, mine, tol.ParallelAngle) && GeometryMath.DistanceToLineXY(axis, end) <= tol.EndpointConnection) continue;
            var gap = s.Shape.ContainsPointXY(end, tol.PointEquality) ? 0 : s.Shape.DistanceToBoundaryXY(end);
            if (gap < bestGap) (best, bestGap) = (s, gap);
        }

        return bestGap <= reach ? (best, Math.Round(bestGap, 2)) : (null, double.PositiveInfinity);
    }

    // ---------------------------------------------------------------- alignment

    /// <summary>Empty when the grid has no intersections at all: nothing to align to is one warning for the caller, not one issue per column.</summary>
    public static IReadOnlyList<AuditIssue> ColumnAlignment(IReadOnlyList<StructuralMember> members, GridSystem grids, double alignmentToleranceMm, double searchRadiusMm, CancellationToken ct)
    {
        var issues = new List<AuditIssue>();
        if (grids.Intersections.Count == 0) return issues;
        foreach (var column in members.Where(m => m.Kind == MemberKind.Column))
        {
            ct.ThrowIfCancellationRequested();
            var nearest = grids.Intersections.Select(i => (i, d: i.PointMm.DistanceXY(column.CenterMm))).Where(x => x.d <= searchRadiusMm).OrderBy(x => x.d).FirstOrDefault();
            if (nearest.i is null)
            {
                issues.Add(Issue(StructuralIssueType.ColumnNoGrid, IssueSeverity.Warning, [column.Handle], column.CenterMm, null,
                    $"Column {column.Handle} at ({column.CenterMm.X:0}, {column.CenterMm.Y:0}) has no grid intersection within {searchRadiusMm:0} mm.",
                    "Add the grid lines, or check the column belongs to this plan."));
                continue;
            }

            if (nearest.d > alignmentToleranceMm)
            {
                var dx = column.CenterMm.X - nearest.i.PointMm.X;
                var dy = column.CenterMm.Y - nearest.i.PointMm.Y;
                issues.Add(Issue(StructuralIssueType.ColumnOffGrid, IssueSeverity.Warning, [column.Handle, nearest.i.HandleA, nearest.i.HandleB], column.CenterMm, Math.Round(nearest.d, 2),
                    $"Column {column.Handle} centre is {nearest.d:0.#} mm off grid {nearest.i.A}/{nearest.i.B} (dx {dx:+0.#;-0.#}, dy {dy:+0.#;-0.#}; tolerance {alignmentToleranceMm} mm).",
                    "Move the column onto the intersection (update_entities_batch move) or confirm the offset is intended (eccentric column)."));
            }
        }

        return Number(issues, StructuralIssueType.AlignmentPrefix);
    }

    // ---------------------------------------------------------------- openings

    public static IReadOnlyList<AuditIssue> OpeningConflicts(IReadOnlyList<StructuralMember> members, GeometryTolerance tol, double clearanceMm, CancellationToken ct)
    {
        var columns = members.Where(m => m.Kind == MemberKind.Column).ToList();
        var hosts = members.Where(m => m.Kind is MemberKind.Slab or MemberKind.Wall && m.Shape.Closed).ToList();
        var issues = new List<AuditIssue>();
        foreach (var opening in members.Where(m => m.Kind == MemberKind.Opening))
        {
            ct.ThrowIfCancellationRequested();
            foreach (var column in columns)
            {
                var m = SpatialPredicates.Evaluate(opening.Shape, column.Shape, SpatialRelation.Intersects, tol);
                // Sharing a face or a corner is the usual shaft detail — tight, not cut: it is reported as zero clearance, never as "through".
                var through = m.Holds && CutsInto(opening.Shape, column.Shape, tol);
                var touches = m.Holds && !through;
                if (through)
                {
                    issues.Add(Issue(StructuralIssueType.OpeningThroughColumn, IssueSeverity.Critical, [opening.Handle, column.Handle], m.Points.Count > 0 ? m.Points[0] : opening.CenterMm, null,
                        $"Opening {opening.Handle} cuts through column {column.Handle}.", "Move or resize the opening; a column cannot be cut by a slab opening."));
                    continue;
                }

                var d = touches ? 0 : SpatialPredicates.DistanceXY(opening.Shape, column.Shape, tol);
                if (d < clearanceMm)
                    issues.Add(Issue(StructuralIssueType.OpeningNearColumn, IssueSeverity.Warning, [opening.Handle, column.Handle], opening.CenterMm, Math.Round(d, 2),
                        $"Opening {opening.Handle} is {d:0.#} mm from column {column.Handle} (clearance {clearanceMm:0} mm).", "Keep the opening clear of the column zone or confirm the detail with the engineer."));
            }

            if (hosts.Count > 0 && !hosts.Any(h => SpatialPredicates.IsWithin(opening.Shape, h.Shape, tol)))
                issues.Add(Issue(StructuralIssueType.OpeningOutsideHost, IssueSeverity.Warning, [opening.Handle], opening.CenterMm, null,
                    $"Opening {opening.Handle} lies outside every slab/wall outline.", "Check the opening's position or the host outline (slab layer)."));
        }

        return Number(issues, StructuralIssueType.OpeningPrefix);
    }

    // ---------------------------------------------------------------- helpers

    /// <summary>Interiors overlap: a proper crossing, a vertex strictly inside the other outline, or one outline inside the other — not a shared edge or corner.</summary>
    private static bool CutsInto(PlanShape a, PlanShape b, GeometryTolerance tol) =>
        SpatialPredicates.Evaluate(a, b, SpatialRelation.Crosses, tol).Holds
        || a.Vertices.Any(v => b.ContainsPointXY(v, tol.PointEquality) && b.DistanceToBoundaryXY(v) > tol.PointEquality)
        || b.Vertices.Any(v => a.ContainsPointXY(v, tol.PointEquality) && a.DistanceToBoundaryXY(v) > tol.PointEquality)
        || SpatialPredicates.IsWithin(a, b, tol) || SpatialPredicates.IsWithin(b, a, tol);

    private static AuditIssue Issue(string type, string severity, IReadOnlyList<string> handles, Pt at, double? valueMm, string description, string action) =>
        new("", StructuralIssueType.Category, type, severity, handles, at.Rounded(), valueMm, description, action);

    private static IReadOnlyList<AuditIssue> Number(IEnumerable<AuditIssue> issues, string prefix) =>
        AuditIssue.Ordered(issues).Select((i, n) => i with { IssueId = $"{prefix}-{n + 1:000}" }).ToArray();
}
