using System.Text.Json;
using HPNavis.Mcp.Server.Hosts;
using HPNavis.Mcp.Server.Resources;
using HPNavis.Mcp.Server.Tools;
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

namespace HPNavis.Mcp.Server.Tests;

/// <summary>
///     The Navisworks tool classes talking through the engine to a bridge listener over a real named pipe,
///     with a fake behind the dispatcher instead of Roamer.exe: the wire carries `navis.*` methods, the
///     context JSON shows the Navisworks block and no Revit-named field, execute passes its parameters
///     through with the 600 s ceiling this profile advertises, and the bridge's refusal codes name the host.
/// </summary>
public sealed class NavisToolsOverPipeTests : IAsyncLifetime
{
    private readonly string _pipeName = "hpnavis-mcp-test-" + Guid.NewGuid().ToString("N");
    private readonly FakeRevitExecutor _executor = new FakeRevitExecutor();
    private readonly BridgeSettings _settings = new BridgeSettings { ExecutionEnabled = true };
    private PipeListener _listener = null!;
    private RevitBridgeClient _client = null!;
    private ContextService _context = null!;
    private ExecuteCodeService _execute = null!;
    private IOptions<BridgeOptions> _options = null!;

    public ValueTask InitializeAsync()
    {
        _listener = new PipeListener(_pipeName, new RequestDispatcher(_executor, _settings, "2026", "Navisworks"));
        _listener.Start();

        _options = Options.Create(new BridgeOptions { HostId = "navis", HostVersion = 2026, PipeName = _pipeName, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 });
        _client = new RevitBridgeClient(_options, NullLogger<RevitBridgeClient>.Instance, NavisHostProfile.Instance);
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

    private static ContextResult NavisContext(bool includeSelection) => new ContextResult
    {
        RevitVersion = "2026", Host = "navis", HostVersion = "2026", DocTitle = "gatehouse_pub.nwd", IsModifiable = true,
        Units = new UnitsInfo("Millimeters"),
        Navis = new NavisInfo("Millimeters", 2, [new ModelSummary("gatehouse_pub.nwd", "Millimeters", null), new ModelSummary("MEP.nwc", "Feet", "MEP.rvt")],
            SelectionSetCount: 3, SavedViewpointCount: 7, ClashTestCount: 1, HasClashModule: true, HeavyOperationsEnabled: false, IsClear: false, IsBusy: false, IsModified: true),
        Selection = includeSelection ? [new ElementInfo(0x5A3F, "Wall", "Basic Wall [123]")] : [],
    };

    [Fact]
    public async Task Context_tool_returns_the_navis_block_and_hides_the_revit_named_fields()
    {
        _executor.ContextHandler = NavisContext;

        var result = await new NavisContextTool(_context).GetContextAsync(includeSelection: true, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        using var json = JsonDocument.Parse(TextOf(result));
        var root = json.RootElement;
        Assert.Equal("navis", root.GetProperty("host").GetString());
        Assert.Equal("2026", root.GetProperty("hostVersion").GetString());
        Assert.False(root.TryGetProperty("revitVersion", out _));
        Assert.False(root.TryGetProperty("isFamily", out _));
        Assert.False(root.TryGetProperty("autocad", out _));
        var navis = root.GetProperty("navis");
        Assert.Equal(2, navis.GetProperty("modelCount").GetInt32());
        Assert.Equal("Feet", navis.GetProperty("models")[1].GetProperty("units").GetString());
        Assert.True(navis.GetProperty("hasClashModule").GetBoolean());
        Assert.False(navis.GetProperty("heavyOperationsEnabled").GetBoolean());
        Assert.Equal(7, navis.GetProperty("savedViewpointCount").GetInt32());
        Assert.Equal("Wall", root.GetProperty("selection")[0].GetProperty("category").GetString());
        Assert.True(_executor.LastContextIncludedSelection);
    }

    [Fact]
    public async Task Document_resource_is_the_same_snapshot_as_json_text()
    {
        _executor.ContextHandler = NavisContext;

        var text = await new NavisDocumentResources(_context).DocumentInfoAsync(TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(text);
        Assert.Equal("gatehouse_pub.nwd", json.RootElement.GetProperty("docTitle").GetString());
        Assert.Equal("Millimeters", json.RootElement.GetProperty("navis").GetProperty("documentUnits").GetString());
        Assert.False(json.RootElement.TryGetProperty("revitVersion", out _));
        Assert.False(_executor.LastContextIncludedSelection);
    }

    [Fact]
    public async Task Execute_tool_sends_the_request_over_the_navis_method_and_returns_the_bridge_result()
    {
        _executor.ExecuteHandler = request => new ExecuteResult
        {
            Value = JsonSerializer.SerializeToElement(new { sets = 2 }), ValueType = "object",
            Changed = new ChangedCounts(1, 0, 0), RolledBack = request.DryRun, DurationMs = 4,
        };

        var args = JsonSerializer.SerializeToElement(new { name = "MCP walls" });
        var result = await new ExecuteNavisCodeTool(_execute).ExecuteAsync(
            "return 1;", TransactionModes.Auto, dryRun: true, timeoutSeconds: 20, label: "walls", args: args, progress: null, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var request = _executor.LastExecuteRequest!;
        Assert.Equal("return 1;", request.Code);
        Assert.Equal("auto", request.Transaction);
        Assert.True(request.DryRun);
        Assert.Equal(20, request.TimeoutSeconds);
        Assert.Equal("walls", request.Label);
        Assert.Equal("MCP walls", request.Args!.Value.GetProperty("name").GetString());

        using var json = JsonDocument.Parse(TextOf(result));
        Assert.True(json.RootElement.GetProperty("rolledBack").GetBoolean());
        Assert.Equal(1, json.RootElement.GetProperty("changed").GetProperty("added").GetInt32());
    }

    [Fact]
    public async Task Execute_tool_lets_a_600_second_timeout_through_and_clamps_above_it()
    {
        // the Navisworks profile advertises 600 s (a clash run or append cannot be interrupted); the default
        // profile's 120 s clamp is covered by the engine tests (NavisProfileTests) — the bridge decides at run time
        var accepted = await new ExecuteNavisCodeTool(_execute).ExecuteAsync(
            "return 1;", TransactionModes.Auto, dryRun: false, timeoutSeconds: 600, label: null, args: null, progress: null, TestContext.Current.CancellationToken);
        Assert.False(accepted.IsError);
        Assert.Equal(600, _executor.LastExecuteRequest!.TimeoutSeconds);

        var clamped = await new ExecuteNavisCodeTool(_execute).ExecuteAsync(
            "return 1;", TransactionModes.Auto, dryRun: false, timeoutSeconds: 900, label: null, args: null, progress: null, TestContext.Current.CancellationToken);
        Assert.False(clamped.IsError);
        Assert.Equal(600, _executor.LastExecuteRequest!.TimeoutSeconds);
    }

    [Fact]
    public async Task Execute_tool_surfaces_the_opt_in_refusal_naming_navisworks()
    {
        _settings.ExecutionEnabled = false;

        var result = await new ExecuteNavisCodeTool(_execute).ExecuteAsync(
            "return 1;", TransactionModes.None, dryRun: false, timeoutSeconds: 5, label: null, args: null, progress: null, TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Contains("Allow AI code execution", TextOf(result));
        Assert.Contains("Navisworks", TextOf(result));
    }

    [Fact]
    public async Task Context_tool_surfaces_busy_and_no_model_as_errors_that_name_navisworks()
    {
        _executor.ContextFailure = BridgeRequestException.Busy("Navisworks");
        var busy = await new NavisContextTool(_context).GetContextAsync(includeSelection: false, TestContext.Current.CancellationToken);
        Assert.True(busy.IsError);
        Assert.Contains("Navisworks", TextOf(busy));
        Assert.Contains("dialog", TextOf(busy));

        _executor.ContextFailure = BridgeRequestException.NoActiveDocument("Navisworks", "model (.nwd/.nwf/.nwc)");
        var noModel = await new NavisContextTool(_context).GetContextAsync(includeSelection: false, TestContext.Current.CancellationToken);
        Assert.True(noModel.IsError);
        Assert.Contains(".nwd", TextOf(noModel));
    }

    [Fact]
    public async Task Without_a_bridge_the_error_names_navisworks_and_carries_no_machine_path()
    {
        await using var orphan = new RevitBridgeClient(
            Options.Create(new BridgeOptions { HostId = "navis", HostVersion = 2026, PipeName = "hpnavis-mcp-nobody-" + Guid.NewGuid().ToString("N"), ConnectTimeoutMs = 300 }),
            NullLogger<RevitBridgeClient>.Instance, NavisHostProfile.Instance);
        var context = new ContextService(orphan, new ResultFormatter());

        var result = await new NavisContextTool(context).GetContextAsync(includeSelection: false, TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        var text = TextOf(result);
        Assert.Contains("Navisworks", text);
        Assert.DoesNotContain(Environment.UserName, text);
        Assert.DoesNotContain(@"C:\", text);
    }
}
