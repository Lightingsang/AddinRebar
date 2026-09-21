using System.Text.Json;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.Core.Scripting;
using HPRebar.Mcp.Server.Services;
using Xunit;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     Adversarial challenger stress tests for Milestone M1 (Excel McpShared Extension).
///     Tests boundary cases, syntax attacks, guard bypasses, pipe naming, and wire deserialization.
/// </summary>
public sealed class ExcelMilestone1ChallengerTests
{
    // =========================================================================
    // 1. GUARD BYPASS & ATTACK TESTS
    // =========================================================================

    [Theory]
    [InlineData("excel.Quit();", "Quit")]
    [InlineData("excel?.Quit();", "Quit")]
    [InlineData("Action a = excel.Quit;", "Quit")]
    [InlineData("Action a = () => excel.Quit();", "Quit")]
    [InlineData("app.Quit();", "Quit")]
    [InlineData("app?.Quit();", "Quit")]
    [InlineData("foo.Quit();", "Quit")]
    [InlineData("foo?.Quit();", "Quit")]
    [InlineData("excel.Application.Quit();", "Quit")]
    [InlineData("excel.ApplicationExit();", "ApplicationExit")]
    [InlineData("excel?.ApplicationExit();", "ApplicationExit")]
    [InlineData("excel.InputBox(\"Prompt\");", "InputBox")]
    [InlineData("excel?.InputBox(\"Prompt\");", "InputBox")]
    [InlineData("Microsoft.VisualBasic.Interaction.InputBox(\"Prompt\");", "InputBox")]
    [InlineData("excel.GetOpenFilename();", "GetOpenFilename")]
    [InlineData("excel?.GetOpenFilename();", "GetOpenFilename")]
    [InlineData("excel.GetSaveAsFilename();", "GetSaveAsFilename")]
    [InlineData("excel?.GetSaveAsFilename();", "GetSaveAsFilename")]
    [InlineData("MessageBox.Show(\"hacked\");", "MessageBox")]
    [InlineData("System.Windows.Forms.MessageBox.Show(\"hacked\");", "System.Windows.Forms")]
    [InlineData("System.Windows.Forms.Form f = null;", "System.Windows.Forms")]
    public void Forbidden_members_and_dialogs_are_denied_across_receivers_and_null_conditionals(string code, string expectedIdentifier)
    {
        var diagnostics = ScriptGuard.Check(code, GuardProfile.Excel);
        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, d => d.Message.Contains(expectedIdentifier, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("global::HPExcel.McpBridge.BridgeEntry.Stop();", "HPExcel.McpBridge")]
    [InlineData("global::HPExcel.McpBridge.Internal.Secret.Steal();", "HPExcel.McpBridge")]
    [InlineData("global::HPRebar.McpBridge.Core.Host.McpBridgeHost.Current.Stop();", "HPRebar.McpBridge.Core.Host")]
    [InlineData("global::System.Windows.Forms.Form f = null;", "System.Windows.Forms")]
    [InlineData("global::System.Windows.Forms.MessageBox.Show(\"x\");", "System.Windows.Forms")]
    [InlineData("using HPExcel.McpBridge;", "HPExcel.McpBridge")]
    [InlineData("using global::HPExcel.McpBridge;", "HPExcel.McpBridge")]
    [InlineData("using static HPExcel.McpBridge.BridgeEntry;", "HPExcel.McpBridge")]
    [InlineData("using Alias = HPExcel.McpBridge;", "HPExcel.McpBridge")]
    public void Namespace_denial_catches_global_aliases_and_using_variants(string code, string expectedNamespace)
    {
        var diagnostics = ScriptGuard.Check(code, GuardProfile.Excel);
        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, d => d.Message.Contains(expectedNamespace, StringComparison.Ordinal));
    }

    [Fact]
    public void McpShared_never_references_office_or_excel_assemblies()
    {
        var forbiddenPrefixes = new[]
        {
            "Microsoft.Office.Interop.Excel",
            "Office",
            "Microsoft.Office.Core",
            "ClosedXML",
            "DocumentFormat.OpenXml",
            "ExcelNumberFormat"
        };

        var assembliesToInspect = new[]
        {
            typeof(PipeNaming).Assembly,
            typeof(PipeListener).Assembly,
            typeof(ResultFormatter).Assembly
        };

        foreach (var assembly in assembliesToInspect)
        {
            var referencedNames = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty);
            foreach (var forbidden in forbiddenPrefixes)
            {
                Assert.DoesNotContain(referencedNames, name => name.StartsWith(forbidden, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    [Theory]
    [InlineData("#r \"System.IO.dll\"\nreturn 1;")]
    [InlineData("#load \"malicious.csx\"\nreturn 1;")]
    [InlineData("dynamic d = excel; d.Quit();")]
    [InlineData("unsafe { int* p = null; }")]
    [InlineData("await Task.Delay(100);")]
    [InlineData("Func<Task> f = async () => await Task.Yield();")]
    [InlineData("System.Diagnostics.Process.Start(\"calc.exe\");")]
    [InlineData("System.IO.File.WriteAllText(\"test.txt\", \"data\");")]
    [InlineData("Type.GetType(\"System.Diagnostics.Process\");")]
    [InlineData("Delegate.CreateDelegate(typeof(Action), null, \"Quit\");")]
    public void Base_guard_rules_remain_fully_active_under_excel_profile(string attackCode)
    {
        var diagnostics = ScriptGuard.Check(attackCode, GuardProfile.Excel);
        Assert.NotEmpty(diagnostics);
    }

    [Fact]
    public void Legitimate_excel_and_closedxml_syntax_passes_guard_cleanly()
    {
        const string script = """
            var ws = excel.ActiveWorkbook.ActiveSheet;
            var range = ws.Range["A1:B10"];
            range.Value2 = "ValidData";
            var formula = args.Str("formula", "=SUM(A1:A10)");
            range.Formula = formula;
            using var wb = new ClosedXML.Excel.XLWorkbook();
            var sheet = wb.AddWorksheet("Data");
            sheet.Cell(1, 1).Value = "Headless";
            log("Processed 10 rows");
            progress(50, 100, "Halfway");
            if (ct.IsCancellationRequested) return "Cancelled";
            return new { success = true, cells = 20 };
            """;

        var diagnostics = ScriptGuard.Check(script, GuardProfile.Excel);
        Assert.Empty(diagnostics);
    }

    // =========================================================================
    // 2. PIPE NAMING ADVERSARIAL CASES
    // =========================================================================

    [Theory]
    [InlineData("excel", 2026, "hpexcel-mcp-2026")]
    [InlineData("EXCEL", 2026, "hpexcel-mcp-2026")]
    [InlineData("Excel", 2026, "hpexcel-mcp-2026")]
    [InlineData("eXcEl", 2024, "hpexcel-mcp-2024")]
    [InlineData("  excel  ", 2021, "hpexcel-mcp-2021")]
    [InlineData("\texcel\r\n", 2026, "hpexcel-mcp-2026")]
    [InlineData("excel", 16, "hpexcel-mcp-16")]
    [InlineData("excel", 365, "hpexcel-mcp-365")]
    public void PipeNaming_For_handles_casing_whitespace_and_versions(string host, int version, string expected)
    {
        Assert.Equal(expected, PipeNaming.For(host, version));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void PipeNaming_For_throws_ArgumentException_on_empty_or_whitespace_host(string? invalidHost)
    {
        Assert.Throws<ArgumentException>(() => PipeNaming.For(invalidHost!, 2026));
    }

    // =========================================================================
    // 3. CONTEXT MESSAGES WIRE INVARIANTS & SERIALIZATION
    // =========================================================================

    [Fact]
    public void ContextResult_when_Excel_is_null_never_contains_excel_in_serialized_json()
    {
        var context = new ContextResult
        {
            Host = "revit",
            HostVersion = "2026",
            RevitVersion = "2026",
            DocTitle = "Project1.rvt",
            Excel = null
        };

        var json = BridgeJson.Serialize(context);
        Assert.DoesNotContain("\"excel\"", json);
        Assert.DoesNotContain("excel", json);
    }

    [Fact]
    public void ContextResult_roundtrip_preserves_all_ExcelInfo_fields_in_camelCase()
    {
        var info = new ExcelInfo(
            IsAttached: true,
            AttachedPid: 9876,
            ExcelVersion: "16.0.14332",
            ActiveWorkbookName: "Budget_2026.xlsx",
            ActiveWorksheetName: "Q1_Summary",
            SelectionAddress: "$C$5:$F$20",
            WriteEnabled: true,
            DestructiveEnabled: false,
            OpenWorkbookCount: 3,
            WorksheetCount: 12,
            HasActiveWorkbook: true
        );

        var context = new ContextResult
        {
            Host = "excel",
            HostVersion = "2026",
            Excel = info
        };

        var json = BridgeJson.Serialize(context);

        // Verify camelCase JSON keys directly in string
        Assert.Contains("\"isAttached\":true", json);
        Assert.Contains("\"attachedPid\":9876", json);
        Assert.Contains("\"excelVersion\":\"16.0.14332\"", json);
        Assert.Contains("\"activeWorkbookName\":\"Budget_2026.xlsx\"", json);
        Assert.Contains("\"activeWorksheetName\":\"Q1_Summary\"", json);
        Assert.Contains("\"selectionAddress\":\"$C$5:$F$20\"", json);
        Assert.Contains("\"writeEnabled\":true", json);
        Assert.Contains("\"destructiveEnabled\":false", json);
        Assert.Contains("\"openWorkbookCount\":3", json);
        Assert.Contains("\"worksheetCount\":12", json);
        Assert.Contains("\"hasActiveWorkbook\":true", json);

        // Deserialization round trip
        var roundtripped = BridgeJson.Deserialize<ContextResult>(json);
        Assert.NotNull(roundtripped);
        Assert.NotNull(roundtripped.Excel);
        Assert.Equal(info, roundtripped.Excel);
        Assert.Equal(9876, roundtripped.Excel.AttachedPid);
        Assert.Equal("Budget_2026.xlsx", roundtripped.Excel.ActiveWorkbookName);
    }

    [Fact]
    public void ContextResult_deserialized_without_excel_has_null_Excel_property()
    {
        const string rawJson = """{"host":"revit","hostVersion":"2026","docTitle":"Sample.rvt"}""";
        var result = BridgeJson.Deserialize<ContextResult>(rawJson);
        Assert.NotNull(result);
        Assert.Null(result.Excel);
    }

    // =========================================================================
    // 4. JSON-RPC METHODS & PREFIX INVARIANTS
    // =========================================================================

    [Theory]
    [InlineData(JsonRpcMethods.PingSuffix, "excel.ping")]
    [InlineData(JsonRpcMethods.ContextSuffix, "excel.context")]
    [InlineData(JsonRpcMethods.InspectSuffix, "excel.inspect")]
    [InlineData(JsonRpcMethods.ExecuteSuffix, "excel.execute")]
    [InlineData(JsonRpcMethods.CancelSuffix, "excel.cancel")]
    [InlineData(JsonRpcMethods.AnalyzeSuffix, "excel.analyze")]
    [InlineData(JsonRpcMethods.ProgressSuffix, "excel.progress")]
    [InlineData(JsonRpcMethods.LogSuffix, "excel.log")]
    [InlineData(JsonRpcMethods.StatusSuffix, "excel.status")]
    public void JsonRpcMethods_For_and_Suffix_are_bijective_for_excel(string suffix, string expectedFull)
    {
        var fullMethod = JsonRpcMethods.For(JsonRpcMethods.ExcelPrefix, suffix);
        Assert.Equal(expectedFull, fullMethod);
        Assert.Equal(suffix, JsonRpcMethods.Suffix(fullMethod));
    }

    [Fact]
    public void JsonRpcMethods_notification_detectors_work_for_excel()
    {
        Assert.True(JsonRpcMethods.IsProgress("excel.progress"));
        Assert.True(JsonRpcMethods.IsLog("excel.log"));
        Assert.True(JsonRpcMethods.IsStatus("excel.status"));

        Assert.False(JsonRpcMethods.IsProgress("excel.execute"));
        Assert.False(JsonRpcMethods.IsLog("excel.context"));
        Assert.False(JsonRpcMethods.IsStatus("excel.ping"));
    }

    // =========================================================================
    // 5. SCRIPT CONTRACTS & ANALYZER PROFILE
    // =========================================================================

    [Fact]
    public void HostScriptContracts_Excel_guarantees_correct_defaults()
    {
        Assert.Equal(600, HostScriptContracts.ExcelHeavyMaxTimeoutSeconds);
        Assert.Equal(new[] { "excel", "workbook", "sheet", "ct", "log", "progress", "args" }, HostScriptContracts.ExcelGlobals);

        Assert.Contains("Microsoft.Office.Interop.Excel", HostScriptContracts.ExcelImports);
        Assert.Contains("ClosedXML.Excel", HostScriptContracts.ExcelImports);
        Assert.Contains("HPRebar.McpBridge.Core.Scripting", HostScriptContracts.ExcelImports);

        // Disallowed imports must not be present
        Assert.DoesNotContain("System.IO", HostScriptContracts.ExcelImports);
        Assert.DoesNotContain("System.Diagnostics", HostScriptContracts.ExcelImports);
        Assert.DoesNotContain("System.Reflection", HostScriptContracts.ExcelImports);
    }

    [Fact]
    public void ScriptAnalyzer_with_Excel_profile_never_claims_transaction_management()
    {
        const string scriptWithTransactions = """
            var t = new Transaction();
            var tg = new TransactionGroup();
            var st = new SubTransaction();
            StartTransaction();
            BeginTransaction();
            return excel.ActiveWorkbook.Name;
            """;

        var analysis = ScriptAnalyzer.Analyze(scriptWithTransactions, AnalyzerProfile.Excel);
        Assert.False(analysis.UsesTransaction);
    }
}
