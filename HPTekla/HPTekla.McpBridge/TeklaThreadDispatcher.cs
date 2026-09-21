using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Threading;
using HPRebar.McpBridge.Core.Host;
using Serilog;
using Tekla.Structures.Model.Operations;

namespace HPTekla.McpBridge;

/// <summary>
///     Synchronizes execution between the Named Pipe listener thread and Tekla's main UI/Model thread.
///     Uses McpShared's MainThreadQueue pumped on ComponentDispatcher.ThreadIdle / WPF Dispatcher,
///     and wakes up the Windows message loop via PostMessage(hwnd, WM_NULL).
/// </summary>
public sealed class TeklaThreadDispatcher : IDisposable
{
    private const uint WmNull = 0x0000;
    private const string HostName = "Tekla Structures";

    private readonly MainThreadQueue _queue;
    private readonly EventHandler _onThreadIdle;
    private readonly Dispatcher _dispatcher;
    private IntPtr _mainWindowHandle;
    private bool _disposed;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    public TeklaThreadDispatcher(TimeSpan? busyGrace = null)
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
        _mainWindowHandle = ResolveMainWindowHandle();

        var grace = busyGrace ?? TimeSpan.FromSeconds(8);
        _queue = new MainThreadQueue(
            isQuiescent: IsQuiescent,
            hostName: HostName,
            busyGrace: grace,
            wakeMainThread: WakeMainThread,
            expireWithoutTicks: true);

        _onThreadIdle = (_, _) =>
        {
            try
            {
                _queue.OnTick();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during Tekla thread dispatcher idle tick");
            }
        };

        ComponentDispatcher.ThreadIdle += _onThreadIdle;
    }

    public int PendingCount => _queue.PendingCount;

    /// <summary>
    ///     True when Tekla is not actively running a macro and can safely process API calls.
    /// </summary>
    public static bool IsQuiescent()
    {
        try
        {
            return !Operation.IsMacroRunning();
        }
        catch
        {
            return true;
        }
    }

    /// <summary>
    ///     Enqueues work to execute on Tekla's main thread and awaits completion.
    /// </summary>
    public async Task<T> InvokeAsync<T>(string name, Func<CancellationToken, T> work, CancellationToken cancellationToken)
    {
        var item = new MainThreadWorkItem(name, ct => work(ct)!, cancellationToken);
        var result = await _queue.RunAsync(item).ConfigureAwait(false);
        return (T)result;
    }

    private void WakeMainThread()
    {
        // 1. Post WM_NULL to ensure Windows message queue awakes even if idle
        var hwnd = _mainWindowHandle;
        if (hwnd == IntPtr.Zero)
        {
            hwnd = ResolveMainWindowHandle();
            _mainWindowHandle = hwnd;
        }

        if (hwnd != IntPtr.Zero)
        {
            PostMessage(hwnd, WmNull, IntPtr.Zero, IntPtr.Zero);
        }

        // 2. Also schedule a low-priority dispatcher pass to pump queue
        try
        {
            if (!_dispatcher.HasShutdownStarted && !_dispatcher.HasShutdownFinished)
            {
                _dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
                {
                    try
                    {
                        _queue.OnTick();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Dispatcher pass tick failed");
                    }
                }));
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to wake main thread via Dispatcher");
        }
    }

    private static IntPtr ResolveMainWindowHandle()
    {
        try
        {
            var proc = Process.GetCurrentProcess();
            return proc.MainWindowHandle;
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ComponentDispatcher.ThreadIdle -= _onThreadIdle;
    }
}
