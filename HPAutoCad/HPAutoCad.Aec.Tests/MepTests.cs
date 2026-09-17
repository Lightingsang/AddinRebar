using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Issues;
using HPAutoCad.Aec.Mep;
using HPAutoCad.Aec.Model;
using Xunit;

namespace HPAutoCad.Aec.Tests;

/// <summary>The MEP network graph (joins, tees, nodes, crossings, near misses, duplicates, systems), its checks, and the envelopes at the caps.</summary>
public sealed class MepTests
{
    private static readonly GeometryTolerance Tol = GeometryTolerance.Default;
    private static readonly JsonSerializerOptions Bridge = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, ReferenceHandler = ReferenceHandler.IgnoreCycles, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    private static Pt P(double x, double y) => new(x, y);

    private static MepRun Run(string handle, string system, params Pt[] points) =>
        new(handle, MepRunKind.Pipe, "M-" + system, system, new PlanShape(points, false), new PlanShape(points, false).LengthMm);

    private static MepNode Node(string handle, string kind, double x, double y, double size = 400) =>
        new(handle, kind, "M-EQPM", PlanShape.Rectangle(Box.Of(P(x - size / 2, y - size / 2), P(x + size / 2, y + size / 2))));

    private static MepOutcome Build(IEnumerable<MepRun> runs, IEnumerable<MepNode>? nodes = null, double nearMissMm = MepNetworkBuilder.DefaultNearMissMm) =>
        MepNetworkBuilder.Build(runs.ToArray(), (nodes ?? []).ToArray(), Tol, nearMissMm, CancellationToken.None);

    [Fact]
    public void Runs_join_end_to_end_tee_into_a_body_and_meet_through_equipment_while_crossings_do_not_connect()
    {
        var runs = new[]
        {
            Run("MAIN", "CHW", P(0, 0), P(10000, 0)),
            Run("BR1", "CHW", P(4000, 0), P(4000, 3000)),        // tee onto the main
            Run("EXT", "CHW", P(10000, 0), P(14000, 0)),         // joined end to end
            Run("AHU-IN", "CHW", P(14000, 0), P(16000, 0)),      // ends on the equipment box
            Run("AHU-OUT", "CHW", P(16400, 0), P(19000, 0)),     // starts on the far side of the equipment box
            Run("CROSS", "CHW", P(7000, -2000), P(7000, 2000)),  // crosses the main mid-body: not connected
        };
        var ahu = Node("AHU", MepNodeKind.Equipment, 16200, 0);

        var o = Build(runs, [ahu]);

        Assert.Equal(2, o.Networks.Count);
        var main = o.Networks[0];
        Assert.Equal("N-001", main.Id);
        Assert.Equal(new[] { "MAIN", "BR1", "EXT", "AHU-IN", "AHU-OUT" }.Order(), main.Runs.Select(r => r.Handle).Order());
        Assert.Equal(["AHU"], main.Nodes.Select(n => n.Handle));
        Assert.Equal("CHW", main.System);
        Assert.Equal(["CROSS"], o.Networks[1].Runs.Select(r => r.Handle));
        Assert.Equal(1, o.Crossings);
        Assert.Equal(MepEndpointState.Tee, o.Endpoints.Single(e => e.RunHandle == "BR1" && e.End == 0).State);
        Assert.Equal(MepEndpointState.Joined, o.Endpoints.Single(e => e.RunHandle == "EXT" && e.End == 0).State);
        Assert.Equal(MepEndpointState.Node, o.Endpoints.Single(e => e.RunHandle == "AHU-OUT" && e.End == 0).State);
        Assert.Equal(5, o.Endpoints.Count(e => e.IsOpen)); // MAIN start, BR1 end, AHU-OUT end, CROSS both ends
    }

    [Fact]
    public void Inline_nodes_are_attached_to_the_run_passing_through_and_a_node_never_merges_systems()
    {
        // a valve and a pump sitting on an unbroken pipe, a VAV on the main with a branch ending inside it
        var runs = new[] { Run("MAIN", "CHW", P(0, 0), P(10000, 0)), Run("BR", "CHW", P(6000, 0), P(6000, -2000)) };
        var valve = Node("VALVE", MepNodeKind.Fitting, 2000, 0, 300);
        var pump = Node("PUMP", MepNodeKind.Equipment, 4000, 0, 600);
        var vav = Node("VAV", MepNodeKind.Equipment, 6000, 0, 800);
        var o = Build(runs, [valve, pump, vav]);
        Assert.Single(o.Networks);
        Assert.Equal(["PUMP", "VALVE", "VAV"], o.Networks[0].Nodes.Select(n => n.Handle).Order());
        Assert.Empty(o.OrphanNodes);
        Assert.Equal(MepEndpointState.Tee, o.Endpoints.Single(e => e.RunHandle == "BR" && e.End == 0).State); // the branch also lands on the main's body

        // supply + return pipes and supply + return ducts into one AHU: four networks, the AHU on each, nothing "mixed"
        MepRun[] four = [Run("CHWS", "CHWS", P(0, 0), P(4800, 0)), Run("CHWR", "CHWR", P(0, 400), P(4800, 400)), Run("SA", "SA", P(6200, 0), P(12000, 0)), Run("RA", "RA", P(6200, 400), P(12000, 400))];
        var ahu = Node("AHU", MepNodeKind.Equipment, 5500, 200, 1400);
        var plant = Build(four, [ahu]);
        Assert.Equal(4, plant.Networks.Count);
        Assert.All(plant.Networks, n => Assert.Equal(["AHU"], n.Nodes.Select(x => x.Handle)));
        Assert.All(plant.Networks, n => Assert.Single(n.Systems));
        Assert.DoesNotContain(MepChecks.Check(plant, CancellationToken.None), i => i.Type == MepIssueType.MixedSystem || i.Type == MepIssueType.OrphanNode);
        // two same-system runs meeting only through the AHU are one network
        var served = Build([Run("A", "CHWS", P(0, 0), P(4800, 0)), Run("B", "CHWS", P(6200, 0), P(9000, 0))], [ahu]);
        Assert.Single(served.Networks);
    }

    [Fact]
    public void An_offset_copy_is_a_duplicate_a_ring_main_finds_its_own_far_end_and_facing_ends_are_one_near_miss()
    {
        var copy = Build([Run("MAIN", "CHW", P(0, 0), P(10000, 0)), Run("COPY", "CHW", P(3000, 2), P(7000, 2))]);
        var dup = Assert.Single(copy.Duplicates);
        Assert.Equal(("COPY", "MAIN", 4000.0), (dup.Handle, dup.OtherHandle, dup.OverlapMm));
        Assert.Equal(4000, copy.Networks[0].DuplicateOverlapMm);
        Assert.Single(MepChecks.Check(copy, CancellationToken.None), i => i.Type == MepIssueType.DuplicateRun && i.ValueMm == 4000);

        var ring = Build([Run("RING", "CHW", P(0, 0), P(6000, 0), P(6000, 4000), P(0, 4000), P(0, 60))]); // a ring main whose ends stop 60 mm apart
        var ends = ring.Endpoints.Where(e => e.IsOpen).ToArray();
        Assert.Equal(2, ends.Length);
        Assert.All(ends, e => Assert.Equal(("RING", 60.0), (e.NearestHandle, e.NearestGapMm)));
        var loop = Build([Run("LOOP", "CHW", P(0, 0), P(6000, 0), P(6000, 4000), P(3000, 4000), P(3000, 0))]); // an end landing on its own body
        Assert.Equal(MepEndpointState.Tee, loop.Endpoints.Single(e => e.End == 1).State);

        var facing = Build([Run("S", "CHW", P(0, 0), P(4985, 0)), Run("R", "CHW", P(5015, 0), P(9000, 0))]);
        var issues = MepChecks.Check(facing, CancellationToken.None);
        var miss = Assert.Single(issues, i => i.Type == MepIssueType.NearMiss);
        Assert.Equal(30, miss.ValueMm);
        Assert.Equal(P(5000, 0), miss.LocationMm);
        Assert.Equal(new[] { "R", "S" }, miss.Handles.Order());
        Assert.Empty(MepChecks.Check(Build([], [Node("WC", MepNodeKind.Fixture, 0, 0)]), CancellationToken.None)); // no runs: no orphan warnings

        var ducts = Build([new MepRun("D1", MepRunKind.Duct, "M-DUCT", "M-DUCT", new PlanShape([P(0, 0), P(8000, 0)], false), 8000), new MepRun("D2", MepRunKind.Duct, "M-DUCT", "M-DUCT", new PlanShape([P(0, 600), P(8000, 600)], false), 8000)]);
        Assert.Equal(2, ducts.DoubleLineDucts);
        Assert.Empty(ducts.Duplicates);
    }

    [Fact]
    public void Open_ends_report_the_near_miss_and_the_checks_name_each_problem_once()
    {
        var runs = new[]
        {
            Run("MAIN", "CHW", P(0, 0), P(10000, 0)),
            Run("SHORT", "CHW", P(4000, 50), P(4000, 3000)),     // stops 50 mm above the main: near miss
            Run("LOST", "CHW", P(20000, 20000), P(23000, 20000)),// nothing anywhere near: disconnected run
            Run("DUP", "CHW", P(1000, 0), P(3500, 0)),           // drawn over the main
        };
        var diffuser = Node("DIF", MepNodeKind.Terminal, 30000, 0);
        var fitting = Node("ELB", MepNodeKind.Fitting, 30000, 5000);

        var o = Build(runs, [diffuser, fitting]);
        var issues = MepChecks.Check(o, CancellationToken.None);

        var shortEnd = o.Endpoints.Single(e => e.RunHandle == "SHORT" && e.End == 0);
        Assert.True(shortEnd.IsOpen);
        Assert.Equal(("MAIN", 50.0), (shortEnd.NearestHandle, shortEnd.NearestGapMm));
        Assert.Single(issues, i => i.Type == MepIssueType.NearMiss && i.Handles.SequenceEqual(["SHORT", "MAIN"]) && i.ValueMm == 50);
        Assert.Single(issues, i => i.Type == MepIssueType.DisconnectedRun && i.Handles.SequenceEqual(["LOST"]));
        Assert.DoesNotContain(issues, i => i.Type == MepIssueType.OpenEnd && i.Handles.Contains("LOST")); // one issue for the run, not two open ends
        Assert.Single(issues, i => i.Type == MepIssueType.DuplicateRun && i.ValueMm == 2500);
        Assert.Single(issues, i => i.Type == MepIssueType.OrphanNode && i.Handles.SequenceEqual(["DIF"]) && i.Severity == IssueSeverity.Warning);
        Assert.Single(issues, i => i.Type == MepIssueType.OrphanNode && i.Handles.SequenceEqual(["ELB"]) && i.Severity == IssueSeverity.Info);
        Assert.Equal("MEP-001", issues[0].IssueId);
        Assert.All(issues.Where(i => i.Type == MepIssueType.OpenEnd), i => Assert.Equal(IssueSeverity.Warning, i.Severity));

        var strict = Build(runs, [diffuser, fitting], nearMissMm: 20);
        Assert.Null(strict.Endpoints.Single(e => e.RunHandle == "SHORT" && e.End == 0).NearestHandle); // beyond the reach it is a plain open end
    }

    [Fact]
    public void Systems_come_from_the_layer_map_and_a_network_joining_two_systems_is_flagged()
    {
        var systems = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase) { ["CHW"] = ["M-CHW*"], ["HHW"] = ["M-HHW*", "*-HOT"] };
        Assert.Equal("CHW", Cad.MepService.SystemOf("M-CHWS-L03", systems));
        Assert.Equal("HHW", Cad.MepService.SystemOf("P-HOT", systems));
        Assert.Equal("M-PIPE", Cad.MepService.SystemOf("M-PIPE", systems));

        var mixed = Build([Run("A", "CHW", P(0, 0), P(5000, 0)), Run("B", "HHW", P(5000, 0), P(9000, 0))]);
        Assert.Single(mixed.Networks);
        Assert.Equal("mixed", mixed.Networks[0].System);
        Assert.Equal(["CHW", "HHW"], mixed.Networks[0].Systems);
        Assert.Single(MepChecks.Check(mixed, CancellationToken.None), i => i.Type == MepIssueType.MixedSystem);

        var separate = Build([Run("A", "CHW", P(0, 0), P(5000, 0)), Run("B", "HHW", P(0, 400), P(5000, 400))]);
        Assert.Equal(2, separate.Networks.Count);
        Assert.Empty(separate.Duplicates); // a parallel run of another system is not a duplicate
    }

    [Fact]
    public void Envelopes_at_the_caps_stay_under_the_result_budget()
    {
        var longHandle = new string('F', 6);
        var runs = Enumerable.Range(0, 60).Select(i => new MepRun($"{0x2A00 + i:X}{longHandle}", MepRunKind.Duct, "M-HVAC-DUCT-SUPL-EXST-L03", "SUPPLY-AIR-LEVEL-03", new PlanShape([P(123456.7, 98765.4), P(223456.7, 98765.4)], false), 100000)).ToArray();
        var nodes = Enumerable.Range(0, 40).Select(i => Node($"{0x3A00 + i:X}{longHandle}", MepNodeKind.Terminal, 123456.7 + i * 1000, 98765.4)).ToArray();
        var open = Enumerable.Range(0, 40).Select(i => new MepEndpoint(runs[i].Handle, runs[i].System, 1, P(123456.7, 98765.4), MepEndpointState.Open, [], $"{0x4A00 + i:X}{longHandle}", 99.9)).ToArray();
        var network = new MepNetwork("N-001", runs, nodes, open, ["SUPPLY-AIR-LEVEL-03", "RETURN-AIR-LEVEL-03"]) { DuplicateOverlapMm = 123456.7 };
        var page = new AnalysisResult<Dictionary<string, object?>> { Items = Enumerable.Range(0, AecTools.MaxNetworkLimit).Select(_ => Cad.MepService.Describe(network, AecTools.MaxNetworkHandles, AecTools.MaxNetworkOpenEnds)).ToArray(), Count = 5000, Truncated = true, Summary = new
        {
            examined = 100000, runs = 20000, nodes = 5000, networks = 5000, totalLengthMm = 123456789.1,
            bySystem = Enumerable.Range(0, 20).ToDictionary(i => $"SUPPLY-AIR-LEVEL-{i:00}", i => new { runs = 1000, lengthMm = 1234567.8 }), byKind = new { pipe = 10000, duct = 8000, tray = 2000 },
            systemsTotal = 200, mixedNetworks = 50, openEnds = 3000, nearMisses = 500, orphanNodes = 200, orphanHandles = nodes.Take(AecTools.MaxNetworkHandles).Select(n => n.Handle).ToArray(), duplicates = 100, duplicateOverlapMm = 1234567.8, crossings = 4000, doubleLineDucts = 300, closedRunsIgnored = 10, tinyRunsIgnored = 5,
            nearMissMm = 100.0, systems = Enumerable.Range(0, 20).Select(i => $"SUPPLY-AIR-LEVEL-{i:00}").ToArray(), ruleSet = new { name = "user", source = "rules\\aec-classification.json" },
        } };
        page.Warnings.Add("limit is capped at 30 per page; page with offset.");
        var pageBytes = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(page, Bridge));
        Assert.True(pageBytes < 60_000, $"networks page {pageBytes} B");

        var byHandle = runs.ToDictionary(r => r.Handle, StringComparer.Ordinal);
        var eight = runs.Take(8).Select(r => r.Handle).ToArray(); // a header: eight runs starting at one point, listed capped
        var ends = Enumerable.Range(0, AecTools.MaxEndpointLimit).Select(i => Cad.MepService.Describe(new MepEndpoint($"{0x2A00 + i:X}{longHandle}", runs[0].System, i % 2, P(1234567.7, 9876543.4), MepEndpointState.Tee, eight, $"{0x4A00 + i:X}{longHandle}", 99.9), byHandle)).ToArray();
        var endpoints = new AnalysisResult<Dictionary<string, object?>> { Items = ends, Count = 5000, Truncated = true, Summary = new { examined = 100000, runs = 20000, endpoints = 40000, byState = new { joined = 1, tee = 2, node = 3, open = 4 }, open = 3000, nearMisses = 500, bySystem = Enumerable.Range(0, 20).ToDictionary(i => $"SUPPLY-AIR-LEVEL-{i:00}", i => 100), nearMissMm = 100.0, includeConnected = true } };
        var endpointBytes = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(endpoints, Bridge));
        Assert.True(endpointBytes < 60_000, $"endpoints page {endpointBytes} B");

        // an issues page of nothing but mixed_system items (the longest issue): handles and systems capped
        MepRun Synthetic(int n) => new($"{0x5A00 + n:X}{longHandle}", MepRunKind.Duct, "M-HVAC-DUCT-SUPL-EXST-L03", $"SUPPLY-AIR-LEVEL-{n % 8:00}", new PlanShape([P(1234567.7, 9876543.4), P(2234567.7, 9876543.4)], false), 1000000);
        var mixedNetworks = Enumerable.Range(0, AecTools.MaxIssueLimit).Select(i => new MepNetwork($"N-{i + 1:000}", Enumerable.Range(0, 8).Select(k => Synthetic(i * 8 + k)).ToArray(), nodes, open, Enumerable.Range(0, 8).Select(k => $"SUPPLY-AIR-LEVEL-{k:00}-ZONE-{i:00}").ToArray())).ToArray();
        var outcome = new MepOutcome(mixedNetworks, [], [], [], 0, runs.Length, nodes.Length, 0);
        var issues = new AnalysisResult<Issues.AuditIssue> { Items = MepChecks.Check(outcome, CancellationToken.None), Count = 5000, Truncated = true, Summary = endpoints.Summary };
        Assert.Equal(AecTools.MaxIssueLimit, issues.Items.Count);
        var issueBytes = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(issues, Bridge));
        Assert.True(issueBytes < 60_000, $"issues page {issueBytes} B");
    }
}
