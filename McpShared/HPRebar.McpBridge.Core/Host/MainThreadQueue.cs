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

    /// <summary>Monotonic milliseconds at enqueue (set by the queue); the busy grace counts from here.</summary>
    public long EnqueuedAtMs { get; internal set; }

    public TaskCompletionSource<object> Completion { get; } =
        new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
}

/// <summary>
///     Marshals work onto a host whose API is only legal on its main thread and that offers no external
///     event of its own (AutoCAD): the host calls <see cref="OnTick"/> from its idle event, the queue runs
///     what is waiting once the host is quiescent. Every wait is bounded by <see cref="BusyGrace"/>: a
///     request older than that is refused with the actionable "busy" code whether the host is quiescent
///     by then or not, because the server has given up on it and late work must not run. A request whose
///     token was cancelled while it waited, or that was completed elsewhere, is skipped for the same
///     reason. Host-neutral so the ordering rules are covered by tests without the host.
/// </summary>
public sealed class MainThreadQueue
{
    private readonly ConcurrentQueue<MainThreadWorkItem> _pending = new ConcurrentQueue<MainThreadWorkItem>();
    private readonly Func<bool> _isQuiescent;
    private readonly string _hostName;
    private readonly Action? _wakeMainThread;
    private readonly Func<long> _clockMs;
    private int _ticking;

    /// <param name="isQuiescent">True when no command, script or dialog is active in the host. Read on the main thread.</param>
    /// <param name="wakeMainThread">
    ///     Optional nudge after an enqueue — a host that only raises its idle event after processing a
    ///     message would otherwise sit on the work until the user moves the mouse.
    /// </param>
    /// <param name="clockMs">Monotonic milliseconds; defaults to <see cref="Environment.TickCount64"/> so a wall-clock jump cannot age a request.</param>
    public MainThreadQueue(Func<bool> isQuiescent, string hostName, TimeSpan busyGrace, Action? wakeMainThread = null, Func<long>? clockMs = null)
    {
        _isQuiescent = isQuiescent;
        _hostName = hostName;
        BusyGrace = busyGrace;
        _wakeMainThread = wakeMainThread;
        _clockMs = clockMs ?? (() => Environment.TickCount64);
    }

    /// <summary>How long a request may wait for a quiescent tick before it fails as busy.</summary>
    public TimeSpan BusyGrace { get; }

    public int PendingCount => _pending.Count;

    /// <summary>Called from the pipe thread. The task completes on a later tick, or with the busy error after the grace period.</summary>
    public Task<object> RunAsync(MainThreadWorkItem item)
    {
        item.EnqueuedAtMs = _clockMs();
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
                if (item.Completion.Task.IsCompleted) continue; // completed elsewhere while it waited

                if (item.CancellationToken.IsCancellationRequested)
                {
                    item.Completion.TrySetCanceled(item.CancellationToken); // the caller gave up: never start it
                    continue;
                }

                if (IsExpired(item))
                {
                    Refuse(item);
                    continue;
                }

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

    /// <summary>Completes every waiting request with the given error; called when the host shuts down or the listener stops.</summary>
    public void FailAll(Exception reason)
    {
        while (_pending.TryDequeue(out var item)) item.Completion.TrySetException(reason);
    }

    private bool IsExpired(MainThreadWorkItem item) => _clockMs() - item.EnqueuedAtMs >= (long)BusyGrace.TotalMilliseconds;

    /// <summary>FIFO: once the head is young enough, everything behind it is too.</summary>
    private void FailExpired()
    {
        while (_pending.TryPeek(out var head) && IsExpired(head) && _pending.TryDequeue(out var item)) Refuse(item);
    }

    private void Refuse(MainThreadWorkItem item)
    {
        if (item.Completion.TrySetException(BridgeRequestException.Busy(_hostName)))
            Log.Information("MCP bridge work '{Name}' refused: {Host} not quiescent within {Grace}s", item.Name, _hostName, BusyGrace.TotalSeconds);
    }
}
