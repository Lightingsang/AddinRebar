using System.Reflection;
using System.Text.Json;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Services;
using HPRebar.Mcp.Server.Tests.Fakes;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     Challenger 2 Empirical Verification Suite:
///     1. Strict Host Neutrality across ALL 8 hosts (zero leaked host assemblies).
///     2. Complete Regression Resistance & Bijective Mapping for Sibling Host Pipe/Prefixes.
///     3. AnalyzerProfile.Excel semantic behavior (zero false transaction flags).
///     4. Multi-Host Wire Isolation in ContextResult (zero cross-host leakage).
///     5. GuardProfile.Excel boundary & evasion stress testing.
/// </summary>
public sealed class ExcelMilestone1Challenger2Tests
{
    // =========================================================================
    // 1. HOST NEUTRALITY & ZERO-HOST-API LEAKAGE ACROSS ALL 8 HOSTS
    // =========================================================================

    private static readonly string[] ForbiddenHostApiPrefixes =
    [
        // Revit
        "RevitAPI", "RevitAPIUI", "Nice3point",
        // AutoCAD
        "AcDbMgd", "AcMgd", "AcCoreMgd", "Autodesk.AutoCAD",
        // Navisworks
        "Autodesk.Navisworks", "Navisworks",
        // ETABS
        "ETABSv1", "CSi",
        // Civil 3D
        "AeccDbMgd", "AeccPressurePipesMgd", "AecBaseMgd",
        // SAP2000
        "SAP2000v1",
        // Power BI
        "Microsoft.AnalysisServices",
        // Excel
        "Microsoft.Office.Interop.Excel", "ClosedXML", "DocumentFormat.OpenXml", "ExcelDataReader",
        // Robot
        "RobotOM", "Interop.RobotOM"
    ];

    public static IEnumerable<object[]> AllSharedAssemblies() =>
    [
        [typeof(PipeNaming).Assembly, "HPRebar.Mcp.Contracts"],
        [typeof(PipeListener).Assembly, "HPRebar.McpBridge.Core"],
        [typeof(ResultFormatter).Assembly, "HPRebar.Mcp.Server.Core"],
    ];

    [Theory]
    [MemberData(nameof(AllSharedAssemblies))]
    public void McpShared_never_references_any_host_api_across_all_eight_supported_hosts(Assembly assembly, string assemblyName)
    {
        Assert.Equal(assemblyName, assembly.GetName().Name);

        var referencedAssemblies = assembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

        foreach (var forbiddenPrefix in ForbiddenHostApiPrefixes)
        {
            Assert.DoesNotContain(referencedAssemblies, name =>
                name.StartsWith(forbiddenPrefix, StringComparison.OrdinalIgnoreCase) ||
                name.Equals(forbiddenPrefix, StringComparison.OrdinalIgnoreCase));
        }
    }

    // =========================================================================
    // 2. ANALYZER PROFILE EXCEL: ZERO FALSE TRANSACTION REPORTS
    // =========================================================================

    [Fact]
    public void AnalyzerProfile_Excel_has_empty_transaction_types_and_methods()
    {
        Assert.Empty(AnalyzerProfile.Excel.TransactionTypeNames);
        Assert.Empty(AnalyzerProfile.Excel.TransactionMethodNames);
    }

    [Theory]
    [InlineData("var t = new Transaction(); return 1;")]
    [InlineData("using (var t = new Transaction()) { } return 1;")]
    [InlineData("using var t = new Autodesk.Revit.DB.Transaction(); return 1;")]
    [InlineData("doc.StartTransaction(); return 1;")]
    [InlineData("db.TransactionManager.StartTransaction(); return 1;")]
    [InlineData("excel.BeginTransaction(); return 1;")]
    [InlineData("var tx = new FinancialTransaction(); return tx.Amount;")]
    [InlineData("class Transaction { public string Id = \"TX1\"; } return new Transaction().Id;")]
    [InlineData("var list = new List<Transaction>(); return list.Count;")]
    public void Excel_analyzer_profile_never_reports_transaction_usage(string code)
    {
        var result = ScriptAnalyzer.Analyze(code, AnalyzerProfile.Excel);
        Assert.False(result.UsesTransaction, $"AnalyzerProfile.Excel incorrectly reported UsesTransaction=true for: {code}");
    }

    [Fact]
    public void Same_transaction_syntax_triggers_in_revit_and_autocad_but_is_ignored_in_excel()
    {
        const string revitCode = "using (var t = new Transaction()) { } return 1;";
        const string autocadCode = "using (var tr = tm.StartTransaction()) { } return 1;";

        // Revit detects constructor
        Assert.True(ScriptAnalyzer.Analyze(revitCode, AnalyzerProfile.Revit).UsesTransaction);
        Assert.False(ScriptAnalyzer.Analyze(revitCode, AnalyzerProfile.Excel).UsesTransaction);

        // AutoCAD detects method
        Assert.True(ScriptAnalyzer.Analyze(autocadCode, AnalyzerProfile.Autocad).UsesTransaction);
        Assert.False(ScriptAnalyzer.Analyze(autocadCode, AnalyzerProfile.Excel).UsesTransaction);

        // Civil 3D detects method
        Assert.True(ScriptAnalyzer.Analyze(autocadCode, AnalyzerProfile.Civil3d).UsesTransaction);
        Assert.False(ScriptAnalyzer.Analyze(autocadCode, AnalyzerProfile.Excel).UsesTransaction);
    }

    // =========================================================================
    // 3. PIPE NAMING REGRESSION RESISTANCE ACROSS ALL 8 SIBLING HOSTS
    // =========================================================================

    [Theory]
    [InlineData("revit", 2026, "hprebar-mcp-r2026")]
    [InlineData("REVIT", 2025, "hprebar-mcp-r2025")]
    [InlineData("autocad", 2026, "hpautocad-mcp-2026")]
    [InlineData("AutoCAD", 2026, "hpautocad-mcp-2026")]
    [InlineData("navis", 2026, "hpnavis-mcp-2026")]
    [InlineData("NAVIS", 2026, "hpnavis-mcp-2026")]
    [InlineData("etabs", 22, "hpetabs-mcp-22")]
    [InlineData("ETABS", 22, "hpetabs-mcp-22")]
    [InlineData("civil3d", 2026, "hpcivil3d-mcp-2026")]
    [InlineData("Civil3D", 2026, "hpcivil3d-mcp-2026")]
    [InlineData("sap2000", 27, "hpsap2000-mcp-27")]
    [InlineData("SAP2000", 27, "hpsap2000-mcp-27")]
    [InlineData("powerbi", 2026, "hppowerbi-mcp-2026")]
    [InlineData("PowerBI", 2026, "hppowerbi-mcp-2026")]
    [InlineData("excel", 2026, "hpexcel-mcp-2026")]
    [InlineData("EXCEL", 2026, "hpexcel-mcp-2026")]
    [InlineData("robot", 2026, "hprobot-mcp-2026")]
    [InlineData("ROBOT", 2026, "hprobot-mcp-2026")]
    public void PipeNaming_produces_distinct_deterministic_pipes_across_all_hosts(string host, int version, string expectedPipe)
    {
        Assert.Equal(expectedPipe, PipeNaming.For(host, version));
    }

    [Fact]
    public void PipeNaming_host_constants_are_all_mutually_distinct()
    {
        var constants = new[]
        {
            PipeNaming.AutocadHost,
            PipeNaming.NavisHost,
            PipeNaming.EtabsHost,
            PipeNaming.Civil3dHost,
            PipeNaming.Sap2000Host,
            PipeNaming.PowerBiHost,
            PipeNaming.ExcelHost,
            PipeNaming.RobotHost
        };

        Assert.Equal(constants.Length, constants.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    // =========================================================================
    // 4. JSON-RPC PREFIX REGRESSION RESISTANCE & ISOLATION
    // =========================================================================

    [Fact]
    public void JsonRpc_prefixes_are_all_distinct_and_end_with_period()
    {
        var prefixes = new[]
        {
            JsonRpcMethods.AutocadPrefix,
            JsonRpcMethods.NavisPrefix,
            JsonRpcMethods.EtabsPrefix,
            JsonRpcMethods.Civil3dPrefix,
            JsonRpcMethods.Sap2000Prefix,
            JsonRpcMethods.PowerBiPrefix,
            JsonRpcMethods.ExcelPrefix,
            JsonRpcMethods.RobotPrefix
        };

        Assert.All(prefixes, p => Assert.EndsWith(".", p));
        Assert.Equal(prefixes.Length, prefixes.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [InlineData("autocad.", "execute", "autocad.execute")]
    [InlineData("navis.", "context", "navis.context")]
    [InlineData("etabs.", "ping", "etabs.ping")]
    [InlineData("civil3d.", "analyze", "civil3d.analyze")]
    [InlineData("sap2000.", "cancel", "sap2000.cancel")]
    [InlineData("powerbi.", "execute", "powerbi.execute")]
    [InlineData("excel.", "execute", "excel.execute")]
    [InlineData("excel.", "context", "excel.context")]
    [InlineData("excel.", "ping", "excel.ping")]
    [InlineData("excel.", "analyze", "excel.analyze")]
    [InlineData("robot.", "execute", "robot.execute")]
    [InlineData("robot.", "context", "robot.context")]
    public void JsonRpc_For_and_Suffix_are_bijective_across_hosts(string prefix, string suffix, string expectedFullMethod)
    {
        var built = JsonRpcMethods.For(prefix, suffix);
        Assert.Equal(expectedFullMethod, built);
        Assert.Equal(suffix, JsonRpcMethods.Suffix(built));
    }

    // =========================================================================
    // 5. CONTEXT RESULT WIRE ISOLATION: 8 HOSTS MUTUAL EXCLUSIVITY
    // =========================================================================

    [Fact]
    public void ContextResult_serialized_for_excel_contains_zero_other_host_payloads()
    {
        var result = new ContextResult
        {
            Host = "excel",
            HostVersion = "2026",
            DocTitle = "Book1.xlsx",
            Excel = new ExcelInfo(
                IsAttached: true,
                AttachedPid: 4242,
                ExcelVersion: "16.0",
                ActiveWorkbookName: "Book1.xlsx",
                ActiveWorksheetName: "Summary",
                SelectionAddress: "$A$1:$D$10",
                WriteEnabled: true,
                DestructiveEnabled: false,
                OpenWorkbookCount: 2,
                WorksheetCount: 5,
                HasActiveWorkbook: true)
        };

        var json = BridgeJson.Serialize(result);

        Assert.Contains("\"excel\"", json);
        Assert.DoesNotContain("\"powerbi\"", json);
        Assert.DoesNotContain("\"sap2000\"", json);
        Assert.DoesNotContain("\"civil3d\"", json);
        Assert.DoesNotContain("\"etabs\"", json);
        Assert.DoesNotContain("\"navis\"", json);
        Assert.DoesNotContain("\"autocad\"", json);
        Assert.DoesNotContain("\"robot\"", json);
    }

    [Fact]
    public async Task ContextService_shaped_json_for_excel_strips_revit_fields_completely()
    {
        var pipe = "hprebar-mcp-test-" + Guid.NewGuid().ToString("N");
        var executor = new FakeRevitExecutor
        {
            ContextHandler = _ => new ContextResult
            {
                RevitVersion = "2026",
                Host = "excel",
                HostVersion = "2026",
                DocTitle = "Financials.xlsx",
                DocPath = @"C:\Data\Financials.xlsx",
                Excel = new ExcelInfo(true, 7777, "16.0", "Financials.xlsx", "Q3", "$B$2:$E$10", true, false, 1, 4, true),
            },
        };

        using var listener = new PipeListener(pipe, new RequestDispatcher(executor, new BridgeSettings(), "2026", "Excel"));
        listener.Start();

        var options = Microsoft.Extensions.Options.Options.Create(new BridgeOptions { PipeName = pipe, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 });
        await using var client = new RevitBridgeClient(
            options,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<RevitBridgeClient>.Instance,
            ExcelTestProfile.Excel());

        var jsonText = await new ContextService(client, new ResultFormatter()).ReadAsync(false, TestContext.Current.CancellationToken);

        using var doc = JsonDocument.Parse(jsonText);
        var root = doc.RootElement;

        // Revit fields stripped
        Assert.False(root.TryGetProperty("revitVersion", out _));
        Assert.False(root.TryGetProperty("isFamily", out _));

        // Excel fields intact
        Assert.Equal("excel", root.GetProperty("host").GetString());
        Assert.Equal("2026", root.GetProperty("hostVersion").GetString());
        Assert.True(root.GetProperty("excel").GetProperty("isAttached").GetBoolean());
        Assert.Equal(7777, root.GetProperty("excel").GetProperty("attachedPid").GetInt32());
        Assert.Equal("Financials.xlsx", root.GetProperty("excel").GetProperty("activeWorkbookName").GetString());

        // Zero other host payloads in shaped JSON
        Assert.False(root.TryGetProperty("powerbi", out _));
        Assert.False(root.TryGetProperty("sap2000", out _));
        Assert.False(root.TryGetProperty("civil3d", out _));
        Assert.False(root.TryGetProperty("etabs", out _));
        Assert.False(root.TryGetProperty("navis", out _));
        Assert.False(root.TryGetProperty("autocad", out _));
        Assert.False(root.TryGetProperty("robot", out _));

        await listener.StopAsync();
    }

    [Fact]
    public void ContextResult_serialized_for_powerbi_contains_zero_excel_payload()
    {
        var result = new ContextResult
        {
            Host = "powerbi",
            HostVersion = "2026",
            PowerBi = new PowerBiInfo(true, 1000, 50000, "Model", "1600", true, 5, 10, 4)
        };

        var json = BridgeJson.Serialize(result);
        Assert.Contains("\"powerbi\"", json);
        Assert.DoesNotContain("\"excel\"", json);
    }

    [Fact]
    public void ContextResult_serialized_for_sap2000_contains_zero_excel_payload()
    {
        var result = new ContextResult
        {
            Host = "sap2000",
            HostVersion = "27",
            Sap2000 = new Sap2000Info(true, 2000, "27.0", false, "kN, m, C", "kN, m, C", false, 10, 20, 5)
        };

        var json = BridgeJson.Serialize(result);
        Assert.Contains("\"sap2000\"", json);
        Assert.DoesNotContain("\"excel\"", json);
    }

    // =========================================================================
    // 6. GUARD PROFILE EXCEL: BOUNDARY & EVASION ATTEMPTS
    // =========================================================================

    [Theory]
    [InlineData("excel.Quit();", "Quit")]
    [InlineData("app.Quit();", "Quit")]
    [InlineData("var x = excel; x.Quit();", "Quit")]
    [InlineData("excel.Application.Quit();", "Quit")]
    [InlineData("excel.Workbooks.Application.Quit();", "Quit")]
    [InlineData("excel.ApplicationExit();", "ApplicationExit")]
    [InlineData("var res = excel.InputBox(\"test\");", "InputBox")]
    [InlineData("var f1 = excel.GetOpenFilename();", "GetOpenFilename")]
    [InlineData("var f2 = excel.GetSaveAsFilename();", "GetSaveAsFilename")]
    [InlineData("MessageBox.Show(\"modal\");", "MessageBox")]
    [InlineData("System.Windows.Forms.MessageBox.Show(\"modal\");", "System.Windows.Forms")]
    [InlineData("using HPExcel.McpBridge.Discovery; return 1;", "HPExcel.McpBridge")]
    [InlineData("HPExcel.McpBridge.Safety.ExcelSafetyGuard.Check(); return 1;", "HPExcel.McpBridge")]
    [InlineData("HPRebar.McpBridge.Core.Host.McpBridgeHost.Current.Stop(); return 1;", "HPRebar.McpBridge.Core.Host")]
    public void GuardProfile_Excel_blocks_all_termination_and_modal_threats(string code, string expectedForbiddenTerm)
    {
        var diagnostics = ScriptGuard.Check(code, GuardProfile.Excel);
        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, d => d.Message.Contains(expectedForbiddenTerm, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("var s = excel.Evaluate(\"SUM(A1:A10)\"); return s;")]
    [InlineData("sheet.Range[\"A1\", \"B10\"].Value2 = 123; return true;")]
    [InlineData("var data = new List<double> { 1.5, 2.5, 3.5 }; return data.Sum();")]
    [InlineData("var filename = args.Str(\"file\", \"output.xlsx\"); return filename;")]
    [InlineData("using var wb = new ClosedXML.Excel.XLWorkbook(); return wb.Worksheets.Count;")]
    public void GuardProfile_Excel_allows_benign_computations_and_closedxml(string code)
    {
        var diagnostics = ScriptGuard.Check(code, GuardProfile.Excel);
        Assert.Empty(diagnostics);
    }
}
