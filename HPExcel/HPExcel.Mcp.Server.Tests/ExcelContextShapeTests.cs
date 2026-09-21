using System.Text.Json;
using HPExcel.Mcp.Server.Hosts;
using HPExcel.Mcp.Server.Tools;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Models;
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
///     Integration tests for Excel ContextService and get_excel_context tool.
///     Verifies serialization shape, selection handling, exclusion of Revit/AutoCAD/Navis fields,
///     and robust handling of error states (detached, busy, missing workbook, missing bridge).
/// </summary>
public sealed class ExcelContextShapeTests : IAsyncLifetime
{
    private const string DisabledText =
        "Code execution is disabled. Ask the user to tick 'Allow AI code execution' in the HPExcel MCP Bridge window.";

    private readonly string _pipeName = "hpexcel-mcp-context-" + Guid.NewGuid().ToString("N");
    private readonly FakeRevitExecutor _executor = new();
    private readonly BridgeSettings _settings = new() { ExecutionEnabled = true };

    private PipeListener _listener = null!;
    private RevitBridgeClient _client = null!;
    private ContextService _context = null!;
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
        _context = new ContextService(_client, _formatter);

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
        await _listener.StopAsync();
        _listener.Dispose();
    }

    private static string TextOf(CallToolResult result) => ((TextContentBlock)result.Content[0]).Text;

    private static ContextResult StandardExcelContext(bool includeSelection) => new()
    {
        Host = "excel",
        HostVersion = "2026",
        RevitVersion = "2026",
        DocTitle = "FinancialModel_2026.xlsx",
        DocPath = @"C:\Spreadsheets\FinancialModel_2026.xlsx",
        IsModifiable = true,
        ExecutionEnabled = true,
        Excel = new ExcelInfo(
            IsAttached: true,
            AttachedPid: 12345,
            ExcelVersion: "16.0",
            ActiveWorkbookName: "FinancialModel_2026.xlsx",
            ActiveWorksheetName: "Quarterly_Summary",
            SelectionAddress: "$B$2:$E$15",
            WriteEnabled: true,
            DestructiveEnabled: false,
            OpenWorkbookCount: 3,
            WorksheetCount: 7,
            HasActiveWorkbook: true),
        Selection = includeSelection ? [new ElementInfo(1, "Range", "$B$2:$E$15")] : []
    };

    [Fact]
    public async Task GetExcelContext_ReturnsExcelBlock_AndStripsRevitFields()
    {
        _executor.ContextHandler = StandardExcelContext;

        var tool = new ExcelContextTool(_context);
        var result = await tool.GetContextAsync(includeSelection: true, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var text = TextOf(result);
        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;

        // Top-level host metadata
        Assert.Equal("excel", root.GetProperty("host").GetString());
        Assert.Equal("2026", root.GetProperty("hostVersion").GetString());
        Assert.Equal("FinancialModel_2026.xlsx", root.GetProperty("docTitle").GetString());
        Assert.Equal(@"C:\Spreadsheets\FinancialModel_2026.xlsx", root.GetProperty("docPath").GetString());
        Assert.True(root.GetProperty("isModifiable").GetBoolean());
        Assert.True(root.GetProperty("executionEnabled").GetBoolean());

        // Host-neutrality: Revit-specific fields must be stripped by ContextService
        Assert.False(root.TryGetProperty("revitVersion", out _));
        Assert.False(root.TryGetProperty("isFamily", out _));

        // Other hosts should not leak into Excel context
        Assert.False(root.TryGetProperty("autocad", out _));
        Assert.False(root.TryGetProperty("navis", out _));
        Assert.False(root.TryGetProperty("etabs", out _));
        Assert.False(root.TryGetProperty("powerBi", out _));

        // Excel-specific block
        Assert.True(root.TryGetProperty("excel", out var excelBlock));
        Assert.True(excelBlock.GetProperty("isAttached").GetBoolean());
        Assert.Equal(12345, excelBlock.GetProperty("attachedPid").GetInt32());
        Assert.Equal("16.0", excelBlock.GetProperty("excelVersion").GetString());
        Assert.Equal("FinancialModel_2026.xlsx", excelBlock.GetProperty("activeWorkbookName").GetString());
        Assert.Equal("Quarterly_Summary", excelBlock.GetProperty("activeWorksheetName").GetString());
        Assert.Equal("$B$2:$E$15", excelBlock.GetProperty("selectionAddress").GetString());
        Assert.True(excelBlock.GetProperty("writeEnabled").GetBoolean());
        Assert.False(excelBlock.GetProperty("destructiveEnabled").GetBoolean());
        Assert.Equal(3, excelBlock.GetProperty("openWorkbookCount").GetInt32());
        Assert.Equal(7, excelBlock.GetProperty("worksheetCount").GetInt32());
        Assert.True(excelBlock.GetProperty("hasActiveWorkbook").GetBoolean());

        // Selection element verification
        Assert.True(_executor.LastContextIncludedSelection);
        var selectionArray = root.GetProperty("selection");
        Assert.Equal(1, selectionArray.GetArrayLength());
        Assert.Equal("Range", selectionArray[0].GetProperty("category").GetString());
        Assert.Equal("$B$2:$E$15", selectionArray[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetExcelContext_WhenIncludeSelectionFalse_ReportsEmptySelection()
    {
        _executor.ContextHandler = StandardExcelContext;

        var tool = new ExcelContextTool(_context);
        var result = await tool.GetContextAsync(includeSelection: false, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.False(_executor.LastContextIncludedSelection);

        using var doc = JsonDocument.Parse(TextOf(result));
        var root = doc.RootElement;
        var selectionArray = root.GetProperty("selection");
        Assert.Equal(0, selectionArray.GetArrayLength());
    }

    [Fact]
    public async Task GetExcelContext_WhenExcelDisconnected_ReportsCleanDetachedState()
    {
        _executor.ContextHandler = _ => new ContextResult
        {
            Host = "excel",
            HostVersion = "2026",
            DocTitle = null,
            DocPath = null,
            IsModifiable = false,
            ExecutionEnabled = false,
            Excel = new ExcelInfo(
                IsAttached: false,
                AttachedPid: null,
                ExcelVersion: null,
                ActiveWorkbookName: null,
                ActiveWorksheetName: null,
                SelectionAddress: null,
                WriteEnabled: false,
                DestructiveEnabled: false,
                OpenWorkbookCount: 0,
                WorksheetCount: 0,
                HasActiveWorkbook: false),
            Selection = []
        };

        var tool = new ExcelContextTool(_context);
        var result = await tool.GetContextAsync(includeSelection: false, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        using var doc = JsonDocument.Parse(TextOf(result));
        var excelBlock = doc.RootElement.GetProperty("excel");

        Assert.False(excelBlock.GetProperty("isAttached").GetBoolean());
        Assert.False(excelBlock.GetProperty("hasActiveWorkbook").GetBoolean());
        Assert.Equal(0, excelBlock.GetProperty("openWorkbookCount").GetInt32());
    }

    [Fact]
    public async Task GetExcelContext_SurfacesBusyError()
    {
        _executor.ContextFailure = BridgeRequestException.Busy("Excel");

        var tool = new ExcelContextTool(_context);
        var result = await tool.GetContextAsync(includeSelection: false, TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        var text = TextOf(result);
        Assert.Contains("Excel", text);
        Assert.Contains("dialog", text);
    }

    [Fact]
    public async Task GetExcelContext_SurfacesNoActiveDocumentError()
    {
        _executor.ContextFailure = BridgeRequestException.NoActiveDocument("Excel", "workbook (.xlsx)");

        var tool = new ExcelContextTool(_context);
        var result = await tool.GetContextAsync(includeSelection: false, TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        var text = TextOf(result);
        Assert.Contains("No workbook (.xlsx) is open in Excel", text);
    }

    [Fact]
    public async Task GetExcelContext_WithoutBridge_ReturnsHelpfulErrorWithoutMachinePaths()
    {
        var orphanPipeName = "hpexcel-orphan-" + Guid.NewGuid().ToString("N");
        await using var orphanClient = new RevitBridgeClient(
            Options.Create(new BridgeOptions
            {
                HostId = "excel",
                HostVersion = 2026,
                PipeName = orphanPipeName,
                ConnectTimeoutMs = 200
            }),
            NullLogger<RevitBridgeClient>.Instance,
            ExcelHostProfile.Instance);

        var orphanContext = new ContextService(orphanClient, new ResultFormatter());
        var tool = new ExcelContextTool(orphanContext);
        var result = await tool.GetContextAsync(includeSelection: false, TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        var text = TextOf(result);
        Assert.Contains("Excel bridge not connected", text);
        Assert.Contains("HPExcel.McpBridge.exe", text);
        Assert.Contains("hpexcel-mcp-2026", text);
        Assert.DoesNotContain(Environment.UserName, text);
        Assert.DoesNotContain(@"C:\", text);
    }
}
