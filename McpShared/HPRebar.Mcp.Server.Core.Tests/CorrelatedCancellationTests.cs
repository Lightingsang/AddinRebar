namespace HPRebar.Mcp.Server.Core.Tests;

using System;
using System.Threading;
using System.Threading.Tasks;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Services;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

public sealed class CorrelatedCancellationTests
{
    [Theory]
    [InlineData("{\"id\": 101}", 101L)]
    [InlineData("{\"Id\": 202}", 202L)]
    [InlineData("{\"id\": null}", null)]
    [InlineData("{}", null)]
    public void CancelParams_Deserializes_Correctly(string json, long? expectedId)
    {
        var cancelParams = BridgeJson.Deserialize<CancelParams>(json);
        Assert.NotNull(cancelParams);
        Assert.Equal(expectedId, cancelParams.Id);
    }

    [Fact]
    public async Task PipeRoundTrip_CorrelatedCancel_PassesTargetRequestId()
    {
        var pipeName = "test-pipe-" + Guid.NewGuid().ToString("N");
        var fake = new FakeCorrelatedExecutor();
        using var listener = new PipeListener(pipeName, new RequestDispatcher(fake, new BridgeSettings { ExecutionEnabled = true }, "2026", "Dynamo"));
        listener.Start();

        var options = Options.Create(new BridgeOptions { PipeName = pipeName, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 });
        await using var client = new RevitBridgeClient(options, NullLogger<RevitBridgeClient>.Instance);

        var res = await client.SendAsync<CancelResult>("dynamo.cancel", new CancelParams(101L), TimeSpan.FromSeconds(5), null, TestContext.Current.CancellationToken);

        Assert.Equal(101L, fake.LastCancelledRequestId);
        Assert.True(fake.CorrelatedCancelCalled);
        Assert.True(res.Cancelled);
    }

    [Fact]
    public async Task PipeRoundTrip_CorrelatedCancel_PassesNull_WhenParamsOmitted()
    {
        var pipeName = "test-pipe-" + Guid.NewGuid().ToString("N");
        var fake = new FakeCorrelatedExecutor();
        using var listener = new PipeListener(pipeName, new RequestDispatcher(fake, new BridgeSettings { ExecutionEnabled = true }, "2026", "Dynamo"));
        listener.Start();

        var options = Options.Create(new BridgeOptions { PipeName = pipeName, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 });
        await using var client = new RevitBridgeClient(options, NullLogger<RevitBridgeClient>.Instance);

        var res = await client.SendAsync<CancelResult>("dynamo.cancel", null, TimeSpan.FromSeconds(5), null, TestContext.Current.CancellationToken);

        Assert.Null(fake.LastCancelledRequestId);
        Assert.True(fake.CorrelatedCancelCalled);
        Assert.True(res.Cancelled);
    }

    [Fact]
    public async Task PipeRoundTrip_Cancel_FallsBackToStandardCancel_WhenNotCorrelated()
    {
        var pipeName = "test-pipe-" + Guid.NewGuid().ToString("N");
        var fake = new FakeStandardExecutor();
        using var listener = new PipeListener(pipeName, new RequestDispatcher(fake, new BridgeSettings { ExecutionEnabled = true }, "2026", "Revit"));
        listener.Start();

        var options = Options.Create(new BridgeOptions { PipeName = pipeName, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 });
        await using var client = new RevitBridgeClient(options, NullLogger<RevitBridgeClient>.Instance);

        var res = await client.SendAsync<CancelResult>("revit.cancel", new CancelParams(303L), TimeSpan.FromSeconds(5), null, TestContext.Current.CancellationToken);

        Assert.True(fake.StandardCancelCalled);
        Assert.True(res.Cancelled);
    }

    [Fact]
    public async Task PipeRoundTrip_Execute_ForwardsRequestId_ToCorrelatedExecutor()
    {
        var pipeName = "test-pipe-" + Guid.NewGuid().ToString("N");
        var fake = new FakeCorrelatedExecutor();
        using var listener = new PipeListener(pipeName, new RequestDispatcher(fake, new BridgeSettings { ExecutionEnabled = true }, "2026", "Dynamo"));
        listener.Start();

        var options = Options.Create(new BridgeOptions { PipeName = pipeName, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 });
        await using var client = new RevitBridgeClient(options, NullLogger<RevitBridgeClient>.Instance);

        var res = await client.SendAsync<ExecuteResult>("dynamo.execute", new ExecuteRequest("return 1;", "none"), TimeSpan.FromSeconds(5), null, TestContext.Current.CancellationToken);

        Assert.False(res.IsError);
        Assert.True(fake.LastExecutedRequestId > 0);
    }

    private class FakeStandardExecutor : IBridgeExecutor
    {
        public bool IsBusy => false;
        public int CompiledScriptCount => 0;
        public string? ActiveDocumentTitle => "Test";
        public bool StandardCancelCalled { get; private set; }

        public event Action? StateChanged { add { } remove { } }
        public event Action<LastRunInfo>? RunCompleted { add { } remove { } }

        public Task<ExecuteResult> ExecuteAsync(ExecuteRequest request, IProgress<ScriptProgress>? progress, CancellationToken cancellationToken) =>
            Task.FromResult(new ExecuteResult { IsError = false });

        public Task<ContextResult> GetContextAsync(bool includeSelection, CancellationToken cancellationToken) =>
            Task.FromResult(new ContextResult());

        public InspectResult Inspect(InspectRequest request) => new();
        public AnalyzeResult Analyze(AnalyzeRequest request) => new();

        public CancelResult Cancel()
        {
            StandardCancelCalled = true;
            return new CancelResult(true, true);
        }
    }

    private sealed class FakeCorrelatedExecutor : FakeStandardExecutor, ICorrelatedCancelExecutor
    {
        public long? LastCancelledRequestId { get; private set; }
        public bool CorrelatedCancelCalled { get; private set; }
        public long? LastExecutedRequestId { get; private set; }

        public Task<ExecuteResult> ExecuteAsync(
            long requestId,
            ExecuteRequest request,
            IProgress<ScriptProgress>? progress,
            CancellationToken cancellationToken)
        {
            LastExecutedRequestId = requestId;
            return Task.FromResult(new ExecuteResult { IsError = false });
        }

        public CancelResult Cancel(long? targetRequestId) => Cancel(targetRequestId, null);

        public CancelResult Cancel(long? targetRequestId, string? sessionId)
        {
            LastCancelledRequestId = targetRequestId;
            CorrelatedCancelCalled = true;
            return new CancelResult(true, true);
        }
    }
}
