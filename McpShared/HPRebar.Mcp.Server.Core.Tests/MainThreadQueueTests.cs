using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     The AutoCAD marshalling rules without AutoCAD: work runs only on a quiescent tick, a busy host
///     fails the request after the grace period with the actionable code, and a request completed while
///     it waited is never run by a late tick.
/// </summary>
public sealed class MainThreadQueueTests
{
    private static readonly TimeSpan Grace = TimeSpan.FromSeconds(10);

    private long _nowMs = 1_000_000;
    private bool _quiescent = true;
    private int _wakes;

    private MainThreadQueue NewQueue() => new MainThreadQueue(() => _quiescent, "AutoCAD", Grace, () => _wakes++, () => _nowMs);

    private void Advance(int seconds) => _nowMs += seconds * 1000L;

    [Fact]
    public async Task Work_runs_on_the_tick_and_completes_the_request()
    {
        var queue = NewQueue();
        var threadOfWork = 0;

        var task = queue.RunAsync(new MainThreadWorkItem("read", _ => { threadOfWork = Environment.CurrentManagedThreadId; return 42; }, CancellationToken.None));

        Assert.False(task.IsCompleted);
        Assert.Equal(1, _wakes);
        Assert.Equal(1, queue.PendingCount);

        queue.OnTick();

        Assert.Equal(42, await task);
        Assert.Equal(Environment.CurrentManagedThreadId, threadOfWork);
        Assert.Equal(0, queue.PendingCount);
    }

    [Fact]
    public async Task Busy_host_keeps_the_request_within_the_grace_and_fails_it_after()
    {
        var queue = NewQueue();
        _quiescent = false;
        var ran = false;

        var task = queue.RunAsync(new MainThreadWorkItem("execute", _ => { ran = true; return 1; }, CancellationToken.None));

        Advance(9);
        queue.OnTick();
        Assert.False(task.IsCompleted);
        Assert.Equal(1, queue.PendingCount);

        Advance(2);
        queue.OnTick();

        var error = await Assert.ThrowsAsync<BridgeRequestException>(() => task);
        Assert.Equal(BridgeErrorCode.Busy, error.Code);
        Assert.Contains("AutoCAD", error.Message);
        Assert.False(ran);
        Assert.Equal(0, queue.PendingCount);
    }

    [Fact]
    public async Task Host_becoming_quiescent_inside_the_grace_runs_the_waiting_work()
    {
        var queue = NewQueue();
        _quiescent = false;

        var task = queue.RunAsync(new MainThreadWorkItem("execute", _ => "done", CancellationToken.None));
        queue.OnTick();

        _quiescent = true;
        Advance(3);
        queue.OnTick();

        Assert.Equal("done", await task);
    }

    [Fact]
    public async Task A_request_older_than_the_grace_is_refused_even_on_a_quiescent_tick()
    {
        // The server stops waiting after its own timeout; work that old must not run late and commit unseen.
        var queue = NewQueue();
        var ran = false;
        var task = queue.RunAsync(new MainThreadWorkItem("execute", _ => { ran = true; return 1; }, CancellationToken.None));

        Advance(11);
        queue.OnTick();

        var error = await Assert.ThrowsAsync<BridgeRequestException>(() => task);
        Assert.Equal(BridgeErrorCode.Busy, error.Code);
        Assert.False(ran);
    }

    [Fact]
    public async Task A_request_cancelled_while_waiting_is_never_started()
    {
        var queue = NewQueue();
        using var cancel = new CancellationTokenSource();
        var ran = false;
        var task = queue.RunAsync(new MainThreadWorkItem("execute", _ => { ran = true; return 1; }, cancel.Token));

        cancel.Cancel(); // cancel_execution or the server went away before the host got to it
        queue.OnTick();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.False(ran);
        Assert.Equal(0, queue.PendingCount);
    }

    [Fact]
    public async Task A_tick_raised_from_inside_the_work_does_not_start_the_next_request()
    {
        // A script that pumps messages makes the host raise Idle again while the first one is still running.
        var order = new List<string>();
        var queue = NewQueue();

        var first = queue.RunAsync(new MainThreadWorkItem("first", _ => { order.Add("first-start"); queue.OnTick(); order.Add("first-end"); return 1; }, CancellationToken.None));
        var second = queue.RunAsync(new MainThreadWorkItem("second", _ => { order.Add("second"); return 2; }, CancellationToken.None));

        queue.OnTick();

        Assert.Equal(1, await first);
        Assert.Equal(2, await second);
        Assert.Equal(["first-start", "first-end", "second"], order);
    }

    [Fact]
    public void A_request_completed_while_waiting_is_skipped_by_the_tick()
    {
        var queue = NewQueue();
        var ran = false;
        var item = new MainThreadWorkItem("execute", _ => { ran = true; return 1; }, CancellationToken.None);

        _ = queue.RunAsync(item);
        item.Completion.TrySetCanceled(TestContext.Current.CancellationToken); // the pipe side gave up (server disconnected, timeout)

        queue.OnTick();

        Assert.False(ran);
        Assert.Equal(0, queue.PendingCount);
    }

    [Fact]
    public async Task An_exception_in_the_work_faults_only_that_request()
    {
        var queue = NewQueue();

        var failing = queue.RunAsync(new MainThreadWorkItem("bad", _ => throw new InvalidOperationException("boom"), CancellationToken.None));
        var fine = queue.RunAsync(new MainThreadWorkItem("good", _ => "ok", CancellationToken.None));

        queue.OnTick();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => failing);
        Assert.Equal("boom", error.Message);
        Assert.Equal("ok", await fine);
    }

    [Fact]
    public async Task FailAll_completes_every_waiting_request()
    {
        var queue = NewQueue();
        var first = queue.RunAsync(new MainThreadWorkItem("a", _ => 1, CancellationToken.None));
        var second = queue.RunAsync(new MainThreadWorkItem("b", _ => 2, CancellationToken.None));

        queue.FailAll(new ObjectDisposedException("bridge"));

        await Assert.ThrowsAsync<ObjectDisposedException>(() => first);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => second);
        Assert.Equal(0, queue.PendingCount);
    }

    [Fact]
    public void A_wake_that_throws_does_not_lose_the_request()
    {
        var queue = new MainThreadQueue(() => true, "AutoCAD", Grace, () => throw new InvalidOperationException("no window"), () => _nowMs);

        var task = queue.RunAsync(new MainThreadWorkItem("read", _ => 1, CancellationToken.None));
        queue.OnTick();

        Assert.Equal(TaskStatus.RanToCompletion, task.Status); // not IsCompletedSuccessfully: this file is also linked into the net48 test project
    }

    [Theory]
    [InlineData(4, "Millimeters", 1.0)]
    [InlineData(1, "Inches", 25.4)]
    [InlineData(2, "Feet", 304.8)]
    [InlineData(5, "Centimeters", 10.0)]
    [InlineData(6, "Meters", 1000.0)]
    public void Insunits_table_matches_AutoCAD_codes(int code, string label, double mmPerUnit)
    {
        var units = AutocadInsunits.For(code);

        Assert.Equal(label, units.Label);
        Assert.Equal(mmPerUnit, units.MmPerUnit, 9);
        Assert.Null(units.Note);
        Assert.Equal(100 / mmPerUnit, units.ToDrawing(100), 9);
    }

    [Fact]
    public void Unitless_and_unknown_codes_fall_back_to_millimetres_with_a_note()
    {
        var unitless = AutocadInsunits.For(AutocadInsunits.Unitless);
        var unknown = AutocadInsunits.For(99);

        Assert.Equal(1, unitless.MmPerUnit);
        Assert.Equal("Unitless", unitless.Label);
        Assert.NotNull(unitless.Note);
        Assert.Equal("Unknown", unknown.Label);
        Assert.Contains("99", unknown.Note);
    }
}
