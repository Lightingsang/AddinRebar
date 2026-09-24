using System.Runtime.InteropServices;
using HPRebar.McpBridge.Core.Pipe;
using Serilog;

namespace HPEtabs.McpBridge;

/// <summary>
///     The STA worker half of the executor: the control lane (Attach/Detach ahead of any queued script), the
///     loop that ticks the queue, the quiescence rule and the COM-disconnect translation. Every OAPI call the
///     bridge makes goes through this thread — the one that holds the COM proxies.
/// </summary>
public sealed partial class EtabsExecutor
{
    /// <summary>Runs a piece of work on the STA worker ahead of any queued script: Attach, Detach and the window's probes.</summary>
    public Task<object?> RunOnWorkerAsync(string name, Func<object?> work)
    {
        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _control.Enqueue((name, work, completion));
        _wake.Set();
        return completion.Task;
    }

    public Task AttachAsync() => RunOnWorkerAsync("attach", () => _attachment.Attach());

    public Task DetachAsync() => RunOnWorkerAsync("detach", () => { _attachment.Detach("user clicked Detach"); return null; });

    public async Task<HPEtabs.McpBridge.Service.EtabsConnectionResult> EnsureConnectedAsync(HPEtabs.McpBridge.Service.EtabsConnectionConfig? config = null)
    {
        var res = await RunOnWorkerAsync("ensure_connected", () => _attachment.EnsureConnected(config)).ConfigureAwait(false);
        return (HPEtabs.McpBridge.Service.EtabsConnectionResult)res!;
    }

    /// <summary>Runs OAPI work on the STA worker; a COM disconnect drops the attachment and becomes the "not attached" refusal.</summary>
    private object OnWorker(Func<object> work)
    {
        try
        {
            return work();
        }
        catch (Exception exception)
        {
            var refusal = _attachment.DetachIfGone(exception);
            if (refusal is not null) throw refusal;
            throw;
        }
    }

    /// <summary>
    ///     Not attached counts as quiescent so the queued work reaches the tick and fails fast; a disabled main window
    ///     means a modal dialog; a handle that no longer names a window (ETABS re-created it) must not read as busy forever.
    /// </summary>
    private bool IsQuiescent()
    {
        if (_running) return false;
        if (!_attachment.Attached) return true;
        var handle = _attachment.MainWindowHandle;
        return handle == IntPtr.Zero || !IsWindow(handle) || IsWindowEnabled(handle);
    }

    private void WorkerLoop()
    {
        Log.Debug("HPEtabs COM worker started (STA={Sta})", Thread.CurrentThread.GetApartmentState() == ApartmentState.STA);

        while (!_stop)
        {
            _wake.WaitOne(TimeSpan.FromMilliseconds(250));
            DrainControlLane();
            try { _queue.OnTick(); }
            catch (Exception exception) { Log.Error(exception, "MCP bridge worker tick failed"); }
        }

        DrainControlLane();
        Log.Debug("HPEtabs COM worker stopped");
    }

    private void DrainControlLane()
    {
        while (_control.TryDequeue(out var item))
        {
            try { item.completion.TrySetResult(item.work()); }
            catch (Exception exception)
            {
                Log.Warning(exception, "MCP bridge control lane '{Name}' failed", item.name);
                item.completion.TrySetException(exception);
            }
            StateChanged?.Invoke();
        }
    }

    [DllImport("user32.dll", SetLastError = false)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowEnabled(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = false)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(IntPtr hWnd);
}
