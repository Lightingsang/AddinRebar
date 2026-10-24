using HPBimToolkit.Core.Contracts;
using HPWebHost.Protocol;

namespace HPBimToolkit.Core.Application;

/// <summary>
///     One long run at a time for a command (a model-wide count walks every element on Revit's UI thread). A request for
///     the same key joins the run in progress; a request for another key answers BUSY until it ends. A run whose own
///     request was cancelled (it timed out, Revit will skip it) is abandoned, so a retry starts fresh instead of inheriting
///     the cancellation. A request that joins shares the first request's deadline (it may time out sooner than its own
///     budget). Not thread-safe: the dispatcher calls it from the WebView's UI thread only.
/// </summary>
public sealed class SingleFlightRun<TKey>(string busyMessage)
    where TKey : notnull
{
    private (Task<object?> Task, TKey Key, CancellationToken Token)? _current;

    public Task<object?> Run(TKey key, CancellationToken token, Func<Task<object?>> start)
    {
        if (_current is { Task.IsCompleted: false, Token.IsCancellationRequested: false } running)
        {
            return EqualityComparer<TKey>.Default.Equals(running.Key, key)
                ? running.Task
                : throw new BridgeException(BridgeErrorCodes.Busy, busyMessage);
        }

        var task = start();
        _current = (task, key, token);
        return task;
    }
}
