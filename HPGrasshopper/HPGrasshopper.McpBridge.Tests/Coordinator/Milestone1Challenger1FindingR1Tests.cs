namespace HPGrasshopper.McpBridge.Tests.Coordinator;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPGrasshopper.McpBridge.Coordinator;
using HPGrasshopper.McpBridge.Host;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using Xunit;

/// <summary>
///     Adversarial empirical stress suite for Finding R1:
///     1. Correlated cancellation of active/queued native requests and IsBusy release.
///     2. Timeout &amp; deadline enforcement when host does not tick.
///     3. Concurrent contention between native tools returning BridgeErrorCode.Busy (-32002).
///     4. Exception resilience and IsBusy cleanup across all lifecycle branches.
/// </summary>
public sealed class Milestone1Challenger1FindingR1Tests : IDisposable
{
    private sealed class AutoDispatchingEventRaiser(GrasshopperBridgeCoordinator coordinator) : IExternalEventRaiser
    {
        public bool Raise()
        {
            coordinator.Execute(null!);
            return true;
        }

        public void Dispose() { }
    }

    private readonly GrasshopperBridgeCoordinator _coordinator = GrasshopperBridgeCoordinator.Instance;
    private readonly BridgeSettings _settings = new() { ExecutionEnabled = true };
    private readonly AutoDispatchingEventRaiser _raiser;
    private readonly GrasshopperRpcDispatcher _dispatcher;

    public Milestone1Challenger1FindingR1Tests()
    {
        _raiser = new AutoDispatchingEventRaiser(_coordinator);
        _coordinator.Initialize(_settings, _raiser);
        _coordinator.ExecutionHook = null;
        _dispatcher = new GrasshopperRpcDispatcher(_coordinator, _settings);
    }

    public void Dispose()
    {
        _coordinator.ExecutionHook = null;
        _coordinator.Dispose();
    }

    [Fact]
    public async Task CorrelatedCancel_DuringNativeRpcExecution_CancelsAndReleasesBusy()
    {
        var runTcs = new TaskCompletionSource<bool>();
        var startedTcs = new TaskCompletionSource<bool>();

        _coordinator.ExecutionHook = async req =>
        {
            startedTcs.TrySetResult(true);
            await runTcs.Task;
            return new ExecuteResult { Message = "completed" };
        };

        var env = JsonRpcEnvelope.Request(101, "grasshopper.read_grasshopper_canvas", null);
        var execTask = _dispatcher.DispatchAsync(101, env, null, CancellationToken.None);

        await startedTcs.Task;
        Assert.True(_coordinator.IsBusy);
        Assert.Equal(101, _coordinator.ActiveRequestId);

        // Cancel via coordinator correlated cancel
        var cancelResult = _coordinator.Cancel(101);
        Assert.True(cancelResult.Cancelled);
        Assert.True(cancelResult.WasRunning);

        runTcs.TrySetCanceled();

        var response = await execTask;
        Assert.NotNull(response);
        Assert.False(_coordinator.IsBusy);

        // Subsequent native request must succeed without being blocked by busy flag
        _coordinator.ExecutionHook = null;
        var followUpEnv = JsonRpcEnvelope.Request(102, "grasshopper.read_grasshopper_canvas", null);
        var followUpRes = await _dispatcher.DispatchAsync(102, followUpEnv, null, CancellationToken.None);

        Assert.NotNull(followUpRes);
        Assert.Null(followUpRes!.Error);
        Assert.False(_coordinator.IsBusy);
    }

    [Fact]
    public async Task ClientCancellation_DuringNativeRpcDispatch_ReleasesBusyPromptly()
    {
        var startedTcs = new TaskCompletionSource<bool>();
        var blockTcs = new TaskCompletionSource<bool>();
        using var clientCts = new CancellationTokenSource();

        _coordinator.ExecutionHook = async req =>
        {
            startedTcs.TrySetResult(true);
            await blockTcs.Task;
            return new ExecuteResult { Message = "ok" };
        };

        var env = JsonRpcEnvelope.Request(201, "grasshopper.read_grasshopper_canvas", null);
        var task = _dispatcher.DispatchAsync(201, env, null, clientCts.Token);

        await startedTcs.Task;
        Assert.True(_coordinator.IsBusy);

        // Client cancels token
        clientCts.Cancel();
        blockTcs.TrySetCanceled(clientCts.Token);

        var result = await task;
        Assert.NotNull(result);
        Assert.False(_coordinator.IsBusy);

        // Immediate follow-up request verifies coordinator is unblocked
        _coordinator.ExecutionHook = null;
        var probeEnv = JsonRpcEnvelope.Request(202, "grasshopper.read_grasshopper_canvas", null);
        var probeResult = await _dispatcher.DispatchAsync(202, probeEnv, null, CancellationToken.None);

        Assert.NotNull(probeResult);
        Assert.Null(probeResult!.Error);
        Assert.False(_coordinator.IsBusy);
    }

    [Fact]
    public async Task DeadlineTimeout_WhenHostDoesNotTick_ReturnsTimeoutErrorAndReleasesBusy()
    {
        // Simulate host that never ticks (OnRaise = null, do not execute on UI thread)
        var nonTickingRaiser = new FakeExternalEventRaiser { ReturnValue = true, OnRaise = null };
        _coordinator.Initialize(_settings, nonTickingRaiser);
        _coordinator.ExecutionHook = null;

        var req = new ExecuteRequest(string.Empty, "none", false, 5) { Label = "read_grasshopper_canvas" };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await _coordinator.ExecuteAsync(301, req, null, CancellationToken.None);
        sw.Stop();

        Assert.True(result.IsError);
        Assert.True(result.TimedOut);
        Assert.True(result.RolledBack);
        Assert.Contains("timed out", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("without host tick", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(_coordinator.IsBusy);

        // Next call with active ticking raiser runs cleanly and is not blocked
        _coordinator.Initialize(_settings, _raiser);
        var probeEnv = JsonRpcEnvelope.Request(302, "grasshopper.read_grasshopper_canvas", null);
        var probeRes = await _dispatcher.DispatchAsync(302, probeEnv, null, CancellationToken.None);

        Assert.NotNull(probeRes);
        Assert.Null(probeRes!.Error);
        Assert.False(_coordinator.IsBusy);
    }

    [Fact]
    public async Task ConcurrentNativeToolDispatch_ContenderIsImmediatelyRejectedWithBusyCode()
    {
        var blockTcs = new TaskCompletionSource<bool>();
        var firstStarted = new TaskCompletionSource<bool>();

        _coordinator.ExecutionHook = async req =>
        {
            firstStarted.TrySetResult(true);
            await blockTcs.Task;
            return new ExecuteResult { Message = "first_complete", Value = BridgeJson.ToElement(new { ok = true }) };
        };

        var env1 = JsonRpcEnvelope.Request(401, "grasshopper.batch_edit_grasshopper_canvas", new { dryRun = true });
        var task1 = _dispatcher.DispatchAsync(401, env1, null, CancellationToken.None);

        await firstStarted.Task;
        Assert.True(_coordinator.IsBusy);

        // Second native call while first is in-flight
        var env2 = JsonRpcEnvelope.Request(402, "grasshopper.read_grasshopper_canvas", null);
        var response2 = await _dispatcher.DispatchAsync(402, env2, null, CancellationToken.None);

        Assert.NotNull(response2);
        Assert.NotNull(response2!.Error);
        Assert.Equal(BridgeErrorCode.Busy, response2.Error!.Code);
        Assert.Contains("Another Grasshopper script or task is still running", response2.Error.Message);

        // Unblock first
        blockTcs.TrySetResult(true);
        var response1 = await task1;

        Assert.NotNull(response1);
        Assert.Null(response1!.Error);
        Assert.False(_coordinator.IsBusy);

        // Third call succeeds cleanly
        _coordinator.ExecutionHook = null;
        var env3 = JsonRpcEnvelope.Request(403, "grasshopper.read_grasshopper_canvas", null);
        var response3 = await _dispatcher.DispatchAsync(403, env3, null, CancellationToken.None);

        Assert.NotNull(response3);
        Assert.Null(response3!.Error);
        Assert.False(_coordinator.IsBusy);
    }

    [Fact]
    public async Task ExceptionInExecutionEngine_ReleasesBusyAndPreservesDispatcherStability()
    {
        _coordinator.ExecutionHook = req =>
            throw new InvalidOperationException("Fatal unexpected execution crash");

        var env = JsonRpcEnvelope.Request(501, "grasshopper.read_grasshopper_canvas", null);
        var response = await _dispatcher.DispatchAsync(501, env, null, CancellationToken.None);

        Assert.NotNull(response);
        Assert.NotNull(response!.Error);
        Assert.Equal(BridgeErrorCode.InternalError, response.Error!.Code);
        Assert.Contains("InvalidOperationException", response.Error.Message);
        Assert.False(_coordinator.IsBusy);

        // Follow-up request succeeds
        _coordinator.ExecutionHook = null;
        var probeEnv = JsonRpcEnvelope.Request(502, "grasshopper.read_grasshopper_canvas", null);
        var probeRes = await _dispatcher.DispatchAsync(502, probeEnv, null, CancellationToken.None);

        Assert.NotNull(probeRes);
        Assert.Null(probeRes!.Error);
        Assert.False(_coordinator.IsBusy);
    }

    [Fact]
    public void Cancel_WhenNotBusy_ReturnsFalseFalseCleanly()
    {
        Assert.False(_coordinator.IsBusy);
        var result = _coordinator.Cancel();
        Assert.False(result.Cancelled);
        Assert.False(result.WasRunning);
    }

    [Fact]
    public void Cancel_WithMismatchedRequestId_ReturnsFalseTrue()
    {
        var runTcs = new TaskCompletionSource<bool>();
        _coordinator.ExecutionHook = async req =>
        {
            await runTcs.Task;
            return new ExecuteResult { Message = "ok" };
        };

        var req = new ExecuteRequest("dummy", "none") { SessionId = "test-session" };
        var execTask = _coordinator.ExecuteAsync(888, req, null, CancellationToken.None);

        while (!_coordinator.IsBusy) Thread.Yield();
        Assert.True(_coordinator.IsBusy);

        var cancelMismatch = _coordinator.Cancel(777, "test-session");
        Assert.False(cancelMismatch.Cancelled);
        Assert.True(cancelMismatch.WasRunning);

        runTcs.TrySetResult(true);
        _coordinator.ExecutionHook = null;
    }
}
