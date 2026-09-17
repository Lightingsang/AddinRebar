using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HPAutoCad.Aec.Classification;
using HPAutoCad.Aec.Coordination;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Issues;
using HPAutoCad.Aec.Model;
using Xunit;

namespace HPAutoCad.Aec.Tests;

/// <summary>Clashes in plan (crossing, containment, touching, clearance), opening requests through lines and outlines, and the envelopes at the caps.</summary>
public sealed class CoordinationTests
{
    private static readonly GeometryTolerance Tol = GeometryTolerance.Default;
    private static readonly JsonSerializerOptions Bridge = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, ReferenceHandler = ReferenceHandler.IgnoreCycles, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    private static Pt P(double x, double y) => new(x, y);

    private static ClashSubject Run(string handle, string type, params Pt[] points) => new(handle, type, "L-" + type, new PlanShape(points, false));

    private static ClashSubject Rect(string handle, string type, double x, double y, double w, double h) => new(handle, type, "L-" + type, new PlanShape([P(x, y), P(x + w, y), P(x + w, y + h), P(x, y + h)], true));

    [Fact]
    public void Clashes_are_crossings_containment_and_touching_and_clearance_is_measured_between_boundaries()
    {
        var mep = new[]
        {
            Run("DUCT", AecType.Duct, P(0, 500), P(10000, 500)),          // crosses the column; 500 mm from the beam (clear at 300)
            Run("PIPE", AecType.Pipe, P(2000, 2000), P(2300, 2000)),       // inside the slab outline
            Run("TRAY", AecType.CableTray, P(0, 1200), P(10000, 1200)),   // 200 mm from the beam: clearance
            Run("FAR", AecType.Pipe, P(0, 5000), P(10000, 5000)),          // clear
        };
        var structure = new[]
        {
            Rect("COL", AecType.StructuralColumn, 4000, 300, 400, 400),
            Run("BEAM", AecType.StructuralBeam, P(0, 1000), P(10000, 1000)),
            Rect("SLAB", AecType.StructuralSlab, 1000, 1500, 3000, 2000),
        };

        var o = ClashDetector.Detect(mep, structure, sameSet: false, clearanceMm: 300, Tol, CancellationToken.None);

        Assert.Equal(1, o.Hard);
        Assert.Equal(1, o.Clearance);
        Assert.Equal(1, o.AreaOverlaps); // the pipe inside the slab outline: the normal state of a floor, info
        var duct = o.Issues.Where(i => i.Handles[0] == "DUCT").ToArray();
        Assert.Single(duct);
        Assert.All(duct, i => Assert.Equal(ClashIssueType.Hard, i.Type));
        Assert.Contains(duct, i => i.Handles[1] == "COL" && (i.LocationMm!.Value.AlmostEqualsXY(P(4000, 500), 1e-6) || i.LocationMm!.Value.AlmostEqualsXY(P(4400, 500), 1e-6)) && i.Description.Contains("crosses"));
        Assert.Contains(o.Issues, i => i.Handles.SequenceEqual(["PIPE", "SLAB"]) && i.Type == ClashIssueType.AreaOverlap && i.Severity == IssueSeverity.Info && i.Description.Contains("lies inside"));
        var near = Assert.Single(o.Issues, i => i.Type == ClashIssueType.Clearance);
        Assert.Equal(["TRAY", "BEAM"], near.Handles);
        Assert.Equal(200, near.ValueMm);
        Assert.Equal(IssueSeverity.Warning, near.Severity);
        Assert.DoesNotContain(o.Issues, i => i.Handles.Contains("FAR"));
        Assert.Equal("CL-0001", o.Issues[0].IssueId);
        Assert.Equal(IssueSeverity.Critical, o.Issues[0].Severity);
        Assert.Equal("Duct×StructuralColumn", duct.First(i => i.Handles[1] == "COL").Rule);

        var hardOnly = ClashDetector.Detect(mep, structure, false, 0, Tol, CancellationToken.None);
        Assert.Equal(0, hardOnly.Clearance);
        Assert.Equal(2, hardOnly.Issues.Count);
    }

    [Fact]
    public void A_set_against_itself_reports_each_pair_once_and_never_an_entity_against_itself()
    {
        var pipes = new[] { Run("A", AecType.Pipe, P(0, 0), P(5000, 0)), Run("B", AecType.Pipe, P(2500, -1000), P(2500, 1000)), Run("C", AecType.Pipe, P(0, 3000), P(5000, 3000)) };

        var o = ClashDetector.Detect(pipes, pipes, sameSet: true, 0, Tol, CancellationToken.None);

        var clash = Assert.Single(o.Issues);
        Assert.Equal(["A", "B"], clash.Handles);
        Assert.Equal(1, o.PairsChecked); // A–B and B–A counted once; A–C and B–C never meet the broad phase
    }

    [Fact]
    public void Opening_requests_sit_where_routes_cross_host_lines_and_pass_through_host_outlines()
    {
        OpeningSubject S(ClashSubject c) => new(c.Handle, c.AecType, c.Layer, c.Shape);
        var routes = new[]
        {
            S(Run("DUCT", AecType.Duct, P(0, 500), P(10000, 500))),                    // through the column outline, across the wall line
            S(Run("PIPE", AecType.Pipe, P(6000, -1000), P(6000, 3000), P(9000, 3000))), // across the beam line, ends inside the slab
            S(Run("STUB", AecType.Pipe, P(4200, 2000), P(4200, 300 + 400))),          // crosses the beam line, ends on the column: no pass through the column
        };
        var hosts = new[]
        {
            S(Rect("COL", AecType.StructuralColumn, 4000, 300, 400, 400)),
            S(Run("WALL", AecType.ArchitecturalWall, P(8000, -2000), P(8000, 4000))),
            S(Run("BEAM", AecType.StructuralBeam, P(0, 1000), P(10000, 1000))),
            S(Rect("SLAB", AecType.StructuralSlab, 5000, 2500, 6000, 3000)),
        };

        var plans = OpeningPlanner.Plan(routes, hosts, OpeningSizes.Default, OpeningPlanner.DefaultMaxChordMm, Tol, CancellationToken.None).Requests;

        Assert.Equal(["OPN-001", "OPN-002", "OPN-003", "OPN-004", "OPN-005"], plans.Select(p => p.Id));
        var col = Assert.Single(plans, p => p.HostHandle == "COL");
        Assert.Equal((P(4200, 500), 500.0, 90.0), (col.CenterMm, col.WidthMm, col.AngleDeg)); // 400 duct + 2 × 50 margin, at the middle of the chord through the column, along the face it enters
        var wall = Assert.Single(plans, p => p.HostHandle == "WALL" && p.RouteHandle == "DUCT");
        Assert.Equal((P(8000, 500), 90.0), (wall.CenterMm, wall.AngleDeg)); // turned along the wall
        var beam = Assert.Single(plans, p => p.HostHandle == "BEAM" && p.RouteHandle == "PIPE");
        Assert.Equal((P(6000, 1000), 250.0), (beam.CenterMm, beam.WidthMm)); // 150 pipe + margin
        Assert.Single(plans, p => p.RouteHandle == "PIPE" && p.HostHandle == "WALL" && p.CenterMm == P(8000, 3000));
        Assert.DoesNotContain(plans, p => p.HostHandle == "SLAB"); // the pipe enters the slab outline once and ends inside: one crossing, no pass
        Assert.DoesNotContain(plans, p => p.RouteHandle == "STUB" && p.HostHandle == "COL"); // ending on the column is not passing through it
        Assert.Single(plans, p => p.RouteHandle == "STUB" && p.HostHandle == "BEAM");
        Assert.Contains("OPN-001: Duct DUCT through StructuralColumn COL (500×500)", plans[0].Label);
    }

    [Fact]
    public void A_route_ending_inside_an_outline_gets_no_pass_only_a_crossing_and_sizes_follow_the_route_kind()
    {
        OpeningSubject S(ClashSubject c) => new(c.Handle, c.AecType, c.Layer, c.Shape);
        var slab = S(Rect("SLAB", AecType.StructuralSlab, 5000, 2500, 6000, 3000));
        var into = OpeningPlanner.Plan([S(Run("PIPE", AecType.Pipe, P(6000, -1000), P(6000, 3000)))], [slab], OpeningSizes.Default, OpeningPlanner.DefaultMaxChordMm, Tol, CancellationToken.None);
        Assert.Empty(into.Requests); // one boundary crossing and an end inside: not a pass through the slab
        Assert.Equal(0, into.LongChordsSkipped);
        var chord = OpeningPlanner.Plan([S(Run("TRAY", AecType.CableTray, P(6000, -1000), P(6000, 9000)))], [slab], new OpeningSizes(150, 400, 600, 25), OpeningPlanner.DefaultMaxChordMm, Tol, CancellationToken.None);
        Assert.Empty(chord.Requests); // a 3 m chord across a slab outline is a run on the floor, not an opening through a member
        Assert.Equal(1, chord.LongChordsSkipped);
        var through = OpeningPlanner.Plan([S(Run("TRAY", AecType.CableTray, P(6000, -1000), P(6000, 9000)))], [slab], new OpeningSizes(150, 400, 600, 25), 5000, Tol, CancellationToken.None);
        var pass = Assert.Single(through.Requests);
        Assert.Equal((P(6000, 4000), 650.0, 650.0), (pass.CenterMm, pass.WidthMm, pass.HeightMm));
    }

    [Fact]
    public void Envelopes_at_the_caps_stay_under_the_result_budget()
    {
        var issues = Enumerable.Range(0, AecTools.MaxClashLimit).Select(i => new AuditIssue($"CL-{i + 1:0000}", ClashIssueType.Category, ClashIssueType.Hard, IssueSeverity.Critical, [$"{0x2A00 + i:X}FFFF", $"{0x3A00 + i:X}FFFF"], new Pt(1234567.8, 9876543.2), 0,
            $"CableTray {0x2A00 + i:X}FFFF encloses StructuralBeam {0x3A00 + i:X}FFFF in plan at (1234567.8, 9876543.2) (+3 more); check the section — the plan has no heights.",
            "Reroute or resize one, or confirm on the sections that they pass at different levels; MEP through structure: aec_create_opening_requests.", "M-HVAC-DUCT-SUPL-EXST-L03", "CableTray×StructuralBeam")).ToArray();
        var page = new AnalysisResult<AuditIssue> { Items = issues, Count = 5000, Truncated = true, Summary = new
        {
            setA = new { examined = 100000, subjects = 5000, aecTypes = "Pipe, Duct, CableTray", byType = new { Pipe = 3000, Duct = 1500, CableTray = 500 } }, setB = new { examined = 100000, subjects = 5000, aecTypes = "all", byType = new { StructuralBeam = 3000, StructuralColumn = 1500, StructuralSlab = 500 } },
            sameSet = false, clearanceMm = 300.0, minSeverity = "warning", pairsChecked = 200000, found = 9000, listed = 5000, belowMinSeverity = 4000, hard = 4000, clearance = 1000, contacts = 3000, areaOverlaps = 1000, bySeverity = new { critical = 4000, warning = 1000, info = 4000 },
            byPair = Enumerable.Range(0, 20).ToDictionary(i => $"CableTray×StructuralBeam-{i:00}", i => 250), tolerance = Tol, ruleSet = new { name = "user", source = "rules\\aec-classification.json" },
        } };
        page.Warnings.Add("the broad phase stopped at 200000 candidate pairs: narrow a set (layers, aecTypes) or lower clearanceMm.");
        var clashBytes = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(page, Bridge));
        Assert.True(clashBytes < 60_000, $"clash page {clashBytes} B");

        var requests = Enumerable.Range(0, OpeningPlanner.MaxRequests).Select(i => new OpeningRequest($"OPN-{i + 1:000}", $"{0x2A00 + i:X}FFFF", AecType.CableTray, $"{0x3A00 + i:X}FFFF", AecType.StructuralBeam, new Pt(1234567.8, 9876543.2), 1650, 1650, 179.99)).ToArray();
        var edit = new EditResult { Items = requests.Select((r, i) => new ItemOutcome(i, true, $"{0x4A00 + i:X}FFFF", "LWPOLYLINE", [r.Id, $"leader:{0x5A00 + i:X}FFFF", "route:" + r.RouteHandle, "host:" + r.HostHandle])).ToArray(), Summary = new
        {
            routes = 500, hosts = 2000, sizes = OpeningSizes.Default, plan = new { requested = requests.Length, drawn = requests.Length, layer = "HP-MCP-OPENINGS-SUPPLEMENTARY", layerCreated = true,
                written = requests.Select((r, i) => new { id = r.Id, rectangleHandle = $"{0x4A00 + i:X}FFFF", leaderHandle = $"{0x5A00 + i:X}FFFF", route = r.RouteHandle, host = r.HostHandle, centerMm = r.CenterMm, widthMm = r.WidthMm, heightMm = r.HeightMm, angleDeg = r.AngleDeg }).ToArray() },
        } };
        foreach (var i in Enumerable.Range(0, requests.Length)) { edit.Created($"{0x4A00 + i:X}FFFF"); edit.Created($"{0x5A00 + i:X}FFFF"); }
        var editBytes = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(edit, Bridge));
        Assert.True(editBytes < 60_000, $"opening requests {editBytes} B");
        var preview = new EditResult { Items = requests.Select((r, i) => new ItemOutcome(i, true, null, "LWPOLYLINE", [r.Label])).ToArray(), Summary = new { requested = requests.Length, drawn = 0, layer = "HP-MCP-OPENINGS", layerCreated = false, requests = requests.Select(Cad.CoordinationService.Describe).ToArray() } };
        var previewBytes = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(preview, Bridge));
        Assert.True(previewBytes < 60_000, $"opening preview {previewBytes} B");
    }
}
