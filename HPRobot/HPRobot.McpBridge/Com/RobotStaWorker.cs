using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace HPRobot.McpBridge.Com;

/// <summary>
///     Dedicated Single-Threaded Apartment (STA) worker thread for executing Robot COM automation calls.
///     Prevents RPC_E_WRONG_THREAD (0x8001010E) and cross-apartment COM marshaling deadlocks.
///     Prioritizes a fast control lane (for Attach, Detach, and state probes) over script execution.
/// </summary>
public sealed class RobotStaWorker : IDisposable
{
    private readonly Thread _thread;
    private readonly AutoResetEvent _wake = new(false);
    private readonly ConcurrentQueue<WorkerTask> _controlLane = new();
    private readonly ConcurrentQueue<WorkerTask> _scriptLane = new();
    private volatile bool _stop;
    private bool _disposed;

    public RobotStaWorker()
    {
        _thread = new Thread(WorkerLoop)
        {
            Name = "HPRobot-STA-Worker",
            IsBackground = true
        };
        _thread.SetApartmentState(ApartmentState.STA);
    }

    public void Start()
    {
        if (!_thread.IsAlive)
        {
            _thread.Start();
            Log.Debug("Robot STA worker thread started");
        }
    }

    /// <summary>
    ///     Executes a fast control action (e.g. Attach, Detach, RefreshContext) ahead of queued scripts.
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
    ///     Executes script execution on the STA thread.
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
        Log.Debug("Robot STA worker loop entered; ApartmentState: {Apartment}", Thread.CurrentThread.GetApartmentState());

        // Install message filter for automatic retry on SERVERCALL_RETRYLATER
        using var filterScope = ComInteropHelper.RegisterMessageFilter();

        while (!_stop)
        {
            _wake.WaitOne(200);

            // 1. Drain control lane first (high priority)
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
        Log.Debug("Robot STA worker loop exited");
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
