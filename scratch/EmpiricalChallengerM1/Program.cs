using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.McpBridge.Core.Scripting;

namespace EmpiricalChallengerM1;

public static class Program
{
    private static int _failures = 0;
    private static int _passes = 0;

    public static int Main()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("TEAMWORK PREVIEW CHALLENGER M1.2: EMPIRICAL VERIFICATION HARNESS");
        Console.WriteLine("================================================================================");

        Test1_AssemblyReferences();
        Test2_ContextSerializationHostIsolation();
        Test3_GuardProfileDynamoEnforcement();
        Test4_AnalyzerProfileDynamoAST();
        Test5_PipeNamingAndWireContracts();
        Test6_HostProfileDynamoMetadata();

        Console.WriteLine("\n================================================================================");
        Console.WriteLine($"EMPIRICAL VERIFICATION COMPLETE: {_passes} PASSED, {_failures} FAILED");
        Console.WriteLine("================================================================================");

        return _failures == 0 ? 0 : 1;
    }

    private static void AssertTrue(bool condition, string testName, string detail)
    {
        if (condition)
        {
            _passes++;
            Console.WriteLine($"  [PASS] {testName}: {detail}");
        }
        else
        {
            _failures++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [FAIL] {testName}: {detail}");
            Console.ResetColor();
        }
    }

    private static void Test1_AssemblyReferences()
    {
        Console.WriteLine("\n--- TEST SUITE 1: Assembly Reference Inspection (0 Host Runtime References) ---");

        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        Console.WriteLine($"Repo root: {repoRoot}");

        var binaries = new[]
        {
            Path.Combine(repoRoot, "McpShared/HPRebar.Mcp.Contracts/bin/Release/netstandard2.0/HPRebar.Mcp.Contracts.dll"),
            Path.Combine(repoRoot, "McpShared/HPRebar.Mcp.Contracts/bin/Release/net48/HPRebar.Mcp.Contracts.dll"),
            Path.Combine(repoRoot, "McpShared/HPRebar.Mcp.Contracts/bin/Debug/netstandard2.0/HPRebar.Mcp.Contracts.dll"),
            Path.Combine(repoRoot, "McpShared/HPRebar.Mcp.Contracts/bin/Debug/net48/HPRebar.Mcp.Contracts.dll"),
            Path.Combine(repoRoot, "McpShared/HPRebar.McpBridge.Core/bin/Release/net8.0/HPRebar.McpBridge.Core.dll"),
            Path.Combine(repoRoot, "McpShared/HPRebar.McpBridge.Core/bin/Release/net48/HPRebar.McpBridge.Core.dll"),
            Path.Combine(repoRoot, "McpShared/HPRebar.McpBridge.Core/bin/Debug/net8.0/HPRebar.McpBridge.Core.dll"),
            Path.Combine(repoRoot, "McpShared/HPRebar.McpBridge.Core/bin/Debug/net48/HPRebar.McpBridge.Core.dll"),
            Path.Combine(repoRoot, "McpShared/HPRebar.Mcp.Server.Core/bin/Release/net10.0/HPRebar.Mcp.Server.Core.dll"),
            Path.Combine(repoRoot, "McpShared/HPRebar.Mcp.Server.Core/bin/Debug/net10.0/HPRebar.Mcp.Server.Core.dll"),
        };

        var forbiddenPrefixes = new[]
        {
            "Dynamo", "DesignScript", "DSCore", "RevitNodes",
            "RevitAPI", "RevitAPIUI", "RevitServices", "Autodesk.Revit",
            "AcDbMgd", "AcMgd", "AcCoreMgd", "Autodesk.AutoCAD",
            "AeccDbMgd", "Autodesk.Civil",
            "Autodesk.Navisworks", "Navisworks",
            "ETABSv1", "SAP2000v1", "RobotOM",
            "Tekla.Structures",
            "Microsoft.Office.Interop.Excel",
            "Microsoft.AnalysisServices",
        };

        foreach (var binaryPath in binaries)
        {
            if (!File.Exists(binaryPath))
            {
                Console.WriteLine($"  [SKIP] File not found: {Path.GetFileName(Path.GetDirectoryName(binaryPath))}/{Path.GetFileName(binaryPath)}");
                continue;
            }

            var relName = Path.GetRelativePath(repoRoot, binaryPath);
            using var fileStream = File.OpenRead(binaryPath);
            using var peReader = new PEReader(fileStream);
            var metadataReader = peReader.GetMetadataReader();

            var refNames = new List<string>();
            foreach (var handle in metadataReader.AssemblyReferences)
            {
                var refObj = metadataReader.GetAssemblyReference(handle);
                var name = metadataReader.GetString(refObj.Name);
                refNames.Add(name);
            }

            var forbiddenHits = new List<string>();
            foreach (var refName in refNames)
            {
                foreach (var forbidden in forbiddenPrefixes)
                {
                    if (refName.StartsWith(forbidden, StringComparison.OrdinalIgnoreCase))
                    {
                        forbiddenHits.Add($"{refName} (matched {forbidden})");
                    }
                }
            }

            AssertTrue(forbiddenHits.Count == 0,
                $"Binary {relName}",
                forbiddenHits.Count == 0
                    ? $"0 forbidden references out of {refNames.Count} referenced assemblies: [{string.Join(", ", refNames)}]"
                    : $"FAILED: Forbidden references found: [{string.Join(", ", forbiddenHits)}]");
        }
    }

    private static void Test2_ContextSerializationHostIsolation()
    {
        Console.WriteLine("\n--- TEST SUITE 2: Cross-Host Context JSON Serialization Leakage ---");

        static bool ContainsDynamoKey(string json)
        {
            using var doc = JsonDocument.Parse(json);
            return HasKeyRecursive(doc.RootElement, "dynamo");
        }

        static bool HasKeyRecursive(JsonElement element, string targetKey)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in element.EnumerateObject())
                {
                    if (string.Equals(prop.Name, targetKey, StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (HasKeyRecursive(prop.Value, targetKey))
                        return true;
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    if (HasKeyRecursive(item, targetKey))
                        return true;
                }
            }
            return false;
        }

        // Host 1: Generic empty context
        var emptyContext = new ContextResult();
        var emptyJson = BridgeJson.Serialize(emptyContext);
        AssertTrue(!ContainsDynamoKey(emptyJson), "Empty Context Serialization", "Never contains 'dynamo' field");

        // Host 2: Revit
        var revitContext = new ContextResult
        {
            RevitVersion = "2026",
            DocTitle = "Project1.rvt",
            DocPath = @"C:\Project1.rvt",
            ActiveView = new ViewInfo(101, "{3D}", "ThreeD"),
            Selection = new[] { new ElementInfo(202, "OST_Columns", "C1") },
        };
        var revitJson = BridgeJson.Serialize(revitContext);
        AssertTrue(!ContainsDynamoKey(revitJson), "Revit Context Serialization", "Omitted 'dynamo' field for Revit context");

        // Host 3: AutoCAD
        var acadContext = new ContextResult
        {
            DocTitle = "Drawing1.dwg",
            Autocad = new AutocadInfo("4", "1", "Model", "0", true, true, true),
        };
        var acadJson = BridgeJson.Serialize(acadContext);
        AssertTrue(!ContainsDynamoKey(acadJson), "AutoCAD Context Serialization", "Omitted 'dynamo' field for AutoCAD context");

        // Host 4: Civil 3D
        var civilContext = new ContextResult
        {
            DocTitle = "CivilSite.dwg",
            Civil3d = new Civil3dInfo("Civil3D", true, "Meters", "VN2000", false, 3, 2, 1, 5, 10, 50),
        };
        var civilJson = BridgeJson.Serialize(civilContext);
        AssertTrue(!ContainsDynamoKey(civilJson), "Civil 3D Context Serialization", "Omitted 'dynamo' field for Civil3d context");

        // Host 5: Navisworks
        var navisContext = new ContextResult
        {
            DocTitle = "Federated.nwd",
            Navis = new NavisInfo("Meters", 1, Array.Empty<ModelSummary>(), 2, 3, 0, false, false, true, false, false),
        };
        var navisJson = BridgeJson.Serialize(navisContext);
        AssertTrue(!ContainsDynamoKey(navisJson), "Navisworks Context Serialization", "Omitted 'dynamo' field for Navisworks context");

        // Host 6: ETABS
        var etabsContext = new ContextResult
        {
            DocTitle = "Tower.edb",
            Etabs = new EtabsInfo(true, 1234, "22.0.0", false, "kN, mm, C", "kN, mm, C", false, 30, 120, 45),
        };
        var etabsJson = BridgeJson.Serialize(etabsContext);
        AssertTrue(!ContainsDynamoKey(etabsJson), "ETABS Context Serialization", "Omitted 'dynamo' field for ETABS context");

        // Host 7: SAP2000
        var sapContext = new ContextResult
        {
            DocTitle = "Bridge.sdb",
            Sap2000 = new Sap2000Info(true, 5678, "27.0.0", false, "kN_m_C", "kN_m_C", false, 500, 1200, 40),
        };
        var sapJson = BridgeJson.Serialize(sapContext);
        AssertTrue(!ContainsDynamoKey(sapJson), "SAP2000 Context Serialization", "Omitted 'dynamo' field for SAP2000 context");

        // Host 8: Power BI
        var pbiContext = new ContextResult
        {
            DocTitle = "Report.pbix",
            PowerBi = new PowerBiInfo(true, 9012, 51234, "FinancialModel", "1600", false, 25, 180, 45),
        };
        var pbiJson = BridgeJson.Serialize(pbiContext);
        AssertTrue(!ContainsDynamoKey(pbiJson), "Power BI Context Serialization", "Omitted 'dynamo' field for Power BI context");

        // Host 9: Excel
        var excelContext = new ContextResult
        {
            DocTitle = "Calculation.xlsx",
            Excel = new ExcelInfo(true, 3456, "16.0", "Book1.xlsx", "Sheet1", "A1:B10", true, false, 1, 3, true),
        };
        var excelJson = BridgeJson.Serialize(excelContext);
        AssertTrue(!ContainsDynamoKey(excelJson), "Excel Context Serialization", "Omitted 'dynamo' field for Excel context");

        // Host 10: Robot
        var robotContext = new ContextResult
        {
            DocTitle = "Structure.rtd",
            Robot = new RobotInfo(true, 7890, "39.0", "Frame3D", true, false, 50, 120, 15, 6),
        };
        var robotJson = BridgeJson.Serialize(robotContext);
        AssertTrue(!ContainsDynamoKey(robotJson), "Robot Context Serialization", "Omitted 'dynamo' field for Robot context");

        // Host 11: Tekla
        var teklaContext = new ContextResult
        {
            DocTitle = "StandardModel",
            Tekla = new TeklaInfo(true, "StandardModel", @"C:\Tekla\Model", "ProjectX", "2025.0", false, 350, 45, 12),
        };
        var teklaJson = BridgeJson.Serialize(teklaContext);
        AssertTrue(!ContainsDynamoKey(teklaJson), "Tekla Context Serialization", "Omitted 'dynamo' field for Tekla context");

        // Target Host: Dynamo Context Serialization
        var dynamoContext = new ContextResult
        {
            DocTitle = "RevitHostProject.rvt",
            Dynamo = new DynamoInfo(
                IsLive: true,
                DynamoVersion: "3.7.0.1234",
                ActiveWorkspaceName: "AutomatedRebarGraph",
                ActiveWorkspacePath: @"C:\Graphs\AutomatedRebarGraph.dyn",
                RunType: "Manual",
                NodeCount: 42,
                WireCount: 58,
                SelectedNodeCount: 3,
                HasUnsavedChanges: true)
        };
        var dynamoJson = BridgeJson.Serialize(dynamoContext);
        AssertTrue(ContainsDynamoKey(dynamoJson), "Dynamo Context Serialization", "Contains 'dynamo' field when DynamoInfo populated");

        // Round-trip deserialization check
        var roundTrip = BridgeJson.Deserialize<ContextResult>(dynamoJson);
        AssertTrue(roundTrip != null && roundTrip.Dynamo != null && roundTrip.Dynamo.NodeCount == 42 && roundTrip.Dynamo.IsLive,
            "Dynamo Context Round-trip",
            "Deserialized DynamoInfo fields perfectly match original record");
    }

    private static void Test3_GuardProfileDynamoEnforcement()
    {
        Console.WriteLine("\n--- TEST SUITE 3: ScriptGuard & GuardProfile.Dynamo Enforcement ---");

        var profile = GuardProfile.Dynamo;

        // Adversarial Script 1: TaskDialog.Show
        var script1 = "TaskDialog.Show(\"Title\", \"Message\");";
        var r1 = ScriptGuard.Check(script1, profile);
        AssertTrue(r1.Count > 0 && r1.Any(e => e.Message.Contains("TaskDialog")), "Adversarial: TaskDialog.Show", $"Blocked: {string.Join("; ", r1.Select(x => x.Message))}");

        // Adversarial Script 2: MessageBox.Show
        var script2 = "MessageBox.Show(\"Freezes Revit thread\");";
        var r2 = ScriptGuard.Check(script2, profile);
        AssertTrue(r2.Count > 0 && r2.Any(e => e.Message.Contains("MessageBox")), "Adversarial: MessageBox.Show", $"Blocked: {string.Join("; ", r2.Select(x => x.Message))}");

        // Adversarial Script 3: Application shutdown
        var script3 = "app.Exit();";
        var r3 = ScriptGuard.Check(script3, profile);
        AssertTrue(r3.Count > 0 && r3.Any(e => e.Message.Contains("Exit")), "Adversarial: app.Exit()", $"Blocked: {string.Join("; ", r3.Select(x => x.Message))}");

        // Adversarial Script 4: Forbidden namespace System.IO
        var script4 = "System.IO.File.Delete(\"C:\\important.dyn\");";
        var r4 = ScriptGuard.Check(script4, profile);
        AssertTrue(r4.Count > 0 && r4.Any(e => e.Message.Contains("System.IO")), "Adversarial: System.IO.File.Delete", $"Blocked: {string.Join("; ", r4.Select(x => x.Message))}");

        // Adversarial Script 5: Process execution
        var script5 = "System.Diagnostics.Process.Start(\"cmd.exe\");";
        var r5 = ScriptGuard.Check(script5, profile);
        AssertTrue(r5.Count > 0 && r5.Any(e => e.Message.Contains("System.Diagnostics.Process")), "Adversarial: Process.Start", $"Blocked: {string.Join("; ", r5.Select(x => x.Message))}");

        // Adversarial Script 6: Direct assembly load trivia #r
        var script6 = "#r \"C:\\malicious.dll\"\nreturn 1;";
        var r6 = ScriptGuard.Check(script6, profile);
        AssertTrue(r6.Count > 0 && r6.Any(e => e.Message.Contains("#r")), "Adversarial: Directive #r", $"Blocked: {string.Join("; ", r6.Select(x => x.Message))}");

        // Adversarial Script 7: Direct script load trivia #load
        var script7 = "#load \"script.cs\"\nreturn 1;";
        var r7 = ScriptGuard.Check(script7, profile);
        AssertTrue(r7.Count > 0 && r7.Any(e => e.Message.Contains("#load")), "Adversarial: Directive #load", $"Blocked: {string.Join("; ", r7.Select(x => x.Message))}");

        // Adversarial Script 8: global:: namespace bypass attempt
        var script8 = "global::System.IO.File.ReadAllText(\"some.txt\");";
        var r8 = ScriptGuard.Check(script8, profile);
        AssertTrue(r8.Count > 0 && r8.Any(e => e.Message.Contains("System.IO")), "Adversarial: global::System.IO bypass", $"Blocked: {string.Join("; ", r8.Select(x => x.Message))}");

        // Adversarial Script 9: Touching HPDynamo.McpBridge internal namespaces
        var script9 = "var x = new HPDynamo.McpBridge.Coordinator.DynamoBridgeCoordinator();";
        var r9 = ScriptGuard.Check(script9, profile);
        AssertTrue(r9.Count > 0 && r9.Any(e => e.Message.Contains("HPDynamo.McpBridge")), "Adversarial: HPDynamo.McpBridge internal access", $"Blocked: {string.Join("; ", r9.Select(x => x.Message))}");

        // Legitimate Dynamo Script
        var legitScript = "var pt = Autodesk.DesignScript.Geometry.Point.ByCoordinates(0, 0, 0); log($\"Created point {pt}\"); return pt;";
        var rLegit = ScriptGuard.Check(legitScript, profile);
        AssertTrue(rLegit.Count == 0, "Legitimate Dynamo Script", "DesignScript geometry passed AST guard checks cleanly");
    }

    private static void Test4_AnalyzerProfileDynamoAST()
    {
        Console.WriteLine("\n--- TEST SUITE 4: AnalyzerProfile.Dynamo Transaction AST Analysis ---");

        var profile = AnalyzerProfile.Dynamo;

        // Test 1: Code with Transaction
        var codeWithTx = "using (var tx = new Transaction(doc, \"Create\")) { tx.Start(); tx.Commit(); }";
        var a1 = ScriptAnalyzer.Analyze(codeWithTx, profile);
        AssertTrue(a1.UsesTransaction, "AST Analyzer: new Transaction", "Correctly identified Transaction creation");

        // Test 2: Code with TransactionGroup
        var codeWithTg = "using (var tg = new TransactionGroup(doc, \"Group\")) { tg.Start(); tg.Assimilate(); }";
        var a2 = ScriptAnalyzer.Analyze(codeWithTg, profile);
        AssertTrue(a2.UsesTransaction, "AST Analyzer: new TransactionGroup", "Correctly identified TransactionGroup creation");

        // Test 3: Code with SubTransaction
        var codeWithSub = "using (var sub = new SubTransaction(doc)) { sub.Start(); sub.Commit(); }";
        var a3 = ScriptAnalyzer.Analyze(codeWithSub, profile);
        AssertTrue(a3.UsesTransaction, "AST Analyzer: new SubTransaction", "Correctly identified SubTransaction creation");

        // Test 4: Pure query code
        var queryCode = "var count = workspace.Nodes.Count(); return count;";
        var a4 = ScriptAnalyzer.Analyze(queryCode, profile);
        AssertTrue(!a4.UsesTransaction, "AST Analyzer: Pure query code", "Correctly reported 0 transactions used");
    }

    private static void Test5_PipeNamingAndWireContracts()
    {
        Console.WriteLine("\n--- TEST SUITE 5: Pipe Naming & Wire Contracts ---");

        AssertTrue(PipeNaming.DynamoHost == "dynamo", "PipeNaming.DynamoHost", "Constant is exactly 'dynamo'");
        AssertTrue(PipeNaming.For("dynamo", 2026) == "hpdynamo-mcp-2026", "PipeNaming.For('dynamo', 2026)", "Returns 'hpdynamo-mcp-2026'");
        AssertTrue(PipeNaming.For(PipeNaming.DynamoHost, 2026) == "hpdynamo-mcp-2026", "PipeNaming.For(PipeNaming.DynamoHost, 2026)", "Matches constant");

        AssertTrue(JsonRpcMethods.DynamoPrefix == "dynamo.", "JsonRpcMethods.DynamoPrefix", "Constant is exactly 'dynamo.'");
        AssertTrue(JsonRpcMethods.For(JsonRpcMethods.DynamoPrefix, "execute") == "dynamo.execute", "JsonRpcMethods.For(prefix, 'execute')", "Produces 'dynamo.execute'");
        AssertTrue(JsonRpcMethods.Suffix("dynamo.execute") == "execute", "JsonRpcMethods.Suffix('dynamo.execute')", "Splits method suffix 'execute'");
        AssertTrue(JsonRpcMethods.Suffix("dynamo.context") == "context", "JsonRpcMethods.Suffix('dynamo.context')", "Splits method suffix 'context'");

        // Verify No pipe collisions across hosts
        var hosts = new[] { "revit", "autocad", "navis", "etabs", "civil3d", "sap2000", "powerbi", "excel", "robot", "tekla", "dynamo" };
        var pipeNames = new HashSet<string>();
        foreach (var h in hosts)
        {
            var p = PipeNaming.For(h, 2026);
            AssertTrue(pipeNames.Add(p), $"Unique Pipe for {h}", $"Pipe name '{p}' is completely unique across all hosts");
        }
    }

    private static void Test6_HostProfileDynamoMetadata()
    {
        Console.WriteLine("\n--- TEST SUITE 6: HostProfile.Dynamo Specification Compliance ---");

        var profile = HostProfile.Dynamo;
        AssertTrue(profile.HostId == "dynamo", "HostProfile.Dynamo.HostId", "HostId is 'dynamo'");
        AssertTrue(profile.DefaultVersion == 2026, "HostProfile.Dynamo.DefaultVersion", "DefaultVersion is 2026");
        AssertTrue(profile.MaxTimeoutSeconds == 600, "HostProfile.Dynamo.MaxTimeoutSeconds", "MaxTimeoutSeconds is 600");
        AssertTrue(profile.ExecuteToolName == "execute_dynamo_code", "HostProfile.Dynamo.ExecuteToolName", "Tool is execute_dynamo_code");
        AssertTrue(profile.ContextToolName == "get_dynamo_context", "HostProfile.Dynamo.ContextToolName", "Tool is get_dynamo_context");
        AssertTrue(profile.MethodPrefix == "dynamo.", "HostProfile.Dynamo.MethodPrefix", "MethodPrefix is 'dynamo.'");
        AssertTrue(profile.ProductFolder == "HPDynamo", "HostProfile.Dynamo.ProductFolder", "ProductFolder is 'HPDynamo'");
        AssertTrue(profile.EnvPrefix == "HPDYNAMO_MCP_", "HostProfile.Dynamo.EnvPrefix", "EnvPrefix is 'HPDYNAMO_MCP_'");

        var expectedCoreTools = new[] { "execute_dynamo_code", "execute_designscript", "get_dynamo_context", "cancel_execution" };
        var actualCoreTools = profile.CoreToolNames.ToArray();
        AssertTrue(expectedCoreTools.SequenceEqual(actualCoreTools),
            "HostProfile.Dynamo.CoreToolNames",
            $"Core tools strictly match [{string.Join(", ", actualCoreTools)}]");
    }
}
