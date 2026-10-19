namespace HPGrasshopper.McpBridge.Coordinator;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Pipe;

/// <summary>
///     Encapsulates thread-safe queuing and dispatching of work requests onto the Revit main thread.
/// </summary>
internal sealed class CoordinatorWorkQueue : IDisposable
{
    private readonly ConcurrentQueue<McpBridgeWorkRequest> _pending = new();
    private readonly ConcurrentDictionary<McpBridgeWorkRequest, WorkItemTracker> _workTrackers = new(ReferenceEqualityComparer.Instance);

    public async Task<object?> RunOnRevitThreadAsync(
        McpBridgeWorkRequest work,
        IExternalEventRaiser? externalEvent,
        Func<ExecuteRequest, Task<ExecuteResult>>? executionHook)
    {
        if (executionHook != null)
            return await executionHook(new ExecuteRequest(work.Name, "none")).ConfigureAwait(false);

        if (externalEvent == null)
            throw new InvalidOperationException("Revit external event raiser is not available.");

        var tracker = new WorkItemTracker();
        _workTrackers[work] = tracker;
        _pending.Enqueue(work);

        using var cancelReg = work.CancellationToken.Register(() => tracker.TryCancelIfQueued(work));
        if (work.CancellationToken.IsCancellationRequested)
        {
            tracker.TryCancelIfQueued(work);
            _workTrackers.TryRemove(work, out _);
            throw new OperationCanceledException(work.CancellationToken);
        }

        try
        {
            if (!externalEvent.Raise())
            {
                tracker.TryTransitionToCompleted();
                throw new InvalidOperationException("ExternalEvent.Raise() was denied or failed to enqueue to Revit event pump.");
            }
            return await work.Completion.Task.ConfigureAwait(false);
        }
        finally
        {
            _workTrackers.TryRemove(work, out _);
        }
    }

    public void Execute(UIApplication application)
    {
        while (_pending.TryDequeue(out var work))
        {
            if (work.CancellationToken.IsCancellationRequested)
            {
                work.Completion.TrySetCanceled(work.CancellationToken);
                continue;
            }

            if (!_workTrackers.TryGetValue(work, out var tracker) || !tracker.TryTransitionToRunning())
                continue;

            try
            {
                work.ValidateActiveDocument(application);
                var result = work.Work(application, work.CancellationToken);
                work.Completion.TrySetResult(result);
            }
            catch (OperationCanceledException oce) { work.Completion.TrySetCanceled(oce.CancellationToken); }
            catch (Exception ex) { work.Completion.TrySetException(ex); }
            finally { tracker?.TryTransitionToCompleted(); }
        }
    }

    public WorkItemState GetWorkItemState(McpBridgeWorkRequest request) =>
        _workTrackers.TryGetValue(request, out var tracker) ? tracker.State : WorkItemState.Completed;

    public void Dispose()
    {
        while (_pending.TryDequeue(out var item))
            if (_workTrackers.TryRemove(item, out var tracker)) tracker.TryCancelIfQueued(item);
            else item.Completion.TrySetCanceled();
        _workTrackers.Clear();
    }
}
