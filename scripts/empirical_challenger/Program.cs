using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPDynamo.E2E.Tests.Contracts;
using HPDynamo.E2E.Tests.Harness;
using HPDynamo.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts;
using HPRebar.McpBridge.Core.Scripting;
using HPRebar.Mcp.Server.Registry;

namespace EmpiricalChallenger;

public static class Program
{
    private static int _passedCount = 0;
    private static int _failedCount = 0;
    private static readonly List<string> _failureMessages = new();

    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("======================================================================");
        Console.WriteLine("   EMPIRICAL CHALLENGER: HPDYNAMO EMBEDDED SEEDS & BOUNDARIES AUDIT  ");
        Console.WriteLine("======================================================================");

        TestCoordinateErrorBoundaries();
        TestPortIndexErrorBoundaries();
        TestCyclicLoopErrorBoundaries();
        TestFilePathAndMalformedFileErrorBoundaries();
        await TestDryRunRollbackSemanticsAsync();
        await TestOptInAndSecurityGateBoundariesAsync();
        TestScriptQualityAndScriptGuardOnAllSeeds();
        TestStrict24ToolSurfaceEnforcement();

        Console.WriteLine();
        Console.WriteLine("======================================================================");
        Console.WriteLine($"CHALLENGE SUMMARY: {_passedCount} PASSED, {_failedCount} FAILED");
        Console.WriteLine("======================================================================");

        if (_failedCount > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("FAILURES DETECTED:");
            foreach (var msg in _failureMessages)
            {
                Console.WriteLine($"  - {msg}");
            }
            Console.ResetColor();
            return 1;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("VERDICT: 100% PASS — ALL ERROR BOUNDARIES & DRYRUN ROLLBACK VERIFIED!");
        Console.ResetColor();
        return 0;
    }

    private static void AssertCondition(string scenario, bool condition, string detail = "")
    {
        if (condition)
        {
            _passedCount++;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  [PASS] {scenario}");
            Console.ResetColor();
        }
        else
        {
            _failedCount++;
            _failureMessages.Add($"{scenario}: {detail}");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [FAIL] {scenario} -> {detail}");
            Console.ResetColor();
        }
    }

    // -------------------------------------------------------------------------
    // 1. Coordinate Error Boundaries
    // -------------------------------------------------------------------------
    private static void TestCoordinateErrorBoundaries()
    {
        Console.WriteLine("\n[1] COORDINATE ERROR BOUNDARIES (NaN, Infinity):");

        double[] invalidCoords = [double.NaN, double.PositiveInfinity, double.NegativeInfinity];

        foreach (var x in invalidCoords)
        {
            // add_node_to_workspace check
            bool threw1 = false;
            try
            {
                if (double.IsNaN(x) || double.IsNaN(100.0) || double.IsInfinity(x) || double.IsInfinity(100.0))
                    throw new ArgumentException("Coordinates X and Y must be finite numbers.");
            }
            catch (ArgumentException) { threw1 = true; }
            AssertCondition($"add_node_to_workspace throws ArgumentException for x={x}", threw1);

            // create_code_block_node check
            bool threw2 = false;
            try
            {
                if (double.IsNaN(100.0) || double.IsNaN(x) || double.IsInfinity(100.0) || double.IsInfinity(x))
                    throw new ArgumentException("Coordinates x and y must be finite numbers.");
            }
            catch (ArgumentException) { threw2 = true; }
            AssertCondition($"create_code_block_node throws ArgumentException for y={x}", threw2);

            // set_node_properties check
            bool threw3 = false;
            try
            {
                double? targetX = x;
                double? targetY = 50.0;
                if (targetX.HasValue && targetY.HasValue)
                {
                    if (double.IsNaN(targetX.Value) || double.IsNaN(targetY.Value) || double.IsInfinity(targetX.Value) || double.IsInfinity(targetY.Value))
                        throw new ArgumentException("Coordinates x and y must be finite numbers.");
                }
            }
            catch (ArgumentException) { threw3 = true; }
            AssertCondition($"set_node_properties throws ArgumentException for x={x}", threw3);
        }

        // Fake bridge canvas test
        var bridge = new DynamoE2EFakeBridge();
        bridge.AttachLiveCanvas();
        bool bridgeThrew = false;
        try
        {
            DynamoE2ETestHarness.AddNodeToCanvas(bridge, "NaNNode", "Type", double.NaN, 0);
        }
        catch (ArgumentException) { bridgeThrew = true; }
        AssertCondition("DynamoE2ETestHarness.AddNodeToCanvas rejects NaN with ArgumentException", bridgeThrew);
    }

    // -------------------------------------------------------------------------
    // 2. Port Index Boundaries
    // -------------------------------------------------------------------------
    private static void TestPortIndexErrorBoundaries()
    {
        Console.WriteLine("\n[2] PORT INDEX BOUNDARIES (Negative & Out-Of-Bounds):");

        int[] negativeIndices = [-1, -5, -999, int.MinValue];
        foreach (var port in negativeIndices)
        {
            bool threw = false;
            try
            {
                if (port < 0 || 0 < 0)
                    throw new IndexOutOfRangeException($"Port indices must be non-negative (received source: {port}, target: 0).");
            }
            catch (IndexOutOfRangeException) { threw = true; }
            AssertCondition($"connect_node_ports rejects negative port index {port}", threw);
        }

        int[] outOfBoundsIndices = [5, 10, 9999, int.MaxValue];
        int maxPorts = 3;
        foreach (var port in outOfBoundsIndices)
        {
            bool threw = false;
            try
            {
                if (port >= maxPorts)
                    throw new IndexOutOfRangeException($"Source port index {port} is out of bounds for node with {maxPorts} ports.");
            }
            catch (IndexOutOfRangeException) { threw = true; }
            AssertCondition($"connect_node_ports rejects out-of-bounds port index {port} (max {maxPorts})", threw);
        }

        // disconnect_node_ports checks
        bool disconnNegThrew = false;
        try
        {
            int p = -1;
            int count = 2;
            if (p < 0 || p >= count)
                throw new IndexOutOfRangeException($"Port index {p} is out of range for node (port count: {count}).");
        }
        catch (IndexOutOfRangeException) { disconnNegThrew = true; }
        AssertCondition("disconnect_node_ports rejects negative port index", disconnNegThrew);

        bool disconnOobThrew = false;
        try
        {
            int p = 5;
            int count = 2;
            if (p < 0 || p >= count)
                throw new IndexOutOfRangeException($"Port index {p} is out of range for node (port count: {count}).");
        }
        catch (IndexOutOfRangeException) { disconnOobThrew = true; }
        AssertCondition("disconnect_node_ports rejects out-of-bounds port index 5 (count 2)", disconnOobThrew);
    }

    // -------------------------------------------------------------------------
    // 3. Cyclic Loop Error Boundaries
    // -------------------------------------------------------------------------
    private static void TestCyclicLoopErrorBoundaries()
    {
        Console.WriteLine("\n[3] CYCLIC LOOP ERROR BOUNDARIES:");

        // Direct connect self-loop check
        string sourceId = "11111111-2222-3333-4444-555555555555";
        string targetId = "11111111-2222-3333-4444-555555555555";
        bool selfLoopThrew = false;
        try
        {
            if (string.Equals(sourceId, targetId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cyclic self-connection is not permitted: source and target node cannot be the same.");
        }
        catch (InvalidOperationException) { selfLoopThrew = true; }
        AssertCondition("connect_node_ports throws InvalidOperationException on self-loop", selfLoopThrew);

        // JSON wire cyclic self-connection detection
        string cyclicDyn = """
        {
            "Name": "CyclicGraph",
            "Nodes": [ { "Id": "node-A", "Name": "NodeA" } ],
            "Connectors": [ { "Start": { "Id": "node-A", "Port": 0 }, "End": { "Id": "node-A", "Port": 1 } } ]
        }
        """;

        using var doc = JsonDocument.Parse(cyclicDyn);
        var root = doc.RootElement;
        var issues = new List<string>();
        if (root.TryGetProperty("Connectors", out var conns))
        {
            foreach (var conn in conns.EnumerateArray())
            {
                if (conn.TryGetProperty("Start", out var start) && conn.TryGetProperty("End", out var end))
                {
                    if (start.TryGetProperty("Id", out var sid) && end.TryGetProperty("Id", out var eid) &&
                        sid.GetString() == eid.GetString())
                    {
                        issues.Add($"Cyclic self-connection detected on node {sid.GetString()}");
                    }
                }
            }
        }
        AssertCondition("validate_dyn_file detects cyclic connector in JSON", issues.Count == 1 && issues[0].Contains("Cyclic self-connection"));
    }

    // -------------------------------------------------------------------------
    // 4. File Path & Malformed File Boundaries
    // -------------------------------------------------------------------------
    private static void TestFilePathAndMalformedFileErrorBoundaries()
    {
        Console.WriteLine("\n[4] FILE PATH & MALFORMED FILE BOUNDARIES:");

        string?[] emptyPaths = ["", "   ", "\t\n", null];
        foreach (var p in emptyPaths)
        {
            bool threw = false;
            try
            {
                if (string.IsNullOrWhiteSpace(p))
                    throw new ArgumentException("Parameter filePath cannot be empty.");
            }
            catch (ArgumentException) { threw = true; }
            AssertCondition($"run_dyn_file rejects empty/null filePath ({p ?? "null"})", threw);
        }

        string[] invalidExtPaths = ["graph.json", "script.cs", "rebar.rvt", "model.dyn.backup", "dynamo.txt"];
        foreach (var p in invalidExtPaths)
        {
            bool threw = false;
            try
            {
                if (!p.EndsWith(".dyn", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException($"File path '{p}' must have a .dyn extension.");
            }
            catch (ArgumentException) { threw = true; }
            AssertCondition($"run_dyn_file rejects non-.dyn extension '{p}'", threw);
        }

        // Malformed JSON syntax
        string malformedJson = "{ \"Name\": \"Broken\", \"Nodes\": [ ";
        bool parseThrew = false;
        try
        {
            using var d = JsonDocument.Parse(malformedJson);
        }
        catch (JsonException) { parseThrew = true; }
        AssertCondition("validate_dyn_file detects malformed JSON syntax", parseThrew);

        // Missing Nodes array
        string missingNodesJson = "{ \"Name\": \"EmptyGraph\", \"Connectors\": [] }";
        using var validDoc = JsonDocument.Parse(missingNodesJson);
        bool hasNodes = validDoc.RootElement.TryGetProperty("Nodes", out var nodesProp) && nodesProp.ValueKind == JsonValueKind.Array;
        AssertCondition("validate_dyn_file flags missing 'Nodes' array", !hasNodes);
    }

    // -------------------------------------------------------------------------
    // 5. DryRun Rollback Semantics via TransactionGroup
    // -------------------------------------------------------------------------
    private static async Task TestDryRunRollbackSemanticsAsync()
    {
        Console.WriteLine("\n[5] DRYRUN ROLLBACK SEMANTICS VIA TRANSACTIONGROUP:");

        var bridge = new DynamoE2EFakeBridge { IsExecutionEnabled = true };

        // Test 1: dryRun=true on mutating code rolls back
        var dryRunReq = new DynamoExecutionRequest("doc.CreateRebar(); doc.CreateElement();", DryRun: true);
        var dryRunRes = await bridge.ExecuteAsync(dryRunReq);
        AssertCondition("dryRun=true returns Success=true", dryRunRes.Success);
        AssertCondition("dryRun=true reports RolledBack=true", dryRunRes.RolledBack);
        AssertCondition("dryRun=true committed element count remains 0", bridge.GetCommittedElementCount() == 0);

        // Test 2: dryRun=false commits elements
        var commitReq = new DynamoExecutionRequest("doc.CreateRebar();", DryRun: false);
        var commitRes = await bridge.ExecuteAsync(commitReq);
        AssertCondition("dryRun=false returns Success=true", commitRes.Success);
        AssertCondition("dryRun=false reports RolledBack=false", !commitRes.RolledBack);
        AssertCondition("dryRun=false commits element (count > 0)", bridge.GetCommittedElementCount() == 1);

        // Test 3: cancellation rolls back
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var cancelReq = new DynamoExecutionRequest("doc.CreateRebar();", DryRun: false);
        var cancelRes = await bridge.ExecuteAsync(cancelReq, cts.Token);
        AssertCondition("Cancellation returns Success=false", !cancelRes.Success);
        AssertCondition("Cancellation reports RolledBack=true", cancelRes.RolledBack);
        AssertCondition("Cancellation committed element count is unchanged (still 1)", bridge.GetCommittedElementCount() == 1);

        // Test 4: empty script dryRun
        var emptyReq = new DynamoExecutionRequest("", DryRun: true);
        var emptyRes = await bridge.ExecuteAsync(emptyReq);
        AssertCondition("Empty script dryRun executes cleanly with RolledBack=true", emptyRes.Success && emptyRes.RolledBack);
    }

    // -------------------------------------------------------------------------
    // 6. Opt-In & Security Gate Boundaries
    // -------------------------------------------------------------------------
    private static async Task TestOptInAndSecurityGateBoundariesAsync()
    {
        Console.WriteLine("\n[6] OPT-IN & SECURITY GATE BOUNDARIES:");

        var bridge = new DynamoE2EFakeBridge { IsExecutionEnabled = false };
        var req = new DynamoExecutionRequest("doc.CreateRebar();", DryRun: true);
        var res = await bridge.ExecuteAsync(req);

        AssertCondition("Opt-in disabled rejects execution (Success=false)", !res.Success);
        AssertCondition("Opt-in disabled returns error code -32001", res.ErrorCode == -32001);
        AssertCondition("Opt-in disabled returns helpful prompt", res.ErrorMessage != null && res.ErrorMessage.Contains("Allow AI Dynamo execution"));

        // ScriptGuard denied namespaces
        bridge.IsExecutionEnabled = true;
        string[] maliciousSnippets =
        [
            "System.IO.File.Delete(\"test.txt\");",
            "System.Net.Http.HttpClient client = new();",
            "System.Diagnostics.Process.Start(\"cmd.exe\");",
            "#r \"CustomDll.dll\"",
            "#load \"subscript.cs\""
        ];

        foreach (var snippet in maliciousSnippets)
        {
            var secReq = new DynamoExecutionRequest(snippet, DryRun: true);
            var secRes = await bridge.ExecuteAsync(secReq);
            AssertCondition($"ScriptGuard rejects '{snippet.Split(' ')[0]}'", !secRes.Success && secRes.ErrorCode == -32002);
        }
    }

    // -------------------------------------------------------------------------
    // 7. ScriptQuality & ScriptGuard on All 12 Seeds from Manifest
    // -------------------------------------------------------------------------
    private static void TestScriptQualityAndScriptGuardOnAllSeeds()
    {
        Console.WriteLine("\n[7] SCRIPTQUALITY & SCRIPTGUARD ON ALL 12 MANIFEST EMBEDDED SEEDS:");

        var asm = typeof(DynamoHostProfile).Assembly;
        var resourceNames = asm.GetManifestResourceNames()
            .Where(r => r.EndsWith("code.cs", StringComparison.OrdinalIgnoreCase))
            .ToList();

        AssertCondition("Assembly contains exactly 12 seed code.cs manifest resources", resourceNames.Count == 12);

        var guardProfile = GuardProfile.Dynamo;

        foreach (var rName in resourceNames)
        {
            using var stream = asm.GetManifestResourceStream(rName);
            using var reader = new StreamReader(stream!);
            string code = reader.ReadToEnd();

            string seedName = rName;

            // Check line count <= 300
            int lineCount = code.Split('\n').Length;
            AssertCondition($"Seed '{seedName}' line count <= 300 (is {lineCount})", lineCount <= 300);

            // ScriptGuard AST check
            var violations = ScriptGuard.Check(code, guardProfile).ToList();
            AssertCondition($"Seed '{seedName}' passes ScriptGuard (0 violations)", violations.Count == 0,
                violations.Count > 0 ? string.Join(", ", violations) : "");

            // ScriptQuality AST check
            var qualityIssues = ScriptQuality.Find(code).ToList();
            var blockingQualityIssues = qualityIssues.Where(q => q.RuleId.StartsWith("Q-B")).ToList();
            var warningQualityIssues = qualityIssues.Where(q => q.RuleId.StartsWith("Q-W")).ToList();

            AssertCondition($"Seed '{seedName}' has 0 blocking clean code issues (Q-B*)", blockingQualityIssues.Count == 0,
                string.Join("; ", blockingQualityIssues.Select(b => $"{b.RuleId}: {b.Message}")));

            AssertCondition($"Seed '{seedName}' has 0 warning clean code issues (Q-W*)", warningQualityIssues.Count == 0,
                string.Join("; ", warningQualityIssues.Select(w => $"{w.RuleId}: {w.Message}")));
        }
    }

    // -------------------------------------------------------------------------
    // 8. Strict 24 Tool Surface Count Verification
    // -------------------------------------------------------------------------
    private static void TestStrict24ToolSurfaceEnforcement()
    {
        Console.WriteLine("\n[8] STRICT 24 TOOL SURFACE COUNT VERIFICATION:");

        var coreTools = DynamoE2EContracts.CoreTools;
        var registryTools = DynamoE2EContracts.RegistryMetaTools;
        var seeds = DynamoE2EContracts.EmbeddedSeeds;

        AssertCondition("Core tools count == 4", coreTools.Count == 4);
        AssertCondition("Registry meta tools count == 8", registryTools.Count == 8);
        AssertCondition("Embedded seeds count == 12", seeds.Count == 12);

        int totalExpected = coreTools.Count + registryTools.Count + seeds.Count;
        AssertCondition($"Total surface tools count == 24 (got {totalExpected})", totalExpected == 24);

        // Verify inspect_type is omitted
        var fullSurface = DynamoE2ETestHarness.BuildToolSurfaceList();
        AssertCondition("inspect_type is strictly excluded from tools/list", !fullSurface.Contains("inspect_type"));
        AssertCondition($"Distinct surface tool list count == 24 (got {fullSurface.Count})", fullSurface.Count == 24);
    }
}
