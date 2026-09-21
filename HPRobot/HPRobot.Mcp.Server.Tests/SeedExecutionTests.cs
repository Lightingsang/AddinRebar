using System.Text.Json;
using HPRobot.Mcp.Server.Hosts.Robot;
using HPRobot.Mcp.Server.Hosts.Robot.Resources;
using HPRobot.Mcp.Server.Hosts.Robot.Tools;
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

namespace HPRobot.Mcp.Server.Tests;

/// <summary>
///     The Robot tool classes talking through the engine to a bridge listener over a real named pipe, with a
///     fake behind the dispatcher instead of the bridge app: the wire carries `robot.*` methods, the context
///     JSON shows the Robot block and no Revit-named field, execute passes its parameters through with the
///     300 s ceiling this profile advertises, and the refusal codes name Robot.
/// </summary>
public sealed class SeedExecutionTests : IAsyncLifetime
{
    private const string DisabledText =
        "Code execution is disabled. Ask the user to tick 'Allow AI execution' in the HPRobot MCP Bridge window (a separate desktop app, not inside Robot).";

    private readonly string _pipeName = "hprobot-mcp-test-" + Guid.NewGuid().ToString("N");
    private readonly FakeRevitExecutor _executor = new();
    private readonly BridgeSettings _settings = new() { ExecutionEnabled = true };

    private PipeListener _listener = null!;
    private RevitBridgeClient _client = null!;
    private ContextService _context = null!;
    private ExecuteCodeService _execute = null!;
    private IOptions<BridgeOptions> _options = null!;

    public ValueTask InitializeAsync()
    {
        _listener = new PipeListener(
            _pipeName,
            new RequestDispatcher(_executor, _settings, "2026", "Robot", DisabledText));
        _listener.Start();

        _options = Options.Create(new BridgeOptions
        {
            HostId = "robot",
            HostVersion = 2026,
            PipeName = _pipeName,
            ConnectTimeoutMs = 3000,
            PingIntervalSeconds = 60
        });

        _client = new RevitBridgeClient(_options, NullLogger<RevitBridgeClient>.Instance, RobotHostProfile.Instance);
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

    private static ContextResult StandardRobotContext(bool includeSelection) => new()
    {
        Host = "robot",
        HostVersion = "2026",
        DocTitle = "TowerFrame.rtd",
        DocPath = @"C:\Models\TowerFrame.rtd",
        IsModifiable = true,
        Units = new UnitsInfo("m"),
        Robot = new RobotInfo(
            IsAttached: true,
            AttachedPid: 5432,
            RobotVersion: "39.0.1.11984",
            StructureType: "I_ST_FRAME_3D",
            IsCalculated: true,
            HeavyOperationsEnabled: true,
            NodeCount: 120,
            BarCount: 240,
            PanelCount: 30,
            LoadCaseCount: 8),
        Selection = includeSelection ? [new ElementInfo(1, "Bar", "HEA 200")] : []
    };

    [Fact]
    public async Task Ping_RoundTripsOverPipe_WithRobotHostState()
    {
        var pong = await _client.SendAsync<BridgePingResult>(
            "robot.ping", null, TimeSpan.FromSeconds(5), null, TestContext.Current.CancellationToken);

        Assert.True(pong.Pong);
        Assert.Equal("2026", pong.RevitVersion);
        Assert.True(pong.ExecutionEnabled);
        Assert.True(_client.IsConnected);

        _settings.ExecutionEnabled = false;
        pong = await _client.SendAsync<BridgePingResult>(
            "robot.ping", null, TimeSpan.FromSeconds(5), null, TestContext.Current.CancellationToken);
        Assert.False(pong.ExecutionEnabled);
    }

    [Fact]
    public async Task Context_Tool_ReturnsRobotBlock_AndHidesRevitNamedFields()
    {
        _executor.ContextHandler = StandardRobotContext;

        var result = await new GetRobotContextTool(_context).GetContextAsync(
            includeSelection: true, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        using var json = JsonDocument.Parse(TextOf(result));
        var root = json.RootElement;

        Assert.Equal("robot", root.GetProperty("host").GetString());
        Assert.Equal("2026", root.GetProperty("hostVersion").GetString());
        Assert.False(root.TryGetProperty("revitVersion", out _));
        Assert.False(root.TryGetProperty("isFamily", out _));
        Assert.False(root.TryGetProperty("etabs", out _));
        Assert.False(root.TryGetProperty("sap2000", out _));
        Assert.False(root.TryGetProperty("navis", out _));

        var robot = root.GetProperty("robot");
        Assert.True(robot.GetProperty("isAttached").GetBoolean());
        Assert.Equal(5432, robot.GetProperty("attachedPid").GetInt32());
        Assert.Equal("39.0.1.11984", robot.GetProperty("robotVersion").GetString());
        Assert.Equal("I_ST_FRAME_3D", robot.GetProperty("structureType").GetString());
        Assert.True(robot.GetProperty("isCalculated").GetBoolean());
        Assert.True(robot.GetProperty("heavyOperationsEnabled").GetBoolean());
        Assert.Equal(120, robot.GetProperty("nodeCount").GetInt32());
        Assert.Equal(240, robot.GetProperty("barCount").GetInt32());

        Assert.Equal("Bar", root.GetProperty("selection")[0].GetProperty("category").GetString());
        Assert.True(_executor.LastContextIncludedSelection);
    }

    [Fact]
    public async Task Context_BeforeAttach_ReportsIsAttachedFalse()
    {
        _executor.ContextHandler = _ => new ContextResult
        {
            Host = "robot",
            HostVersion = "2026",
            IsModifiable = false,
            Robot = new RobotInfo(false, null, null, null, false, false, 0, 0, 0, 0)
        };

        var result = await new GetRobotContextTool(_context).GetContextAsync(
            includeSelection: false, TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(TextOf(result));
        var robot = json.RootElement.GetProperty("robot");
        Assert.False(robot.GetProperty("isAttached").GetBoolean());
        Assert.False(json.RootElement.GetProperty("isModifiable").GetBoolean());
    }

    [Fact]
    public async Task Model_Resource_ReturnsSameSnapshotAsJsonText()
    {
        _executor.ContextHandler = StandardRobotContext;

        var text = await new RobotResourceProvider(_context).ModelInfoAsync(TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(text);
        Assert.Equal("TowerFrame.rtd", json.RootElement.GetProperty("docTitle").GetString());
        Assert.Equal(240, json.RootElement.GetProperty("robot").GetProperty("barCount").GetInt32());
        Assert.False(json.RootElement.TryGetProperty("revitVersion", out _));
    }

    [Fact]
    public async Task Execute_Tool_SendsRequestOverRobotMethod_AndReturnsSnapshot()
    {
        _executor.ExecuteHandler = request => new ExecuteResult
        {
            Value = JsonSerializer.SerializeToElement(new { createdBar = 42 }),
            ValueType = "object",
            Changed = new ChangedCounts(1, 0, 0),
            RolledBack = false,
            DurationMs = 15,
            Snapshot = "20260921-221500-draw_bar.rtd"
        };

        var args = JsonSerializer.SerializeToElement(new { section = "HEA 200" });
        var result = await new ExecuteRobotCodeTool(_execute).ExecuteAsync(
            "return 1;", TransactionModes.Auto, dryRun: false, timeoutSeconds: 30,
            label: "draw bar", args: args, progress: null, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var request = _executor.LastExecuteRequest!;
        Assert.Equal("return 1;", request.Code);
        Assert.Equal("auto", request.Transaction);
        Assert.False(request.DryRun);
        Assert.Equal(30, request.TimeoutSeconds);
        Assert.Equal("draw bar", request.Label);
        Assert.Equal("HEA 200", request.Args!.Value.GetProperty("section").GetString());

        using var json = JsonDocument.Parse(TextOf(result));
        Assert.Equal("20260921-221500-draw_bar.rtd", json.RootElement.GetProperty("snapshot").GetString());
        Assert.False(json.RootElement.GetProperty("rolledBack").GetBoolean());
    }

    [Fact]
    public async Task Execute_Tool_AllowsUpTo300Seconds_AndClampsAbove()
    {
        var accepted = await new ExecuteRobotCodeTool(_execute).ExecuteAsync(
            "return 1;", TransactionModes.Auto, dryRun: false, timeoutSeconds: 300,
            label: null, args: null, progress: null, TestContext.Current.CancellationToken);
        Assert.False(accepted.IsError);
        Assert.Equal(300, _executor.LastExecuteRequest!.TimeoutSeconds);

        var clamped = await new ExecuteRobotCodeTool(_execute).ExecuteAsync(
            "return 1;", TransactionModes.Auto, dryRun: false, timeoutSeconds: 600,
            label: null, args: null, progress: null, TestContext.Current.CancellationToken);
        Assert.False(clamped.IsError);
        Assert.Equal(300, _executor.LastExecuteRequest!.TimeoutSeconds);
    }

    [Fact]
    public async Task StaticPreview_ComesBackAsError_WithPreviewDiagnostic()
    {
        _executor.ExecuteHandler = _ => new ExecuteResult
        {
            IsError = true,
            RolledBack = true,
            Message = "static preview: would call structure.Bars.Create — send transaction:auto with dryRun:false to run it",
            Diagnostics = [new ScriptDiagnostic(1, 1, "PREVIEW", "structure.Bars.Create (W)")]
        };

        var result = await new ExecuteRobotCodeTool(_execute).ExecuteAsync(
            "structure.Bars.Create(1, 1, 2); return 1;", TransactionModes.None, dryRun: true,
            timeoutSeconds: 30, label: null, args: null, progress: null, TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Contains("PREVIEW", TextOf(result));
        Assert.Contains("structure.Bars.Create", TextOf(result));
    }

    [Fact]
    public async Task Execute_SurfacesExecutionDisabledRefusal_NamingHPRobotBridge()
    {
        _settings.ExecutionEnabled = false;

        var result = await new ExecuteRobotCodeTool(_execute).ExecuteAsync(
            "return 1;", TransactionModes.None, dryRun: false, timeoutSeconds: 5,
            label: null, args: null, progress: null, TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Contains("Allow AI execution", TextOf(result));
        Assert.Contains("HPRobot MCP Bridge", TextOf(result));
    }

    [Fact]
    public async Task Execute_SurfacesHeavyOperationsDisabledRefusal()
    {
        _executor.ExecuteHandler = _ => throw new BridgeRequestException(
            BridgeErrorCode.ExecutionDisabled,
            "Heavy operations are disabled. Ask the user to tick 'Allow heavy/destructive operations' in the HPRobot MCP Bridge window.");

        var result = await new ExecuteRobotCodeTool(_execute).ExecuteAsync(
            "robot.Project.CalcEngine.Calculate(); return 1;", TransactionModes.Auto, dryRun: false,
            timeoutSeconds: 300, label: "calc", args: null, progress: null, TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Contains("Allow heavy/destructive operations", TextOf(result));
    }

    [Fact]
    public async Task Context_Tool_SurfacesBusy_NotAttached_AndNoModel_AsErrorsNamingRobot()
    {
        _executor.ContextFailure = BridgeRequestException.Busy("Robot");
        var busy = await new GetRobotContextTool(_context).GetContextAsync(false, TestContext.Current.CancellationToken);
        Assert.True(busy.IsError);
        Assert.Contains("Robot", TextOf(busy));
        Assert.Contains("dialog", TextOf(busy));

        _executor.ContextFailure = new BridgeRequestException(BridgeErrorCode.NoActiveDocument, "Robot not attached — click Attach in HPRobot MCP Bridge.");
        var detached = await new GetRobotContextTool(_context).GetContextAsync(false, TestContext.Current.CancellationToken);
        Assert.True(detached.IsError);
        Assert.Contains("click Attach", TextOf(detached));

        _executor.ContextFailure = BridgeRequestException.NoActiveDocument("Robot", "model (.rtd)");
        var noModel = await new GetRobotContextTool(_context).GetContextAsync(false, TestContext.Current.CancellationToken);
        Assert.True(noModel.IsError);
        Assert.Contains(".rtd", TextOf(noModel));
    }

    [Fact]
    public async Task WithoutBridge_ErrorNamesBridgeExe_AndCarriesNoMachinePath()
    {
        await using var orphan = new RevitBridgeClient(
            Options.Create(new BridgeOptions
            {
                HostId = "robot",
                HostVersion = 2026,
                PipeName = "hprobot-mcp-nobody-" + Guid.NewGuid().ToString("N"),
                ConnectTimeoutMs = 300
            }),
            NullLogger<RevitBridgeClient>.Instance, RobotHostProfile.Instance);

        var context = new ContextService(orphan, new ResultFormatter());
        var result = await new GetRobotContextTool(context).GetContextAsync(false, TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        var text = TextOf(result);
        Assert.Contains("Robot Structural Analysis bridge not connected", text);
        Assert.Contains("HPRobot.McpBridge.exe", text);
        Assert.Contains("hprobot-mcp-2026", text);
        Assert.DoesNotContain("enable the HP MCP Bridge", text);
        Assert.DoesNotContain(Environment.UserName, text);
        Assert.DoesNotContain(@"C:\", text);
    }

    [Fact]
    public async Task Cancel_DispatchesToBridgeExecutor()
    {
        var cancelResult = await _client.SendAsync<CancelResult>(
            "robot.cancel", new { id = 123 }, TimeSpan.FromSeconds(5), null, TestContext.Current.CancellationToken);

        Assert.Equal(1, _executor.CancelCalls);
    }

    [Fact]
    public async Task Timeout_InformsModelThatChangesMayHavePersisted()
    {
        _executor.ProgressSteps = 10;
        _executor.ProgressDelayMs = 100;

        await using var impatientClient = new RevitBridgeClient(
            Options.Create(new BridgeOptions
            {
                HostId = "robot",
                HostVersion = 2026,
                PipeName = _pipeName,
                ConnectTimeoutMs = 3000,
                PingIntervalSeconds = 60,
                ExtraTimeoutSeconds = 0
            }),
            NullLogger<RevitBridgeClient>.Instance, RobotHostProfile.Instance);

        var error = await Assert.ThrowsAsync<BridgeTimeoutException>(() => impatientClient.SendAsync<ExecuteResult>(
            "robot.execute", new ExecuteRequest("return 1;", "none", false, 5, "slow", null),
            TimeSpan.FromMilliseconds(200), null, TestContext.Current.CancellationToken));

        Assert.Contains("persisted (no rollback)", error.Message);
        Assert.Contains("snapshot", error.Message);
        Assert.DoesNotContain("nothing has been committed", error.Message);

        var deadline = DateTime.UtcNow.AddSeconds(3);
        while (DateTime.UtcNow < deadline && _executor.CancelCalls == 0)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }
        Assert.True(_executor.CancelCalls > 0, "Cancel() was not called on the executor when client timed out.");
    }
}
