using System.Text.Json;
using HPEtabs.Mcp.Server.Hosts;
using HPEtabs.Mcp.Server.Resources;
using HPEtabs.Mcp.Server.Tools;
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

namespace HPEtabs.Mcp.Server.Tests;

/// <summary>
///     The ETABS tool classes talking through the engine to a bridge listener over a real named pipe, with a
///     fake behind the dispatcher instead of the bridge app: the wire carries `etabs.*` methods, the context
///     JSON shows the ETABS block and no Revit-named field, execute passes its parameters through with the
///     600 s ceiling this profile advertises, the refusal codes name ETABS, and the messages for a missing
///     bridge or a timeout are the ones the profile supplies (naming the exe, never a machine path).
/// </summary>
public sealed class EtabsToolsOverPipeTests : IAsyncLifetime
{
    private const string DisabledText = "Code execution is disabled. Ask the user to tick 'Allow AI code execution' in the HPEtabs MCP Bridge window (a separate app, not inside ETABS).";

    private readonly string _pipeName = "hpetabs-mcp-test-" + Guid.NewGuid().ToString("N");
    private readonly FakeRevitExecutor _executor = new FakeRevitExecutor();
    private readonly BridgeSettings _settings = new BridgeSettings { ExecutionEnabled = true };
    private PipeListener _listener = null!;
    private RevitBridgeClient _client = null!;
    private ContextService _context = null!;
    private ExecuteCodeService _execute = null!;
    private IOptions<BridgeOptions> _options = null!;

    public ValueTask InitializeAsync()
    {
        _listener = new PipeListener(_pipeName, new RequestDispatcher(_executor, _settings, "22", "ETABS", DisabledText));
        _listener.Start();

        _options = Options.Create(new BridgeOptions { HostId = "etabs", HostVersion = 22, PipeName = _pipeName, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 });
        _client = new RevitBridgeClient(_options, NullLogger<RevitBridgeClient>.Instance, EtabsHostProfile.Instance);
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

    private static ContextResult EtabsContext(bool includeSelection) => new ContextResult
    {
        RevitVersion = "22", Host = "etabs", HostVersion = "22", DocTitle = "Tower.EDB", DocPath = "<path>", IsModifiable = true,
        Units = new UnitsInfo("mm"),
        Etabs = new EtabsInfo(IsAttached: true, AttachedPid: 4321, OapiVersion: "2.10.0.0", IsLocked: true, PresentUnits: "kN_m_C", DatabaseUnits: "kN_m_C",
            DestructiveOperationsEnabled: false, PointCount: 120, FrameCount: 340, AreaCount: 60),
        Selection = includeSelection ? [new ElementInfo(1, "Frame", "F12")] : [],
    };

    [Fact]
    public async Task Context_tool_returns_the_etabs_block_and_hides_the_revit_named_fields()
    {
        _executor.ContextHandler = EtabsContext;

        var result = await new EtabsContextTool(_context).GetContextAsync(includeSelection: true, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        using var json = JsonDocument.Parse(TextOf(result));
        var root = json.RootElement;
        Assert.Equal("etabs", root.GetProperty("host").GetString());
        Assert.Equal("22", root.GetProperty("hostVersion").GetString());
        Assert.False(root.TryGetProperty("revitVersion", out _));
        Assert.False(root.TryGetProperty("isFamily", out _));
        Assert.False(root.TryGetProperty("autocad", out _));
        Assert.False(root.TryGetProperty("navis", out _));
        var etabs = root.GetProperty("etabs");
        Assert.True(etabs.GetProperty("isAttached").GetBoolean());
        Assert.Equal(4321, etabs.GetProperty("attachedPid").GetInt32());
        Assert.Equal("2.10.0.0", etabs.GetProperty("oapiVersion").GetString());
        Assert.True(etabs.GetProperty("isLocked").GetBoolean());
        Assert.Equal("kN_m_C", etabs.GetProperty("presentUnits").GetString());
        Assert.False(etabs.GetProperty("destructiveOperationsEnabled").GetBoolean());
        Assert.Equal(340, etabs.GetProperty("frameCount").GetInt32());
        Assert.Equal("Frame", root.GetProperty("selection")[0].GetProperty("category").GetString());
        Assert.True(_executor.LastContextIncludedSelection);
    }

    [Fact]
    public async Task Context_before_attach_reports_is_attached_false_without_units()
    {
        _executor.ContextHandler = _ => new ContextResult
        {
            RevitVersion = "22", Host = "etabs", HostVersion = "22", IsModifiable = false,
            Etabs = new EtabsInfo(false, 0, null, false, null, null, false, 0, 0, 0),
        };

        var result = await new EtabsContextTool(_context).GetContextAsync(includeSelection: false, TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(TextOf(result));
        var etabs = json.RootElement.GetProperty("etabs");
        Assert.False(etabs.GetProperty("isAttached").GetBoolean());
        Assert.False(etabs.TryGetProperty("oapiVersion", out _));
        Assert.False(etabs.TryGetProperty("presentUnits", out _));
        Assert.False(json.RootElement.GetProperty("isModifiable").GetBoolean());
    }

    [Fact]
    public async Task Model_resource_is_the_same_snapshot_as_json_text()
    {
        _executor.ContextHandler = EtabsContext;

        var text = await new EtabsDocumentResources(_context).ModelInfoAsync(TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(text);
        Assert.Equal("Tower.EDB", json.RootElement.GetProperty("docTitle").GetString());
        Assert.Equal(60, json.RootElement.GetProperty("etabs").GetProperty("areaCount").GetInt32());
        Assert.False(json.RootElement.TryGetProperty("revitVersion", out _));
        Assert.False(_executor.LastContextIncludedSelection);
    }

    [Fact]
    public async Task Execute_tool_sends_the_request_over_the_etabs_method_and_returns_the_bridge_result_with_the_snapshot_name()
    {
        _executor.ExecuteHandler = request => new ExecuteResult
        {
            Value = JsonSerializer.SerializeToElement(new { assigned = 3 }), ValueType = "object",
            Changed = new ChangedCounts(0, 0, 0), RolledBack = false, DurationMs = 4,
            Snapshot = "20260917-001500-assign-C40x40.EDB",
        };

        var args = JsonSerializer.SerializeToElement(new { section = "C40x40" });
        var result = await new ExecuteEtabsCodeTool(_execute).ExecuteAsync(
            "return 1;", TransactionModes.Auto, dryRun: false, timeoutSeconds: 20, label: "assign C40x40", args: args, progress: null, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var request = _executor.LastExecuteRequest!;
        Assert.Equal("return 1;", request.Code);
        Assert.Equal("auto", request.Transaction);
        Assert.False(request.DryRun);
        Assert.Equal(20, request.TimeoutSeconds);
        Assert.Equal("assign C40x40", request.Label);
        Assert.Equal("C40x40", request.Args!.Value.GetProperty("section").GetString());

        using var json = JsonDocument.Parse(TextOf(result));
        Assert.Equal("20260917-001500-assign-C40x40.EDB", json.RootElement.GetProperty("snapshot").GetString());
        Assert.False(json.RootElement.GetProperty("rolledBack").GetBoolean());
    }

    [Fact]
    public async Task Execute_tool_lets_a_600_second_timeout_through_and_clamps_above_it()
    {
        var accepted = await new ExecuteEtabsCodeTool(_execute).ExecuteAsync(
            "return 1;", TransactionModes.Auto, dryRun: false, timeoutSeconds: 600, label: null, args: null, progress: null, TestContext.Current.CancellationToken);
        Assert.False(accepted.IsError);
        Assert.Equal(600, _executor.LastExecuteRequest!.TimeoutSeconds);

        var clamped = await new ExecuteEtabsCodeTool(_execute).ExecuteAsync(
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

        var result = await new ExecuteEtabsCodeTool(_execute).ExecuteAsync(
            "return sapModel.FrameObj.SetSection(\"F1\", \"C40x40\");", TransactionModes.None, dryRun: true, timeoutSeconds: 10, label: null, args: null, progress: null, TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Contains("PREVIEW", TextOf(result));
        Assert.Contains("FrameObj.SetSection", TextOf(result));
    }

    [Fact]
    public async Task Execute_tool_surfaces_the_opt_in_refusal_that_names_the_separate_app()
    {
        _settings.ExecutionEnabled = false;

        var result = await new ExecuteEtabsCodeTool(_execute).ExecuteAsync(
            "return 1;", TransactionModes.None, dryRun: false, timeoutSeconds: 5, label: null, args: null, progress: null, TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Contains("Allow AI code execution", TextOf(result));
        Assert.Contains("not inside ETABS", TextOf(result));
    }

    [Fact]
    public async Task Execute_tool_surfaces_the_destructive_refusal_as_the_same_code_without_recording_a_run()
    {
        _executor.ExecuteHandler = _ => throw new BridgeRequestException(BridgeErrorCode.ExecutionDisabled,
            "Destructive operations are disabled. Ask the user to tick 'Allow destructive operations' in the HPEtabs MCP Bridge window.");

        var result = await new ExecuteEtabsCodeTool(_execute).ExecuteAsync(
            "return sapModel.Analyze.RunAnalysis();", TransactionModes.Auto, dryRun: false, timeoutSeconds: 600, label: "analysis", args: null, progress: null, TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Contains("Allow destructive operations", TextOf(result));
    }

    [Fact]
    public async Task Context_tool_surfaces_busy_not_attached_and_no_model_as_errors_that_name_etabs()
    {
        _executor.ContextFailure = BridgeRequestException.Busy("ETABS");
        var busy = await new EtabsContextTool(_context).GetContextAsync(includeSelection: false, TestContext.Current.CancellationToken);
        Assert.True(busy.IsError);
        Assert.Contains("ETABS", TextOf(busy));
        Assert.Contains("dialog", TextOf(busy));

        _executor.ContextFailure = new BridgeRequestException(BridgeErrorCode.NoActiveDocument, "ETABS not attached — click Attach in the HPEtabs MCP Bridge window.");
        var detached = await new EtabsContextTool(_context).GetContextAsync(includeSelection: false, TestContext.Current.CancellationToken);
        Assert.True(detached.IsError);
        Assert.Contains("click Attach", TextOf(detached));

        _executor.ContextFailure = BridgeRequestException.NoActiveDocument("ETABS", "model (.EDB)");
        var noModel = await new EtabsContextTool(_context).GetContextAsync(includeSelection: false, TestContext.Current.CancellationToken);
        Assert.True(noModel.IsError);
        Assert.Contains(".EDB", TextOf(noModel));
    }

    [Fact]
    public async Task Without_a_bridge_the_error_names_the_bridge_exe_and_carries_no_machine_path()
    {
        await using var orphan = new RevitBridgeClient(
            Options.Create(new BridgeOptions { HostId = "etabs", HostVersion = 22, PipeName = "hpetabs-mcp-nobody-" + Guid.NewGuid().ToString("N"), ConnectTimeoutMs = 300 }),
            NullLogger<RevitBridgeClient>.Instance, EtabsHostProfile.Instance);
        var context = new ContextService(orphan, new ResultFormatter());

        var result = await new EtabsContextTool(context).GetContextAsync(includeSelection: false, TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        var text = TextOf(result);
        Assert.Contains("ETABS bridge not connected", text);
        Assert.Contains("HPEtabs.McpBridge.exe", text);
        Assert.Contains("hpetabs-mcp-22", text);
        Assert.DoesNotContain("enable the HP MCP Bridge", text);
        Assert.DoesNotContain(Environment.UserName, text);
        Assert.DoesNotContain(@"C:\", text);
    }

    [Fact]
    public async Task A_timeout_tells_the_model_that_changes_may_have_persisted()
    {
        _executor.ProgressSteps = 10;
        _executor.ProgressDelayMs = 100;
        await using var impatient = new RevitBridgeClient(
            Options.Create(new BridgeOptions { HostId = "etabs", HostVersion = 22, PipeName = _pipeName, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60, ExtraTimeoutSeconds = 0 }),
            NullLogger<RevitBridgeClient>.Instance, EtabsHostProfile.Instance);

        var error = await Assert.ThrowsAsync<BridgeTimeoutException>(() => impatient.SendAsync<ExecuteResult>(
            "etabs.execute", new ExecuteRequest("return 1;", "none", false, 5, "slow", null), TimeSpan.FromMilliseconds(300), null, TestContext.Current.CancellationToken));

        Assert.Contains("persisted", error.Message);
        Assert.Contains("no rollback", error.Message);
        Assert.DoesNotContain("nothing has been committed", error.Message);
    }
}
