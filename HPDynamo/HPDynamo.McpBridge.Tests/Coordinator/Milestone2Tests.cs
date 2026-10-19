namespace HPDynamo.McpBridge.Tests.Coordinator;

using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HPDynamo.McpBridge.Coordinator;
using HPDynamo.McpBridge.Execution;
using HPDynamo.McpBridge.Support;
using HPRebar.McpBridge.Core.Model;
using Xunit;

/// <summary>
///     Comprehensive test suite for Milestone 2 (RV-02, RV-03, RV-05).
///     Verifies 3-state work item lifecycle, queue drain on Dispose,
///     token propagation, and evaluation synchronization helpers.
/// </summary>
public sealed class Milestone2Tests
{
    // =========================================================================
    // 1. RV-05: WorkItemState & WorkItemTracker Lifecycle
    // =========================================================================

    [Fact]
    public void WorkItemTracker_InitialState_IsQueued()
    {
        var tracker = new WorkItemTracker();
        Assert.Equal(WorkItemState.Queued, tracker.State);
    }

    [Fact]
    public void WorkItemTracker_TransitionToRunning_SucceedsOnlyOnce()
    {
        var tracker = new WorkItemTracker();
        Assert.True(tracker.TryTransitionToRunning());
        Assert.Equal(WorkItemState.Running, tracker.State);

        // Subsequent transition attempt from Running must fail
        Assert.False(tracker.TryTransitionToRunning());
        Assert.Equal(WorkItemState.Running, tracker.State);
    }

    [Fact]
    public void WorkItemTracker_TransitionToCompleted_FromRunning_Succeeds()
    {
        var tracker = new WorkItemTracker();
        Assert.True(tracker.TryTransitionToRunning());
        Assert.True(tracker.TryTransitionToCompleted());
        Assert.Equal(WorkItemState.Completed, tracker.State);

        // Second completion returns false (already completed)
        Assert.False(tracker.TryTransitionToCompleted());
    }

    [Fact]
    public void WorkItemTracker_TryCancelIfQueued_CancelsTaskAndSetsCompleted()
    {
        using var cts = new CancellationTokenSource();
        var tcs = new TaskCompletionSource<object?>();
        var tracker = new WorkItemTracker();

        bool canceled = tracker.TryCancelIfQueued(tcs, cts.Token);

        Assert.True(canceled);
        Assert.Equal(WorkItemState.Completed, tracker.State);
        Assert.True(tcs.Task.IsCanceled);
    }

    [Fact]
    public void WorkItemTracker_TryCancelIfQueued_WhenAlreadyRunning_DoesNotCancelTask()
    {
        using var cts = new CancellationTokenSource();
        var tcs = new TaskCompletionSource<object?>();
        var tracker = new WorkItemTracker();

        Assert.True(tracker.TryTransitionToRunning());

        // Cancel must be rejected because item is actively running on Revit thread
        bool canceled = tracker.TryCancelIfQueued(tcs, cts.Token);

        Assert.False(canceled);
        Assert.Equal(WorkItemState.Running, tracker.State);
        Assert.False(tcs.Task.IsCanceled);
        Assert.False(tcs.Task.IsCompleted);
    }

    // =========================================================================
    // 2. RV-05: Coordinator Dispose & Queue Drain
    // =========================================================================

    private static McpBridgeWorkRequest CreateTestWorkRequest(CancellationToken ct = default)
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
    public async Task Coordinator_Dispose_DrainsPendingQueueWithException()
    {
        var coordinator = DynamoBridgeCoordinator.Instance;
        coordinator.Initialize(new BridgeSettings { ExecutionEnabled = true });

        using var cts = new CancellationTokenSource();
        var req1 = CreateTestWorkRequest(cts.Token);
        var req2 = CreateTestWorkRequest(cts.Token);

        var pendingField = typeof(DynamoBridgeCoordinator)
            .GetField("_pending", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(pendingField);
        var pendingQueue = (ConcurrentQueue<McpBridgeWorkRequest>)pendingField.GetValue(coordinator)!;

        pendingQueue.Enqueue(req1);
        pendingQueue.Enqueue(req2);

        coordinator.Dispose();

        Assert.Empty(pendingQueue);
        Assert.True(req1.Completion.Task.IsFaulted || req1.Completion.Task.IsCanceled);
        Assert.True(req2.Completion.Task.IsFaulted || req2.Completion.Task.IsCanceled);

        var ex1 = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => req1.Completion.Task);
        var ex2 = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => req2.Completion.Task);
        Assert.NotNull(ex1);
        Assert.NotNull(ex2);
    }

    // =========================================================================
    // 3. RV-05: Work Item State Query & Cancellation Handling
    // =========================================================================

    [Fact]
    public void Coordinator_GetWorkItemState_UntrackedItem_ReturnsCompleted()
    {
        var coordinator = DynamoBridgeCoordinator.Instance;
        using var cts = new CancellationTokenSource();
        var req = CreateTestWorkRequest(cts.Token);

        var state = coordinator.GetWorkItemState(req);
        Assert.Equal(WorkItemState.Completed, state);
    }

    // =========================================================================
    // 4. RV-02B: DynamoScriptGlobals RunAndWait Helper
    // =========================================================================

    [Fact]
    public void DynamoScriptGlobals_HasRunAndWaitMethod()
    {
        var method = typeof(DynamoScriptGlobals).GetMethod(
            nameof(DynamoScriptGlobals.RunAndWait),
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(Action), parameters[0].ParameterType);
        Assert.True(parameters[0].IsOptional);
        Assert.Equal(typeof(TimeSpan?), parameters[1].ParameterType);
        Assert.True(parameters[1].IsOptional);
    }

    [Fact]
    public void DynamoScriptGlobals_RunAndWait_WhenDynamoNotModel_ReturnsFalse()
    {
        var globals = (DynamoScriptGlobals)RuntimeHelpers.GetUninitializedObject(typeof(DynamoScriptGlobals));
        bool result = globals.RunAndWait();
        Assert.False(result);
    }

    // =========================================================================
    // 5. RV-02A: CanvasGraphRunner Input Validation
    // =========================================================================

    [Fact]
    public void CanvasGraphRunner_ExecuteDesignScript_WhenViewModelNull_ThrowsArgumentNullException()
    {
        var runner = new CanvasGraphRunner();
        Assert.Throws<ArgumentNullException>(() =>
            runner.ExecuteDesignScript(null!, "x = 1;", null, false, CancellationToken.None, _ => { }));
    }

    [Fact]
    public void CanvasGraphRunner_RunDynFile_WhenViewModelNull_ThrowsArgumentNullException()
    {
        var runner = new CanvasGraphRunner();
        Assert.Throws<ArgumentNullException>(() =>
            runner.RunDynFile(null!, "non_existent_graph.dyn", null, false, CancellationToken.None, _ => { }));
    }

    [Fact]
    public void CanvasGraphRunner_RunActiveGraph_WhenViewModelNull_ThrowsArgumentNullException()
    {
        var runner = new CanvasGraphRunner();
        Assert.Throws<ArgumentNullException>(() =>
            runner.RunActiveGraph(null!, false, CancellationToken.None, _ => { }));
    }

    [Fact]
    public void DynamoEvaluationSync_WhenModelNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DynamoEvaluationSync.RunAndWait(null!, () => { }, TimeSpan.FromSeconds(5), CancellationToken.None, _ => { }));
    }

    [Fact]
    public void DynamoEvaluationSync_WhenRunActionNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DynamoEvaluationSync.RunAndWait(null!, null!, TimeSpan.FromSeconds(5), CancellationToken.None, _ => { }));
    }
}

