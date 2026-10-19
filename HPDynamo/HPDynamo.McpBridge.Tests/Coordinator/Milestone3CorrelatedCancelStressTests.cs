namespace HPDynamo.McpBridge.Tests.Coordinator;

using System;
using System.Threading;
using System.Threading.Tasks;
using HPDynamo.McpBridge.Coordinator;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using Xunit;

/// <summary>
///     Empirical stress tests for RV-04 (Correlated Cancellation and Request Routing).
///     Tests request ID matching, stale cancel rejection, unconditional fallbacks,
///     and cross-request cancellation isolation.
/// </summary>
public sealed class Milestone3CorrelatedCancelStressTests
{
    private static DynamoBridgeCoordinator CreateCoordinator()
    {
        var coordinator = DynamoBridgeCoordinator.Instance;
        coordinator.Initialize(new BridgeSettings { ExecutionEnabled = true }, null);
        coordinator.UnregisterLiveCanvas();
        return coordinator;
    }

    [Fact]
    public async Task CorrelatedCancel_MatchingId_LateCancelAfterCommit_PreservesCommittedResult()
    {
        var coordinator = CreateCoordinator();
        var hold = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        coordinator.ExecutionHook = async _ =>
        {
            await hold.Task;
            return new ExecuteResult { IsError = false, Message = "Success" };
        };

        try
        {
            var execTask = coordinator.ExecuteAsync(5001L, new ExecuteRequest("x = 1;", "none"), null, CancellationToken.None);
            while (!coordinator.IsBusy) await Task.Yield();

            Assert.Equal(5001L, coordinator.ActiveRequestId);

            var cancelResult = coordinator.Cancel(5001L);
            Assert.True(cancelResult.Cancelled);
            Assert.True(cancelResult.WasRunning);

            hold.SetResult(true);
            var result = await execTask;

            // Transaction boundary is single source of truth: late cancel does not overwrite committed result
            Assert.False(result.IsError);
            Assert.False(result.RolledBack);
            Assert.Equal(0L, coordinator.ActiveRequestId);
            Assert.False(coordinator.IsBusy);
        }
        finally
        {
            coordinator.ExecutionHook = null;
            hold.TrySetResult(true);
        }
    }

    [Fact]
    public async Task CorrelatedCancel_MatchingId_BoundaryThrowsCancellation_RollsBack()
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
            var execTask = coordinator.ExecuteAsync(5001L, new ExecuteRequest("x = 1;", "none"), null, CancellationToken.None);
            while (!coordinator.IsBusy) await Task.Yield();

            var cancelResult = coordinator.Cancel(5001L);
            Assert.True(cancelResult.Cancelled);

            hold.SetResult(true);
            var result = await execTask;

            Assert.True(result.IsError);
            Assert.True(result.RolledBack);
            Assert.Equal(0L, coordinator.ActiveRequestId);
            Assert.False(coordinator.IsBusy);
        }
        finally
        {
            coordinator.ExecutionHook = null;
            hold.TrySetResult(true);
        }
    }

    [Fact]
    public async Task CorrelatedCancel_MismatchingStaleId_IgnoresCancelAndCompletesUninterrupted()
    {
        var coordinator = CreateCoordinator();
        var hold = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        coordinator.ExecutionHook = async _ =>
        {
            await hold.Task;
            return new ExecuteResult { IsError = false, Message = "Completed Successfully" };
        };

        try
        {
            var execTask = coordinator.ExecuteAsync(5002L, new ExecuteRequest("x = 2;", "none"), null, CancellationToken.None);
            while (!coordinator.IsBusy) await Task.Yield();

            var staleResult = coordinator.Cancel(9999L);
            Assert.False(staleResult.Cancelled);
            Assert.True(staleResult.WasRunning);
            Assert.True(coordinator.IsBusy);
            Assert.Equal(5002L, coordinator.ActiveRequestId);

            hold.SetResult(true);
            var result = await execTask;

            Assert.False(result.IsError);
            Assert.Equal("Completed Successfully", result.Message);
            Assert.False(result.RolledBack);
            Assert.Equal(0L, coordinator.ActiveRequestId);
        }
        finally
        {
            coordinator.ExecutionHook = null;
            hold.TrySetResult(true);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0L)]
    public async Task CorrelatedCancel_NullAndZero_CancelsUnconditionally(long? cancelTarget)
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
            var execTask = coordinator.ExecuteAsync(5003L, new ExecuteRequest("x = 3;", "none"), null, CancellationToken.None);
            while (!coordinator.IsBusy) await Task.Yield();

            var cancelResult = coordinator.Cancel(cancelTarget);
            Assert.True(cancelResult.Cancelled);
            Assert.True(cancelResult.WasRunning);

            hold.SetResult(true);
            var result = await execTask;

            Assert.True(result.IsError);
            Assert.True(result.RolledBack);
        }
        finally
        {
            coordinator.ExecutionHook = null;
            hold.TrySetResult(true);
        }
    }

    [Fact]
    public async Task CorrelatedCancel_ExtremeBoundaryIds_HandledCorrectly()
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
            var execTask = coordinator.ExecuteAsync(long.MaxValue, new ExecuteRequest("x = 4;", "none"), null, CancellationToken.None);
            while (!coordinator.IsBusy) await Task.Yield();

            var minResult = coordinator.Cancel(long.MinValue);
            Assert.False(minResult.Cancelled);
            Assert.True(minResult.WasRunning);

            var maxResult = coordinator.Cancel(long.MaxValue);
            Assert.True(maxResult.Cancelled);

            hold.SetResult(true);
            var result = await execTask;
            Assert.True(result.IsError);
        }
        finally
        {
            coordinator.ExecutionHook = null;
            hold.TrySetResult(true);
        }
    }

    [Fact]
    public async Task CorrelatedCancel_MismatchedSession_DoesNotCancelActiveRequest()
    {
        var coordinator = CreateCoordinator();
        var hold = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        coordinator.ExecutionHook = async _ =>
        {
            await hold.Task;
            return new ExecuteResult { IsError = false, Message = "Session1 Success" };
        };

        try
        {
            var req = new ExecuteRequest("x = 1;", "none", SessionId: "session-1");
            var execTask = coordinator.ExecuteAsync(7001L, req, null, CancellationToken.None);
            while (!coordinator.IsBusy) await Task.Yield();

            // Cancel with matching request ID but different session ID
            var cancelResult = coordinator.Cancel(7001L, "session-2");
            Assert.False(cancelResult.Cancelled);
            Assert.True(cancelResult.WasRunning);
            Assert.True(coordinator.IsBusy);

            hold.SetResult(true);
            var result = await execTask;

            Assert.False(result.IsError);
            Assert.Equal("Session1 Success", result.Message);
        }
        finally
        {
            coordinator.ExecutionHook = null;
            hold.TrySetResult(true);
        }
    }

    [Fact]
    public async Task CorrelatedCancel_CrossRequestIsolation_DelayedCancelDoesNotCancelNextRequest()
    {
        var coordinator = CreateCoordinator();

        coordinator.ExecutionHook = _ => Task.FromResult(new ExecuteResult { IsError = false, Message = "Req1 Done" });
        var res1 = await coordinator.ExecuteAsync(6001L, new ExecuteRequest("x = 1;", "none"), null, CancellationToken.None);
        Assert.False(res1.IsError);
        Assert.Equal(0L, coordinator.ActiveRequestId);

        var hold2 = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        coordinator.ExecutionHook = async _ =>
        {
            await hold2.Task;
            return new ExecuteResult { IsError = false, Message = "Req2 Done" };
        };

        try
        {
            var execTask2 = coordinator.ExecuteAsync(6002L, new ExecuteRequest("x = 2;", "none"), null, CancellationToken.None);
            while (!coordinator.IsBusy) await Task.Yield();

            var delayedCancel1 = coordinator.Cancel(6001L);
            Assert.False(delayedCancel1.Cancelled);
            Assert.True(delayedCancel1.WasRunning);
            Assert.True(coordinator.IsBusy);
            Assert.Equal(6002L, coordinator.ActiveRequestId);

            hold2.SetResult(true);
            var res2 = await execTask2;
            Assert.False(res2.IsError);
            Assert.Equal("Req2 Done", res2.Message);
        }
        finally
        {
            coordinator.ExecutionHook = null;
            hold2.TrySetResult(true);
        }
    }
}
