using System.Text.Json;
using HPAutoCad.Mcp.Server.Hosts;
using HPAutoCad.Mcp.Server.Resources;
using HPAutoCad.Mcp.Server.Tools;
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

namespace HPAutoCad.Mcp.Server.Tests;

/// <summary>
///     The AutoCAD tool classes talking through the engine to a bridge listener over a real named pipe,
///     with a fake behind the dispatcher instead of acad.exe: the wire carries `autocad.*` methods, the
///     context JSON shows the AutoCAD facts and no Revit-named field, execute passes its parameters through.
/// </summary>
public sealed class AutocadToolsOverPipeTests : IAsyncLifetime
{
    private readonly string _pipeName = "hpautocad-mcp-test-" + Guid.NewGuid().ToString("N");
    private readonly FakeRevitExecutor _executor = new FakeRevitExecutor();
    private readonly BridgeSettings _settings = new BridgeSettings { ExecutionEnabled = true };
    private PipeListener _listener = null!;
    private RevitBridgeClient _client = null!;
    private ContextService _context = null!;
    private ExecuteCodeService _execute = null!;

    public ValueTask InitializeAsync()
    {
        _listener = new PipeListener(_pipeName, new RequestDispatcher(_executor, _settings, "2026", "AutoCAD"));
        _listener.Start();

        var options = Options.Create(new BridgeOptions { HostId = "autocad", HostVersion = 2026, PipeName = _pipeName, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 });
        _client = new RevitBridgeClient(options, NullLogger<RevitBridgeClient>.Instance, AutocadHostProfile.Instance);
        var formatter = new ResultFormatter();
        _context = new ContextService(_client, formatter);
        _execute = new ExecuteCodeService(_client, formatter, options);

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
        await _listener.StopAsync();
        _listener.Dispose();
    }

    private static string TextOf(CallToolResult result) => ((TextContentBlock)result.Content[0]).Text;

    [Fact]
    public async Task Context_tool_returns_the_autocad_facts_and_hides_the_revit_named_version()
    {
        _executor.ContextHandler = includeSelection => new ContextResult
        {
            RevitVersion = "2026", Host = "autocad", HostVersion = "2026", DocTitle = "Drawing1.dwg", IsModifiable = true,
            Units = new UnitsInfo("Millimeters"),
            Autocad = new AutocadInfo("Millimeters", "Metric", "Model", "0", true, true, false),
            Selection = includeSelection ? [new ElementInfo(0x25E, "WALLS", "LINE")] : [],
        };

        var result = await new AutocadContextTool(_context).GetContextAsync(includeSelection: true, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        using var json = JsonDocument.Parse(TextOf(result));
        var root = json.RootElement;
        Assert.Equal("autocad", root.GetProperty("host").GetString());
        Assert.Equal("2026", root.GetProperty("hostVersion").GetString());
        Assert.False(root.TryGetProperty("revitVersion", out _));
        Assert.False(root.TryGetProperty("isFamily", out _));
        Assert.Equal("Metric", root.GetProperty("autocad").GetProperty("measurement").GetString());
        Assert.True(root.GetProperty("autocad").GetProperty("isQuiescent").GetBoolean());
        Assert.Equal("WALLS", root.GetProperty("selection")[0].GetProperty("category").GetString());
        Assert.Equal("LINE", root.GetProperty("selection")[0].GetProperty("name").GetString());
        Assert.True(_executor.LastContextIncludedSelection);
    }

    [Fact]
    public async Task Document_resource_is_the_same_snapshot_as_json_text()
    {
        _executor.ContextHandler = _ => new ContextResult { RevitVersion = "2026", Host = "autocad", HostVersion = "2026", DocTitle = "Plan.dwg" };

        var text = await new AutocadDocumentResources(_context).DocumentInfoAsync(TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(text);
        Assert.Equal("Plan.dwg", json.RootElement.GetProperty("docTitle").GetString());
        Assert.False(json.RootElement.TryGetProperty("revitVersion", out _));
    }

    [Fact]
    public async Task Execute_tool_sends_the_request_over_the_autocad_method_and_returns_the_bridge_result()
    {
        _executor.ExecuteHandler = request => new ExecuteResult
        {
            Value = JsonSerializer.SerializeToElement(new { handle = "25E" }), ValueType = "object",
            Changed = new ChangedCounts(1, 0, 0), RolledBack = request.DryRun, DurationMs = 4,
        };

        var args = JsonSerializer.SerializeToElement(new { lengthMm = 1500 });
        var result = await new ExecuteAutocadCodeTool(_execute).ExecuteAsync(
            "return 1;", TransactionModes.Auto, dryRun: true, timeoutSeconds: 20, label: "line", args: args, progress: null, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var request = _executor.LastExecuteRequest!;
        Assert.Equal("return 1;", request.Code);
        Assert.Equal("auto", request.Transaction);
        Assert.True(request.DryRun);
        Assert.Equal(20, request.TimeoutSeconds);
        Assert.Equal("line", request.Label);
        Assert.Equal(1500, request.Args!.Value.GetProperty("lengthMm").GetInt32());

        using var json = JsonDocument.Parse(TextOf(result));
        Assert.True(json.RootElement.GetProperty("rolledBack").GetBoolean());
        Assert.Equal(1, json.RootElement.GetProperty("changed").GetProperty("added").GetInt32());
    }

    [Fact]
    public async Task Execute_tool_surfaces_the_bridge_refusal_codes_as_tool_errors()
    {
        _settings.ExecutionEnabled = false;

        var result = await new ExecuteAutocadCodeTool(_execute).ExecuteAsync(
            "return 1;", TransactionModes.None, dryRun: false, timeoutSeconds: 5, label: null, args: null, progress: null, TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Contains("Allow AI code execution", TextOf(result));
        Assert.Contains("AutoCAD", TextOf(result));
    }
}
