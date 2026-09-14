using System.Collections.Concurrent;
using HPRebar.McpBridge.Core.Pipe;
using Serilog;

namespace HPRebar.McpBridge.Core.Host;

/// <summary>
///     One unit of work waiting for the host's main thread. The pipe thread parks it in a
///     <see cref="MainThreadQueue"/> and awaits <see cref="Completion"/>; the host's idle tick runs
///     <see cref="Work"/> and completes it. Continuations run asynchronously so nothing resumes on the
///     host's thread by accident.
/// </summary>
public sealed class MainThreadWorkItem
{
    public MainThreadWorkItem(string name, Func<CancellationToken, object> work, CancellationToken cancellationToken)
    {
        Name = name;
        Work = work;
        CancellationToken = cancellationToken;
    }

    /// <summary>Short label for logs, e.g. "execute: count lines".</summary>
    public string Name { get; }

    /// <summary>Runs on the host's main thread; the token is the script's cooperative cancellation.</summary>
    public Func<CancellationToken, object> Work { get; }

    public CancellationToken CancellationToken { get; }

    /// <summary>Set by the queue; the busy grace period counts from here.</summary>
    public DateTimeOffset EnqueuedAt { get; internal set; }

    public TaskCompletionSource<object> Completion { get; } =
        new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
}

/// <summary>
///     Marshals work onto a host whose API is only legal on its main thread and that offers no external
///     event of its own (AutoCAD): the host calls <see cref="OnTick"/> from its idle event, the queue runs
///     what is waiting once the host is quiescent. A tick that finds the host busy (a command or dialog in
///     progress) leaves the work queued for up to <see cref="BusyGrace"/> and then fails it with the
///     actionable "busy" code, so the server sees a clear answer instead of a timeout. A request that was
///     completed elsewhere (cancelled, timed out) is skipped: a late tick never runs stale work.
///     Host-neutral so the ordering rules are covered by tests without the host.
/// </summary>
public sealed class MainThreadQueue
{
    private readonly ConcurrentQueue<MainThreadWorkItem> _pending = new ConcurrentQueue<MainThreadWorkItem>();
    private readonly Func<bool> _isQuiescent;
    private readonly string _hostName;
    private readonly Action? _wakeMainThread;
    private readonly Func<DateTimeOffset> _clock;
    private int _ticking;

    /// <param name="isQuiescent">True when no command, script or dialog is active in the host. Read on the main thread.</param>
    /// <param name="wakeMainThread">
    ///     Optional nudge after an enqueue — a host that only raises its idle event after processing a
    ///     message would otherwise sit on the work until the user moves the mouse.
    /// </param>
    public MainThreadQueue(Func<bool> isQuiescent, string hostName, TimeSpan busyGrace, Action? wakeMainThread = null, Func<DateTimeOffset>? clock = null)
    {
        _isQuiescent = isQuiescent;
        _hostName = hostName;
        BusyGrace = busyGrace;
        _wakeMainThread = wakeMainThread;
        _clock = clock ?? (() => DateTimeOffset.Now);
    }

    /// <summary>How long a request waits for the host to become quiescent before it fails as busy.</summary>
    public TimeSpan BusyGrace { get; }

    public int PendingCount => _pending.Count;

    /// <summary>Called from the pipe thread. The task completes on a later tick, or with the busy error after the grace period.</summary>
    public Task<object> RunAsync(MainThreadWorkItem item)
    {
        item.EnqueuedAt = _clock();
        _pending.Enqueue(item);

        try
        {
            _wakeMainThread?.Invoke();
        }
        catch (Exception exception)
        {
            Log.Debug(exception, "MCP bridge could not wake the main thread; waiting for the next idle tick");
        }

        return item.Completion.Task;
    }

    /// <summary>
    ///     The host's idle handler. Cheap when nothing waits; re-entrancy (a host that raises idle while the
    ///     script pumps messages) is ignored so one script never starts inside another.
    /// </summary>
    public void OnTick()
    {
        if (_pending.IsEmpty) return;
        if (Interlocked.CompareExchange(ref _ticking, 1, 0) != 0) return;

        try
        {
            if (!_isQuiescent())
            {
                FailExpired();
                return;
            }

            while (_pending.TryDequeue(out var item))
            {
                if (item.Completion.Task.IsCompleted) continue; // cancelled or timed out while it waited

                try
                {
                    item.Completion.TrySetResult(item.Work(item.CancellationToken));
                }
                catch (Exception exception)
                {
                    if (exception is not BridgeRequestException) Log.Error(exception, "MCP bridge work '{Name}' threw on the {Host} main thread", item.Name, _hostName);
                    item.Completion.TrySetException(exception);
                }
            }
        }
        finally
        {
            Volatile.Write(ref _ticking, 0);
        }
    }

    /// <summary>Completes every waiting request as busy; called when the host shuts down or the listener stops.</summary>
    public void FailAll(Exception reason)
    {
        while (_pending.TryDequeue(out var item)) item.Completion.TrySetException(reason);
    }

    private void FailExpired()
    {
        var now = _clock();

        while (_pending.TryPeek(out var head) && now - head.EnqueuedAt >= BusyGrace)
        {
            _pending.TryDequeue(out _);
            if (head.Completion.TrySetException(BridgeRequestException.Busy(_hostName)))
                Log.Information("MCP bridge work '{Name}' refused: {Host} not quiescent for {Grace}s", head.Name, _hostName, BusyGrace.TotalSeconds);
        }
    }
}
