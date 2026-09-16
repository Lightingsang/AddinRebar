using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HPAutoCad.Aec.Classification;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Issues;
using HPAutoCad.Aec.Model;
using HPAutoCad.Aec.Structural;
using Xunit;

namespace HPAutoCad.Aec.Tests;

/// <summary>The phase-E review's findings, pinned: envelopes under the cap at the page maxima, grids that survive stubs / block bubbles /
/// corner bubbles, near-duplicate beams that support nothing, tight openings that are not "through", numbers reserved as numbers.</summary>
public sealed class StructuralReviewTests
{
    private static readonly GeometryTolerance Tol = GeometryTolerance.Default;
    private static readonly JsonSerializerOptions Bridge = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, ReferenceHandler = ReferenceHandler.IgnoreCycles, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    private static Pt P(double x, double y) => new(x, y);

    private static AecObject Obj(string handle, string aecType, PlanShape shape, string type = "LWPOLYLINE", string layer = "S-GRID", IReadOnlyDictionary<string, object>? properties = null) => new()
    {
        Handle = handle, Type = type, Layer = layer, AecType = aecType, Confidence = 0.9, BoundsMm = shape.Bounds, Properties = properties ?? new Dictionary<string, object>(),
        Record = new AecEntityRecord { Handle = handle, Type = type, Layer = layer, Shape = shape, BoundsMm = shape.Bounds },
    };

    private static AecEntityRecord Text(string handle, string text, Pt at) =>
        new() { Handle = handle, Type = "TEXT", Layer = "S-GRID", Text = text, PositionMm = at, BoundsMm = Box.Of(at, at + P(200, 100)), Shape = PlanShape.Rectangle(Box.Of(at, at + P(200, 100))) };

    private static PlanShape Rect(double x, double y, double w, double h) => new([P(x, y), P(x + w, y), P(x + w, y + h), P(x, y + h)], true);

    private static PlanShape Circle(Pt c, double r) => new(PlanShape.ArcPoints(c, r, 0, 360, 1, 0), true, 2 * Math.PI * r, Math.PI * r * r, approximate: true);

    private static StructuralMember Member(string handle, string kind, PlanShape shape, string? mark = null, string type = "")
    {
        var aecType = kind switch { MemberKind.Column => AecType.StructuralColumn, MemberKind.Beam => AecType.StructuralBeam, MemberKind.Wall => AecType.StructuralWall, MemberKind.Slab => AecType.StructuralSlab, _ => AecType.StructuralOpening };
        var o = Obj(handle, aecType, shape, type.Length > 0 ? type : shape.Closed ? "LWPOLYLINE" : "LINE", "S-X");
        return StructuralMember.From(o, ShapeMetrics.Of(o.Record))! with { Mark = mark };
    }

    // ---------------------------------------------------------------- grids

    [Fact]
    public void A_bubble_stub_merges_into_its_grid_line_a_block_bubble_labels_by_attribute_and_a_corner_bubble_is_not_taken()
    {
        var objects = new[]
        {
            Obj("GA", AecType.StructuralGrid, PlanShape.Segment(P(0, 0), P(7500, 0)), "LINE"),
            Obj("GAS", AecType.StructuralGrid, PlanShape.Segment(P(-1500, 0), P(0, 0)), "LINE"),                 // the stub carrying bubble A
            Obj("GB", AecType.StructuralGrid, PlanShape.Segment(P(-1500, 5010), P(7500, 5010)), "LINE"),
            Obj("G1", AecType.StructuralGrid, PlanShape.Segment(P(0, -1500), P(0, 6500)), "LINE"),
            Obj("G2", AecType.StructuralGrid, PlanShape.Segment(P(6000, -1500), P(6000, 6500)), "LINE"),
            Obj("BA", AecType.StructuralGrid, Circle(P(-1900, 0), 400), "CIRCLE"),
            Obj("BB", AecType.StructuralGrid, Rect(-2300, 4610, 800, 800), "INSERT", properties: new Dictionary<string, object> { ["attributes"] = new Dictionary<string, string> { ["GRID"] = "B" } }),
            Obj("B1", AecType.StructuralGrid, Circle(P(0, 6900), 400), "CIRCLE"),
            Obj("B2", AecType.StructuralGrid, Circle(P(6000, -1900), 400), "CIRCLE"),                                // at the bottom of grid 2: 2 421 mm from A's end, not on A's axis
        };
        var texts = new AecClassifier.TextIndex([Text("TA", "A", P(-1950, -50)), Text("T1", "1", P(-50, 6850)), Text("T2", "2", P(5950, -1950))]);
        var unbounded = new AecObject { Handle = "GX2", Type = "XLINE", Layer = "S-GRID", AecType = AecType.StructuralGrid, Record = new AecEntityRecord { Handle = "GX2", Type = "XLINE", Layer = "S-GRID" } };

        var grids = GridDetector.Detect([.. objects, unbounded], texts, Tol);

        Assert.Equal(4, grids.Lines.Count);
        var a = grids.Lines.Single(l => l.Handle == "GA");
        Assert.Equal(2, a.MergedSegments);
        Assert.Equal(P(-1500, 0), a.StartMm);
        Assert.Equal("A", a.Label);
        Assert.Equal("B", grids.Lines.Single(l => l.Handle == "GB").Label);
        Assert.Equal("2", grids.Lines.Single(l => l.Handle == "G2").Label);
        Assert.Equal(4, grids.Intersections.Count);
        Assert.Equal([5010.0], grids.SpacingMm[GridLine.Horizontal]);
        Assert.Equal(1, grids.UnboundedCount);
    }

    [Fact]
    public void A_jogged_grid_polyline_follows_its_longest_segment_and_ids_are_stable_on_reversed_input()
    {
        var objects = new[]
        {
            Obj("GJ", AecType.StructuralGrid, new PlanShape([P(0, 0), P(6000, 0), P(6000, 800)], false), "LWPOLYLINE"),
            Obj("G1", AecType.StructuralGrid, PlanShape.Segment(P(3000, -1500), P(3000, 3000)), "LINE"),
        };

        var grids = GridDetector.Detect(objects, AecClassifier.TextIndex.Empty, Tol);
        var reversed = GridDetector.Detect(objects.Reverse().ToArray(), AecClassifier.TextIndex.Empty, Tol);

        var j = grids.Lines.Single(l => l.Handle == "GJ");
        Assert.Equal(GridLine.Horizontal, j.Direction);
        Assert.Equal(6000, j.LengthMm);
        Assert.Equal(grids.Lines.Select(l => l.Handle), reversed.Lines.Select(l => l.Handle));
        Assert.Equal(grids.Intersections.Select(i => i.PointMm), reversed.Intersections.Select(i => i.PointMm));
    }

    // ---------------------------------------------------------------- checks

    [Fact]
    public void A_beam_copied_2_mm_beside_the_first_supports_nothing_and_no_grid_means_no_alignment_issues()
    {
        var members = new[]
        {
            Member("BL", MemberKind.Beam, PlanShape.Segment(P(0, 0), P(6000, 0))),
            Member("BL2", MemberKind.Beam, PlanShape.Segment(P(0, 2), P(6000, 2))),
            Member("C1", MemberKind.Column, Rect(-200, -200, 400, 400)),
        };

        var issues = StructuralChecks.Connectivity(members, Tol, CancellationToken.None);

        Assert.Equal(2, issues.Count(i => i.Type == StructuralIssueType.UnsupportedEnd)); // the far ends of both beams: the other beam is not a support
        Assert.DoesNotContain(issues, i => i.Type == StructuralIssueType.BeamWithoutSupports);
        Assert.Empty(StructuralChecks.ColumnAlignment(members, new GridSystem([], [], new Dictionary<string, IReadOnlyList<double>>()), 25, StructuralChecks.GridSearchRadiusMm, CancellationToken.None));
    }

    [Fact]
    public void An_opening_sharing_a_column_face_or_corner_is_zero_clearance_not_through()
    {
        var members = new[]
        {
            Member("C1", MemberKind.Column, Rect(0, 0, 400, 400)),
            Member("OF", MemberKind.Opening, Rect(400, 0, 600, 400)),      // shares the east face
            Member("OC", MemberKind.Opening, Rect(400, 400, 500, 500)),    // touches the corner only
            Member("OT", MemberKind.Opening, Rect(300, 300, 500, 500)),    // really cuts in
        };

        var issues = StructuralChecks.OpeningConflicts(members, Tol, StructuralChecks.DefaultOpeningClearanceMm, CancellationToken.None);

        Assert.Equal(["OT", "C1"], Assert.Single(issues, i => i.Type == StructuralIssueType.OpeningThroughColumn).Handles);
        var tight = issues.Where(i => i.Type == StructuralIssueType.OpeningNearColumn).ToArray();
        Assert.Equal(["OC", "OF"], tight.Select(i => i.Handles[0]).OrderBy(x => x));
        Assert.All(tight, i => Assert.Equal(0, i.ValueMm));
    }

    // ---------------------------------------------------------------- members

    [Fact]
    public void A_block_members_MARK_attribute_is_its_mark_a_round_column_reads_diameter_and_a_closed_beam_reads_length()
    {
        var block = Obj("BLK", AecType.StructuralColumn, PlanShape.Rectangle(Box.Of(P(0, 0), P(400, 400))), "INSERT", "S-COL",
            new Dictionary<string, object> { ["attributes"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["mark"] = " C7 " } });
        var column = StructuralMember.From(block, ShapeMetrics.Of(block.Record))!;
        var round = Member("RC", MemberKind.Column, Circle(P(0, 0), 200), type: "CIRCLE");
        var outline = Member("BO", MemberKind.Beam, Rect(0, 0, 6000, 300));

        Assert.Equal("C7", column.Mark);
        Assert.Equal("BLK", column.MarkHandle);
        Assert.Equal(StructuralMember.MarkFromAttribute, column.MarkSource);
        Assert.Equal("Ø400", round.Section);
        Assert.Equal("L 6000", outline.Section);
        Assert.Equal(300, outline.WidthMm);
    }

    // ---------------------------------------------------------------- tagging

    [Fact]
    public void Numbers_are_reserved_as_numbers_foreign_marks_are_kept_and_duplicates_are_reported()
    {
        var members = new[]
        {
            Member("A", MemberKind.Column, Rect(0, 0, 400, 400)),
            Member("B", MemberKind.Column, Rect(1000, 0, 400, 400), "C01"),     // reserves 1 whatever the padding
            Member("C", MemberKind.Column, Rect(2000, 0, 400, 400), "C-12"),    // hyphenated, same prefix
            Member("D", MemberKind.Column, Rect(3000, 0, 400, 400), "C-12"),    // duplicate
            Member("E", MemberKind.Beam, PlanShape.Segment(P(0, 1000), P(6000, 1000)), "D1"), // Vietnamese beam mark under prefix B
        };

        var tags = MemberTagging.Assign(MemberTagging.Order(members, "row"), new Dictionary<string, string>(), 1, 1, overwrite: false);

        Assert.Equal(["C2", "C01", "C-12", "C-12"], tags.Where(t => t.Member.Kind == MemberKind.Column).OrderBy(t => t.Member.Handle, StringComparer.Ordinal).Select(t => t.Mark));
        Assert.Equal(MemberTag.KeptForeign, tags.Single(t => t.Member.Handle == "E").Outcome);
        Assert.Equal("D1", tags.Single(t => t.Member.Handle == "E").Mark);
        Assert.Equal(["C-12 ×2"], MemberTagging.DuplicateExisting(members));
        var padded = MemberTagging.Assign(MemberTagging.Order([members[0], Member("F", MemberKind.Column, Rect(5000, 0, 400, 400), "C2")], "row"), new Dictionary<string, string>(), 1, 2, overwrite: false);
        Assert.Equal("C01", padded.Single(t => t.Member.Handle == "A").Mark); // C2 reserves 2, so 1 is free and padded
    }

    [Fact]
    public void A_row_whose_centres_straddle_a_band_boundary_stays_one_row_left_to_right()
    {
        var members = new[]
        {
            Member("HI", MemberKind.Column, Rect(4800, -74, 400, 400)),   // centre y 126
            Member("FAR", MemberKind.Column, Rect(9800, -76, 400, 400)),  // centre y 124
            Member("LO", MemberKind.Column, Rect(-200, -76, 400, 400)),   // centre y 124
            Member("UP", MemberKind.Column, Rect(-200, 4800, 400, 400)),  // next row
        };

        var ordered = MemberTagging.Order(members, "row");

        Assert.Equal(["UP", "LO", "HI", "FAR"], ordered.Select(m => m.Handle));
    }

    // ---------------------------------------------------------------- envelope sizes at the page maxima

    [Fact]
    public void A_full_page_of_grid_lines_with_the_listed_intersections_stays_under_the_result_cap()
    {
        var lines = Enumerable.Range(0, AecTools.MaxGridLimit).Select(i => new GridLine($"{0x2A00 + i:X}", $"AA{i}", i % 2 == 0 ? GridLine.Horizontal : GridLine.Vertical, new Pt(-123456.7, 98765.4), new Pt(123456.7, 98765.4), 246913.4, 179.999, $"{0x3A00 + i:X}", 2)).ToArray();
        var intersections = Enumerable.Range(0, AecTools.MaxIntersectionsListed).Select(i => new GridIntersection($"AA{i}", $"BB{i}", new Pt(123456.7, 98765.4), $"{0x2A00 + i:X}", $"{0x2B00 + i:X}")).ToArray();
        var result = new AnalysisResult<GridLine> { Items = lines, Count = 5000, Truncated = true, Summary = new
        {
            lines = 5000, labeled = 5000, merged = 4000, unbounded = 3, byDirection = new { horizontal = 2500, vertical = 2500 },
            labels = lines.Select(l => l.Label).ToArray(), spacingMm = new { horizontal = Enumerable.Repeat(5010.5, 200).ToArray(), vertical = Enumerable.Repeat(6000.5, 200).ToArray() },
            intersectionCount = 250000, intersections, intersectionsTruncated = true, bubbleReachMm = 2500.0,
        } };

        Assert.True(Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(result, Bridge)) < 60_000);
    }

    [Fact]
    public void A_full_page_of_members_and_a_full_tagging_summary_stay_under_the_result_cap()
    {
        var members = Enumerable.Range(0, AecTools.MaxTagMembers).Select(i =>
            Member($"{0x2A00 + i:X}", MemberKind.Beam, PlanShape.Segment(P(-123456.7, 98765.4), P(123456.7, 98765.4)), $"KC-{i:0000}") with { Layer = "S-BEAM-CONCRETE-LEVEL-03-SUPPLEMENTARY", MarkHandle = $"{0x3A00 + i:X}", MarkSource = StructuralMember.MarkFromText }).ToArray();
        var page = new AnalysisResult<Dictionary<string, object?>> { Items = members.Take(AecTools.MaxMemberLimit).Select(Cad.StructuralService.Describe).ToArray(), Count = 5000, Truncated = true, Summary = new
        {
            examined = 100000, members = 5000, byKind = new { column = 1000, beam = 4000 }, sections = Enumerable.Range(0, 20).Select(i => new { kind = "beam", section = $"L {i * 1000}", count = 200 }).ToArray(),
            marked = 5000, duplicateExisting = Enumerable.Range(0, 50).Select(i => $"KC-{i:0000} ×2").ToArray(), gridLines = 40, ruleSet = new { name = "user", source = "rules\\aec-classification.json", version = 2 },
        } };
        var tags = members.Select(m => new MemberTag(m, m.Mark, m.Mark, MemberTag.Overwritten)).ToArray();
        var edit = new EditResult { Items = tags.Select((t, i) => new ItemOutcome(i, true, t.Member.MarkHandle, "TEXT", ["mark:" + t.Mark, "was:" + t.ExistingMark, "for:" + t.Member.Handle])).ToArray(), Summary = new
        {
            requested = tags.Length, assigned = 0, overwritten = tags.Length, keptExisting = 0, keptForeign = 0, byKind = new { beam = tags.Length }, layer = "S-ANNO-TEXT-SUPPLEMENTARY", layerCreated = true,
            marks = tags.Select(t => new { handle = t.Member.Handle, kind = t.Member.Kind, mark = t.Mark, outcome = t.Outcome, existing = t.ExistingMark, markHandle = t.Member.MarkHandle }).ToArray(),
            written = tags.Select(t => new { handle = t.Member.Handle, mark = t.Mark, via = "text", textHandle = t.Member.MarkHandle, was = t.ExistingMark }).ToArray(),
        } };
        foreach (var t in tags) edit.Modified(t.Member.MarkHandle!);
        for (var i = 0; i < 50; i++) edit.Warn($"existing mark KC-{i:0000} ×2 is carried by several members; kept as they are — renumber with overwrite: true.");

        Assert.True(Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(page, Bridge)) < 60_000, "members page");
        Assert.True(Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(edit, Bridge)) < 60_000, "tag envelope");
    }
}
