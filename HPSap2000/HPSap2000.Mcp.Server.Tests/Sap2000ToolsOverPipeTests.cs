using System.Text.Json;
using HPSap2000.Mcp.Server.Hosts;
using HPSap2000.Mcp.Server.Resources;
using HPSap2000.Mcp.Server.Tools;
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

namespace HPSap2000.Mcp.Server.Tests;

/// <summary>
///     The SAP2000 tool classes talking through the engine to a bridge listener over a real named pipe, with a
///     fake behind the dispatcher instead of the bridge app: the wire carries `sap2000.*` methods, the context
///     JSON shows the SAP2000 block and no Revit-named field, execute passes its parameters through with the
///     600 s ceiling this profile advertises, and the refusal codes name SAP2000.
/// </summary>
public sealed class Sap2000ToolsOverPipeTests : IAsyncLifetime
{
    private const string DisabledText = "Code execution is disabled. Ask the user to tick 'Allow AI code execution' in the HPSap2000 MCP Bridge window (a separate app, not inside SAP2000).";

    private readonly string _pipeName = "hpsap2000-mcp-test-" + Guid.NewGuid().ToString("N");
    private readonly FakeRevitExecutor _executor = new FakeRevitExecutor();
    private readonly BridgeSettings _settings = new BridgeSettings { ExecutionEnabled = true };
    private PipeListener _listener = null!;
    private RevitBridgeClient _client = null!;
    private ContextService _context = null!;
    private ExecuteCodeService _execute = null!;
    private IOptions<BridgeOptions> _options = null!;

    public ValueTask InitializeAsync()
    {
        _listener = new PipeListener(_pipeName, new RequestDispatcher(_executor, _settings, "27", "SAP2000", DisabledText));
        _listener.Start();

        _options = Options.Create(new BridgeOptions { HostId = "sap2000", HostVersion = 27, PipeName = _pipeName, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 });
        _client = new RevitBridgeClient(_options, NullLogger<RevitBridgeClient>.Instance, Sap2000HostProfile.Instance);
        var formatter = new ResultFormatter();
        _context = new ContextService(_client, formatter);
        _execute = new ExecuteCodeService(_client, formatter, _options);

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
        await _listener.StopAsync();
        _listener.Dispose();
    }

    private static string TextOf(CallToolResult result) => ((TextContentBlock)result.Content[0]).Text;

    private static ContextResult Sap2000Context(bool includeSelection) => new ContextResult
    {
        RevitVersion = "27", Host = "sap2000", HostVersion = "27", DocTitle = "Bridge.SDB", DocPath = "<path>", IsModifiable = true,
        Units = new UnitsInfo("m"),
        Sap2000 = new Sap2000Info(IsAttached: true, AttachedPid: 4321, OapiVersion: "2.13.0.0", IsLocked: true, PresentUnits: "kN_m_C", DatabaseUnits: "kN_m_C",
            DestructiveOperationsEnabled: false, PointCount: 120, FrameCount: 340, AreaCount: 60),
        Selection = includeSelection ? [new ElementInfo(1, "Frame", "F12")] : [],
    };

    [Fact]
    public async Task Context_tool_returns_the_sap2000_block_and_hides_the_revit_named_fields()
    {
        _executor.ContextHandler = Sap2000Context;

        var result = await new Sap2000ContextTool(_context).GetContextAsync(includeSelection: true, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        using var json = JsonDocument.Parse(TextOf(result));
        var root = json.RootElement;
        Assert.Equal("sap2000", root.GetProperty("host").GetString());
        Assert.Equal("27", root.GetProperty("hostVersion").GetString());
        Assert.False(root.TryGetProperty("revitVersion", out _));
        Assert.False(root.TryGetProperty("isFamily", out _));
        Assert.False(root.TryGetProperty("autocad", out _));
        Assert.False(root.TryGetProperty("navis", out _));
        Assert.False(root.TryGetProperty("etabs", out _));
        var sap = root.GetProperty("sap2000");
        Assert.True(sap.GetProperty("isAttached").GetBoolean());
        Assert.Equal(4321, sap.GetProperty("attachedPid").GetInt32());
        Assert.Equal("2.13.0.0", sap.GetProperty("oapiVersion").GetString());
        Assert.True(sap.GetProperty("isLocked").GetBoolean());
        Assert.Equal("kN_m_C", sap.GetProperty("presentUnits").GetString());
        Assert.False(sap.GetProperty("destructiveOperationsEnabled").GetBoolean());
        Assert.Equal(340, sap.GetProperty("frameCount").GetInt32());
        Assert.Equal("Frame", root.GetProperty("selection")[0].GetProperty("category").GetString());
        Assert.True(_executor.LastContextIncludedSelection);
    }

    [Fact]
    public async Task Context_before_attach_reports_is_attached_false_without_units()
    {
        _executor.ContextHandler = _ => new ContextResult
        {
            RevitVersion = "27", Host = "sap2000", HostVersion = "27", IsModifiable = false,
            Sap2000 = new Sap2000Info(false, 0, null, false, null, null, false, 0, 0, 0),
        };

        var result = await new Sap2000ContextTool(_context).GetContextAsync(includeSelection: false, TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(TextOf(result));
        var sap = json.RootElement.GetProperty("sap2000");
        Assert.False(sap.GetProperty("isAttached").GetBoolean());
        Assert.False(sap.TryGetProperty("oapiVersion", out _));
        Assert.False(sap.TryGetProperty("presentUnits", out _));
        Assert.False(json.RootElement.GetProperty("isModifiable").GetBoolean());
    }

    [Fact]
    public async Task Model_resource_is_the_same_snapshot_as_json_text()
    {
        _executor.ContextHandler = Sap2000Context;

        var text = await new Sap2000DocumentResources(_context).ModelInfoAsync(TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(text);
        Assert.Equal("Bridge.SDB", json.RootElement.GetProperty("docTitle").GetString());
        Assert.Equal(60, json.RootElement.GetProperty("sap2000").GetProperty("areaCount").GetInt32());
        Assert.False(json.RootElement.TryGetProperty("revitVersion", out _));
        Assert.False(_executor.LastContextIncludedSelection);
    }

    [Fact]
    public async Task Execute_tool_sends_the_request_over_the_sap2000_method_and_returns_the_bridge_result_with_the_snapshot_name()
    {
        _executor.ExecuteHandler = request => new ExecuteResult
        {
            Value = JsonSerializer.SerializeToElement(new { assigned = 3 }), ValueType = "object",
            Changed = new ChangedCounts(0, 0, 0), RolledBack = false, DurationMs = 4,
            Snapshot = "20260921-001500-assign-W14X90.SDB",
        };

        var args = JsonSerializer.SerializeToElement(new { section = "W14X90" });
        var result = await new ExecuteSap2000CodeTool(_execute).ExecuteAsync(
            "return 1;", TransactionModes.Auto, dryRun: false, timeoutSeconds: 20, label: "assign W14X90", args: args, progress: null, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var request = _executor.LastExecuteRequest!;
        Assert.Equal("return 1;", request.Code);
        Assert.Equal("auto", request.Transaction);
        Assert.False(request.DryRun);
        Assert.Equal(20, request.TimeoutSeconds);
        Assert.Equal("assign W14X90", request.Label);
        Assert.Equal("W14X90", request.Args!.Value.GetProperty("section").GetString());

        using var json = JsonDocument.Parse(TextOf(result));
        Assert.Equal("20260921-001500-assign-W14X90.SDB", json.RootElement.GetProperty("snapshot").GetString());
        Assert.False(json.RootElement.GetProperty("rolledBack").GetBoolean());
    }

    [Fact]
    public async Task Execute_tool_lets_a_600_second_timeout_through_and_clamps_above_it()
    {
        var accepted = await new ExecuteSap2000CodeTool(_execute).ExecuteAsync(
            "return 1;", TransactionModes.Auto, dryRun: false, timeoutSeconds: 600, label: null, args: null, progress: null, TestContext.Current.CancellationToken);
        Assert.False(accepted.IsError);
        Assert.Equal(600, _executor.LastExecuteRequest!.TimeoutSeconds);

        var clamped = await new ExecuteSap2000CodeTool(_execute).ExecuteAsync(
            "return 1;", TransactionModes.Auto, dryRun: false, timeoutSeconds: 900, label: null, args: null, progress: null, TestContext.Current.CancellationToken);
        Assert.False(clamped.IsError);
        Assert.Equal(600, _executor.LastExecuteRequest!.TimeoutSeconds);
    }

    [Fact]
    public async Task A_static_preview_comes_back_as_an_error_with_the_PREVIEW_diagnostic()
    {
        _executor.ExecuteHandler = _ => new ExecuteResult
        {
            IsError = true, RolledBack = true,
            Message = "static preview: the script would call FrameObj.SetSection — send transaction:auto with dryRun:false to run it",
            Diagnostics = [new ScriptDiagnostic(1, 1, "PREVIEW", "FrameObj.SetSection (W)")],
        };

        var result = await new ExecuteSap2000CodeTool(_execute).ExecuteAsync(
            "return sapModel.FrameObj.SetSection(\"F1\", \"W14X90\");", TransactionModes.None, dryRun: true, timeoutSeconds: 10, label: null, args: null, progress: null, TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Contains("PREVIEW", TextOf(result));
        Assert.Contains("FrameObj.SetSection", TextOf(result));
    }

    [Fact]
    public async Task Execute_tool_surfaces_the_opt_in_refusal_that_names_the_separate_app()
    {
        _settings.ExecutionEnabled = false;

        var result = await new ExecuteSap2000CodeTool(_execute).ExecuteAsync(
            "return 1;", TransactionModes.None, dryRun: false, timeoutSeconds: 5, label: null, args: null, progress: null, TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Contains("Allow AI code execution", TextOf(result));
        Assert.Contains("not inside SAP2000", TextOf(result));
    }

    [Fact]
    public async Task Execute_tool_surfaces_the_destructive_refusal_as_the_same_code_without_recording_a_run()
    {
        _executor.ExecuteHandler = _ => throw new BridgeRequestException(BridgeErrorCode.ExecutionDisabled,
            "Destructive operations are disabled. Ask the user to tick 'Allow destructive operations' in the HPSap2000 MCP Bridge window.");

        var result = await new ExecuteSap2000CodeTool(_execute).ExecuteAsync(
            "return sapModel.Analyze.RunAnalysis();", TransactionModes.Auto, dryRun: false, timeoutSeconds: 600, label: "analysis", args: null, progress: null, TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Contains("Allow destructive operations", TextOf(result));
    }
}
