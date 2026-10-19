using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using HPDynamo.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Registry;
using HPRebar.McpBridge.Core.Scripting;

namespace M4Challenge;

public static class Program
{
    private static readonly string[] ExpectedSeeds =
    [
        "Graph/run_dyn_file",
        "Workspace/get_active_graph_info",
        "Library/search_nodes_library",
        "Library/inspect_node_type",
        "Node/add_node_to_workspace",
        "Wire/connect_node_ports",
        "Wire/disconnect_node_ports",
        "Node/set_node_properties",
        "Data/get_node_output_values",
        "Export/export_graph_to_image",
        "Graph/validate_dyn_file",
        "CodeBlock/create_code_block_node"
    ];

    public static int Main()
    {
        Console.WriteLine("======================================================================");
        Console.WriteLine("   M4 FORENSIC AUDIT & EMPIRICAL CHALLENGE — HPDynamo MCP SEEDS       ");
        Console.WriteLine("======================================================================");

        var failures = new List<string>();
        var serverAssembly = typeof(DynamoHostProfile).Assembly;

        // ------------------------------------------------------------------
        // TEST 1: MANIFEST RESOURCE INSPECTION
        // ------------------------------------------------------------------
        Console.WriteLine("\n[1] MANIFEST RESOURCE INSPECTION:");
        var resourceNames = serverAssembly.GetManifestResourceNames();
        Console.WriteLine($"Total manifest resources: {resourceNames.Length}");

        var seedResources = resourceNames.Where(r => r.StartsWith(SeedInstaller.ResourcePrefix, StringComparison.Ordinal)).ToList();
        Console.WriteLine($"Resources starting with '{SeedInstaller.ResourcePrefix}': {seedResources.Count}");

        if (seedResources.Count != 36)
        {
            failures.Add($"Expected exactly 36 SeedLibrary resources, found {seedResources.Count}");
        }

        foreach (var r in seedResources.OrderBy(r => r))
        {
            Console.WriteLine($"  -> {r}");
        }

        // Verify each expected seed triad
        foreach (var expected in ExpectedSeeds)
        {
            var parts = expected.Split('/');
            var cat = parts[0];
            var name = parts[1];

            string[] triadFiles = [ToolLibraryStore.ToolFile, ToolLibraryStore.CodeFile, ToolLibraryStore.ExamplesFile];
            foreach (var tf in triadFiles)
            {
                var expectedResourceKey1 = $"{SeedInstaller.ResourcePrefix}{cat}/{name}/{tf}";
                var expectedResourceKey2 = $"{SeedInstaller.ResourcePrefix}{cat}\\{name}\\{tf}";
                bool exists = seedResources.Any(r => r == expectedResourceKey1 || r == expectedResourceKey2);
                if (!exists)
                {
                    failures.Add($"Missing manifest resource: {expectedResourceKey1}");
                }
            }
        }

        // Check for statically compiled types from SeedLibrary
        Console.WriteLine("\n[2] STATIC TYPE COMPILATION VERIFICATION:");
        var types = serverAssembly.GetTypes();
        Console.WriteLine($"Total types defined in HPDynamo.Mcp.Server.dll: {types.Length}");
        foreach (var t in types)
        {
            Console.WriteLine($"  Defined type: {t.FullName}");
            // Verify no seed scripts were compiled into types
            foreach (var s in ExpectedSeeds)
            {
                var seedName = s.Split('/')[1];
                if (t.Name.Contains(seedName, StringComparison.OrdinalIgnoreCase))
                {
                    failures.Add($"Type '{t.FullName}' appears to be compiled statically from seed '{s}'!");
                }
            }
        }
        Console.WriteLine("Confirmed: No seed script classes exist in assembly type definitions.");

        // ------------------------------------------------------------------
        // TEST 3: SEED LOADING & EMBEDDED CONTENT
        // ------------------------------------------------------------------
        Console.WriteLine("\n[3] SEED LOADING VIA SeedInstaller:");
        var loadedSeeds = SeedInstaller.LoadSeeds(serverAssembly);
        Console.WriteLine($"Successfully loaded seeds count: {loadedSeeds.Count}");
        if (loadedSeeds.Count != 12)
        {
            failures.Add($"Expected 12 loaded seeds, got {loadedSeeds.Count}");
        }

        // ------------------------------------------------------------------
        // TEST 4: AST QUALITY CHECKS (ScriptQuality.Find)
        // ------------------------------------------------------------------
        Console.WriteLine("\n[4] AST SCRIPT QUALITY AUDIT (ScriptQuality.Find):");
        foreach (var seed in loadedSeeds)
        {
            var key = $"{seed.Category}/{seed.Name}";
            var findings = ScriptQuality.Find(seed.Code);
            var lineCount = ScriptQuality.LineCount(seed.Code);
            Console.WriteLine($"\n--- Seed: {key} ({lineCount} lines) ---");

            var errors = findings.Where(f => f.Severity == ScriptQuality.Error).ToList();
            var warnings = findings.Where(f => f.Severity == ScriptQuality.Warning).ToList();

            if (lineCount > ScriptQuality.MaxScriptLines)
            {
                failures.Add($"Seed {key} exceeds max lines: {lineCount} > {ScriptQuality.MaxScriptLines}");
            }

            if (errors.Count > 0)
            {
                foreach (var err in errors)
                {
                    Console.WriteLine($"  [ERROR] {err.RuleId} Line {err.Line}:{err.Column} - {err.Message}");
                    failures.Add($"AST Error in {key}: {err.RuleId} ({err.Message})");
                }
            }
            else
            {
                Console.WriteLine("  [OK] 0 AST Errors (Q-B1, Q-B2, Q-B3 clean)");
            }

            if (warnings.Count > 0)
            {
                foreach (var w in warnings)
                {
                    Console.WriteLine($"  [WARNING] {w.RuleId} Line {w.Line}:{w.Column} - {w.Message}");
                    failures.Add($"AST Warning in {key}: {w.RuleId} ({w.Message})");
                }
            }
            else
            {
                Console.WriteLine("  [OK] 0 AST Warnings (Q-W3 vague names clean)");
            }
        }

        // ------------------------------------------------------------------
        // TEST 5: SCRIPTGUARD CONFORMANCE (ScriptGuard.Check)
        // ------------------------------------------------------------------
        Console.WriteLine("\n[5] SCRIPTGUARD SECURITY CONFORMANCE (ScriptGuard.Check):");
        foreach (var seed in loadedSeeds)
        {
            var key = $"{seed.Category}/{seed.Name}";
            var guardDiagnostics = ScriptGuard.Check(seed.Code, GuardProfile.Dynamo);
            if (guardDiagnostics.Count > 0)
            {
                foreach (var diag in guardDiagnostics)
                {
                    Console.WriteLine($"  [SECURITY VIOLATION] {key} Line {diag.Line}:{diag.Column} - {diag.Message}");
                    failures.Add($"ScriptGuard violation in {key}: {diag.Message}");
                }
            }
            else
            {
                Console.WriteLine($"  [OK] {key} passes ScriptGuard (0 violations)");
            }
        }

        // ------------------------------------------------------------------
        // TEST 6: ARGUMENTS PARITY & SCHEMA VALIDITY
        // ------------------------------------------------------------------
        Console.WriteLine("\n[6] JSON SCHEMA & EXAMPLES VALIDITY:");
        foreach (var seed in loadedSeeds)
        {
            var key = $"{seed.Category}/{seed.Name}";
            try
            {
                using var toolDoc = JsonDocument.Parse(seed.ToolJson);
                var root = toolDoc.RootElement;
                if (!root.TryGetProperty("name", out var nameProp) || nameProp.GetString() != seed.Name)
                {
                    failures.Add($"tool.json name mismatch in {key}");
                }
                if (!root.TryGetProperty("inputSchema", out _))
                {
                    failures.Add($"tool.json missing inputSchema in {key}");
                }
            }
            catch (Exception ex)
            {
                failures.Add($"tool.json invalid JSON in {key}: {ex.Message}");
            }

            try
            {
                using var exDoc = JsonDocument.Parse(seed.ExamplesJson);
                if (exDoc.RootElement.ValueKind != JsonValueKind.Array || exDoc.RootElement.GetArrayLength() == 0)
                {
                    failures.Add($"examples.json must be non-empty array in {key}");
                }
            }
            catch (Exception ex)
            {
                failures.Add($"examples.json invalid JSON in {key}: {ex.Message}");
            }
        }

        Console.WriteLine("\n======================================================================");
        if (failures.Count == 0)
        {
            Console.WriteLine("  AUDIT VERDICT: 100% PASS — ZERO FAILURES, ZERO VIOLATIONS");
            Console.WriteLine("======================================================================");
            return 0;
        }
        else
        {
            Console.WriteLine($"  AUDIT VERDICT: FAIL — {failures.Count} FAILURE(S) DETECTED:");
            foreach (var f in failures)
            {
                Console.WriteLine($"  * {f}");
            }
            Console.WriteLine("======================================================================");
            return 1;
        }
    }
}
