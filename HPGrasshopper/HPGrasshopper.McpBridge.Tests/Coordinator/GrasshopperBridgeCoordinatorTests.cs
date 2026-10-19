namespace HPGrasshopper.McpBridge.Tests.Coordinator;

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using HPGrasshopper.McpBridge.Coordinator;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using Xunit;

using HPGrasshopper.McpBridge.Host;

public sealed class FakeExternalEventRaiser : IExternalEventRaiser
{
    public int RaiseCount { get; private set; }
    public bool ReturnValue { get; set; } = true;
    public Action? OnRaise { get; set; }

    public bool Raise()
    {
        RaiseCount++;
        OnRaise?.Invoke();
        return ReturnValue;
    }

    public void Dispose() { }
}

public sealed class GrasshopperBridgeCoordinatorTests : IDisposable
{
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool SetDllDirectory(string lpPathName);

    static GrasshopperBridgeCoordinatorTests()
    {
        var revitDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Autodesk", "Revit 2026");
        if (Directory.Exists(revitDir))
        {
            SetDllDirectory(revitDir);
        }
        GrasshopperAssemblyResolver.Install();
    }

    private readonly GrasshopperBridgeCoordinator _coordinator = GrasshopperBridgeCoordinator.Instance;
    private readonly FakeExternalEventRaiser _fakeRaiser = new();
    private readonly BridgeSettings _settings = new() { ExecutionEnabled = true };

    public GrasshopperBridgeCoordinatorTests()
    {
        _coordinator.Initialize(_settings, _fakeRaiser);
        _coordinator.ExecutionHook = null;
    }

    public void Dispose()
    {
        _coordinator.ExecutionHook = null;
        _coordinator.Dispose();
    }

    [Fact]
    public void Coordinator_Instance_CanBeInitialized()
    {
        Assert.NotNull(_coordinator);
        Assert.False(_coordinator.IsBusy);
        Assert.Equal("HPGrasshopperBridgeCoordinator", _coordinator.GetName());
    }

    [Fact]
    public async Task RunOnRevitThreadAsync_EnqueuesWork_AndRaisesExternalEvent()
    {
        var executed = false;
        var work = new McpBridgeWorkRequest("TestWork", (app, ct) =>
        {
            executed = true;
            return "success_result";
        }, CancellationToken.None);

        var task = _coordinator.RunOnRevitThreadAsync(work);

        Assert.Equal(1, _fakeRaiser.RaiseCount);
        Assert.Equal(WorkItemState.Queued, _coordinator.GetWorkItemState(work));
        Assert.False(executed);

        _coordinator.Execute(null!);

        var result = await task;
        Assert.Equal("success_result", result);
        Assert.True(executed);
        Assert.Equal(WorkItemState.Completed, _coordinator.GetWorkItemState(work));
    }

    [Fact]
    public async Task RunOnRevitThreadAsync_PreCanceledToken_DoesNotExecuteWork()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var executed = false;
        var work = new McpBridgeWorkRequest("CanceledWork", (app, ct) =>
        {
            executed = true;
            return "unreachable";
        }, cts.Token);

        var task = _coordinator.RunOnRevitThreadAsync(work);
        _coordinator.Execute(null!);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.False(executed);
    }

    [Fact]
    public async Task RunOnRevitThreadAsync_ExceptionInWork_PropagatesToAwaitingTask()
    {
        var work = new McpBridgeWorkRequest("FaultyWork", (app, ct) =>
        {
            throw new InvalidOperationException("Simulated error in UI thread");
        }, CancellationToken.None);

        var task = _coordinator.RunOnRevitThreadAsync(work);
        _coordinator.Execute(null!);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => task);
        Assert.Equal("Simulated error in UI thread", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WhenExecutionDisabled_RefusesRequestImmediately()
    {
        _settings.ExecutionEnabled = false;
        var request = new ExecuteRequest("test code", "none") { SessionId = "sess-1" };

        var result = await _coordinator.ExecuteAsync(101, request, null, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains("Execution is disabled in bridge settings", result.Message);
        Assert.False(_coordinator.IsBusy);
    }

    [Fact]
    public async Task ExecuteAsync_PreventsConcurrentExecution()
    {
        var tcs = new TaskCompletionSource<ExecuteResult>();
        _coordinator.ExecutionHook = req => tcs.Task;

        var request1 = new ExecuteRequest("code1", "none");
        var task1 = _coordinator.ExecuteAsync(1, request1, null, CancellationToken.None);

        Assert.True(_coordinator.IsBusy);

        var request2 = new ExecuteRequest("code2", "none");
        var result2 = await _coordinator.ExecuteAsync(2, request2, null, CancellationToken.None);

        Assert.True(result2.IsError);
        Assert.Contains("Another Grasshopper script or task is still running", result2.Message);

        tcs.SetResult(new ExecuteResult { Message = "done" });
        var result1 = await task1;
        Assert.False(result1.IsError);
        Assert.False(_coordinator.IsBusy);
    }

    [Fact]
    public async Task CorrelatedCancel_CancelsMatchingActiveRequest()
    {
        var hookCts = new CancellationTokenSource();
        _coordinator.ExecutionHook = async req =>
        {
            await Task.Delay(10000, hookCts.Token);
            return new ExecuteResult { Message = "ok" };
        };

        var request = new ExecuteRequest("long script", "none") { SessionId = "sess-cancel" };
        using var cts = new CancellationTokenSource();

        var execTask = _coordinator.ExecuteAsync(555, request, null, cts.Token);
        while (!_coordinator.IsBusy) await Task.Yield();

        Assert.True(_coordinator.IsBusy);
        Assert.Equal(555, _coordinator.ActiveRequestId);
        Assert.Equal("sess-cancel", _coordinator.ActiveSessionId);

        // Cancel with mismatched ID should fail
        var rejectResult = _coordinator.Cancel(999, "sess-cancel");
        Assert.False(rejectResult.Cancelled);
        Assert.True(rejectResult.WasRunning);

        // Cancel with matching ID should succeed
        var cancelResult = _coordinator.Cancel(555, "sess-cancel");
        Assert.True(cancelResult.Cancelled);
        Assert.True(cancelResult.WasRunning);
        hookCts.Cancel();

        var finalResult = await execTask;
        Assert.True(finalResult.IsError);
        Assert.True(finalResult.RolledBack);
        Assert.Contains("cancelled", finalResult.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(_coordinator.IsBusy);
    }

    [Fact]
    public async Task GetContextAsync_ReflectsExecutionEnabledSetting()
    {
        _fakeRaiser.OnRaise = () => _coordinator.Execute(null!);
        _settings.ExecutionEnabled = false;
        var ctxDisabled = await _coordinator.GetContextAsync(false, CancellationToken.None);
        Assert.False(ctxDisabled.ExecutionEnabled);

        _settings.ExecutionEnabled = true;
        var ctxEnabled = await _coordinator.GetContextAsync(false, CancellationToken.None);
        Assert.True(ctxEnabled.ExecutionEnabled);
    }
}
