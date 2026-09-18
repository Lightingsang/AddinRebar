using System.Text.Json;
using HPCivil3d.Mcp.Server.Hosts;
using HPCivil3d.Mcp.Server.Resources;
using HPCivil3d.Mcp.Server.Tools;
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

namespace HPCivil3d.Mcp.Server.Tests;

/// <summary>
///     The Civil tool classes talking through the engine to a bridge listener over a real named pipe, with a fake
///     behind the dispatcher instead of acad.exe: the wire carries `civil3d.*` methods, the context JSON shows the
///     `civil3d` and `autocad` blocks and no Revit-named field, execute passes its parameters through.
/// </summary>
public sealed class Civil3dToolsOverPipeTests : IAsyncLifetime
{
    private readonly string _pipeName = "hpcivil3d-mcp-test-" + Guid.NewGuid().ToString("N");
    private readonly FakeRevitExecutor _executor = new FakeRevitExecutor();
    private readonly BridgeSettings _settings = new BridgeSettings { ExecutionEnabled = true };
    private PipeListener _listener = null!;
    private RevitBridgeClient _client = null!;
    private ContextService _context = null!;
    private ExecuteCodeService _execute = null!;

    public ValueTask InitializeAsync()
    {
        _listener = new PipeListener(_pipeName, new RequestDispatcher(_executor, _settings, "2026", "Civil 3D"));
        _listener.Start();

        var options = Options.Create(new BridgeOptions { HostId = "civil3d", HostVersion = 2026, PipeName = _pipeName, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 });
        _client = new RevitBridgeClient(options, NullLogger<RevitBridgeClient>.Instance, Civil3dHostProfile.Instance);
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
    public async Task Context_tool_returns_the_civil_and_autocad_blocks_and_hides_the_revit_named_fields()
    {
        _executor.ContextHandler = includeSelection => new ContextResult
        {
            RevitVersion = "2026", Host = "civil3d", HostVersion = "2026", DocTitle = "Align-7C.dwg", IsModifiable = true,
            Units = new UnitsInfo("Meters"),
            Autocad = new AutocadInfo("Meters", "Metric", "Model", "C-ROAD", true, true, true),
            Civil3d = new Civil3dInfo("Civil3D", true, "Meters", null, false, 1, 1, 0, 0, 0, 0),
            Selection = includeSelection ? [new ElementInfo(0x10DB5, "C-ROAD", "AECC_ALIGNMENT")] : [],
        };

        var result = await new Civil3dContextTool(_context).GetContextAsync(includeSelection: true, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        using var json = JsonDocument.Parse(TextOf(result));
        var root = json.RootElement;
        Assert.Equal("civil3d", root.GetProperty("host").GetString());
        Assert.Equal("2026", root.GetProperty("hostVersion").GetString());
        Assert.False(root.TryGetProperty("revitVersion", out _));
        Assert.False(root.TryGetProperty("isFamily", out _));
        Assert.Equal("Meters", root.GetProperty("units").GetProperty("length").GetString());
        Assert.Equal("Metric", root.GetProperty("autocad").GetProperty("measurement").GetString());
        var civil = root.GetProperty("civil3d");
        Assert.Equal("Civil3D", civil.GetProperty("product").GetString());
        Assert.True(civil.GetProperty("isCivilDocument").GetBoolean());
        Assert.Equal("Meters", civil.GetProperty("drawingUnit").GetString());
        Assert.False(civil.TryGetProperty("coordinateSystemCode", out _), "a null zone is dropped, not written as null");
        Assert.False(civil.GetProperty("insunitsMismatch").GetBoolean());
        Assert.Equal(1, civil.GetProperty("alignmentCount").GetInt32());
        Assert.Equal("AECC_ALIGNMENT", root.GetProperty("selection")[0].GetProperty("name").GetString());
        Assert.True(_executor.LastContextIncludedSelection);
    }

    [Fact]
    public async Task Document_resource_is_the_same_snapshot_as_json_text()
    {
        _executor.ContextHandler = _ => new ContextResult { RevitVersion = "2026", Host = "civil3d", HostVersion = "2026", DocTitle = "Corridor-5c.dwg" };

        var text = await new Civil3dDocumentResources(_context).DocumentInfoAsync(TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(text);
        Assert.Equal("Corridor-5c.dwg", json.RootElement.GetProperty("docTitle").GetString());
        Assert.False(json.RootElement.TryGetProperty("revitVersion", out _));
    }

    [Fact]
    public async Task Execute_tool_sends_the_request_over_the_civil3d_method_and_returns_the_bridge_result()
    {
        _executor.ExecuteHandler = request => new ExecuteResult
        {
            Value = JsonSerializer.SerializeToElement(new { handle = "10DB5", name = "Alignment - (1)" }), ValueType = "object",
            Changed = new ChangedCounts(1, 0, 0), RolledBack = request.DryRun, DurationMs = 4,
        };

        var args = JsonSerializer.SerializeToElement(new { alignment = "Alignment - (1)", sampleStepMm = 50000 });
        var result = await new ExecuteCivil3dCodeTool(_execute).ExecuteAsync(
            "return 1;", TransactionModes.Auto, dryRun: true, timeoutSeconds: 20, label: "alignment", args: args, progress: null, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var request = _executor.LastExecuteRequest!;
        Assert.Equal("return 1;", request.Code);
        Assert.Equal("auto", request.Transaction);
        Assert.True(request.DryRun);
        Assert.Equal(20, request.TimeoutSeconds);
        Assert.Equal("alignment", request.Label);
        Assert.Equal(50000, request.Args!.Value.GetProperty("sampleStepMm").GetInt32());

        using var json = JsonDocument.Parse(TextOf(result));
        Assert.True(json.RootElement.GetProperty("rolledBack").GetBoolean());
        Assert.Equal(1, json.RootElement.GetProperty("changed").GetProperty("added").GetInt32());
    }

    [Fact]
    public async Task Execute_tool_surfaces_the_bridge_refusal_codes_as_tool_errors_naming_civil_3d()
    {
        _settings.ExecutionEnabled = false;

        var result = await new ExecuteCivil3dCodeTool(_execute).ExecuteAsync(
            "return 1;", TransactionModes.None, dryRun: false, timeoutSeconds: 5, label: null, args: null, progress: null, TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Contains("Allow AI code execution", TextOf(result));
        Assert.Contains("Civil 3D", TextOf(result));
        Assert.DoesNotContain("AutoCAD", TextOf(result));
    }
}
