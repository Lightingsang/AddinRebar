using System.Text.Json;
using HPTekla.Mcp.Server.Hosts.Tekla;
using HPTekla.Mcp.Server.Hosts.Tekla.Resources;
using HPTekla.Mcp.Server.Hosts.Tekla.Tools;
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

namespace HPTekla.Mcp.Server.Tests;

/// <summary>
///     The Tekla tool classes talking through the engine to a bridge listener over a real named pipe, with a
///     fake behind the dispatcher: the wire carries `tekla.*` methods, the context JSON shows the Tekla block
///     and no Revit-named field, execute passes its parameters through with the 600 s ceiling this profile
///     advertises, and the refusal codes name Tekla.
/// </summary>
public sealed class SeedExecutionTests : IAsyncLifetime
{
    private const string DisabledText =
        "Code execution is disabled. Ask the user to tick 'Allow AI execution' in the HPTekla MCP Bridge extension inside Tekla Structures.";

    private readonly string _pipeName = "hptekla-mcp-test-" + Guid.NewGuid().ToString("N");
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
            new RequestDispatcher(_executor, _settings, "2025", "Tekla Structures", DisabledText));
        _listener.Start();

        _options = Options.Create(new BridgeOptions
        {
            HostId = "tekla",
            HostVersion = 2025,
            PipeName = _pipeName,
            ConnectTimeoutMs = 3000,
            PingIntervalSeconds = 60
        });

        _client = new RevitBridgeClient(_options, NullLogger<RevitBridgeClient>.Instance, TeklaHostProfile.Instance);
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

    private static ContextResult StandardTeklaContext(bool includeSelection) => new()
    {
        Host = "tekla",
        HostVersion = "2025",
        DocTitle = "TowerModel",
        DocPath = @"C:\TeklaStructuresModels\TowerModel",
        IsModifiable = true,
        Units = new UnitsInfo("mm"),
        Tekla = new TeklaInfo(
            IsConnected: true,
            ModelName: "TowerModel",
            ModelPath: @"C:\TeklaStructuresModels\TowerModel",
            ProjectName: "HighRiseTower",
            TeklaVersion: "2025.0",
            HeavyOperationsEnabled: true,
            PartCount: 350,
            RebarCount: 1200,
            DrawingCount: 42),
        Selection = includeSelection ? [new ElementInfo(101, "Beam", "HEA 300")] : []
    };

    [Fact]
    public async Task Ping_RoundTripsOverPipe_WithTeklaHostState()
    {
        var pong = await _client.SendAsync<BridgePingResult>(
            "tekla.ping", null, TimeSpan.FromSeconds(5), null, TestContext.Current.CancellationToken);

        Assert.True(pong.Pong);
        Assert.Equal("2025", pong.RevitVersion);
        Assert.True(pong.ExecutionEnabled);
        Assert.True(_client.IsConnected);

        _settings.ExecutionEnabled = false;
        pong = await _client.SendAsync<BridgePingResult>(
            "tekla.ping", null, TimeSpan.FromSeconds(5), null, TestContext.Current.CancellationToken);
        Assert.False(pong.ExecutionEnabled);
    }

    [Fact]
    public async Task Context_Tool_ReturnsTeklaBlock_AndHidesRevitNamedFields()
    {
        _executor.ContextHandler = StandardTeklaContext;

        var result = await new GetTeklaContextTool(_context).GetContextAsync(
            includeSelection: true, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        using var json = JsonDocument.Parse(TextOf(result));
        var root = json.RootElement;

        Assert.Equal("tekla", root.GetProperty("host").GetString());
        Assert.Equal("2025", root.GetProperty("hostVersion").GetString());
        Assert.False(root.TryGetProperty("revitVersion", out _));
        Assert.False(root.TryGetProperty("isFamily", out _));
        Assert.False(root.TryGetProperty("etabs", out _));
        Assert.False(root.TryGetProperty("sap2000", out _));
        Assert.False(root.TryGetProperty("navis", out _));
        Assert.False(root.TryGetProperty("robot", out _));
        Assert.False(root.TryGetProperty("excel", out _));

        var tekla = root.GetProperty("tekla");
        Assert.True(tekla.GetProperty("isConnected").GetBoolean());
        Assert.Equal("TowerModel", tekla.GetProperty("modelName").GetString());
        Assert.Equal(@"C:\TeklaStructuresModels\TowerModel", tekla.GetProperty("modelPath").GetString());
        Assert.Equal("HighRiseTower", tekla.GetProperty("projectName").GetString());
        Assert.Equal("2025.0", tekla.GetProperty("teklaVersion").GetString());
        Assert.True(tekla.GetProperty("heavyOperationsEnabled").GetBoolean());
        Assert.Equal(350, tekla.GetProperty("partCount").GetInt32());
        Assert.Equal(1200, tekla.GetProperty("rebarCount").GetInt32());
        Assert.Equal(42, tekla.GetProperty("drawingCount").GetInt32());

        Assert.Equal("Beam", root.GetProperty("selection")[0].GetProperty("category").GetString());
        Assert.True(_executor.LastContextIncludedSelection);
    }

    [Fact]
    public async Task Context_BeforeAttach_ReportsIsConnectedFalse()
    {
        _executor.ContextHandler = _ => new ContextResult
        {
            Host = "tekla",
            HostVersion = "2025",
            IsModifiable = false,
            Tekla = new TeklaInfo(false, null, null, null, null, false, 0, 0, 0)
        };

        var result = await new GetTeklaContextTool(_context).GetContextAsync(
            includeSelection: false, TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(TextOf(result));
        var tekla = json.RootElement.GetProperty("tekla");
        Assert.False(tekla.GetProperty("isConnected").GetBoolean());
        Assert.False(json.RootElement.GetProperty("isModifiable").GetBoolean());
    }

    [Fact]
    public async Task Model_Resource_ReturnsSameSnapshotAsJsonText()
    {
        _executor.ContextHandler = StandardTeklaContext;

        var text = await new TeklaResourceProvider(_context).ModelInfoAsync(TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(text);
        Assert.Equal("TowerModel", json.RootElement.GetProperty("docTitle").GetString());
        Assert.Equal(350, json.RootElement.GetProperty("tekla").GetProperty("partCount").GetInt32());
        Assert.False(json.RootElement.TryGetProperty("revitVersion", out _));
    }

    [Fact]
    public async Task Execute_Tool_SendsRequestOverTeklaMethod_AndReturnsResult()
    {
        _executor.ExecuteHandler = request => new ExecuteResult
        {
            Value = JsonSerializer.SerializeToElement(new { createdId = 1002 }),
            ValueType = "object",
            Changed = new ChangedCounts(1, 0, 0),
            RolledBack = false,
            DurationMs = 25
        };

        var args = JsonSerializer.SerializeToElement(new { profile = "HEA300" });
        var result = await new ExecuteTeklaCodeTool(_execute).ExecuteAsync(
            "return 1;", TransactionModes.Auto, dryRun: false, timeoutSeconds: 30,
            label: "create beam", args: args, progress: null, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var request = _executor.LastExecuteRequest!;
        Assert.Equal("return 1;", request.Code);
        Assert.Equal("auto", request.Transaction);
        Assert.False(request.DryRun);
        Assert.Equal(30, request.TimeoutSeconds);
        Assert.Equal("create beam", request.Label);
        Assert.Equal("HEA300", request.Args!.Value.GetProperty("profile").GetString());

        using var json = JsonDocument.Parse(TextOf(result));
        Assert.Equal(1002, json.RootElement.GetProperty("value").GetProperty("createdId").GetInt32());
        Assert.False(json.RootElement.GetProperty("rolledBack").GetBoolean());
    }

    [Fact]
    public async Task Execute_Tool_AllowsUpTo600Seconds_AndClampsAbove()
    {
        var accepted = await new ExecuteTeklaCodeTool(_execute).ExecuteAsync(
            "return 1;", TransactionModes.Auto, dryRun: false, timeoutSeconds: 600,
            label: null, args: null, progress: null, TestContext.Current.CancellationToken);
        Assert.False(accepted.IsError);
        Assert.Equal(600, _executor.LastExecuteRequest!.TimeoutSeconds);

        var clamped = await new ExecuteTeklaCodeTool(_execute).ExecuteAsync(
            "return 1;", TransactionModes.Auto, dryRun: false, timeoutSeconds: 1200,
            label: null, args: null, progress: null, TestContext.Current.CancellationToken);
        Assert.False(clamped.IsError);
        Assert.Equal(600, _executor.LastExecuteRequest!.TimeoutSeconds);
    }

    [Fact]
    public async Task Execute_SurfacesExecutionDisabledRefusal_NamingHPTeklaBridge()
    {
        _settings.ExecutionEnabled = false;

        var result = await new ExecuteTeklaCodeTool(_execute).ExecuteAsync(
            "return 1;", TransactionModes.None, dryRun: false, timeoutSeconds: 5,
            label: null, args: null, progress: null, TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Contains("Allow AI execution", TextOf(result));
        Assert.Contains("HPTekla MCP Bridge", TextOf(result));
    }

    [Fact]
    public async Task Execute_SurfacesHeavyOperationsDisabledRefusal()
    {
        _executor.ExecuteHandler = _ => throw new BridgeRequestException(
            BridgeErrorCode.ExecutionDisabled,
            "Heavy operations are disabled. Ask the user to tick 'Allow heavy operations' in the HPTekla MCP Bridge extension.");

        var result = await new ExecuteTeklaCodeTool(_execute).ExecuteAsync(
            "export_ifc(); return 1;", TransactionModes.Auto, dryRun: false,
            timeoutSeconds: 600, label: "ifc export", args: null, progress: null, TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Contains("Allow heavy operations", TextOf(result));
    }

    [Fact]
    public async Task Context_Tool_SurfacesBusy_NotConnected_AndNoModel_AsErrorsNamingTekla()
    {
        _executor.ContextFailure = BridgeRequestException.Busy("Tekla Structures");
        var busy = await new GetTeklaContextTool(_context).GetContextAsync(false, TestContext.Current.CancellationToken);
        Assert.True(busy.IsError);
        Assert.Contains("Tekla Structures", TextOf(busy));
        Assert.Contains("dialog", TextOf(busy));

        _executor.ContextFailure = new BridgeRequestException(BridgeErrorCode.NoActiveDocument, "Tekla bridge not connected — load HPTekla MCP Bridge in Tekla Structures.");
        var detached = await new GetTeklaContextTool(_context).GetContextAsync(false, TestContext.Current.CancellationToken);
        Assert.True(detached.IsError);
        Assert.Contains("HPTekla MCP Bridge", TextOf(detached));

        _executor.ContextFailure = BridgeRequestException.NoActiveDocument("Tekla Structures", "model");
        var noModel = await new GetTeklaContextTool(_context).GetContextAsync(false, TestContext.Current.CancellationToken);
        Assert.True(noModel.IsError);
        Assert.Contains("model", TextOf(noModel));
    }

    [Fact]
    public async Task WithoutBridge_ErrorNamesBridgePlugin_AndCarriesNoMachinePath()
    {
        await using var orphan = new RevitBridgeClient(
            Options.Create(new BridgeOptions
            {
                HostId = "tekla",
                HostVersion = 2025,
                PipeName = "hptekla-mcp-nobody-" + Guid.NewGuid().ToString("N"),
                ConnectTimeoutMs = 300
            }),
            NullLogger<RevitBridgeClient>.Instance, TeklaHostProfile.Instance);

        var context = new ContextService(orphan, new ResultFormatter());
        var result = await new GetTeklaContextTool(context).GetContextAsync(false, TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        var text = TextOf(result);
        Assert.Contains("Tekla Structures bridge not connected", text);
        Assert.Contains("Tekla Structures 2025", text);
        Assert.Contains("HPTekla MCP Bridge", text);
        Assert.Contains("hptekla-mcp-2025", text);
        Assert.DoesNotContain(Environment.UserName, text);
        Assert.DoesNotContain(@"C:\", text);
    }

    [Fact]
    public async Task Cancel_DispatchesToBridgeExecutor()
    {
        var cancelResult = await _client.SendAsync<CancelResult>(
            "tekla.cancel", new { id = 123 }, TimeSpan.FromSeconds(5), null, TestContext.Current.CancellationToken);

        Assert.Equal(1, _executor.CancelCalls);
    }

    [Fact]
    public async Task Timeout_InformsModelThatChangesMayHavePersistedOrDiscarded()
    {
        _executor.ProgressSteps = 10;
        _executor.ProgressDelayMs = 100;

        await using var impatientClient = new RevitBridgeClient(
            Options.Create(new BridgeOptions
            {
                HostId = "tekla",
                HostVersion = 2025,
                PipeName = _pipeName,
                ConnectTimeoutMs = 3000,
                PingIntervalSeconds = 60,
                ExtraTimeoutSeconds = 0
            }),
            NullLogger<RevitBridgeClient>.Instance, TeklaHostProfile.Instance);

        var error = await Assert.ThrowsAsync<BridgeTimeoutException>(() => impatientClient.SendAsync<ExecuteResult>(
            "tekla.execute", new ExecuteRequest("return 1;", "none", false, 5, "slow", null),
            TimeSpan.FromMilliseconds(200), null, TestContext.Current.CancellationToken));

        Assert.Contains("timed out", error.Message);
        Assert.Contains("dryRun=true", error.Message);

        var deadline = DateTime.UtcNow.AddSeconds(3);
        while (DateTime.UtcNow < deadline && _executor.CancelCalls == 0)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }
        Assert.True(_executor.CancelCalls > 0, "Cancel() was not called on the executor when client timed out.");
    }
}
