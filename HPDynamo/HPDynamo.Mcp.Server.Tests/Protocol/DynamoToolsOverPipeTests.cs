namespace HPDynamo.Mcp.Server.Tests.Protocol;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPDynamo.Mcp.Server.Hosts;
using HPDynamo.Mcp.Server.Tools;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Services;
using HPRebar.Mcp.Server.Tests.Fakes;
using HPRebar.Mcp.Server.Tools;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using Xunit;

public sealed class DynamoToolsOverPipeTests : IAsyncLifetime
{
    private readonly string _pipeName = "hpdynamo-mcp-test-" + Guid.NewGuid().ToString("N");
    private readonly FakeRevitExecutor _executor = new();
    private readonly BridgeSettings _settings = new() { ExecutionEnabled = true };
    private PipeListener _listener = null!;
    private RevitBridgeClient _client = null!;
    private ContextService _contextService = null!;
    private ExecuteCodeService _executeService = null!;

    public ValueTask InitializeAsync()
    {
        _listener = new PipeListener(
            _pipeName,
            new RequestDispatcher(_executor, _settings, "2026", "Dynamo"));
        _listener.Start();

        var options = Options.Create(new BridgeOptions
        {
            HostId = "dynamo",
            HostVersion = 2026,
            PipeName = _pipeName,
            ConnectTimeoutMs = 3000,
            PingIntervalSeconds = 60
        });

        _client = new RevitBridgeClient(options, NullLogger<RevitBridgeClient>.Instance, DynamoHostProfile.Instance);
        var formatter = new ResultFormatter();
        _contextService = new ContextService(_client, formatter);
        _executeService = new ExecuteCodeService(_client, formatter, options);

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync().ConfigureAwait(false);
        await _listener.StopAsync().ConfigureAwait(false);
        _listener.Dispose();
    }

    private static string TextOf(CallToolResult result) => ((TextContentBlock)result.Content[0]).Text;

    [Fact]
    public async Task ContextTool_ReturnsDynamoInfoAndHidesRevitOnlyFields()
    {
        _executor.ContextHandler = includeSel => new ContextResult
        {
            Host = "dynamo",
            HostVersion = "2026",
            DocTitle = "Project1.rvt",
            Dynamo = new DynamoInfo(
                IsLive: true,
                DynamoVersion: "3.7.0",
                ActiveWorkspaceName: "SampleGraph",
                ActiveWorkspacePath: "C:\\Graphs\\SampleGraph.dyn",
                RunType: "Manual",
                NodeCount: 15,
                WireCount: 12,
                SelectedNodeCount: 0,
                HasUnsavedChanges: false)
        };

        var tool = new DynamoContextTool(_contextService);
        var result = await tool.GetContextAsync(false, CancellationToken.None);

        Assert.False(result.IsError);
        var text = TextOf(result);
        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;

        Assert.Equal("dynamo", root.GetProperty("host").GetString());
        Assert.Equal("2026", root.GetProperty("hostVersion").GetString());
        Assert.True(root.TryGetProperty("dynamo", out var dynamoBlock));
        Assert.True(dynamoBlock.GetProperty("isLive").GetBoolean());
        Assert.Equal(15, dynamoBlock.GetProperty("nodeCount").GetInt32());
    }

    [Fact]
    public async Task ExecuteDynamoCode_PassesParametersAndReceivesResult()
    {
        _executor.ExecuteHandler = req => new ExecuteResult
        {
            Value = JsonSerializer.SerializeToElement(42),
            DurationMs = 10,
            RolledBack = req.DryRun
        };

        var tool = new ExecuteDynamoCodeTool(_executeService);
        var result = await tool.ExecuteAsync("return 42;", "auto", dryRun: true, cancellationToken: CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("return 42;", _executor.LastExecuteRequest!.Code);
        Assert.True(_executor.LastExecuteRequest.DryRun);
    }

    [Fact]
    public async Task ExecuteDesignScript_PrefixesLabelAndPassesCode()
    {
        _executor.ExecuteHandler = req => new ExecuteResult
        {
            Value = JsonSerializer.SerializeToElement("Point(0,0,0)"),
            DurationMs = 15,
            RolledBack = req.DryRun
        };

        var tool = new ExecuteDesignScriptTool(_executeService);
        var result = await tool.ExecuteAsync("pt = Point.ByCoordinates(0,0,0);", "auto", dryRun: false, label: "CreatePoint", cancellationToken: CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("DesignScript: CreatePoint", _executor.LastExecuteRequest!.Label);
        Assert.Equal("pt = Point.ByCoordinates(0,0,0);", _executor.LastExecuteRequest.Code);
    }

    [Fact]
    public async Task CancelExecution_DispatchesCancellation()
    {
        _executor.IsBusy = true;
        var tool = new CancelExecutionTool(_client, new ResultFormatter());

        var result = await tool.CancelAsync(CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(1, _executor.CancelCalls);
    }
}
