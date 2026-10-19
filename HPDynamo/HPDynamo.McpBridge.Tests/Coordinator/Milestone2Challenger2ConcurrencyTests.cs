namespace HPDynamo.McpBridge.Tests.Coordinator;

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HPDynamo.McpBridge.Coordinator;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using Xunit;

/// <summary>
///     Adversarial stress-test harness 2 for Milestone 2 (RV-05).
///     Empirically validates WorkItemTracker 3-state transitions under high contention,
///     cancellation thread ownership retention (_busy = 1), and Dispose queue drain.
/// </summary>
public sealed class Milestone2Challenger2ConcurrencyTests
{
    private static McpBridgeWorkRequest CreateWorkRequest(CancellationToken ct = default)
    {
        var req = (McpBridgeWorkRequest)RuntimeHelpers.GetUninitializedObject(typeof(McpBridgeWorkRequest));
        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var tcsField = typeof(McpBridgeWorkRequest).GetField("<Completion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        tcsField?.SetValue(req, tcs);
        var ctField = typeof(McpBridgeWorkRequest).GetField("<CancellationToken>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        ctField?.SetValue(req, ct);
        return req;
    }

    [Fact]
    public void WorkItemTracker_1000Iterations_ConcurrentRunningVsCancel_StrictMutualExclusion()
    {
        for (int i = 0; i < 1000; i++)
        {
            var tracker = new WorkItemTracker();
            var tcs = new TaskCompletionSource<object?>();
            using var cts = new CancellationTokenSource();
            int runningWins = 0;
            int cancelWins = 0;

            Parallel.Invoke(
                () => { if (tracker.TryTransitionToRunning()) Interlocked.Increment(ref runningWins); },
                () => { if (tracker.TryTransitionToRunning()) Interlocked.Increment(ref runningWins); },
                () => { if (tracker.TryCancelIfQueued(tcs, cts.Token)) Interlocked.Increment(ref cancelWins); },
                () => { if (tracker.TryCancelIfQueued(tcs, cts.Token)) Interlocked.Increment(ref cancelWins); },
                () => tracker.TryTransitionToCompleted()
            );

            Assert.True(runningWins <= 1, $"Iter {i}: runningWins was {runningWins}");
            Assert.True(cancelWins <= 1, $"Iter {i}: cancelWins was {cancelWins}");
            Assert.True(runningWins + cancelWins <= 1, $"Iter {i}: Both running and cancel won!");
            Assert.Equal(cancelWins == 1, tcs.Task.IsCanceled);
            Assert.Equal(WorkItemState.Completed, tracker.State);
        }
    }

    [Fact]
    public void WorkItemTracker_PostCompletedTransitions_AlwaysRejected()
    {
        var tracker = new WorkItemTracker();
        Assert.True(tracker.TryTransitionToCompleted());
        Assert.Equal(WorkItemState.Completed, tracker.State);

        int runningAttempts = 0;
        int cancelAttempts = 0;

        Parallel.For(0, 50, _ =>
        {
            var tcs = new TaskCompletionSource<object?>();
            if (tracker.TryTransitionToRunning()) Interlocked.Increment(ref runningAttempts);
            if (tracker.TryCancelIfQueued(tcs)) Interlocked.Increment(ref cancelAttempts);
        });

        Assert.Equal(0, runningAttempts);
        Assert.Equal(0, cancelAttempts);
        Assert.Equal(WorkItemState.Completed, tracker.State);
    }

    [Fact]
    public async Task Coordinator_CancellationRetainsBusyUntilRevitThreadCompletesRollback()
    {
        var coordinator = DynamoBridgeCoordinator.Instance;
        coordinator.Initialize(new BridgeSettings { ExecutionEnabled = true }, null);
        coordinator.UnregisterLiveCanvas();

        var execStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var rollbackFinished = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        coordinator.ExecutionHook = async _ =>
        {
            execStarted.SetResult(true);
            await cancelSignal.Task;
            await rollbackFinished.Task;
            throw new OperationCanceledException();
        };

        try
        {
            var req = new ExecuteRequest("return 1;", "none");
            var execTask = coordinator.ExecuteAsync(req, null, CancellationToken.None);

            await execStarted.Task;
            Assert.True(coordinator.IsBusy);

            var cancelResult = coordinator.Cancel();
            Assert.True(cancelResult.WasRunning);
            Assert.True(cancelResult.Cancelled);

            // Signal the hook that cancellation happened, so it enters the rollback phase
            cancelSignal.TrySetResult(true);

            Assert.True(coordinator.IsBusy, "Busy flag must remain 1 during rollback.");

            var concurrentTasks = Enumerable.Range(0, 20)
                .Select(_ => coordinator.ExecuteAsync(req, null, CancellationToken.None))
                .ToArray();

            var rejected = await Task.WhenAll(concurrentTasks);
            Assert.All(rejected, r =>
            {
                Assert.True(r.IsError);
                Assert.Contains("Another Dynamo script or graph is still running", r.Message);
            });

            Assert.True(coordinator.IsBusy, "Busy flag must remain 1 until rollback finishes.");

            rollbackFinished.SetResult(true);
            var finalResult = await execTask;

            Assert.True(finalResult.IsError);
            Assert.True(finalResult.RolledBack);
            Assert.False(coordinator.IsBusy, "Busy flag must reset to 0 after completion.");
        }
        finally
        {
            coordinator.ExecutionHook = null;
            execStarted.TrySetResult(true);
            cancelSignal.TrySetResult(true);
            rollbackFinished.TrySetResult(true);
        }
    }

    [Fact]
    public async Task Coordinator_Dispose_UnderHighConcurrency_DrainsQueueWithoutHangingTasks()
    {
        var coordinator = DynamoBridgeCoordinator.Instance;
        coordinator.Initialize(new BridgeSettings { ExecutionEnabled = true }, null);

        var pendingField = typeof(DynamoBridgeCoordinator).GetField("_pending", BindingFlags.NonPublic | BindingFlags.Instance);
        var trackersField = typeof(DynamoBridgeCoordinator).GetField("_workTrackers", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(pendingField);
        Assert.NotNull(trackersField);

        var pendingQueue = (ConcurrentQueue<McpBridgeWorkRequest>)pendingField.GetValue(coordinator)!;
        var trackers = (ConcurrentDictionary<McpBridgeWorkRequest, WorkItemTracker>)trackersField.GetValue(coordinator)!;

        var requests = Enumerable.Range(0, 100)
            .Select(_ => CreateWorkRequest())
            .ToList();

        foreach (var r in requests)
        {
            trackers.TryAdd(r, new WorkItemTracker());
            pendingQueue.Enqueue(r);
        }

        Assert.Equal(100, pendingQueue.Count);

        Parallel.Invoke(
            () =>
            {
                for (int i = 0; i < 50; i++)
                    if (trackers.TryGetValue(requests[i], out var t)) t.TryCancelIfQueued(requests[i].Completion);
            },
            () => coordinator.Dispose(),
            () =>
            {
                for (int i = 50; i < 100; i++)
                    if (trackers.TryGetValue(requests[i], out var t)) t.TryCancelIfQueued(requests[i].Completion);
            }
        );

        Assert.Empty(pendingQueue);
        Assert.Empty(trackers);

        foreach (var req in requests)
        {
            Assert.True(req.Completion.Task.IsCompleted, "Task must not hang.");
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => req.Completion.Task);
        }
    }

    [Fact]
    public void WorkItemTracker_WhenQueuedAndCancellationTokenFires_TransitionsToCompletedAndBlocksRunning()
    {
        var tracker = new WorkItemTracker();
        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cts = new CancellationTokenSource();

        using (cts.Token.Register(() => tracker.TryCancelIfQueued(tcs, cts.Token)))
        {
            Assert.Equal(WorkItemState.Queued, tracker.State);
            Assert.False(tcs.Task.IsCompleted);

            cts.Cancel();

            Assert.Equal(WorkItemState.Completed, tracker.State);
            Assert.True(tcs.Task.IsCanceled);

            // Revit thread attempting to transition to running MUST fail and skip
            Assert.False(tracker.TryTransitionToRunning());
            Assert.Equal(WorkItemState.Completed, tracker.State);
        }
    }
}
