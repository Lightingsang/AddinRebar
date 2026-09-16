using HPAutoCad.Aec.Classification;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Issues;
using HPAutoCad.Aec.Model;
using HPAutoCad.Aec.Structural;
using Xunit;

namespace HPAutoCad.Aec.Tests;

public sealed class StructuralTests
{
    private static readonly GeometryTolerance Tol = GeometryTolerance.Default;

    private static Pt P(double x, double y) => new(x, y);

    private static AecObject Obj(string handle, string aecType, PlanShape shape, string type = "LWPOLYLINE", string layer = "S-GRID") => new()
    {
        Handle = handle, Type = type, Layer = layer, AecType = aecType, Confidence = 0.9, BoundsMm = shape.Bounds,
        Record = new AecEntityRecord { Handle = handle, Type = type, Layer = layer, Shape = shape, BoundsMm = shape.Bounds },
    };

    private static AecEntityRecord Text(string handle, string text, Pt at) =>
        new() { Handle = handle, Type = "TEXT", Layer = "S-GRID", Text = text, PositionMm = at, BoundsMm = Box.Of(at, at + P(200, 100)), Shape = PlanShape.Rectangle(Box.Of(at, at + P(200, 100))) };

    private static PlanShape Rect(double x, double y, double w, double h) => new([P(x, y), P(x + w, y), P(x + w, y + h), P(x, y + h)], true);

    private static PlanShape Circle(Pt c, double r) => new(PlanShape.ArcPoints(c, r, 0, 360, 1, 0), true, 2 * Math.PI * r, Math.PI * r * r, approximate: true);

    private static StructuralMember Member(string handle, string kind, PlanShape shape, string? mark = null)
    {
        var aecType = kind switch { MemberKind.Column => AecType.StructuralColumn, MemberKind.Beam => AecType.StructuralBeam, MemberKind.Wall => AecType.StructuralWall, MemberKind.Slab => AecType.StructuralSlab, _ => AecType.StructuralOpening };
        var o = Obj(handle, aecType, shape, shape.Closed ? "LWPOLYLINE" : "LINE", "S-X");
        return StructuralMember.From(o, ShapeMetrics.Of(o.Record))! with { Mark = mark };
    }

    // ---------------------------------------------------------------- grids

    private static (GridSystem Grids, AecClassifier.TextIndex Texts) SampleGrid()
    {
        var objects = new[]
        {
            Obj("GA", AecType.StructuralGrid, PlanShape.Segment(P(-1500, 0), P(7500, 0)), "LINE"),
            Obj("GB", AecType.StructuralGrid, PlanShape.Segment(P(-1500, 5010), P(7500, 5010)), "LINE"),
            Obj("G1", AecType.StructuralGrid, PlanShape.Segment(P(0, -1500), P(0, 6500)), "LINE"),
            Obj("G2", AecType.StructuralGrid, PlanShape.Segment(P(6000, -1500), P(6000, 6500)), "LINE"),
            Obj("BA", AecType.StructuralGrid, Circle(P(-1900, 0), 400), "CIRCLE"),
            Obj("BB", AecType.StructuralGrid, Circle(P(-1900, 5010), 400), "CIRCLE"),
            Obj("B1", AecType.StructuralGrid, Circle(P(0, 6900), 400), "CIRCLE"),
            Obj("B2", AecType.StructuralGrid, Circle(P(6000, 6900), 400), "CIRCLE"),
        };
        var texts = new AecClassifier.TextIndex([Text("TA", "A", P(-1950, -50)), Text("TB", "B", P(-1950, 4960)), Text("T1", "1", P(-50, 6850)), Text("T2", "2", P(5950, 6850)), Text("TN", "GENERAL NOTES HERE", P(2000, 2000))]);
        return (GridDetector.Detect(objects, texts, Tol), texts);
    }

    [Fact]
    public void Grid_lines_get_labels_from_bubbles_directions_intersections_and_spacing()
    {
        var (grids, _) = SampleGrid();

        Assert.Equal(4, grids.Lines.Count);
        Assert.Equal(["A", "B", "1", "2"], grids.Lines.Select(l => l.Label));
        Assert.Equal([GridLine.Horizontal, GridLine.Horizontal, GridLine.Vertical, GridLine.Vertical], grids.Lines.Select(l => l.Direction));
        Assert.Equal("BA", grids.Lines[0].BubbleHandle);
        Assert.Equal(4, grids.Intersections.Count);
        Assert.Contains(grids.Intersections, i => i.A == "B" && i.B == "2" && i.PointMm.AlmostEqualsXY(P(6000, 5010), 1e-6));
        Assert.Equal([5010.0], grids.SpacingMm[GridLine.Horizontal]);
        Assert.Equal([6000.0], grids.SpacingMm[GridLine.Vertical]);
    }

    [Fact]
    public void Lines_that_stop_short_of_each_other_still_intersect_within_the_reach_and_a_far_bubble_is_ignored()
    {
        var objects = new[]
        {
            Obj("GA", AecType.StructuralGrid, PlanShape.Segment(P(500, 0), P(7500, 0)), "LINE"),       // starts 500 mm right of grid 1
            Obj("G1", AecType.StructuralGrid, PlanShape.Segment(P(0, 500), P(0, 6500)), "LINE"),
            Obj("BX", AecType.StructuralGrid, Circle(P(20000, 0), 400), "CIRCLE"),                   // too far to belong to A
        };
        var texts = new AecClassifier.TextIndex([Text("TX", "X", P(19950, -50))]);

        var grids = GridDetector.Detect(objects, texts, Tol);

        Assert.Single(grids.Intersections);
        Assert.True(grids.Intersections[0].PointMm.AlmostEqualsXY(P(0, 0), 1e-6));
        Assert.Null(grids.Lines.Single(l => l.Handle == "GA").Label);
        Assert.Equal("GA", grids.Intersections[0].A == "GA" ? "GA" : grids.Intersections[0].B);
    }

    // ---------------------------------------------------------------- members + checks

    [Fact]
    public void Members_carry_sections_axes_and_the_default_prefix_per_kind()
    {
        var column = Member("C1", MemberKind.Column, Rect(-200, -200, 400, 400));
        var beam = Member("B1", MemberKind.Beam, PlanShape.Segment(P(200, 0), P(5800, 0)));

        Assert.Equal("400×400", column.Section);
        Assert.Null(column.Axis); // a square has no dominant direction
        Assert.Equal("L 5600", beam.Section);
        Assert.NotNull(beam.Axis);
        Assert.Equal(5600, beam.LengthMm);
        Assert.Equal("C", MemberKind.DefaultPrefix(MemberKind.Column));
        Assert.Equal("O", MemberKind.DefaultPrefix(MemberKind.Opening));
    }

    [Fact]
    public void Connectivity_reports_landed_gapped_and_unsupported_beam_ends_and_ignores_the_beam_drawn_twice()
    {
        var members = new[]
        {
            Member("C1", MemberKind.Column, Rect(-200, -200, 400, 400)),
            Member("C2", MemberKind.Column, Rect(5800, -200, 400, 400)),
            Member("B1", MemberKind.Beam, PlanShape.Segment(P(200, 0), P(5800, 0))),     // lands on both
            Member("B1D", MemberKind.Beam, PlanShape.Segment(P(5800, 0), P(200, 0))),    // duplicate of B1 (must not support itself)
            Member("B2", MemberKind.Beam, PlanShape.Segment(P(200, 100), P(5780, 100))), // 20 mm short of C2 → gap (≤ 3×10)
            Member("B3", MemberKind.Beam, PlanShape.Segment(P(200, 3000), P(4000, 3000))), // one end free
        };

        var issues = StructuralChecks.Connectivity(members, Tol, CancellationToken.None);

        Assert.DoesNotContain(issues, i => i.Handles.Contains("B1") || i.Handles.Contains("B1D"));
        var gap = Assert.Single(issues, i => i.Type == StructuralIssueType.GapToSupport);
        Assert.Equal(["B2", "C2"], gap.Handles);
        Assert.Equal(20, gap.ValueMm);
        Assert.Equal(IssueSeverity.Warning, gap.Severity);
        var free = issues.Where(i => i.Type == StructuralIssueType.UnsupportedEnd).ToArray();
        Assert.Equal(2, free.Length);
        Assert.All(free, i => Assert.Equal(["B3"], i.Handles));
        Assert.Contains(free, i => i.LocationMm!.Value.AlmostEqualsXY(P(4000, 3000), 1e-6));
        Assert.Single(issues, i => i.Type == StructuralIssueType.BeamWithoutSupports);
        Assert.StartsWith("STR-CON-", issues[0].IssueId);
        Assert.Equal(IssueSeverity.Critical, issues[0].Severity); // critical first
    }

    [Fact]
    public void A_beam_supported_nowhere_is_one_critical_issue_per_end_plus_the_whole_beam()
    {
        var members = new[] { Member("B9", MemberKind.Beam, PlanShape.Segment(P(0, 0), P(3000, 0))) };

        var issues = StructuralChecks.Connectivity(members, Tol, CancellationToken.None);

        Assert.Equal(2, issues.Count(i => i.Type == StructuralIssueType.UnsupportedEnd));
        Assert.Single(issues, i => i.Type == StructuralIssueType.BeamWithoutSupports && i.ValueMm == 3000);
    }

    [Fact]
    public void Column_alignment_measures_the_offset_to_the_nearest_intersection_and_flags_columns_without_a_grid()
    {
        var (grids, _) = SampleGrid();
        var members = new[]
        {
            Member("C1", MemberKind.Column, Rect(-200, -200, 400, 400)),     // on A/1
            Member("C3", MemberKind.Column, Rect(-200, 4800, 400, 400)),     // centre y 5000, grid B at 5010 → 10 mm off
            Member("C9", MemberKind.Column, Rect(19800, 19800, 400, 400)),   // no grid within 2 000
        };

        var loose = StructuralChecks.ColumnAlignment(members, grids, 25, StructuralChecks.GridSearchRadiusMm, CancellationToken.None);
        var tight = StructuralChecks.ColumnAlignment(members, grids, 5, StructuralChecks.GridSearchRadiusMm, CancellationToken.None);

        Assert.Single(loose, i => i.Type == StructuralIssueType.ColumnNoGrid && i.Handles[0] == "C9");
        Assert.DoesNotContain(loose, i => i.Type == StructuralIssueType.ColumnOffGrid);
        var off = Assert.Single(tight, i => i.Type == StructuralIssueType.ColumnOffGrid);
        Assert.Equal(["C3", "GB", "G1"], off.Handles);
        Assert.Equal(10, off.ValueMm);
        Assert.Contains("B/1", off.Description);
        Assert.StartsWith("STR-ALN-", off.IssueId);
    }

    [Fact]
    public void Opening_conflicts_through_near_and_outside_host()
    {
        var members = new[]
        {
            Member("C1", MemberKind.Column, Rect(-200, -200, 400, 400)),
            Member("SL", MemberKind.Slab, Rect(-500, -500, 7000, 6000)),
            Member("O1", MemberKind.Opening, Rect(100, 100, 300, 300)),      // through C1
            Member("O2", MemberKind.Opening, Rect(400, 0, 300, 300)),        // 200 mm from C1 face at x=200
            Member("O3", MemberKind.Opening, Rect(3000, 3000, 500, 500)),    // fine, inside the slab
            Member("O4", MemberKind.Opening, Rect(20000, 0, 500, 500)),      // outside the slab
        };

        var issues = StructuralChecks.OpeningConflicts(members, Tol, StructuralChecks.DefaultOpeningClearanceMm, CancellationToken.None);

        Assert.Equal(["O1", "C1"], Assert.Single(issues, i => i.Type == StructuralIssueType.OpeningThroughColumn).Handles);
        var near = Assert.Single(issues, i => i.Type == StructuralIssueType.OpeningNearColumn);
        Assert.Equal("O2", near.Handles[0]);
        Assert.Equal(200, near.ValueMm);
        Assert.Equal(["O4"], Assert.Single(issues, i => i.Type == StructuralIssueType.OpeningOutsideHost).Handles);
        Assert.DoesNotContain(issues, i => i.Handles.Contains("O3"));
        Assert.Equal(IssueSeverity.Critical, issues[0].Severity);
        Assert.StartsWith("STR-OPN-", issues[0].IssueId);
    }

    // ---------------------------------------------------------------- tagging + schedule

    [Fact]
    public void Marks_follow_reading_order_keep_existing_marks_and_reserve_their_numbers()
    {
        var members = new[]
        {
            Member("C-BR", MemberKind.Column, Rect(5800, -200, 400, 400)),           // bottom right
            Member("C-TL", MemberKind.Column, Rect(-200, 4800, 400, 400), "C2"),     // top left, already C2
            Member("C-TR", MemberKind.Column, Rect(5800, 4800, 400, 400)),           // top right
            Member("B-1", MemberKind.Beam, PlanShape.Segment(P(200, 0), P(5800, 0)), "OLD"), // a mark with another prefix is kept untouched, never renumbered silently
        };

        var ordered = MemberTagging.Order(members, "row", 250);
        var tags = MemberTagging.Assign(ordered, new Dictionary<string, string>(), 1, 1, overwrite: false);

        // reading order is by position, not by kind: the beam at y = 0 comes before the bottom-right column at x = 6000
        Assert.Equal(["C-TL", "C-TR", "B-1", "C-BR"], ordered.Select(m => m.Handle));
        Assert.Equal(["C2", "C1", "OLD", "C3"], tags.Select(t => t.Mark));
        Assert.Equal([MemberTag.KeptExisting, MemberTag.Assigned, MemberTag.KeptForeign, MemberTag.Assigned], tags.Select(t => t.Outcome));

        var renumbered = MemberTagging.Assign(ordered, new Dictionary<string, string> { ["column"] = "KC" }, 10, 2, overwrite: true);
        Assert.Equal(["KC10", "KC11", "B10", "KC12"], renumbered.Select(t => t.Mark));
        Assert.Throws<ArgumentException>(() => MemberTagging.Order(members, "spiral", 250));
    }

    [Fact]
    public void Schedule_rows_group_by_kind_and_section_with_totals_and_marks()
    {
        var members = new[]
        {
            Member("C1", MemberKind.Column, Rect(0, 0, 400, 400), "C1"),
            Member("C2", MemberKind.Column, Rect(6000, 0, 400, 400), "C2"),
            Member("C3", MemberKind.Column, Rect(0, 5000, 400, 600)),
            Member("B1", MemberKind.Beam, PlanShape.Segment(P(200, 0), P(5800, 0)), "B1"),
            Member("B2", MemberKind.Beam, PlanShape.Segment(P(200, 5000), P(5800, 5000))),
        };

        var rows = MemberTagging.Schedule(members);

        Assert.Equal(3, rows.Count);
        Assert.Equal(("column", "400×400", 2), (rows[0].Kind, rows[0].Section, rows[0].Count));
        Assert.Equal(["C1", "C2"], rows[0].Marks);
        Assert.Equal(("column", "400×600", 1), (rows[1].Kind, rows[1].Section, rows[1].Count));
        Assert.Equal(("beam", "L 5600", 2), (rows[2].Kind, rows[2].Section, rows[2].Count));
        Assert.Equal(11200, rows[2].TotalLengthMm);
    }
}
