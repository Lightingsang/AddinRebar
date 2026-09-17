using HPAutoCad.Aec.Classification;
using HPAutoCad.Aec.Coordination;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Issues;
using Xunit;

namespace HPAutoCad.Aec.Tests;

/// <summary>
///     Pins the coordination review round: contacts and area overlaps are info, not clashes; crossings through a vertex still pass;
///     entry/exit pairs never draw a phantom opening; a clearance location sits in the gap; long chords are runs, not openings;
///     subjects in different spaces never pair; and the clearance search stays fast on curved runs.
/// </summary>
public sealed class CoordinationReviewTests
{
    private static readonly GeometryTolerance Tol = GeometryTolerance.Default;

    private static Pt P(double x, double y) => new(x, y);

    private static ClashSubject Run(string handle, string type, params Pt[] points) => new(handle, type, "L-" + type, new PlanShape(points, false));

    private static ClashSubject Rect(string handle, string type, double x, double y, double w, double h) => new(handle, type, "L-" + type, new PlanShape([P(x, y), P(x + w, y), P(x + w, y + h), P(x, y + h)], true));

    private static OpeningSubject O(ClashSubject c) => new(c.Handle, c.AecType, c.Layer, c.Shape, c.Space);

    private static OpeningPlan Plan(IEnumerable<ClashSubject> routes, IEnumerable<ClashSubject> hosts, double maxChordMm = OpeningPlanner.DefaultMaxChordMm) =>
        OpeningPlanner.Plan(routes.Select(O).ToArray(), hosts.Select(O).ToArray(), OpeningSizes.Default, maxChordMm, Tol, CancellationToken.None);

    [Fact]
    public void A_frame_checked_against_itself_is_joints_not_clashes()
    {
        // beams drawn to the column faces, one to the column centre, one running along a wall line: every meeting is a contact (info)
        var frame = new[]
        {
            Rect("C1", AecType.StructuralColumn, -200, -200, 400, 400), Rect("C2", AecType.StructuralColumn, 5800, -200, 400, 400), Rect("C3", AecType.StructuralColumn, -200, 4800, 400, 400),
            Run("B1", AecType.StructuralBeam, P(200, 0), P(5800, 0)), Run("B2", AecType.StructuralBeam, P(0, 200), P(0, 4800)), Run("B3", AecType.StructuralBeam, P(6000, 0), P(6000, 5000)),
            Run("W1", AecType.ArchitecturalWall, P(200, 0), P(3000, 0)),
        };

        var o = ClashDetector.Detect(frame, frame, sameSet: true, 0, Tol, CancellationToken.None);

        Assert.Equal(0, o.Hard);
        Assert.True(o.Contacts >= 5, $"contacts {o.Contacts}");
        Assert.All(o.Issues, i => Assert.Equal(ClashIssueType.Contact, i.Type));
        Assert.All(o.Issues, i => Assert.Equal(IssueSeverity.Info, i.Severity));
        Assert.Contains(o.Issues, i => i.Handles.SequenceEqual(["B1", "W1"]) && i.Description.Contains("runs along"));
        Assert.Contains(o.Issues, i => i.Handles.SequenceEqual(["C2", "B3"]) && i.Description.Contains("meets")); // a beam drawn to the column centre
    }

    [Fact]
    public void A_pipe_network_reports_the_crossing_and_counts_the_tees_as_contacts()
    {
        var pipes = new[]
        {
            Run("MAIN", AecType.Pipe, P(0, 0), P(10000, 0)),
            Run("TEE", AecType.Pipe, P(3000, 0), P(3000, 2000)),        // branch from the main's body
            Run("OVER", AecType.Pipe, P(6000, -5), P(6000, 2000)),      // branch drawn 5 mm past the main: still the joint
            Run("NEXT", AecType.Pipe, P(10000, 0), P(14000, 0)),        // end-to-end continuation
            Run("DUCT", AecType.Duct, P(8000, -1000), P(8000, 1000)),   // a different service crossing the main
            Rect("AHU", AecType.Equipment, 14000, -500, 1000, 1000),      // the continuation ends on its face
        };

        var o = ClashDetector.Detect(pipes, pipes, sameSet: true, 0, Tol, CancellationToken.None);

        var hard = Assert.Single(o.Issues, i => i.Type == ClashIssueType.Hard);
        Assert.Equal(["MAIN", "DUCT"], hard.Handles);
        Assert.Contains("crosses", hard.Description);
        Assert.Equal(4, o.Contacts); // TEE, OVER, NEXT on the main; NEXT ending on the AHU
        Assert.Contains(o.Issues, i => i.Handles.SequenceEqual(["MAIN", "OVER"]) && i.Description.Contains("tees into"));
        Assert.Contains(o.Issues, i => i.Handles.SequenceEqual(["NEXT", "AHU"]) && i.Description.Contains("connects to"));
    }

    [Fact]
    public void Area_outlines_hold_their_floor_and_a_pipe_into_a_column_is_a_clash()
    {
        var mep = new[] { Run("P1", AecType.Pipe, P(1000, 1000), P(9000, 1000)), Run("P2", AecType.Pipe, P(1000, 3000), P(9000, 3000)), Run("STUB", AecType.Pipe, P(4200, 2500), P(4200, 500)) };
        var hosts = new[] { Rect("SLAB", AecType.StructuralSlab, 0, 0, 10000, 5000), Rect("ROOM", AecType.Room, 500, 500, 9000, 4000), Rect("COL", AecType.StructuralColumn, 4000, 300, 400, 400), Run("BEAM", AecType.StructuralBeam, P(0, 2000), P(10000, 2000)) };

        var o = ClashDetector.Detect(mep, hosts, false, 0, Tol, CancellationToken.None);

        Assert.Equal(6, o.AreaOverlaps); // every run inside the slab and the room
        Assert.All(o.Issues.Where(i => i.Type == ClashIssueType.AreaOverlap), i => Assert.Equal(IssueSeverity.Info, i.Severity));
        var hard = o.Issues.Where(i => i.Type == ClashIssueType.Hard).ToArray();
        Assert.Equal(2, hard.Length);
        Assert.Contains(hard, i => i.Handles.SequenceEqual(["STUB", "COL"]) && i.Description.Contains("crosses") && i.LocationMm!.Value.AlmostEqualsXY(P(4200, 700), 1e-6));
        Assert.Contains(hard, i => i.Handles.SequenceEqual(["STUB", "BEAM"]) && i.Description.Contains("crosses"));
        Assert.Equal("CL-0001", o.Issues[0].IssueId);
        Assert.Equal(IssueSeverity.Critical, o.Issues[0].Severity);
    }

    [Fact]
    public void A_clearance_clash_is_located_in_the_gap_not_at_a_far_vertex()
    {
        var pipe = new[] { Run("PIPE", AecType.Pipe, P(0, 430), P(10000, 430), P(10000, 5000)) };
        var column = new[] { Rect("COL", AecType.StructuralColumn, 5000, 0, 400, 400) }; // top face y 400: a 30 mm gap along x 5000..5400

        var o = ClashDetector.Detect(pipe, column, false, 100, Tol, CancellationToken.None);

        var near = Assert.Single(o.Issues);
        Assert.Equal(ClashIssueType.Clearance, near.Type);
        Assert.Equal(30, near.ValueMm);
        Assert.InRange(near.LocationMm!.Value.X, 5000, 5400);
        Assert.Equal(415, near.LocationMm.Value.Y, 0.01);
    }

    [Fact]
    public void Crossings_through_a_vertex_still_pass_and_entry_exit_pairs_never_draw_a_phantom_opening()
    {
        var wall = Rect("WALL", AecType.ArchitecturalWall, 5000, -2000, 200, 4000); // faces x 5000 and 5200
        var line = Run("LINE", AecType.StructuralBeam, P(0, 5000), P(10000, 5000));
        var vertexOnFarFace = Run("A", AecType.Pipe, P(0, 500), P(5200, 500), P(9000, 500));
        var vertexOnNearFace = Run("B", AecType.Pipe, P(0, 600), P(5000, 600), P(9000, 600));
        var throughOutlineVertex = Run("C", AecType.Pipe, P(4000, 3000), P(6000, 1000)); // through the corner (5000, 2000) and out at (5200, 1800)
        var vertexOnLine = Run("D", AecType.Pipe, P(2000, 4000), P(2000, 5000), P(2000, 6000));
        var touchAndTurn = Run("E", AecType.Pipe, P(3000, 4000), P(3000, 5000), P(3500, 4000));
        var uShape = Run("U", AecType.Duct, P(0, 0), P(5200, 0), P(7000, 0), P(7000, 1000), P(0, 1000)); // through the wall twice, a vertex on the far face
        var startInside = Run("S", AecType.Pipe, P(5100, -1000), P(5400, -1000), P(5400, 300), P(5100, 300), P(5100, 600), P(6000, 600)); // out, back in, out: one pass at (5100, 450)
        var startOnFace = Run("F", AecType.Pipe, P(5000, -1500), P(8000, -1500)); // from a fixture drawn on the near face, through the far face
        var alongFace = Run("G", AecType.Pipe, P(5000, -1800), P(5000, -1700)); // a stub running along the near face

        var plan = Plan([vertexOnFarFace, vertexOnNearFace, throughOutlineVertex, vertexOnLine, touchAndTurn, uShape, startInside, startOnFace, alongFace], [wall, line]);

        Pt[] Of(string route) => plan.Requests.Where(r => r.RouteHandle == route).Select(r => r.CenterMm).ToArray();
        Assert.Equal([P(5100, 500)], Of("A"));
        Assert.Equal([P(5100, 600)], Of("B"));
        Assert.Equal([P(5100, 1900)], Of("C"));
        Assert.Equal([P(2000, 5000)], Of("D"));
        Assert.Empty(Of("E"));
        Assert.Equal([P(5100, 0), P(5100, 1000)], Of("U").OrderBy(p => p.Y));
        Assert.Equal([P(5100, 450)], Of("S"));
        Assert.Equal([P(5100, -1500)], Of("F"));
        Assert.Empty(Of("G"));
        Assert.Equal(0, plan.LongChordsSkipped);
    }

    [Fact]
    public void A_run_drawn_inside_a_wall_cavity_is_not_an_opening_and_is_counted()
    {
        var wall = Rect("WALL", AecType.ArchitecturalWall, 0, 0, 6000, 200);
        var cavityRun = Run("CAV", AecType.Pipe, P(-500, 100), P(4000, 100), P(4000, 1000)); // in through the end cap, 4 m along the cavity, out through the top face
        var acrossRun = Run("X", AecType.Pipe, P(2000, -500), P(2000, 700));

        var plan = Plan([cavityRun, acrossRun], [wall]);

        var x = Assert.Single(plan.Requests);
        Assert.Equal(("X", P(2000, 100)), (x.RouteHandle, x.CenterMm));
        Assert.Equal(1, plan.LongChordsSkipped);
        Assert.Equal(2, Plan([cavityRun, acrossRun], [wall], maxChordMm: 5000).Requests.Count);
    }

    [Fact]
    public void Subjects_in_different_spaces_never_pair()
    {
        var model = Run("PIPE", AecType.Pipe, P(0, 500), P(10000, 500));
        var sheet = Rect("COL", AecType.StructuralColumn, 4000, 300, 400, 400) with { Space = "SHEET-1" };
        var wall = Run("WALL", AecType.ArchitecturalWall, P(5000, -2000), P(5000, 4000)) with { Space = "SHEET-1" };

        Assert.Empty(ClashDetector.Detect([model], [sheet, wall], false, 500, Tol, CancellationToken.None).Issues);
        Assert.Empty(Plan([model], [sheet, wall]).Requests);
        Assert.Single(Plan([model], [wall with { Space = "Model" }]).Requests);
    }

    [Fact]
    public void The_clearance_search_stays_fast_on_stacked_curved_runs_and_on_long_polylines()
    {
        var arcs = Enumerable.Range(0, 60).Select(k => new ClashSubject($"A{k}", AecType.Pipe, "M-PIPE",
            new PlanShape(Enumerable.Range(0, 113).Select(i => { var t = Math.PI * i / 112; var r = 20000 + 50 * k; return P(r * Math.Cos(t), r * Math.Sin(t)); }).ToArray(), false))).ToArray();
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var o = ClashDetector.Detect(arcs, arcs, true, 100, Tol, CancellationToken.None);
        Assert.True(watch.ElapsedMilliseconds < 2000, $"{watch.ElapsedMilliseconds} ms for {o.PairsChecked} pairs");
        Assert.InRange(o.Clearance, 59, 117); // each arc 50 mm from its neighbour; the next but one sits at 100 less the chord sagitta

        var routes = Enumerable.Range(0, 45).Select(k => new ClashSubject($"R{k}", AecType.Duct, "M-DUCT", new PlanShape(Enumerable.Range(0, 64).Select(i => P(i * 300, 1000 * k + (i % 2) * 100)).ToArray(), false))).ToArray();
        var members = Enumerable.Range(0, 45).Select(k => new ClashSubject($"M{k}", AecType.StructuralBeam, "S-BEAM", new PlanShape(Enumerable.Range(0, 64).Select(i => P(i * 300, 1000 * k + 500 + (i % 2) * 100)).ToArray(), false))).ToArray();
        watch.Restart();
        var p = ClashDetector.Detect(routes, members, false, 5000, Tol, CancellationToken.None);
        Assert.True(watch.ElapsedMilliseconds < 2000, $"{watch.ElapsedMilliseconds} ms for {p.PairsChecked} pairs");
        Assert.True(p.Clearance >= 45, $"clearance {p.Clearance}");
    }
}
