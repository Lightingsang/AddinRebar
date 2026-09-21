using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace HPExcel.McpBridge.Com;

/// <summary>
///     Dedicated STA worker thread for executing Excel COM automation calls.
///     Avoids RPC_E_WRONG_THREAD (0x8001010E) and apartment marshaling deadlocks.
///     Maintains a control lane (for Attach/Detach/Status) prioritized ahead of script execution.
/// </summary>
public sealed class ExcelStaWorker : IDisposable
{
    private readonly Thread _thread;
    private readonly AutoResetEvent _wake = new(false);
    private readonly ConcurrentQueue<WorkerTask> _controlLane = new();
    private readonly ConcurrentQueue<WorkerTask> _scriptLane = new();
    private volatile bool _stop;
    private bool _disposed;

    public ExcelStaWorker()
    {
        _thread = new Thread(WorkerLoop)
        {
            Name = "HPExcel-STA-Worker",
            IsBackground = true
        };
        _thread.SetApartmentState(ApartmentState.STA);
    }

    public void Start()
    {
        if (!_thread.IsAlive)
        {
            _thread.Start();
            Log.Debug("Excel STA worker thread started");
        }
    }

    /// <summary>
    ///     Executes a fast control action (e.g. Attach, Detach, Probes) ahead of any queued scripts.
    /// </summary>
    public Task<T> RunOnControlLaneAsync<T>(string name, Func<T> work)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _controlLane.Enqueue(new WorkerTask(name, () =>
        {
            try
            {
                var res = work();
                tcs.TrySetResult(res);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }));
        _wake.Set();
        return tcs.Task;
    }

    public Task RunOnControlLaneAsync(string name, Action work)
    {
        return RunOnControlLaneAsync(name, () =>
        {
            work();
            return true;
        });
    }

    /// <summary>
    ///     Executes script or data operations on the STA thread.
    /// </summary>
    public Task<T> RunAsync<T>(string name, Func<T> work)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _scriptLane.Enqueue(new WorkerTask(name, () =>
        {
            try
            {
                var res = work();
                tcs.TrySetResult(res);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }));
        _wake.Set();
        return tcs.Task;
    }

    public Task RunAsync(string name, Action work)
    {
        return RunAsync(name, () =>
        {
            work();
            return true;
        });
    }

    private void WorkerLoop()
    {
        Log.Debug("Excel STA worker loop entered; ApartmentState: {Apartment}", Thread.CurrentThread.GetApartmentState());

        // Install message filter for automatic retry on SERVERCALL_RETRYLATER
        using var filterScope = ComInteropHelper.RegisterMessageFilter();

        while (!_stop)
        {
            _wake.WaitOne(200);

            // 1. Drain control lane first (priority)
            DrainQueue(_controlLane);

            // 2. Process one script task
            if (_scriptLane.TryDequeue(out var scriptTask))
            {
                ExecuteTask(scriptTask);
            }
        }

        // Final drain on shutdown
        DrainQueue(_controlLane);
        DrainQueue(_scriptLane);
        Log.Debug("Excel STA worker loop exited");
    }

    private void DrainQueue(ConcurrentQueue<WorkerTask> queue)
    {
        while (queue.TryDequeue(out var task))
        {
            ExecuteTask(task);
        }
    }

    private static void ExecuteTask(WorkerTask task)
    {
        try
        {
            task.Work();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error executing task '{Name}' on STA worker", task.Name);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _stop = true;
        _wake.Set();
        if (_thread.IsAlive)
        {
            _thread.Join(1000);
        }
        _wake.Dispose();
    }

    private sealed record WorkerTask(string Name, Action Work);
}
