using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HPAutoCad.Aec.Architecture;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Issues;
using HPAutoCad.Aec.Model;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

namespace HPAutoCad.Aec.Tests;

/// <summary>Rooms from walls (loops, gaps, T-junctions, overshoots, double lines, islands), labels, boundary issues, the area schedule, the dimension rules, and the envelope at the cap.</summary>
public sealed class ArchitectureTests
{
    private static readonly GeometryTolerance Tol = GeometryTolerance.Default;
    private static readonly JsonSerializerOptions Bridge = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, ReferenceHandler = ReferenceHandler.IgnoreCycles, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    private static Pt P(double x, double y) => new(x, y);

    private static WallSegment W(string handle, double x1, double y1, double x2, double y2) => new(new Seg(P(x1, y1), P(x2, y2)), handle);

    private static IEnumerable<WallSegment> Rect(string prefix, double x, double y, double w, double h) =>
    [
        W(prefix + "S", x, y, x + w, y), W(prefix + "E", x + w, y, x + w, y + h), W(prefix + "N", x + w, y + h, x, y + h), W(prefix + "W", x, y + h, x, y),
    ];

    private static LoopOutcome Find(IEnumerable<WallSegment> walls, double gapMm = 25) =>
        RoomLoopFinder.Find(walls.ToArray(), Tol, LoopSettings.Default(Tol) with { GapMm = gapMm }, CancellationToken.None);

    private static Room RoomOf(RoomLoop loop, int n, params (string Handle, string Text, Pt At)[] texts)
    {
        var outline = new PlanShape(loop.Ring, true, loop.PerimeterMm, loop.AreaMm2);
        var room = new Room($"R-{n:000}", Room.FromWalls, loop.Handles, outline, loop.AreaMm2, loop.PerimeterMm, outline.Centroid, Room.InsidePoint(outline, Tol));
        return RoomLabels.Apply(room, texts.Select(t => new RoomLabels.Label(t.Handle, t.Text, t.At)).ToArray(), RoomLabelRules.Default);
    }

    private static ScriptArgs Args(object o) => new(JsonSerializer.SerializeToElement(o));

    [Fact]
    public void Four_walls_are_one_room_and_two_rooms_share_a_wall_whatever_the_T_junctions_and_overshoots()
    {
        var one = Find(Rect("A", 0, 0, 4000, 3000));
        Assert.Single(one.Loops);
        Assert.Equal(12_000_000, one.Loops[0].AreaMm2, 1);
        Assert.Equal(4, one.Loops[0].Ring.Count);
        Assert.Equal(["AE", "AN", "AS", "AW"], one.Loops[0].Handles);

        // an 8 × 3 m box with a middle wall that overshoots the bottom wall by 300 mm and stops on the top wall (T-junction)
        var two = Find([.. Rect("B", 0, 0, 8000, 3000), W("MID", 4000, -300, 4000, 3000)]);
        Assert.Equal(2, two.Loops.Count);
        Assert.All(two.Loops, l => Assert.Equal(12_000_000, l.AreaMm2, 1));
        Assert.Empty(two.OpenEnds);
        Assert.Equal(1, two.Overshoots);
        Assert.Contains(two.Loops, l => l.Handles.Contains("MID") && l.Handles.Contains("BW"));
    }

    [Fact]
    public void A_small_gap_is_closed_and_reported_a_wide_gap_leaves_the_room_open_with_both_ends_located()
    {
        var closed = Find([W("S", 0, 0, 4000, 0), W("E", 4000, 0, 4000, 3000), W("N", 4000, 3000, 0, 3000), W("W", 0, 3000, 0, 20)]); // W stops 20 mm short of S
        Assert.Single(closed.Loops);
        var gap = Assert.Single(closed.ClosedGaps);
        Assert.Equal(20.0, gap.GapMm);
        Assert.Equal(new HashSet<string> { "W", "S" }, new HashSet<string> { gap.Handle, gap.OtherHandle }); // the corner closes as a corner: one end moves onto the other's endpoint
        Assert.True(gap.ToMm == P(0, 0) || gap.ToMm == P(0, 20));

        var open = Find([W("S", 0, 0, 4000, 0), W("E", 4000, 0, 4000, 3000), W("N", 4000, 3000, 0, 3000), W("W", 0, 3000, 0, 200)]);
        Assert.Empty(open.Loops);
        Assert.Equal(2, open.OpenEnds.Count); // the chain S-E-N-W came loose: both tips are open ends
        var tip = Assert.Single(open.OpenEnds, e => e.Handle == "W");
        Assert.Equal(P(0, 200), tip.PointMm);
        Assert.Equal(("S", 200.0), (tip.NearestHandle, tip.NearestGapMm));
        Assert.Equal(0, open.Overshoots);
    }

    [Fact]
    public void An_L_room_gets_an_inside_label_point_an_island_is_not_a_room_and_double_line_walls_yield_one_room()
    {
        var l = Find([W("1", 0, 0, 6000, 0), W("2", 6000, 0, 6000, 2000), W("3", 6000, 2000, 2000, 2000), W("4", 2000, 2000, 2000, 6000), W("5", 2000, 6000, 0, 6000), W("6", 0, 6000, 0, 0)]);
        var room = Assert.Single(l.Loops);
        Assert.Equal(6000 * 2000 + 2000 * 4000, room.AreaMm2, 1);
        var outline = new PlanShape(room.Ring, true);
        var inside = Room.InsidePoint(outline, Tol);
        Assert.True(outline.ContainsPointXY(inside, Tol.PointEquality) && outline.DistanceToBoundaryXY(inside) > 400);

        var island = Find([.. Rect("R", 0, 0, 5000, 4000), .. Rect("C", 2000, 1500, 400, 400)]);
        Assert.Single(island.Loops);
        Assert.Equal(20_000_000, island.Loops[0].AreaMm2, 1); // the column face is tiny and dropped; its area is not subtracted (documented)
        Assert.Equal(1, island.TinyFaces);

        var doubled = Find([.. Rect("O", -200, -200, 5400, 4400), .. Rect("I", 0, 0, 5000, 4000)]);
        Assert.Single(doubled.Loops);
        Assert.Equal(20_000_000, doubled.Loops[0].AreaMm2, 1);
        Assert.Equal(1, doubled.Nested); // the outer line encloses the room: not a room itself

        // a 12 × 8 m hall with a free-standing 2 × 2 m shaft: both are rooms, the hall keeps its gross area; a closet hung off the south wall by one partition too
        var hall = Find([.. Rect("H", 0, 0, 12000, 8000), .. Rect("S", 5000, 3000, 2000, 2000)]);
        Assert.Equal(2, hall.Loops.Count);
        Assert.Equal(0, hall.Nested);
        Assert.Contains(hall.Loops, l => Math.Abs(l.AreaMm2 - 96_000_000) < 1);
        var closet = Find([.. Rect("R", 0, 0, 6000, 4000), W("P1", 1500, 0, 1500, 1500), W("P2", 1500, 1500, 3000, 1500), W("P3", 3000, 1500, 3000, 0)]);
        Assert.Equal(2, closet.Loops.Count);
        Assert.Contains(closet.Loops, l => Math.Abs(l.AreaMm2 - (24_000_000 - 2_250_000)) < 1); // the room's face wraps around the closet: net
        Assert.Contains(closet.Loops, l => Math.Abs(l.AreaMm2 - 2_250_000) < 1);
    }

    [Fact]
    public void Doorways_are_bridged_so_rooms_stay_apart_single_line_walls_and_jambs_of_double_line_walls()
    {
        // two 4 × 3 m rooms, a 900 mm door in the party wall and another in the outer wall of the left room
        var single = Find([W("S1", 0, 0, 1500, 0), W("S2", 2400, 0, 8000, 0), W("E", 8000, 0, 8000, 3000), W("N", 8000, 3000, 0, 3000), W("W", 0, 3000, 0, 0), W("M1", 4000, 0, 4000, 1000), W("M2", 4000, 1900, 4000, 3000)]);
        Assert.Equal(2, single.Loops.Count);
        Assert.All(single.Loops, l => Assert.Equal(12_000_000, l.AreaMm2, 1));
        Assert.Equal(2, single.Openings.Count);
        Assert.Contains(single.Openings, o => o.WidthMm == 900 && new[] { o.Handle, o.OtherHandle }.Order().SequenceEqual(new[] { "M1", "M2" }));
        Assert.Empty(single.OpenEnds);

        // the same two rooms with 200 mm double-line walls and jamb lines at the party-wall door: 2 rooms, the doorway cell and the cavities dropped
        WallSegment[] doubled =
        [
            .. Rect("O", -200, -200, 8400, 3400), .. Rect("I1", 0, 0, 3900, 3000), .. Rect("I2", 4100, 0, 3900, 3000),
        ];
        // I1's east line and I2's west line are the party wall's two lines; cut a 900 door into both and add the jambs
        var walls = doubled.Where(w => w.Handle is not ("I1E" or "I2W")).ToList();
        walls.AddRange([W("I1E1", 3900, 0, 3900, 1000), W("I1E2", 3900, 1900, 3900, 3000), W("I2W1", 4100, 0, 4100, 1000), W("I2W2", 4100, 1900, 4100, 3000), W("J1", 3900, 1000, 4100, 1000), W("J2", 3900, 1900, 4100, 1900)]);
        var two = Find(walls);
        Assert.Equal(2, two.Loops.Count);
        Assert.All(two.Loops, l => Assert.Equal(3900 * 3000, l.AreaMm2, 1));
        Assert.Single(two.Openings, o => o.WidthMm == 900);
        Assert.Empty(two.OpenEnds);

        // a 400 mm gap between facing ends is a gap, not a doorway — reported with the other wall when maxGapMm reaches it
        WallSegment[] gapped = [W("S1", 0, 0, 3800, 0), W("S2", 4200, 0, 8000, 0), W("E", 8000, 0, 8000, 3000), W("N", 8000, 3000, 0, 3000), W("W", 0, 3000, 0, 0)];
        var gap = RoomLoopFinder.Find(gapped, Tol, LoopSettings.Default(Tol) with { MaxGapMm = 500 }, CancellationToken.None);
        Assert.Empty(gap.Openings);
        Assert.Equal(2, gap.OpenEnds.Count);
        Assert.All(gap.OpenEnds, e => Assert.Equal(400, e.NearestGapMm));
        Assert.All(Find(gapped).OpenEnds, e => Assert.Null(e.NearestGapMm)); // beyond the default 300 mm reach it is an open boundary
    }

    [Fact]
    public void An_overshoot_is_the_tail_of_a_wall_past_its_crossing_at_any_angle_and_a_stub_missing_its_wall_is_a_gap()
    {
        var diagonal = Find([.. Rect("R", 0, 0, 6000, 4000), W("Z", -300, -300, 4300, 4300)]); // a 45° wall crossing the room, 424 mm past each wall
        Assert.Equal(2, diagonal.Loops.Count);
        Assert.Equal(2, diagonal.Overshoots);
        Assert.Empty(diagonal.OpenEnds);
        var tail = Find([.. Rect("R", 0, 0, 6000, 4000), W("T", 3000, 0, 3000, 6000)]); // a partition running 2 m past the north wall is a real wall end, not an overshoot
        Assert.Single(tail.OpenEnds, e => e.Handle == "T" && e.PointMm == P(3000, 6000));
        Assert.Equal(0, tail.Overshoots);

        var stub = Find([.. Rect("R", 0, 0, 6000, 4000), W("K", 3000, 4000, 3000, 3850), W("P", 0, 3650, 6000, 3650)]); // a 150 mm stub off the north wall missing the partition below it by 200
        var tip = Assert.Single(stub.OpenEnds);
        Assert.Equal(("K", "P", 200.0), (tip.Handle, tip.NearestHandle, tip.NearestGapMm));
        Assert.Equal(0, stub.Overshoots);
    }

    [Fact]
    public void Collinear_overlapping_walls_and_a_crossing_become_nodes_and_ids_are_stable_on_reversed_input()
    {
        WallSegment[] walls = [W("S1", 0, 0, 5000, 0), W("S2", 3000, 0, 8000, 0), W("E", 8000, 0, 8000, 3000), W("N", 8000, 3000, 0, 3000), W("W", 0, 3000, 0, 0), W("X", 4000, -500, 4000, 3500)];
        var a = Find(walls);
        var b = Find(walls.Reverse().ToArray());

        Assert.Equal(2, a.Loops.Count);
        Assert.Equal(2, a.Overshoots);
        Assert.Equal(a.Loops.Select(l => Box.Of(l.Ring).Min), b.Loops.Select(l => Box.Of(l.Ring).Min));
        Assert.Equal(a.Loops.Select(l => l.AreaMm2), b.Loops.Select(l => l.AreaMm2));
    }

    [Fact]
    public void Labels_inside_a_room_become_number_name_and_department_by_the_callers_patterns()
    {
        var loop = Find(Rect("A", 0, 0, 4000, 3000)).Loops[0];
        var room = RoomOf(loop, 1, ("T0", "B01", P(2000, 1600)), ("T1", "101", P(2000, 1800)), ("T2", "PHONG KHACH", P(2000, 1500)), ("T3", "12.5 m2", P(2000, 1200)), ("T4", "OUTSIDE", P(9000, 9000)), ("T5", "C1", P(2100, 1500)));

        Assert.Equal(("101", "PHONG KHACH", null), (room.Number, room.Name, room.Department)); // a structural mark beside the number is never the number
        Assert.Equal(["T0", "T1", "T2", "T3", "T4", "T5"], room.TextHandles); // every text handed over is recorded; the caller filtered by position
        var tagged = RoomLabels.Apply(room, [new("M1", "OFFICE\\P24.5 m²", P(2000, 1500)), new("M2", "A-101\n2nd floor", P(2000, 1400))], RoomLabelRules.Default);
        Assert.Equal(("A-101", "2nd floor"), (tagged.Number, tagged.Name)); // MTEXT lines are separate labels; the tool's own area line is ignored; the longest remaining line names the room
        foreach (var ok in new[] { "101", "A-101", "P.101", "1.01", "P 101", "01.02" }) Assert.Matches(RoomLabelRules.DefaultNumberPattern, ok);
        foreach (var no in new[] { "B01", "C1", "D01", "W1", "KT-12", "1" }) Assert.DoesNotMatch(RoomLabelRules.DefaultNumberPattern, no);

        var rules = RoomLabelRules.From(Args(new { numberPattern = "^P-\\d+$", departmentPattern = "^DEPT:\\s*(\\w+)$" }));
        var custom = RoomLabels.Apply(room, [new("T5", "P-7", P(1, 1)), new("T6", "DEPT: SALES", P(1, 2)), new("T7", "MEETING", P(1, 3))], rules);
        Assert.Equal(("P-7", "MEETING", "SALES"), (custom.Number, custom.Name, custom.Department));
        Assert.Throws<ArgumentException>(() => RoomLabelRules.From(Args(new { numberPattern = "(" })));
    }

    [Fact]
    public void Boundary_check_reports_closed_gaps_open_ends_duplicates_and_unlabelled_rooms_in_severity_order()
    {
        var loops = Find([W("S", 0, 0, 4000, 0), W("E", 4000, 0, 4000, 3000), W("N", 4000, 3000, 0, 3000), W("W", 0, 3000, 0, 20), W("F", 9000, 0, 12000, 0)]);
        var room = RoomOf(loops.Loops[0], 1);
        var outline = new PlanShape([P(20000, 0), P(24000, 0), P(24000, 3000), P(20000, 3000)], true);
        var dupA = new Room("R-002", Room.FromOutline, ["OA"], outline, outline.AreaMm2, outline.LengthMm, outline.Centroid, outline.Centroid) { Name = "HALL" };
        var dupB = dupA with { Id = "R-003", Handles = ["OB"] };

        var issues = RoomBoundaryChecks.Check(loops, [room, dupA, dupB], Tol, RoomLoopFinder.DefaultMaxGapMm, CancellationToken.None);

        Assert.Equal(2, issues.Count(i => i.Type == RoomIssueType.OpenBoundary)); // the free wall F, both ends
        Assert.Single(issues, i => i.Type == RoomIssueType.BoundaryGapClosed && i.ValueMm == 20 && i.Handles.Order().SequenceEqual(["S", "W"]));
        Assert.Single(issues, i => i.Type == RoomIssueType.DuplicateRoom && i.Handles.SequenceEqual(["OA", "OB"]));
        Assert.Single(issues, i => i.Type == RoomIssueType.UnlabelledRoom && i.Handles.Contains("AS") == false && i.Description.Contains("R-001"));
        Assert.Equal(IssueSeverity.Critical, issues[0].Severity);
        Assert.Equal("ARC-001", issues[0].IssueId);
    }

    [Fact]
    public void Tiled_outlines_are_not_overlaps_a_wide_gap_is_one_issue_and_an_outline_inside_another_is_info()
    {
        Room Outline(string id, string handle, double x, double y, double w, double h)
        {
            var shape = new PlanShape([P(x, y), P(x + w, y), P(x + w, y + h), P(x, y + h)], true);
            return new Room(id, Room.FromOutline, [handle], shape, shape.AreaMm2, shape.LengthMm, shape.Centroid, shape.Centroid) { Name = id };
        }

        var tiled = new[] { Outline("R-001", "A", 0, 0, 4000, 3000), Outline("R-002", "B", 4000, 0, 4000, 3000), Outline("R-003", "C", 0, 3000, 4000, 3000), Outline("R-004", "D", 4000, 3000, 4000, 3000) };
        var overlapping = new[] { tiled[0], Outline("R-005", "E", 3000, 1000, 4000, 3000) };
        var inside = new[] { Outline("R-006", "F", 0, 0, 12000, 8000), Outline("R-007", "G", 2000, 2000, 2000, 2000) };
        var empty = new LoopOutcome([], [], [], [], 0, 0, 0, 0, 0, 0);

        Assert.DoesNotContain(RoomBoundaryChecks.Check(empty, tiled, Tol, RoomLoopFinder.DefaultMaxGapMm, CancellationToken.None), i => i.Type == RoomIssueType.RoomOverlap);
        Assert.Single(RoomBoundaryChecks.Check(empty, overlapping, Tol, RoomLoopFinder.DefaultMaxGapMm, CancellationToken.None), i => i.Type == RoomIssueType.RoomOverlap);
        Assert.Single(RoomBoundaryChecks.Check(empty, inside, Tol, RoomLoopFinder.DefaultMaxGapMm, CancellationToken.None), i => i.Type == RoomIssueType.RoomInsideRoom && i.Severity == IssueSeverity.Info);

        var gapLoops = Find([W("S", 0, 0, 4000, 0), W("E", 4000, 0, 4000, 3000), W("N", 4000, 3000, 0, 3000), W("W", 0, 3000, 0, 200)]);
        var gapIssues = RoomBoundaryChecks.Check(gapLoops, [], Tol, RoomLoopFinder.DefaultMaxGapMm, CancellationToken.None);
        var gap = Assert.Single(gapIssues, i => i.Type == RoomIssueType.BoundaryGap);
        Assert.Equal(200, gap.ValueMm);
        Assert.Equal(P(0, 100), gap.LocationMm);
        Assert.DoesNotContain(gapIssues, i => i.Type == RoomIssueType.OpenBoundary);
    }

    [Fact]
    public void Area_schedule_groups_by_department_with_percentages_and_refuses_an_unknown_grouping()
    {
        var a = RoomOf(Find(Rect("A", 0, 0, 4000, 3000)).Loops[0], 1, ("T1", "101", P(2000, 1500)), ("T2", "OFFICE", P(2000, 1200)));
        var b = RoomOf(Find(Rect("B", 0, 0, 8000, 3000)).Loops[0], 2, ("T3", "102", P(4000, 1500)));
        a = a with { Department = "SALES" };
        b = b with { Department = "SALES" };
        var c = RoomOf(Find(Rect("C", 0, 0, 2000, 2000)).Loops[0], 3);

        var rows = AreaSchedule.Build([a, b, c], "department");

        Assert.Equal(2, rows.Count);
        Assert.Equal(("SALES", 2, 36.0, 90.0), (rows[0].Group, rows[0].Count, rows[0].AreaM2, rows[0].Percent));
        Assert.Equal(["101 OFFICE", "102"], rows[0].Rooms);
        Assert.Equal((AreaSchedule.Ungrouped, 1, 4.0, 10.0), (rows[1].Group, rows[1].Count, rows[1].AreaM2, rows[1].Percent));
        Assert.Equal(3, AreaSchedule.Build([a, b, c], "room").Count);
        Assert.Throws<ArgumentException>(() => AreaSchedule.Build([a], "colour"));
    }

    [Fact]
    public void The_overall_rule_plans_extents_on_the_requested_sides_and_unknown_rules_or_sides_are_the_callers_error()
    {
        var subject = new DimensionSubject("R-001", new PlanShape([P(1000, 1000), P(5000, 1000), P(5000, 4000), P(1000, 4000)], true), ["A"]);

        var plans = AutoDimensionRules.Plan([Args(new { rule = "overall", offsetMm = 600, sides = new[] { "bottom", "right" } })], [subject], Tol, CancellationToken.None);

        Assert.Equal(2, plans.Count);
        Assert.Equal((P(1000, 1000), P(5000, 1000), P(3000, 400), 4000.0), (plans[0].P1Mm, plans[0].P2Mm, plans[0].DimLineMm, plans[0].MeasurementMm));
        Assert.Equal(("right", P(5600, 2500), 3000.0), (plans[1].Side, plans[1].DimLineMm, plans[1].MeasurementMm));
        Assert.Equal(["overall"], AutoDimensionRules.Known);
        Assert.Throws<ArgumentException>(() => AutoDimensionRules.Plan([Args(new { rule = "chain" })], [subject], Tol, CancellationToken.None));
        Assert.Throws<ArgumentException>(() => AutoDimensionRules.Plan([Args(new { rule = "overall", sides = new[] { "north" } })], [subject], Tol, CancellationToken.None));
        Assert.Throws<ArgumentException>(() => AutoDimensionRules.Plan([], [subject], Tol, CancellationToken.None));
    }

    [Fact]
    public void A_full_page_of_rooms_with_outlines_stays_under_the_result_cap()
    {
        var ring = Enumerable.Range(0, AecTools.MaxOutlineVertices + 8).Select(i => new Pt(123456.7 + i * 1000, 98765.4 + (i % 2) * 1000)).ToArray();
        var outline = new PlanShape(ring, true);
        // the unfriendly case: rooms drawn from many wall pieces with many labels — the handle lists are capped, the counts stay exact
        var rooms = Enumerable.Range(0, AecTools.MaxRoomLimit).Select(i => new Room($"R-{i + 1:000}", Room.FromWalls, Enumerable.Range(0, 60).Select(k => $"{0x2A00 + i * 60 + k:X}").ToArray(), outline, 123456789.1, 123456.7, ring[0], ring[1])
            { Name = "PHONG LAM VIEC TRUONG PHONG KINH DOANH", Number = "A-1204", Department = "KINH DOANH QUOC TE", TextHandles = Enumerable.Range(0, 200).Select(k => $"{0x3A00 + i * 200 + k:X}").ToArray() }).ToArray();
        var result = new AnalysisResult<Dictionary<string, object?>> { Items = rooms.Select(r => r.Describe(AecTools.MaxOutlineVertices)).ToArray(), Count = 5000, Truncated = true, Summary = new
        {
            examined = 100000, walls = 5000, wallSegments = 20000, outlines = 200, zones = 10, rooms = 5000, fromWalls = 4800, fromOutlines = 200, loopsReplacedByOutlines = 150, totalAreaM2 = 123456.78, labelled = 4000,
            openEnds = 300, closedGaps = 200, openings = 400, overshoots = 100, cavities = 4000, tinyFaces = 800, nested = 50, detection = LoopSettings.Default(Tol), ruleSet = new { name = "user", source = "rules\\aec-classification.json" },
        } };
        result.Warnings.Add("limit is capped at 30 per page; page with offset.");
        Assert.True(Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(result, Bridge)) < 60_000, "rooms page");

        // area rows at the cap with 20 long labels each; dimension plans at the cap with 7-digit coordinates (preview lists them); 120 tags after the write
        var rows = Enumerable.Range(0, AecTools.MaxAreaRowLimit).Select(i => new AreaRow($"PHONG KINH DOANH QUOC TE {i:00}", 20, 123456789.1, 123456.79, 12.3, Enumerable.Range(0, 20).Select(k => $"A-{k:000} PHONG LAM VIEC TRUONG PHONG").ToArray())).ToArray();
        var schedule = new AnalysisResult<AreaRow> { Items = rows, Count = 500, Truncated = true, Summary = new { rooms = 5000, groups = 500, groupBy = "department", totalAreaMm2 = 1234567890.1, totalAreaM2 = 1234.57, unlabelled = 100, byDepartment = 4000, zones = 10, ruleSet = new { name = "user", source = "rules\\aec-classification.json" } } };
        Assert.True(Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(schedule, Bridge)) < 60_000, "area rows");
        var plans = Enumerable.Range(0, Cad.RoomWriteService.MaxDimensions).Select(i => new DimensionPlan("overall", $"R-{i / 4 + 1:000}", "bottom", new Pt(1234567.8, 9876543.2), new Pt(2234567.8, 9876543.2), new Pt(1734567.8, 9875943.2), 1000000)).ToArray();
        var preview = new EditResult { Items = plans.Select((p, i) => new ItemOutcome(i, true, null, "DIMENSION", [$"{p.Rule}:{p.Subject}:{p.Side}", $"{p.MeasurementMm:0} mm"])).ToArray(), Summary = new
        {
            planned = plans.Length, drawn = 0, layer = "A-ANNO-DIMS-SUPPLEMENTARY", dimStyle = "HP-DIM-ARCH-1-100", byRule = new { overall = plans.Length }, bySubject = plans.GroupBy(p => p.Subject).ToDictionary(g => g.Key, g => g.Count()),
            dimensions = plans.Select(p => new { rule = p.Rule, subject = p.Subject, side = p.Side, p1Mm = p.P1Mm.Rounded(), p2Mm = p.P2Mm.Rounded(), dimLineMm = p.DimLineMm.Rounded(), measurementMm = Math.Round(p.MeasurementMm, 1) }).ToArray(),
        } };
        Assert.True(Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(preview, Bridge)) < 60_000, "dimension preview");
        var tags = new EditResult { Items = Enumerable.Range(0, AecTools.MaxRoomTags).Select(i => new ItemOutcome(i, true, $"{0x2A00 + i:X}", "MTEXT", [$"room:R-{i + 1:000}"])).ToArray(), Summary = new
        {
            requested = AecTools.MaxRoomTags, tagged = AecTools.MaxRoomTags, layer = "A-ANNO-ROOM-SUPPLEMENTARY", layerCreated = true, blockName = (string?)null, tags = (object?)null,
            written = Enumerable.Range(0, AecTools.MaxRoomTags).Select(i => new { room = $"R-{i + 1:000}", handle = $"{0x2A00 + i:X}", text = $"A-{i:000} PHONG LAM VIEC TRUONG PHONG KINH DOANH QUOC TE\\PKINH DOANH QUOC TE\\P123.45 m²" }).ToArray(),
        } };
        foreach (var i in Enumerable.Range(0, AecTools.MaxRoomTags)) tags.Created($"{0x2A00 + i:X}");
        Assert.True(Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(tags, Bridge)) < 60_000, "tags");
    }
}
