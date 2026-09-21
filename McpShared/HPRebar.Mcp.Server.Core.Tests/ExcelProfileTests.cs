using System.Text.Json;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Services;
using HPRebar.Mcp.Server.Tests.Fakes;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using static HPRebar.Mcp.Server.Tests.ExcelTestProfile;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     Engine tests for Excel host profile and contracts: pipe naming, prefix, imports, globals, guard, analyzer,
///     hints, options configuration, context shaping, and wire isolation.
/// </summary>
public sealed class ExcelProfileTests
{
    [Fact]
    public void Excel_constants_produce_the_pipe_prefix_imports_and_globals()
    {
        Assert.Equal("hpexcel-mcp-2026", PipeNaming.For(PipeNaming.ExcelHost, 2026));
        Assert.Equal("hpexcel-mcp-2026", PipeNaming.For("EXCEL", 2026));
        Assert.Equal("excel.execute", JsonRpcMethods.For(JsonRpcMethods.ExcelPrefix, JsonRpcMethods.ExecuteSuffix));
        Assert.Equal("execute", JsonRpcMethods.Suffix("excel.execute"));
        Assert.Equal("hpexcel-mcp-2026", new BridgeOptions { HostId = "excel", HostVersion = 2026 }.PipeName);
        Assert.Contains("Microsoft.Office.Interop.Excel", HostScriptContracts.ExcelImports);
        Assert.Contains("ClosedXML.Excel", HostScriptContracts.ExcelImports);
        Assert.Contains("HPRebar.McpBridge.Core.Scripting", HostScriptContracts.ExcelImports);
        Assert.DoesNotContain("System.IO", HostScriptContracts.ExcelImports);
        Assert.Equal(["excel", "workbook", "sheet", "ct", "log", "progress", "args"], HostScriptContracts.ExcelGlobals);
        Assert.Equal(600, HostScriptContracts.ExcelHeavyMaxTimeoutSeconds);
        Assert.Equal("hpexcel-mcp-2026", Excel().PipeName(2026));
        Assert.Equal("excel.context", Excel().Method(JsonRpcMethods.ContextSuffix));
        Assert.Equal("excel.analyze", Excel().Method(JsonRpcMethods.AnalyzeSuffix));
    }

    [Theory]
    [InlineData("excel.Quit(); return 1;", "Quit")]
    [InlineData("app.Quit(); return 1;", "Quit")]
    [InlineData("excel.ApplicationExit(); return 1;", "ApplicationExit")]
    [InlineData("var v = excel.InputBox(\"test\"); return 1;", "InputBox")]
    [InlineData("var f = excel.GetOpenFilename(); return 1;", "GetOpenFilename")]
    [InlineData("var f = excel.GetSaveAsFilename(); return 1;", "GetSaveAsFilename")]
    [InlineData("MessageBox.Show(\"hi\"); return 1;", "MessageBox")]
    [InlineData("HPExcel.McpBridge.BridgeEntry.Stop(); return 1;", "HPExcel.McpBridge")]
    [InlineData("HPRebar.McpBridge.Core.Host.McpBridgeHost.Current.Stop(); return 1;", "HPRebar.McpBridge.Core.Host")]
    [InlineData("using System.Windows.Forms; return 1;", "System.Windows.Forms")]
    [InlineData("var p = System.Diagnostics.Process.Start(\"cmd\"); return 1;", "System.Diagnostics.Process")]
    [InlineData("var o = Marshal.GetActiveObject(\"x\"); return 1;", "Marshal")]
    [InlineData("using System.Runtime.InteropServices; return 1;", "System.Runtime.InteropServices")]
    public void Excel_guard_profile_denies_quit_dialogs_bridge_internals_and_the_base_list(string code, string expected)
    {
        var violations = ScriptGuard.Check(code, GuardProfile.Excel);

        Assert.NotEmpty(violations);
        Assert.Contains(violations, v => v.Message.Contains(expected, StringComparison.Ordinal));
        Assert.All(violations, v => Assert.True(v.Message.Contains("Excel", StringComparison.Ordinal) || v.Message.Contains("bridge owns", StringComparison.Ordinal)));
    }

    [Fact]
    public void Global_alias_does_not_bypass_the_excel_bridge_namespace_denial()
    {
        var violations = ScriptGuard.Check("var h = global::HPRebar.McpBridge.Core.Host.McpBridgeHost.Current; return 1;", GuardProfile.Excel);

        Assert.Contains(violations, v => v.Message.Contains("HPRebar.McpBridge.Core.Host", StringComparison.Ordinal));
    }

    [Fact]
    public void Excel_guard_profile_lets_reads_and_writes_through()
    {
        const string fine =
            "var ws = excel.ActiveSheet;\n" +
            "var range = ws.Range(\"A1:B10\");\n" +
            "range.Value2 = \"Hello\";\n" +
            "var text = args.Str(\"val\", \"123\");\n" +
            "return new { text, address = range.Address };";

        Assert.Empty(ScriptGuard.Check(fine, GuardProfile.Excel));
        Assert.Empty(ScriptGuard.Check("return excel.ActiveWorkbook.Name;", GuardProfile.Excel));
    }

    [Fact]
    public void Excel_analyzer_profile_never_reports_a_transaction()
    {
        const string code = "using (var t = new Transaction()) { } return excel.ActiveWorkbook.Name;";

        Assert.False(ScriptAnalyzer.Analyze(code, AnalyzerProfile.Excel).UsesTransaction);
        Assert.False(ScriptAnalyzer.Analyze("var t = excel.BeginTransaction(); return 1;", AnalyzerProfile.Excel).UsesTransaction);
        Assert.True(ScriptAnalyzer.Analyze(code, AnalyzerProfile.Revit).UsesTransaction);
    }

    [Fact]
    public void Hints_and_timeout_ceiling_survive_WithHostAssembly_and_default_to_null()
    {
        var copy = Excel(notConnected: NotConnectedHint, timeoutHint: TimeoutHint).WithHostAssembly(typeof(HostProfile).Assembly);

        Assert.Equal(600, copy.MaxTimeoutSeconds);
        Assert.Equal(NotConnectedHint, copy.BridgeNotConnectedHint);
        Assert.Equal(TimeoutHint, copy.TimeoutSemanticsHint);
        Assert.Null(Excel().BridgeNotConnectedHint);
    }

    [Fact]
    public void Validator_ceiling_follows_the_excel_profile()
    {
        Assert.DoesNotContain(ToolValidator.Validate(Candidate(600), null, [], false, Excel()).Errors, e => e.Contains("timeoutSeconds"));
        Assert.Contains(ToolValidator.Validate(Candidate(601), null, [], false, Excel()).Errors, e => e.Contains("between 5 and 600"));
        Assert.Contains(ToolValidator.Validate(Candidate(600), null, [], false, Excel(maxTimeout: 120)).Errors, e => e.Contains("between 5 and 120"));
    }

    [Fact]
    public void ConfigureOptions_seeds_HostVersion_from_the_profile_and_configuration_still_wins()
    {
        Assert.Equal(2026, BridgeOptionsFor(Excel(), new Dictionary<string, string?>()).HostVersion);
        Assert.Equal("hpexcel-mcp-2026", BridgeOptionsFor(Excel(), new Dictionary<string, string?>()).PipeName);
        Assert.Throws<OptionsValidationException>(() => BridgeOptionsFor(Excel(), new Dictionary<string, string?> { ["Bridge:HostVersion"] = "1997" }));
    }

    [Fact]
    public void Wire_additions_are_invisible_when_unused()
    {
        var context = BridgeJson.Serialize(new ContextResult { RevitVersion = "2026", DocTitle = "Project1" });
        Assert.DoesNotContain("\"excel\"", context);
    }

    [Fact]
    public void Excel_info_round_trips_in_camel_case_and_is_omitted_when_null()
    {
        var context = new ContextResult
        {
            Host = "excel", HostVersion = "2026",
            Excel = new ExcelInfo(true, 1234, "16.0", "Book1.xlsx", "Sheet1", "A1:B10", true, false, 1, 3, true),
        };

        var json = BridgeJson.Serialize(context);
        using var doc = JsonDocument.Parse(json);
        var excel = doc.RootElement.GetProperty("excel");
        Assert.True(excel.GetProperty("isAttached").GetBoolean());
        Assert.Equal(1234, excel.GetProperty("attachedPid").GetInt32());
        Assert.Equal("16.0", excel.GetProperty("excelVersion").GetString());
        Assert.Equal("Book1.xlsx", excel.GetProperty("activeWorkbookName").GetString());
        Assert.Equal("Sheet1", excel.GetProperty("activeWorksheetName").GetString());
        Assert.Equal("A1:B10", excel.GetProperty("selectionAddress").GetString());
        Assert.True(excel.GetProperty("writeEnabled").GetBoolean());
        Assert.False(excel.GetProperty("destructiveEnabled").GetBoolean());
        Assert.Equal(1, excel.GetProperty("openWorkbookCount").GetInt32());
        Assert.Equal(3, excel.GetProperty("worksheetCount").GetInt32());
        Assert.True(excel.GetProperty("hasActiveWorkbook").GetBoolean());
        Assert.Equal(11, excel.EnumerateObject().Count());

        var back = BridgeJson.Deserialize<ContextResult>(json)!;
        Assert.Equal(context.Excel, back.Excel);

        var withoutExcel = BridgeJson.Serialize(new ContextResult { Host = "autocad" });
        Assert.DoesNotContain("excel", withoutExcel);
    }

    [Fact]
    public async Task Context_shape_for_excel_drops_revit_fields_and_keeps_excel_block()
    {
        var pipe = NewPipe();
        var executor = new FakeRevitExecutor
        {
            ContextHandler = _ => new ContextResult
            {
                RevitVersion = "2026", Host = "excel", HostVersion = "2026", DocTitle = "Book1.xlsx", DocPath = @"C:\Data\Book1.xlsx",
                Excel = new ExcelInfo(true, 5678, "16.0", "Book1.xlsx", "Sheet1", "$A$1", true, true, 2, 5, true),
            },
        };
        using var listener = new PipeListener(pipe, new RequestDispatcher(executor, new BridgeSettings(), "2026", "Excel"));
        listener.Start();

        await using var client = new RevitBridgeClient(
            Options.Create(PipeOptions(pipe)),
            NullLogger<RevitBridgeClient>.Instance, Excel());

        var text = await new ContextService(client, new ResultFormatter()).ReadAsync(false, TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(text);
        var root = json.RootElement;
        Assert.False(root.TryGetProperty("revitVersion", out _));
        Assert.False(root.TryGetProperty("isFamily", out _));
        Assert.Equal("excel", root.GetProperty("host").GetString());
        Assert.True(root.GetProperty("excel").GetProperty("isAttached").GetBoolean());
        Assert.Equal(5678, root.GetProperty("excel").GetProperty("attachedPid").GetInt32());
        Assert.Equal("Book1.xlsx", root.GetProperty("excel").GetProperty("activeWorkbookName").GetString());
        Assert.Equal("Sheet1", root.GetProperty("excel").GetProperty("activeWorksheetName").GetString());
        Assert.Equal("$A$1", root.GetProperty("excel").GetProperty("selectionAddress").GetString());
        Assert.False(root.TryGetProperty("autocad", out _));
        Assert.False(root.TryGetProperty("navis", out _));
        Assert.False(root.TryGetProperty("etabs", out _));
        Assert.False(root.TryGetProperty("civil3d", out _));
        Assert.False(root.TryGetProperty("sap2000", out _));
        Assert.False(root.TryGetProperty("powerbi", out _));
        await listener.StopAsync();
    }

    private static BridgeOptions BridgeOptionsFor(IHostProfile profile, Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        McpServerHost.ConfigureOptions(services, configuration, profile);
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<BridgeOptions>>().Value;
    }
}
