namespace HPGrasshopper.McpBridge.Tests.Coordinator;

using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HPGrasshopper.McpBridge.Coordinator;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using Xunit;

public sealed class QueueLifecycleAndIdentityAdversarialTests : IDisposable
{
    private readonly GrasshopperBridgeCoordinator _coordinator = GrasshopperBridgeCoordinator.Instance;
    private readonly FakeExternalEventRaiser _fakeRaiser = new();
    private readonly BridgeSettings _settings = new() { ExecutionEnabled = true };

    public QueueLifecycleAndIdentityAdversarialTests()
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
    public async Task F5_RunOnRevitThreadAsync_WhenRaiseFails_ThrowsFast()
    {
        _fakeRaiser.ReturnValue = false;
        var work = new McpBridgeWorkRequest("FailRaise", (_, _) => "never", CancellationToken.None);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _coordinator.RunOnRevitThreadAsync(work));
        Assert.Contains("ExternalEvent.Raise() was denied", ex.Message);
        Assert.False(_coordinator.IsBusy);
    }

    [Fact]
    public async Task F5_RunOnRevitThreadAsync_WhenRaiseFails_MustNotExecuteWorkOnSubsequentTick()
    {
        _fakeRaiser.ReturnValue = false;
        var executed = false;
        var work = new McpBridgeWorkRequest("ZombieWork", (_, _) =>
        {
            executed = true;
            return "executed";
        }, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _coordinator.RunOnRevitThreadAsync(work));

        // Host ticks later — work was rejected at Raise time, so it must not run
        _coordinator.Execute(null!);
        Assert.False(executed);
    }

    [Fact]
    public async Task F5_QueueCancellation_UnblocksCompletionImmediatelyWithoutHostTick()
    {
        _fakeRaiser.ReturnValue = true;
        using var cts = new CancellationTokenSource();
        var executed = false;
        var work = new McpBridgeWorkRequest("CancelWork", (_, _) =>
        {
            executed = true;
            return "ok";
        }, cts.Token);

        var task = _coordinator.RunOnRevitThreadAsync(work);
        Assert.Equal(WorkItemState.Queued, _coordinator.GetWorkItemState(work));

        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.Equal(WorkItemState.Completed, _coordinator.GetWorkItemState(work));

        // When host ticks later, cancelled item must not execute
        _coordinator.Execute(null!);
        Assert.False(executed);
    }

    [Fact]
    public async Task F5_DeadlineTimeout_UnblocksCoordinatorAndResetsBusy_EvenIfHostNeverTicks()
    {
        _fakeRaiser.ReturnValue = true;
        _fakeRaiser.OnRaise = null; // Do not pump Execute

        var req = new ExecuteRequest("1 + 1", "none") { TimeoutSeconds = 5 };
        var sw = Stopwatch.StartNew();
        var result = await _coordinator.ExecuteAsync(999, req, null, CancellationToken.None);
        sw.Stop();

        Assert.True(result.IsError);
        Assert.True(result.TimedOut);
        Assert.True(result.RolledBack);
        Assert.Contains("timed out", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(_coordinator.IsBusy);
        Assert.True(sw.Elapsed >= TimeSpan.FromSeconds(4.5));
    }

    [Fact]
    public void F6_ValidateActiveDocument_NullTargets_DoesNotThrow()
    {
        var work = new McpBridgeWorkRequest("NoTarget", (_, _) => null, CancellationToken.None);
        work.ValidateActiveDocument(null);
    }

    [Fact]
    public void F6_ValidateActiveDocument_GuidEmpty_DoesNotThrow()
    {
        var work = new McpBridgeWorkRequest("EmptyGuid", (_, _) => null, CancellationToken.None, TargetDefId: Guid.Empty);
        work.ValidateActiveDocument(null);
    }

    [Fact]
    public void F6_ValidateActiveDocument_TargetDocTitle_WhenNoDocOpen_Throws()
    {
        var work = new McpBridgeWorkRequest("TitleReq", (_, _) => null, CancellationToken.None, TargetDocTitle: "Model.rvt");
        var ex = Assert.Throws<InvalidOperationException>(() => work.ValidateActiveDocument(null));
        Assert.Contains("Document identity mismatch", ex.Message);
        Assert.Contains("Model.rvt", ex.Message);
    }

    [Fact]
    public void F6_ValidateActiveDocument_TargetDocPath_WhenNoDocOpen_Throws()
    {
        var work = new McpBridgeWorkRequest("PathReq", (_, _) => null, CancellationToken.None, TargetDocPath: "C:\\Proj\\Model.rvt");
        var ex = Assert.Throws<InvalidOperationException>(() => work.ValidateActiveDocument(null));
        Assert.Contains("Document identity mismatch", ex.Message);
        Assert.Contains("C:\\Proj\\Model.rvt", ex.Message);
    }

    [Fact]
    public async Task F7_GetContextAsync_FallsBackGracefully_WhenHostNeverTicks()
    {
        _fakeRaiser.ReturnValue = true;
        _fakeRaiser.OnRaise = null;

        var sw = Stopwatch.StartNew();
        var context = await _coordinator.GetContextAsync(false, CancellationToken.None);
        sw.Stop();

        Assert.NotNull(context);
        Assert.Equal(PipeNaming.GrasshopperHost, context.Host);
        Assert.True(sw.Elapsed >= TimeSpan.FromMilliseconds(400));
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void F7_InvalidateCompiler_ClearsCachedCompilerAndInspector()
    {
        var compilerBefore = _coordinator.Compiler;
        Assert.NotNull(compilerBefore);

        _coordinator.InvalidateCompiler();

        var type = typeof(GrasshopperBridgeCoordinator);
        var compField = type.GetField("_compiler", BindingFlags.NonPublic | BindingFlags.Instance);
        var inspField = type.GetField("_inspector", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.Null(compField!.GetValue(_coordinator));
        Assert.Null(inspField!.GetValue(_coordinator));

        var compilerAfter = _coordinator.Compiler;
        Assert.NotNull(compilerAfter);
    }
}
