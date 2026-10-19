namespace HPDynamo.McpBridge.Tests.Coordinator;

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HPDynamo.McpBridge.Coordinator;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using Xunit;

public sealed class CoordinatorTests
{
    private static DynamoBridgeCoordinator CreateInitializedCoordinator(bool executionEnabled = true)
    {
        var coordinator = DynamoBridgeCoordinator.Instance;
        coordinator.Initialize(new BridgeSettings { ExecutionEnabled = executionEnabled }, externalEvent: null);
        return coordinator;
    }

    [Fact]
    public void InitialState_LiveCanvasIsNotAttached()
    {
        var coordinator = CreateInitializedCoordinator();
        coordinator.UnregisterLiveCanvas();

        Assert.False(coordinator.IsLiveCanvasAttached);
        Assert.Null(coordinator.ActiveViewModel);
        Assert.Null(coordinator.ActiveWorkspace);
        Assert.False(coordinator.IsBusy);
    }

    [Fact]
    public void UnregisterLiveCanvas_DetachesCanvasAndFiresStateChanged()
    {
        var coordinator = CreateInitializedCoordinator();
        var stateChangedFired = false;
        coordinator.StateChanged += () => stateChangedFired = true;

        coordinator.UnregisterLiveCanvas();

        Assert.False(coordinator.IsLiveCanvasAttached);
        Assert.True(stateChangedFired);
    }

    [Fact]
    public async Task GetContextAsync_HeadlessFallback_PopulatesDynamoInfo()
    {
        var coordinator = CreateInitializedCoordinator(executionEnabled: true);
        coordinator.UnregisterLiveCanvas();

        var context = await coordinator.GetContextAsync(includeSelection: false, CancellationToken.None);

        Assert.NotNull(context);
        Assert.Equal("dynamo", context.Host);
        Assert.NotNull(context.Dynamo);
        Assert.False(context.Dynamo.IsLive);
        Assert.Equal("Manual", context.Dynamo.RunType);
        Assert.Empty(context.Selection);
    }

    [Fact]
    public void SetActiveDocumentTitle_UpdatesTitleAndFiresStateChanged()
    {
        var coordinator = CreateInitializedCoordinator();
        var stateChangedFired = false;
        coordinator.StateChanged += () => stateChangedFired = true;

        coordinator.SetActiveDocumentTitle("Hospital_Wing_A.rvt");

        Assert.Equal("Hospital_Wing_A.rvt", coordinator.ActiveDocumentTitle);
        Assert.True(stateChangedFired);
    }

    [Fact]
    public async Task ExecuteAsync_ConcurrencyGuard_RejectsSimultaneousRuns()
    {
        var coordinator = CreateInitializedCoordinator();
        var busyField = typeof(DynamoBridgeCoordinator).GetField("_busy", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(busyField);

        try
        {
            busyField.SetValue(coordinator, 1);
            Assert.True(coordinator.IsBusy);

            var req = new ExecuteRequest("return 1;", "none");
            var result = await coordinator.ExecuteAsync(req, null, CancellationToken.None);

            Assert.True(result.IsError);
            Assert.Contains("Another Dynamo script or graph is still running", result.Message);
        }
        finally
        {
            busyField.SetValue(coordinator, 0);
        }
    }

    [Fact]
    public void Cancel_WhenIdle_ReturnsNotRunning()
    {
        var coordinator = CreateInitializedCoordinator();
        var result = coordinator.Cancel();

        Assert.False(result.WasRunning);
        Assert.False(result.Cancelled);
    }

    [Fact]
    public void Analyze_ValidScript_ReturnsAnalysisResultWithDynamoProfile()
    {
        var coordinator = CreateInitializedCoordinator();
        var result = coordinator.Analyze(new AnalyzeRequest("return 42;", "auto"));

        Assert.NotNull(result);
        Assert.True(result.Compiles);
        Assert.Empty(result.GuardViolations);
    }

    [Fact]
    public async Task ExecuteAsync_ConcurrentCalls_ExactlyOneSucceedsOthersRejected()
    {
        var coordinator = CreateInitializedCoordinator();
        var holdSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        coordinator.ExecutionHook = async _ =>
        {
            await holdSignal.Task;
            return new ExecuteResult { IsError = false, Message = "Success" };
        };

        try
        {
            var req = new ExecuteRequest("return 1;", "none");

            var firstTask = coordinator.ExecuteAsync(req, null, CancellationToken.None);

            while (!coordinator.IsBusy)
            {
                await Task.Yield();
            }

            var concurrentTasks = Enumerable.Range(0, 10)
                .Select(_ => coordinator.ExecuteAsync(req, null, CancellationToken.None))
                .ToArray();

            var rejectedResults = await Task.WhenAll(concurrentTasks);

            Assert.All(rejectedResults, res =>
            {
                Assert.True(res.IsError);
                Assert.Contains("Another Dynamo script or graph is still running", res.Message);
            });

            holdSignal.SetResult(true);
            var firstResult = await firstTask;

            Assert.False(firstResult.IsError);
            Assert.False(coordinator.IsBusy);
        }
        finally
        {
            coordinator.ExecutionHook = null;
            holdSignal.TrySetResult(true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_CorrelatedCancel_IgnoresStaleIdAndCancelsMatchingId()
    {
        var coordinator = CreateInitializedCoordinator();
        var holdSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        coordinator.ExecutionHook = async _ =>
        {
            await holdSignal.Task;
            throw new OperationCanceledException();
        };

        try
        {
            var req = new ExecuteRequest("return 1;", "none");
            var execTask = coordinator.ExecuteAsync(101L, req, null, CancellationToken.None);

            while (!coordinator.IsBusy)
            {
                await Task.Yield();
            }

            Assert.Equal(101L, coordinator.ActiveRequestId);

            // Stale cancel targeting another request ID (999) must be ignored
            var staleCancel = coordinator.Cancel(999L);
            Assert.False(staleCancel.Cancelled);
            Assert.True(staleCancel.WasRunning);
            Assert.True(coordinator.IsBusy);
            Assert.Equal(101L, coordinator.ActiveRequestId);

            // Matching cancel targeting 101 must cancel
            var matchingCancel = coordinator.Cancel(101L);
            Assert.True(matchingCancel.Cancelled);
            Assert.True(matchingCancel.WasRunning);

            holdSignal.SetResult(true);
            var res = await execTask;
            Assert.True(res.IsError || res.RolledBack);
        }
        finally
        {
            coordinator.ExecutionHook = null;
            holdSignal.TrySetResult(true);
        }
    }

    [Fact]
    public async Task Cancel_WithNullOrZero_CancelsUnconditionally()
    {
        var coordinator = CreateInitializedCoordinator();
        var holdSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        coordinator.ExecutionHook = async _ =>
        {
            await holdSignal.Task;
            return new ExecuteResult { IsError = false, Message = "Completed" };
        };

        try
        {
            var req = new ExecuteRequest("return 1;", "none");
            var execTask = coordinator.ExecuteAsync(202L, req, null, CancellationToken.None);

            while (!coordinator.IsBusy)
            {
                await Task.Yield();
            }

            // Global cancel (null target ID)
            var cancelResult = coordinator.Cancel(null);
            Assert.True(cancelResult.Cancelled);

            holdSignal.SetResult(true);
            await execTask;
        }
        finally
        {
            coordinator.ExecutionHook = null;
            holdSignal.TrySetResult(true);
        }
    }
}
