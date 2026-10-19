namespace HPGrasshopper.McpBridge.Coordinator;

using System.Threading;
using System.Threading.Tasks;

internal enum WorkItemState { Queued, Running, Completed }

internal sealed class WorkItemTracker
{
    private int _state = (int)WorkItemState.Queued;
    public WorkItemState State => (WorkItemState)Volatile.Read(ref _state);

    public bool TryTransitionToRunning() =>
        Interlocked.CompareExchange(ref _state, (int)WorkItemState.Running, (int)WorkItemState.Queued) == (int)WorkItemState.Queued;

    public bool TryTransitionToCompleted() =>
        Interlocked.Exchange(ref _state, (int)WorkItemState.Completed) != (int)WorkItemState.Completed;

    public bool TryCancelIfQueued(TaskCompletionSource<object?> completion, CancellationToken ct = default)
    {
        if (Interlocked.CompareExchange(ref _state, (int)WorkItemState.Completed, (int)WorkItemState.Queued) != (int)WorkItemState.Queued)
            return false;
        completion.TrySetCanceled(ct);
        return true;
    }

    public bool TryCancelIfQueued(McpBridgeWorkRequest req) =>
        TryCancelIfQueued(req.Completion, req.CancellationToken);
}
