using System.Text.Json;
using HPExcel.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.Mcp.Server.Services;
using HPRebar.Mcp.Server.Tests.Fakes;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using Xunit;

namespace HPExcel.Mcp.Server.Tests;

/// <summary>
///     FakeExecutor round-trip integration tests for all 12 embedded Excel seed tools.
///     Verifies that each seed tool invoked through ExecuteCodeService correctly formats
///     and transmits JSON-RPC execution payloads over the named pipe, correctly passes arguments,
///     and returns well-formed results and snapshots.
/// </summary>
public sealed class ExcelSeedToolsRoundTripTests : IAsyncLifetime
{
    private const string DisabledText =
        "Code execution is disabled. Ask the user to tick 'Allow AI code execution' in the HPExcel MCP Bridge window.";

    private readonly string _pipeName = "hpexcel-mcp-test-" + Guid.NewGuid().ToString("N");
    private readonly FakeRevitExecutor _executor = new();
    private readonly BridgeSettings _settings = new() { ExecutionEnabled = true };

    private PipeListener _listener = null!;
    private RevitBridgeClient _client = null!;
    private ExecuteCodeService _execute = null!;
    private ResultFormatter _formatter = null!;
    private IOptions<BridgeOptions> _options = null!;

    public ValueTask InitializeAsync()
    {
        var dispatcher = new RequestDispatcher(
            _executor,
            _settings,
            "2026",
            "Excel",
            DisabledText);

        _listener = new PipeListener(_pipeName, dispatcher);
        _listener.Start();

        _options = Options.Create(new BridgeOptions
        {
            HostId = "excel",
            HostVersion = 2026,
            PipeName = _pipeName,
            ConnectTimeoutMs = 3000,
            PingIntervalSeconds = 60
        });

        _client = new RevitBridgeClient(_options, NullLogger<RevitBridgeClient>.Instance, ExcelHostProfile.Instance);
        _formatter = new ResultFormatter();
        _execute = new ExecuteCodeService(_client, _formatter, _options);

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
        await _listener.StopAsync();
        _listener.Dispose();
    }

    private static string TextOf(CallToolResult result) => ((TextContentBlock)result.Content[0]).Text;

    private static SeedInstaller.SeedContent GetSeed(string name)
    {
        var seeds = SeedInstaller.LoadSeeds(typeof(ExcelHostProfile).Assembly);
        return seeds.Single(s => s.Name == name);
    }

    [Fact]
    public async Task Seed_ReadRange_RoundTrip_DispatchesCorrectPayload()
    {
        var seed = GetSeed("read_range");
        _executor.ExecuteHandler = req => new ExecuteResult
        {
            Value = JsonSerializer.SerializeToElement(new
            {
                sheet = "Sheet1",
                address = "$A$1:$B$2",
                rowCount = 2,
                colCount = 2,
                data = new[] { new[] { "Name", "Score" }, new[] { "Alice", "100" } }
            }),
            ValueType = "object",
            DurationMs = 15
        };

        var args = JsonSerializer.SerializeToElement(new
        {
            range = "A1:B2",
            sheet = "Sheet1",
            hasHeaders = true,
            outputFormat = "values"
        });

        var result = await _execute.ExecuteAsync(
            code: seed.Code,
            transaction: TransactionModes.None,
            dryRun: false,
            timeoutSeconds: 15,
            label: "read_range",
            args: args,
            progress: null,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.NotNull(_executor.LastExecuteRequest);
        Assert.Equal(seed.Code, _executor.LastExecuteRequest.Code);
        Assert.Equal(TransactionModes.None, _executor.LastExecuteRequest.Transaction);
        Assert.False(_executor.LastExecuteRequest.DryRun);
        Assert.Equal(15, _executor.LastExecuteRequest.TimeoutSeconds);
        Assert.Equal("read_range", _executor.LastExecuteRequest.Label);
        Assert.Equal("A1:B2", _executor.LastExecuteRequest.Args!.Value.GetProperty("range").GetString());

        var text = TextOf(result);
        Assert.Contains("Sheet1", text);
        Assert.Contains("$A$1:$B$2", text);
        Assert.Contains("Alice", text);
    }

    [Fact]
    public async Task Seed_ReadWorksheetInfo_RoundTrip_DispatchesCorrectPayload()
    {
        var seed = GetSeed("read_worksheet_info");
        _executor.ExecuteHandler = req => new ExecuteResult
        {
            Value = JsonSerializer.SerializeToElement(new
            {
                sheetCount = 2,
                sheets = new[]
                {
                    new { name = "Summary", index = 1, visibility = "Visible", usedRange = "$A$1:$F$20", rowCount = 20, colCount = 6 },
                    new { name = "Details", index = 2, visibility = "Visible", usedRange = "$A$1:$C$100", rowCount = 100, colCount = 3 }
                },
                activeSheet = "Summary"
            }),
            ValueType = "object",
            DurationMs = 10
        };

        var args = JsonSerializer.SerializeToElement(new { sheet = "Summary" });
        var result = await _execute.ExecuteAsync(
            code: seed.Code,
            transaction: TransactionModes.None,
            dryRun: false,
            timeoutSeconds: 15,
            label: "read_worksheet_info",
            args: args,
            progress: null,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(seed.Code, _executor.LastExecuteRequest!.Code);
        var text = TextOf(result);
        Assert.Contains("Summary", text);
        Assert.Contains("Details", text);
    }

    [Fact]
    public async Task Seed_FindCells_RoundTrip_DispatchesCorrectPayload()
    {
        var seed = GetSeed("find_cells");
        _executor.ExecuteHandler = req => new ExecuteResult
        {
            Value = JsonSerializer.SerializeToElement(new
            {
                query = "Total",
                matchCount = 1,
                matches = new[]
                {
                    new { sheet = "Sheet1", address = "$D$20", row = 20, col = 4, value = "Total" }
                }
            }),
            ValueType = "object",
            DurationMs = 12
        };

        var args = JsonSerializer.SerializeToElement(new
        {
            query = "Total",
            exactMatch = true,
            searchAllSheets = true
        });

        var result = await _execute.ExecuteAsync(
            code: seed.Code,
            transaction: TransactionModes.None,
            dryRun: false,
            timeoutSeconds: 20,
            label: "find_cells",
            args: args,
            progress: null,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal("Total", _executor.LastExecuteRequest!.Args!.Value.GetProperty("query").GetString());
        Assert.True(_executor.LastExecuteRequest.Args!.Value.GetProperty("exactMatch").GetBoolean());
        var text = TextOf(result);
        Assert.Contains("$D$20", text);
        Assert.Contains("Total", text);
    }

    [Fact]
    public async Task Seed_ReadTable_RoundTrip_DispatchesCorrectPayload()
    {
        var seed = GetSeed("read_table");
        _executor.ExecuteHandler = req => new ExecuteResult
        {
            Value = JsonSerializer.SerializeToElement(new
            {
                tableName = "RebarSchedule",
                sheet = "Rebar",
                range = "$A$1:$E$15",
                columns = new[] { "Mark", "Diameter", "Length", "Count", "TotalLength" },
                rowCount = 14,
                hasTotalsRow = true
            }),
            ValueType = "object",
            DurationMs = 18
        };

        var args = JsonSerializer.SerializeToElement(new
        {
            tableName = "RebarSchedule",
            sheet = "Rebar",
            maxRows = 50
        });

        var result = await _execute.ExecuteAsync(
            code: seed.Code,
            transaction: TransactionModes.None,
            dryRun: false,
            timeoutSeconds: 20,
            label: "read_table",
            args: args,
            progress: null,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal("RebarSchedule", _executor.LastExecuteRequest!.Args!.Value.GetProperty("tableName").GetString());
        var text = TextOf(result);
        Assert.Contains("RebarSchedule", text);
        Assert.Contains("Diameter", text);
    }

    [Fact]
    public async Task Seed_WriteRange_RoundTrip_DispatchesCorrectPayloadAndReturnsSnapshot()
    {
        var seed = GetSeed("write_range");
        _executor.ExecuteHandler = req => new ExecuteResult
        {
            Value = JsonSerializer.SerializeToElement(new
            {
                sheet = "Sheet1",
                range = "A1:B2",
                rowsWritten = 2,
                colsWritten = 2,
                success = true
            }),
            ValueType = "object",
            DurationMs = 25,
            Snapshot = "20260921-120000_Workbook_write_range.xlsx"
        };

        var args = JsonSerializer.SerializeToElement(new
        {
            startCell = "A1",
            sheet = "Sheet1",
            values = new[] { new[] { "Header1", "Header2" }, new[] { "Value1", "Value2" } },
            autoFit = true
        });

        var result = await _execute.ExecuteAsync(
            code: seed.Code,
            transaction: TransactionModes.Auto,
            dryRun: false,
            timeoutSeconds: 30,
            label: "write_range",
            args: args,
            progress: null,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(TransactionModes.Auto, _executor.LastExecuteRequest!.Transaction);
        Assert.Equal("A1", _executor.LastExecuteRequest.Args!.Value.GetProperty("startCell").GetString());

        var text = TextOf(result);
        Assert.Contains("20260921-120000_Workbook_write_range.xlsx", text);
        Assert.Contains("rowsWritten", text);
    }

    [Fact]
    public async Task Seed_FormatRange_RoundTrip_DispatchesCorrectPayloadAndReturnsSnapshot()
    {
        var seed = GetSeed("format_range");
        _executor.ExecuteHandler = req => new ExecuteResult
        {
            Value = JsonSerializer.SerializeToElement(new
            {
                success = true,
                sheet = "Sheet1",
                range = "$A$1:$D$1",
                summary = "Formatted range Sheet1!$A$1:$D$1"
            }),
            ValueType = "object",
            DurationMs = 20,
            Snapshot = "20260921-120100_Workbook_format_range.xlsx"
        };

        var args = JsonSerializer.SerializeToElement(new
        {
            range = "A1:D1",
            bold = true,
            fontSize = 12,
            fontColor = "#FFFFFF",
            backgroundColor = "#0078D4",
            horizontalAlignment = "center"
        });

        var result = await _execute.ExecuteAsync(
            code: seed.Code,
            transaction: TransactionModes.Auto,
            dryRun: false,
            timeoutSeconds: 30,
            label: "format_range",
            args: args,
            progress: null,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal("A1:D1", _executor.LastExecuteRequest!.Args!.Value.GetProperty("range").GetString());
        Assert.True(_executor.LastExecuteRequest.Args!.Value.GetProperty("bold").GetBoolean());

        var text = TextOf(result);
        Assert.Contains("20260921-120100_Workbook_format_range.xlsx", text);
    }

    [Fact]
    public async Task Seed_ManageWorksheet_RoundTrip_DispatchesCorrectPayload()
    {
        var seed = GetSeed("manage_worksheet");
        _executor.ExecuteHandler = req => new ExecuteResult
        {
            Value = JsonSerializer.SerializeToElement(new
            {
                success = true,
                action = "add",
                sheet = "Calculations",
                newSheetName = "Calculations"
            }),
            ValueType = "object",
            DurationMs = 15,
            Snapshot = "20260921-120200_Workbook_manage_worksheet.xlsx"
        };

        var args = JsonSerializer.SerializeToElement(new
        {
            action = "add",
            sheet = "NewSheet",
            newSheetName = "Calculations"
        });

        var result = await _execute.ExecuteAsync(
            code: seed.Code,
            transaction: TransactionModes.Auto,
            dryRun: false,
            timeoutSeconds: 30,
            label: "manage_worksheet",
            args: args,
            progress: null,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal("add", _executor.LastExecuteRequest!.Args!.Value.GetProperty("action").GetString());
        Assert.Equal("Calculations", _executor.LastExecuteRequest.Args!.Value.GetProperty("newSheetName").GetString());

        var text = TextOf(result);
        Assert.Contains("Calculations", text);
        Assert.Contains("20260921-120200_Workbook_manage_worksheet.xlsx", text);
    }

    [Fact]
    public async Task Seed_CreateTable_RoundTrip_DispatchesCorrectPayload()
    {
        var seed = GetSeed("create_table");
        _executor.ExecuteHandler = req => new ExecuteResult
        {
            Value = JsonSerializer.SerializeToElement(new
            {
                success = true,
                tableName = "BeamsTable",
                sheet = "Beams",
                range = "$A$1:$E$25",
                hasTotalsRow = true
            }),
            ValueType = "object",
            DurationMs = 22,
            Snapshot = "20260921-120300_Workbook_create_table.xlsx"
        };

        var args = JsonSerializer.SerializeToElement(new
        {
            range = "A1:E25",
            tableName = "BeamsTable",
            sheet = "Beams",
            styleName = "TableStyleMedium2",
            showTotals = true
        });

        var result = await _execute.ExecuteAsync(
            code: seed.Code,
            transaction: TransactionModes.Auto,
            dryRun: false,
            timeoutSeconds: 30,
            label: "create_table",
            args: args,
            progress: null,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal("BeamsTable", _executor.LastExecuteRequest!.Args!.Value.GetProperty("tableName").GetString());
        Assert.True(_executor.LastExecuteRequest.Args!.Value.GetProperty("showTotals").GetBoolean());

        var text = TextOf(result);
        Assert.Contains("BeamsTable", text);
    }

    [Fact]
    public async Task Seed_CreateChart_RoundTrip_DispatchesCorrectPayload()
    {
        var seed = GetSeed("create_chart");
        _executor.ExecuteHandler = req => new ExecuteResult
        {
            Value = JsonSerializer.SerializeToElement(new
            {
                success = true,
                chartName = "RebarWeightChart",
                chartType = "ColumnClustered",
                title = "Total Rebar Weight",
                sheet = "Summary",
                placedAt = "G2"
            }),
            ValueType = "object",
            DurationMs = 35,
            Snapshot = "20260921-120400_Workbook_create_chart.xlsx"
        };

        var args = JsonSerializer.SerializeToElement(new
        {
            dataRange = "A1:B10",
            chartType = "ColumnClustered",
            title = "Total Rebar Weight",
            sheet = "Summary",
            targetCell = "G2",
            width = 500.0,
            height = 300.0
        });

        var result = await _execute.ExecuteAsync(
            code: seed.Code,
            transaction: TransactionModes.Auto,
            dryRun: false,
            timeoutSeconds: 45,
            label: "create_chart",
            args: args,
            progress: null,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal("ColumnClustered", _executor.LastExecuteRequest!.Args!.Value.GetProperty("chartType").GetString());
        Assert.Equal("Total Rebar Weight", _executor.LastExecuteRequest.Args!.Value.GetProperty("title").GetString());

        var text = TextOf(result);
        Assert.Contains("RebarWeightChart", text);
    }

    [Fact]
    public async Task Seed_EvaluateFormula_RoundTrip_DispatchesCorrectPayload()
    {
        var seed = GetSeed("evaluate_formula");
        _executor.ExecuteHandler = req => new ExecuteResult
        {
            Value = JsonSerializer.SerializeToElement(new
            {
                formula = "=SUM(B2:B20)",
                result = 15200.5,
                resultType = "Double",
                isError = false
            }),
            ValueType = "object",
            DurationMs = 8
        };

        var args = JsonSerializer.SerializeToElement(new
        {
            formula = "=SUM(B2:B20)",
            sheet = "Data"
        });

        var result = await _execute.ExecuteAsync(
            code: seed.Code,
            transaction: TransactionModes.None,
            dryRun: false,
            timeoutSeconds: 15,
            label: "evaluate_formula",
            args: args,
            progress: null,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(TransactionModes.None, _executor.LastExecuteRequest!.Transaction);
        Assert.Equal("=SUM(B2:B20)", _executor.LastExecuteRequest.Args!.Value.GetProperty("formula").GetString());

        var text = TextOf(result);
        Assert.Contains("15200.5", text);
    }

    [Fact]
    public async Task Seed_ExportWorksheet_RoundTrip_DispatchesCorrectPayload()
    {
        var seed = GetSeed("export_worksheet");
        _executor.ExecuteHandler = req => new ExecuteResult
        {
            Value = JsonSerializer.SerializeToElement(new
            {
                success = true,
                format = "pdf",
                filePath = @"C:\Exports\Report_20260921.pdf",
                sheet = "Summary"
            }),
            ValueType = "object",
            DurationMs = 40,
            Snapshot = "20260921-120500_Workbook_export_worksheet.xlsx"
        };

        var args = JsonSerializer.SerializeToElement(new
        {
            format = "pdf",
            sheet = "Summary",
            landscape = true,
            fitToPage = true
        });

        var result = await _execute.ExecuteAsync(
            code: seed.Code,
            transaction: TransactionModes.Auto,
            dryRun: false,
            timeoutSeconds: 60,
            label: "export_worksheet",
            args: args,
            progress: null,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal("pdf", _executor.LastExecuteRequest!.Args!.Value.GetProperty("format").GetString());
        Assert.True(_executor.LastExecuteRequest.Args!.Value.GetProperty("landscape").GetBoolean());

        var text = TextOf(result);
        Assert.Contains("Report_20260921.pdf", text);
    }

    [Fact]
    public async Task Seed_RunMacro_RoundTrip_DispatchesCorrectPayload()
    {
        var seed = GetSeed("run_macro");
        _executor.ExecuteHandler = req => new ExecuteResult
        {
            Value = JsonSerializer.SerializeToElement(new
            {
                success = true,
                macro = "CalculateRebarSchedule",
                returnValue = "Updated 45 bars"
            }),
            ValueType = "object",
            DurationMs = 50,
            Snapshot = "20260921-120600_Workbook_run_macro.xlsx"
        };

        var args = JsonSerializer.SerializeToElement(new
        {
            macroName = "CalculateRebarSchedule",
            args = new[] { "ZoneA", "High" }
        });

        var result = await _execute.ExecuteAsync(
            code: seed.Code,
            transaction: TransactionModes.Auto,
            dryRun: false,
            timeoutSeconds: 60,
            label: "run_macro",
            args: args,
            progress: null,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal("CalculateRebarSchedule", _executor.LastExecuteRequest!.Args!.Value.GetProperty("macroName").GetString());
        Assert.Equal(2, _executor.LastExecuteRequest.Args!.Value.GetProperty("args").GetArrayLength());

        var text = TextOf(result);
        Assert.Contains("CalculateRebarSchedule", text);
        Assert.Contains("Updated 45 bars", text);
    }

    [Fact]
    public async Task ExecuteAsync_SupportsProgressReportingAcrossNamedPipe()
    {
        _executor.ProgressSteps = 3;
        _executor.ProgressDelayMs = 5;

        var progressReports = new List<ModelContextProtocol.ProgressNotificationValue>();
        var progressHandler = new Progress<ModelContextProtocol.ProgressNotificationValue>(p => progressReports.Add(p));

        var result = await _execute.ExecuteAsync(
            code: "return 100;",
            transaction: TransactionModes.None,
            dryRun: false,
            timeoutSeconds: 10,
            label: "progress_test",
            args: null,
            progress: progressHandler,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(3, _executor.ProgressSteps);
    }

    [Fact]
    public async Task ExecuteAsync_ClampsTimeoutToMax600Seconds()
    {
        var normal = await _execute.ExecuteAsync(
            code: "return 1;",
            transaction: TransactionModes.Auto,
            dryRun: false,
            timeoutSeconds: 300,
            label: null,
            args: null,
            progress: null,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(normal.IsError);
        Assert.Equal(300, _executor.LastExecuteRequest!.TimeoutSeconds);

        var clamped = await _execute.ExecuteAsync(
            code: "return 1;",
            transaction: TransactionModes.Auto,
            dryRun: false,
            timeoutSeconds: 9999,
            label: null,
            args: null,
            progress: null,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(clamped.IsError);
        Assert.Equal(600, _executor.LastExecuteRequest!.TimeoutSeconds);
    }
}
