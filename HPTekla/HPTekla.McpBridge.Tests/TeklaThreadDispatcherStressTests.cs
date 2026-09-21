using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Pipe;
using Xunit;

namespace HPTekla.McpBridge.Tests;

public sealed class TeklaThreadDispatcherStressTests
{
    [Fact]
    public async Task EnqueueAndExecution_RunsWorkAndReturnsResult()
    {
        using var dispatcher = new TeklaThreadDispatcher(TimeSpan.FromSeconds(2));

        // When work is enqueued and OnTick is simulated
        var task = dispatcher.InvokeAsync("test-work", ct =>
        {
            return 42 * 2;
        }, CancellationToken.None);

        // Access private queue to trigger OnTick
        var queueField = typeof(TeklaThreadDispatcher).GetField("_queue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var queue = (MainThreadQueue)queueField.GetValue(dispatcher);

        // Simulate idle tick
        queue.OnTick();

        var result = await task;
        Assert.Equal(84, result);
        Assert.Equal(0, dispatcher.PendingCount);
    }

    [Fact]
    public async Task ExpireWithoutTicks_CompletesWithBusyExceptionWhenNoTicksOccur()
    {
        // EMPIRICAL CHALLENGE:
        // When a native modal dialog is running in TeklaStructures.exe, neither ComponentDispatcher.ThreadIdle
        // nor Dispatcher.BeginInvoke(ApplicationIdle) will ever execute.
        // Therefore, 0 ticks occur. What happens?
        // With `expireWithoutTicks: true`, MainThreadQueue must fail the item after BusyGrace + 50ms.
        var shortGrace = TimeSpan.FromMilliseconds(300);
        using var dispatcher = new TeklaThreadDispatcher(shortGrace);

        var sw = Stopwatch.StartNew();

        // Enqueue without pumping any ticks
        var task = dispatcher.InvokeAsync("modal-dialog-blocked-work", ct =>
        {
            return "should never run";
        }, CancellationToken.None);

        Assert.Equal(1, dispatcher.PendingCount);

        // Await the task without calling OnTick
        var exception = await Assert.ThrowsAsync<BridgeRequestException>(() => task);
        sw.Stop();

        // Verify that it threw BridgeRequestException.Busy (-32002)
        Assert.Equal(BridgeErrorCode.Busy, exception.Code);
        Assert.Contains("Tekla Structures", exception.Message);

        // Verify timing: it should expire close to shortGrace (300ms + ~50ms)
        Assert.True(sw.ElapsedMilliseconds >= 250, $"Elapsed was {sw.ElapsedMilliseconds}ms, expected >= 250ms");
        Assert.True(sw.ElapsedMilliseconds < 2500, $"Elapsed was {sw.ElapsedMilliseconds}ms, expected < 2500ms");

        // Verify queue is clean
        Assert.Equal(0, dispatcher.PendingCount);
    }

    [Fact]
    public async Task MultipleQueuedItems_ExpireInOrderWhenNoTicksOccur()
    {
        var shortGrace = TimeSpan.FromMilliseconds(200);
        using var dispatcher = new TeklaThreadDispatcher(shortGrace);

        var task1 = dispatcher.InvokeAsync("work1", ct => 1, CancellationToken.None);
        await Task.Delay(50);
        var task2 = dispatcher.InvokeAsync("work2", ct => 2, CancellationToken.None);

        Assert.Equal(2, dispatcher.PendingCount);

        var ex1 = await Assert.ThrowsAsync<BridgeRequestException>(() => task1);
        Assert.Equal(BridgeErrorCode.Busy, ex1.Code);

        var ex2 = await Assert.ThrowsAsync<BridgeRequestException>(() => task2);
        Assert.Equal(BridgeErrorCode.Busy, ex2.Code);

        Assert.Equal(0, dispatcher.PendingCount);
    }

    [Fact]
    public async Task Cancellation_CancelsWaitingItemBeforeExecution()
    {
        using var dispatcher = new TeklaThreadDispatcher(TimeSpan.FromSeconds(5));
        using var cts = new CancellationTokenSource();

        var workStarted = false;
        var task = dispatcher.InvokeAsync("cancellable-work", ct =>
        {
            workStarted = true;
            return "ok";
        }, cts.Token);

        Assert.Equal(1, dispatcher.PendingCount);

        // Cancel while item is pending in queue
        cts.Cancel();

        // Trigger OnTick
        var queueField = typeof(TeklaThreadDispatcher).GetField("_queue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var queue = (MainThreadQueue)queueField.GetValue(dispatcher);
        queue.OnTick();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.False(workStarted, "Work must never have started execution");
        Assert.Equal(0, dispatcher.PendingCount);
    }

    [Fact]
    public async Task ExceptionInWork_PropagatesToCallerCleanly()
    {
        using var dispatcher = new TeklaThreadDispatcher(TimeSpan.FromSeconds(2));

        var task = dispatcher.InvokeAsync<string>("faulty-work", ct =>
        {
            throw new InvalidOperationException("Simulated Tekla API model error");
        }, CancellationToken.None);

        var queueField = typeof(TeklaThreadDispatcher).GetField("_queue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var queue = (MainThreadQueue)queueField.GetValue(dispatcher);
        queue.OnTick();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => task);
        Assert.Equal("Simulated Tekla API model error", ex.Message);
        Assert.Equal(0, dispatcher.PendingCount);
    }

    [Fact]
    public void IsQuiescent_ReturnsBooleanWithoutThrowing()
    {
        // When Tekla Structures is not running or Operation.IsMacroRunning throws,
        // IsQuiescent catches and safely returns true.
        var quiescent = TeklaThreadDispatcher.IsQuiescent();
        Assert.True(quiescent);
    }
}
