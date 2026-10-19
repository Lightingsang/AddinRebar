namespace HPGrasshopper.McpBridge.Tests.Coordinator;

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HPGrasshopper.McpBridge.Coordinator;
using HPGrasshopper.McpBridge.Execution;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using Xunit;

public sealed class Milestone2QueueAndContextTests : IDisposable
{
    private readonly GrasshopperBridgeCoordinator _coordinator = GrasshopperBridgeCoordinator.Instance;
    private readonly FakeExternalEventRaiser _fakeRaiser = new();
    private readonly BridgeSettings _settings = new() { ExecutionEnabled = true };

    public Milestone2QueueAndContextTests()
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
    public async Task RunOnRevitThreadAsync_RaiseReturnsFalse_ThrowsImmediately()
    {
        _fakeRaiser.ReturnValue = false;
        var work = new McpBridgeWorkRequest("DeniedWork", (_, _) => "ok", CancellationToken.None);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _coordinator.RunOnRevitThreadAsync(work));
        Assert.Contains("ExternalEvent.Raise() was denied", ex.Message);
        Assert.Equal(WorkItemState.Completed, _coordinator.GetWorkItemState(work));
    }

    [Fact]
    public async Task RunOnRevitThreadAsync_QueueCancellation_UnblocksWithoutHostTick()
    {
        _fakeRaiser.ReturnValue = true;
        using var cts = new CancellationTokenSource();
        var work = new McpBridgeWorkRequest("QueuedWork", (_, _) => "ok", cts.Token);

        var task = _coordinator.RunOnRevitThreadAsync(work);
        Assert.Equal(WorkItemState.Queued, _coordinator.GetWorkItemState(work));

        // Cancel while queued — must unblock immediately without host tick calling Execute()
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.Equal(WorkItemState.Completed, _coordinator.GetWorkItemState(work));
    }

    [Fact]
    public void McpBridgeWorkRequest_ValidateActiveDocument_Mismatch_Throws()
    {
        var workWithTitle = new McpBridgeWorkRequest("TestWork", (_, _) => null, CancellationToken.None)
        {
            TargetDocTitle = "MyProject.rvt"
        };
        var ex = Assert.Throws<InvalidOperationException>(() => workWithTitle.ValidateActiveDocument(null));
        Assert.Contains("Document identity mismatch", ex.Message);

        var workNoTitle = new McpBridgeWorkRequest("TestWorkNoTitle", (_, _) => null, CancellationToken.None);
        workNoTitle.ValidateActiveDocument(null);
    }

    [Fact]
    public void InvalidateCompiler_ResetsCachedCompilerAndInspector()
    {
        var comp1 = _coordinator.Compiler;
        Assert.NotNull(comp1);

        _coordinator.InvalidateCompiler();

        var type = typeof(GrasshopperBridgeCoordinator);
        var compField = type.GetField("_compiler", BindingFlags.NonPublic | BindingFlags.Instance);
        var inspField = type.GetField("_inspector", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.Null(compField!.GetValue(_coordinator));
        Assert.Null(inspField!.GetValue(_coordinator));

        // Re-accessing Compiler property creates a new instance cleanly
        var comp2 = _coordinator.Compiler;
        Assert.NotNull(comp2);
    }

    [Fact]
    public async Task GetContextAsync_FallsBackGracefully_WhenExternalEventDoesNotTick()
    {
        // Do NOT wire OnRaise to Execute so it simulates a non-responsive UI thread
        _fakeRaiser.OnRaise = null;
        _fakeRaiser.ReturnValue = true;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var context = await _coordinator.GetContextAsync(false, cts.Token);

        Assert.NotNull(context);
        Assert.Equal("grasshopper", context.Host);
    }
}
