using System.Runtime.InteropServices;
using HPRebar.McpBridge.Core.Pipe;
using Serilog;

namespace HPSap2000.McpBridge;

/// <summary>
///     The STA worker half of the executor.
/// </summary>
public sealed partial class SapExecutor
{
    public Task<object?> RunOnWorkerAsync(string name, Func<object?> work)
    {
        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _control.Enqueue((name, work, completion));
        _wake.Set();
        return completion.Task;
    }

    public Task AttachAsync() => RunOnWorkerAsync("attach", () => _attachment.Attach());

    public Task DetachAsync() => RunOnWorkerAsync("detach", () => { _attachment.Detach("user clicked Detach"); return null; });

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

    private bool IsQuiescent()
    {
        if (_running) return false;
        if (!_attachment.Attached) return true;
        var handle = _attachment.MainWindowHandle;
        return handle == IntPtr.Zero || !IsWindow(handle) || IsWindowEnabled(handle);
    }

    private void WorkerLoop()
    {
        Service.SapAttachment.EnsureDefaultDesktop();
        Log.Debug("HPSap2000 COM worker started (STA={Sta})", Thread.CurrentThread.GetApartmentState() == ApartmentState.STA);

        while (!_stop)
        {
            _wake.WaitOne(TimeSpan.FromMilliseconds(250));
            DrainControlLane();
            try { _queue.OnTick(); }
            catch (Exception exception) { Log.Error(exception, "MCP bridge worker tick failed"); }
        }

        DrainControlLane();
        Log.Debug("HPSap2000 COM worker stopped");
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
