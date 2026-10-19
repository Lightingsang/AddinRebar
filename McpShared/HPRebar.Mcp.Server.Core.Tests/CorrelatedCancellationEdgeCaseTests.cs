namespace HPRebar.Mcp.Server.Core.Tests;

using System;
using System.Collections.Concurrent;
using System.Linq;
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

public sealed class CorrelatedCancellationEdgeCaseTests
{
    [Theory]
    [InlineData("{\"id\": 0}", 0L)]
    [InlineData("{\"id\": -1}", -1L)]
    [InlineData("{\"id\": 9223372036854775807}", 9223372036854775807L)]
    [InlineData("{\"id\": -9223372036854775808}", -9223372036854775808L)]
    [InlineData("{\"id\": 42, \"unexpectedField\": \"ignoreMe\"}", 42L)]
    public void CancelParams_Deserializes_BoundaryAndExtraFieldValues(string json, long? expectedId)
    {
        var cancelParams = BridgeJson.Deserialize<CancelParams>(json);
        Assert.NotNull(cancelParams);
        Assert.Equal(expectedId, cancelParams.Id);
    }

    [Fact]
    public async Task PipeRoundTrip_MismatchedCancel_ReturnsNotCancelledRunning()
    {
        var pipeName = "test-pipe-mismatch-" + Guid.NewGuid().ToString("N");
        var fake = new StatefulCorrelatedExecutor(activeRequestId: 500L);
        using var listener = new PipeListener(pipeName, new RequestDispatcher(fake, new BridgeSettings { ExecutionEnabled = true }, "2026", "Dynamo"));
        listener.Start();

        var options = Options.Create(new BridgeOptions { PipeName = pipeName, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 });
        await using var client = new RevitBridgeClient(options, NullLogger<RevitBridgeClient>.Instance);

        var res = await client.SendAsync<CancelResult>("dynamo.cancel", new CancelParams(999L), TimeSpan.FromSeconds(5), null, TestContext.Current.CancellationToken);

        Assert.NotNull(res);
        Assert.False(res.Cancelled);
        Assert.True(res.WasRunning);
        Assert.Equal(999L, fake.LastCancelledRequestId);
    }

    [Fact]
    public async Task PipeRoundTrip_MatchingCancel_ReturnsCancelledRunning()
    {
        var pipeName = "test-pipe-match-" + Guid.NewGuid().ToString("N");
        var fake = new StatefulCorrelatedExecutor(activeRequestId: 500L);
        using var listener = new PipeListener(pipeName, new RequestDispatcher(fake, new BridgeSettings { ExecutionEnabled = true }, "2026", "Dynamo"));
        listener.Start();

        var options = Options.Create(new BridgeOptions { PipeName = pipeName, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 });
        await using var client = new RevitBridgeClient(options, NullLogger<RevitBridgeClient>.Instance);

        var res = await client.SendAsync<CancelResult>("dynamo.cancel", new CancelParams(500L), TimeSpan.FromSeconds(5), null, TestContext.Current.CancellationToken);

        Assert.NotNull(res);
        Assert.True(res.Cancelled);
        Assert.True(res.WasRunning);
        Assert.Equal(500L, fake.LastCancelledRequestId);
    }

    [Fact]
    public async Task PipeRoundTrip_ConcurrentCancelCalls_AllHandledDeterministically()
    {
        var pipeName = "test-pipe-conc-" + Guid.NewGuid().ToString("N");
        var fake = new StatefulCorrelatedExecutor(activeRequestId: 777L);
        using var listener = new PipeListener(pipeName, new RequestDispatcher(fake, new BridgeSettings { ExecutionEnabled = true }, "2026", "Dynamo"));
        listener.Start();

        var options = Options.Create(new BridgeOptions { PipeName = pipeName, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 });
        await using var client = new RevitBridgeClient(options, NullLogger<RevitBridgeClient>.Instance);

        var tasks = Enumerable.Range(0, 20).Select(async i =>
        {
            var targetId = i == 10 ? 777L : 1000L + i;
            return await client.SendAsync<CancelResult>("dynamo.cancel", new CancelParams(targetId), TimeSpan.FromSeconds(5), null, TestContext.Current.CancellationToken);
        }).ToArray();

        var results = await Task.WhenAll(tasks);
        Assert.Equal(20, results.Length);
        Assert.Contains(results, r => r.Cancelled);
        Assert.Contains(results, r => !r.Cancelled);
    }

    private sealed class StatefulCorrelatedExecutor : ICorrelatedCancelExecutor
    {
        private readonly long _activeRequestId;
        public long? LastCancelledRequestId { get; private set; }

        public StatefulCorrelatedExecutor(long activeRequestId) => _activeRequestId = activeRequestId;

        public bool IsBusy => true;
        public int CompiledScriptCount => 0;
        public string? ActiveDocumentTitle => "Test";

        public event Action? StateChanged { add { } remove { } }
        public event Action<LastRunInfo>? RunCompleted { add { } remove { } }

        public Task<ExecuteResult> ExecuteAsync(ExecuteRequest request, IProgress<ScriptProgress>? progress, CancellationToken cancellationToken) =>
            Task.FromResult(new ExecuteResult { IsError = false });

        public Task<ExecuteResult> ExecuteAsync(long requestId, ExecuteRequest request, IProgress<ScriptProgress>? progress, CancellationToken cancellationToken) =>
            Task.FromResult(new ExecuteResult { IsError = false });

        public Task<ContextResult> GetContextAsync(bool includeSelection, CancellationToken cancellationToken) =>
            Task.FromResult(new ContextResult());

        public InspectResult Inspect(InspectRequest request) => new();
        public AnalyzeResult Analyze(AnalyzeRequest request) => new();
        public CancelResult Cancel() => Cancel(null);

        public CancelResult Cancel(long? targetRequestId) => Cancel(targetRequestId, null);

        public CancelResult Cancel(long? targetRequestId, string? sessionId)
        {
            LastCancelledRequestId = targetRequestId;
            if (targetRequestId.HasValue && targetRequestId.Value != 0 && targetRequestId.Value != _activeRequestId)
                return new CancelResult(false, true);

            return new CancelResult(true, true);
        }
    }
}
