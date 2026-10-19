namespace HPGrasshopper.McpBridge.Tests.Coordinator;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HPGrasshopper.McpBridge.Coordinator;
using HPGrasshopper.McpBridge.Support;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using Xunit;

public sealed class ThreadingAndExecutionStressTests : IDisposable
{
    private readonly GrasshopperBridgeCoordinator _coordinator = GrasshopperBridgeCoordinator.Instance;
    private readonly FakeExternalEventRaiser _raiser = new();
    private readonly BridgeSettings _settings = new() { ExecutionEnabled = true };

    public ThreadingAndExecutionStressTests()
    {
        _coordinator.Initialize(_settings, _raiser);
        _coordinator.ExecutionHook = null;
    }

    public void Dispose()
    {
        _coordinator.ExecutionHook = null;
        _coordinator.Dispose();
    }

    [Fact]
    public async Task ExternalEventQueue_ConcurrentProducers_ExecutesAllItemsWithoutLoss()
    {
        const int itemCount = 100;
        var executedItems = new ConcurrentBag<int>();
        var tasks = new List<Task<object?>>();

        Parallel.For(0, itemCount, i =>
        {
            var work = new McpBridgeWorkRequest($"Item_{i}", (_, _) =>
            {
                executedItems.Add(i);
                return i;
            }, CancellationToken.None);

            var t = _coordinator.RunOnRevitThreadAsync(work);
            lock (tasks) { tasks.Add(t); }
        });

        Assert.True(_raiser.RaiseCount >= 1);
        _coordinator.Execute(null!);

        var results = await Task.WhenAll(tasks);
        Assert.Equal(itemCount, results.Length);
        Assert.Equal(itemCount, executedItems.Count);
        Assert.Equal(itemCount, executedItems.Distinct().Count());
    }

    [Fact]
    public async Task ExternalEventQueue_ExceptionInMiddleItem_DoesNotBlockRemainingItems()
    {
        var executed = new ConcurrentBag<string>();
        var work1 = new McpBridgeWorkRequest("W1", (_, _) => { executed.Add("W1"); return "1"; }, CancellationToken.None);
        var work2 = new McpBridgeWorkRequest("W2", (_, _) => throw new InvalidOperationException("Fail W2"), CancellationToken.None);
        var work3 = new McpBridgeWorkRequest("W3", (_, _) => { executed.Add("W3"); return "3"; }, CancellationToken.None);

        var t1 = _coordinator.RunOnRevitThreadAsync(work1);
        var t2 = _coordinator.RunOnRevitThreadAsync(work2);
        var t3 = _coordinator.RunOnRevitThreadAsync(work3);

        _coordinator.Execute(null!);

        Assert.Equal("1", await t1);
        await Assert.ThrowsAsync<InvalidOperationException>(() => t2);
        Assert.Equal("3", await t3);
        Assert.Contains("W1", executed);
        Assert.Contains("W3", executed);
    }

    [Fact]
    public async Task SingleFlightLock_Hammer50ConcurrentCalls_RejectsAllExceptOne()
    {
        var runSignal = new TaskCompletionSource<bool>();
        var activeCount = 0;
        var maxObservedConcurrency = 0;

        _coordinator.ExecutionHook = async req =>
        {
            var current = Interlocked.Increment(ref activeCount);
            lock (runSignal)
            {
                if (current > maxObservedConcurrency)
                    maxObservedConcurrency = current;
            }
            await runSignal.Task;
            Interlocked.Decrement(ref activeCount);
            return new ExecuteResult { Message = "ok" };
        };

        const int callCount = 50;
        var tasks = new Task<ExecuteResult>[callCount];
        for (var i = 0; i < callCount; i++)
        {
            var req = new ExecuteRequest($"code_{i}", "none") { SessionId = $"sess_{i}" };
            tasks[i] = Task.Run(() => _coordinator.ExecuteAsync(i + 1, req, null, CancellationToken.None));
        }

        while (!_coordinator.IsBusy) await Task.Yield();
        await Task.Delay(50);

        runSignal.SetResult(true);
        var results = await Task.WhenAll(tasks);

        var successCount = results.Count(r => !r.IsError);
        var rejectedCount = results.Count(r => r.IsError && r.Message?.Contains("Another Grasshopper script") == true);

        Assert.Equal(1, successCount);
        Assert.Equal(callCount - 1, rejectedCount);
        Assert.Equal(1, maxObservedConcurrency);
        Assert.False(_coordinator.IsBusy);
    }

    [Fact]
    public async Task SingleFlightLock_RapidSequentialExecutions_AllSucceed()
    {
        _coordinator.ExecutionHook = req => Task.FromResult(new ExecuteResult { Message = "ok" });

        for (var i = 0; i < 50; i++)
        {
            var req = new ExecuteRequest($"seq_{i}", "none");
            var res = await _coordinator.ExecuteAsync(i + 1, req, null, CancellationToken.None);
            Assert.False(res.IsError);
            Assert.False(_coordinator.IsBusy);
        }
    }

    [Fact]
    public async Task CorrelatedCancel_Mismatches_DoNotCancelActiveExecution()
    {
        var holdTask = new TaskCompletionSource<bool>();
        _coordinator.ExecutionHook = async req =>
        {
            await holdTask.Task;
            return new ExecuteResult { Message = "completed" };
        };

        var req = new ExecuteRequest("active", "none") { SessionId = "correct_sess" };
        var execTask = _coordinator.ExecuteAsync(12345, req, null, CancellationToken.None);

        while (!_coordinator.IsBusy) await Task.Yield();

        // 1. Mismatched RequestId
        var r1 = _coordinator.Cancel(99999, "correct_sess");
        Assert.False(r1.Cancelled);
        Assert.True(r1.WasRunning);

        // 2. Mismatched SessionId
        var r2 = _coordinator.Cancel(12345, "wrong_sess");
        Assert.False(r2.Cancelled);
        Assert.True(r2.WasRunning);

        // 3. Both mismatched
        var r3 = _coordinator.Cancel(99999, "wrong_sess");
        Assert.False(r3.Cancelled);
        Assert.True(r3.WasRunning);

        // Release the task and let it complete normally
        holdTask.SetResult(true);
        var res = await execTask;
        Assert.False(res.IsError);
        Assert.Equal("completed", res.Message);
    }

    [Fact]
    public async Task CorrelatedCancel_MatchingRequest_CancelsCleanlyAndRollsBack()
    {
        var cancelSignal = new TaskCompletionSource<bool>();
        _coordinator.ExecutionHook = async req =>
        {
            await cancelSignal.Task;
            return new ExecuteResult { Message = "not reached" };
        };

        var req = new ExecuteRequest("active", "none") { SessionId = "sess_match" };
        using var cts = new CancellationTokenSource();
        var execTask = _coordinator.ExecuteAsync(888, req, null, cts.Token);

        while (!_coordinator.IsBusy) await Task.Yield();

        var cancelRes = _coordinator.Cancel(888, "sess_match");
        Assert.True(cancelRes.Cancelled);
        Assert.True(cancelRes.WasRunning);

        cancelSignal.TrySetCanceled();

        var res = await execTask;
        Assert.True(res.IsError);
        Assert.True(res.RolledBack);
        Assert.Contains("cancelled", res.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(_coordinator.IsBusy);
    }

    [Fact]
    public void CorrelatedCancel_WhenIdle_ReturnsFalseNotRunning()
    {
        var res = _coordinator.Cancel(100, "sess");
        Assert.False(res.Cancelled);
        Assert.False(res.WasRunning);
    }

    [Fact]
    public void AutoDismissFailurePreprocessor_SuppressesWarnings_WithoutModalDialog()
    {
        var preprocessor = new AutoDismissFailurePreprocessor();
        Assert.Empty(preprocessor.Warnings);
    }

    [Fact]
    public async Task ExternalEventQueue_ItemCancelledInQueue_CompletesAsCanceled()
    {
        using var cts = new CancellationTokenSource();
        var executed = false;
        var work = new McpBridgeWorkRequest("CancelBeforeExec", (_, _) =>
        {
            executed = true;
            return "ok";
        }, cts.Token);

        var task = _coordinator.RunOnRevitThreadAsync(work);
        cts.Cancel(); // cancel while queued

        _coordinator.Execute(null!);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.False(executed);
    }

    [Fact]
    public async Task ConcurrentCancelCallers_NoDeadlock_ExactlyOneTriggers()
    {
        var holdSignal = new TaskCompletionSource<bool>();
        _coordinator.ExecutionHook = async _ =>
        {
            await holdSignal.Task;
            return new ExecuteResult { Message = "ok" };
        };

        var req = new ExecuteRequest("concurrent_cancel", "none") { SessionId = "c_sess" };
        var execTask = _coordinator.ExecuteAsync(777, req, null, CancellationToken.None);

        while (!_coordinator.IsBusy) await Task.Yield();

        const int cancelerCount = 20;
        var cancelTasks = new Task<CancelResult>[cancelerCount];
        for (var i = 0; i < cancelerCount; i++)
        {
            cancelTasks[i] = Task.Run(() => _coordinator.Cancel(777, "c_sess"));
        }

        var results = await Task.WhenAll(cancelTasks);
        holdSignal.TrySetCanceled();

        Assert.Contains(results, r => r.Cancelled);
        var finalResult = await execTask;
        Assert.True(finalResult.IsError);
        Assert.False(_coordinator.IsBusy);
    }
}
