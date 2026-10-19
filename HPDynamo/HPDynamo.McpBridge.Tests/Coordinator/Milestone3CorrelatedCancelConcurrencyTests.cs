namespace HPDynamo.McpBridge.Tests.Coordinator;

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HPDynamo.McpBridge.Coordinator;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using Xunit;

/// <summary>
///     Adversarial concurrency stress tests for RV-04 (Correlated Cancellation and Request Routing).
///     Tests high-contention lock safety under _cancelLock and active ID resets on exception.
/// </summary>
public sealed class Milestone3CorrelatedCancelConcurrencyTests
{
    private static DynamoBridgeCoordinator CreateCoordinator()
    {
        var coordinator = DynamoBridgeCoordinator.Instance;
        coordinator.Initialize(new BridgeSettings { ExecutionEnabled = true }, null);
        coordinator.UnregisterLiveCanvas();
        return coordinator;
    }

    [Fact]
    public async Task CorrelatedCancel_100ConcurrentMismatchedCancels_DoNotInterruptActiveRun()
    {
        var coordinator = CreateCoordinator();
        var hold = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        coordinator.ExecutionHook = async _ =>
        {
            await hold.Task;
            return new ExecuteResult { IsError = false, Message = "Survived" };
        };

        try
        {
            var execTask = coordinator.ExecuteAsync(7001L, new ExecuteRequest("x = 7;", "none"), null, CancellationToken.None);
            while (!coordinator.IsBusy) await Task.Yield();

            var results = new ConcurrentBag<CancelResult>();
            Parallel.For(1, 101, i =>
            {
                var cancelRes = coordinator.Cancel(8000L + i);
                results.Add(cancelRes);
            });

            Assert.Equal(100, results.Count);
            Assert.All(results, r =>
            {
                Assert.False(r.Cancelled);
                Assert.True(r.WasRunning);
            });

            Assert.True(coordinator.IsBusy);
            Assert.Equal(7001L, coordinator.ActiveRequestId);

            hold.SetResult(true);
            var res = await execTask;
            Assert.False(res.IsError);
            Assert.Equal("Survived", res.Message);
        }
        finally
        {
            coordinator.ExecutionHook = null;
            hold.TrySetResult(true);
        }
    }

    [Fact]
    public async Task CorrelatedCancel_ConcurrentMixedCancels_ThreadSafeUnderCancelLock()
    {
        var coordinator = CreateCoordinator();
        var hold = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        coordinator.ExecutionHook = async _ =>
        {
            await hold.Task;
            throw new OperationCanceledException();
        };

        try
        {
            var execTask = coordinator.ExecuteAsync(7002L, new ExecuteRequest("x = 8;", "none"), null, CancellationToken.None);
            while (!coordinator.IsBusy) await Task.Yield();

            var cancelResults = new ConcurrentBag<CancelResult>();
            var startGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var cancelTasks = Enumerable.Range(0, 50).Select(i => Task.Run(async () =>
            {
                await startGate.Task;
                var targetId = i == 25 ? 7002L : 9000L + i;
                cancelResults.Add(coordinator.Cancel(targetId));
            })).ToArray();

            startGate.SetResult();
            await Task.WhenAll(cancelTasks);

            hold.SetResult(true);
            var res = await execTask;

            Assert.True(res.IsError);
            Assert.True(res.RolledBack);
            Assert.Equal(50, cancelResults.Count);
            Assert.Contains(cancelResults, r => r.Cancelled);
        }
        finally
        {
            coordinator.ExecutionHook = null;
            hold.TrySetResult(true);
        }
    }

    [Fact]
    public async Task CorrelatedCancel_ResetsActiveRequestIdOnException()
    {
        var coordinator = CreateCoordinator();
        coordinator.ExecutionHook = _ => throw new InvalidOperationException("Simulated host explosion");

        var result = await coordinator.ExecuteAsync(8001L, new ExecuteRequest("x = 1;", "none"), null, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(0L, coordinator.ActiveRequestId);
        Assert.False(coordinator.IsBusy);

        var postCancel = coordinator.Cancel(8001L);
        Assert.False(postCancel.Cancelled);
        Assert.False(postCancel.WasRunning);
    }
}
