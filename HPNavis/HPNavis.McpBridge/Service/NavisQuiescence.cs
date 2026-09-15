using System.Diagnostics;
using System.Runtime.InteropServices;
using Autodesk.Navisworks.Api;
using Serilog;

namespace HPNavis.McpBridge.Service;

/// <summary>
///     Navisworks has no "is the host idle" flag, so the bridge composes one: no progress operation
///     running (file load, clash run, publish — counted, because sub-operations nest), no modal dialog
///     owning the main window, no transaction open on the document. A boolean would stick forever if a
///     <c>ProgressEnded</c> never came, so the depth resets when the bridge's own work starts or ends and
///     decays after a quiet period while the main window is enabled again.
/// </summary>
public sealed class NavisQuiescence : IDisposable
{

    private readonly TimeSpan _staleAfter;
    private readonly Stopwatch _sinceLastProgressEvent = Stopwatch.StartNew();
    private readonly EventHandler<ProgressBeginningEventArgs> _onBeginning;
    private readonly EventHandler<ProgressEndedEventArgs> _onEnded;
    private readonly EventHandler<ProgressSubOperationBeganEventArgs> _onSubBegan;
    private readonly EventHandler<ProgressSubOperationEndedEventArgs> _onSubEnded;
    private int _progressDepth;

    /// <summary>Main window handle, read on the main thread once the GUI exists; IntPtr.Zero until then.</summary>
    public IntPtr MainWindow { get; set; }

    public NavisQuiescence(TimeSpan staleAfter)
    {
        _staleAfter = staleAfter;
        _onBeginning = (_, e) => Bump(+1, "ProgressBeginning: " + e.Title);
        _onEnded = (_, _) => Bump(-1, "ProgressEnded");
        _onSubBegan = (_, _) => Bump(+1, "ProgressSubOperationBegan");
        _onSubEnded = (_, _) => Bump(-1, "ProgressSubOperationEnded");

        Application.ProgressBeginning += _onBeginning;
        Application.ProgressEnded += _onEnded;
        Application.ProgressSubOperationBegan += _onSubBegan;
        Application.ProgressSubOperationEnded += _onSubEnded;
    }

    public int ProgressDepth => Volatile.Read(ref _progressDepth);

    public bool ProgressActive => ProgressDepth > 0;

    /// <summary>
    ///     A modal dialog (WPF, WinForms or native) disables its owner, the main window. Only that test: the
    ///     GW_ENABLEDPOPUP walk also reports the bridge's own modeless status window (owned by the main window),
    ///     which made every request "busy" while the window was open.
    /// </summary>
    public bool ModalOpen
    {
        get
        {
            var main = MainWindow;
            return main != IntPtr.Zero && !IsWindowEnabled(main);
        }
    }

    /// <summary>Read on the main thread from the idle tick. Also where a stale depth is forgiven.</summary>
    public bool IsQuiescent(Document? document)
    {
        if (ProgressActive && _sinceLastProgressEvent.Elapsed > _staleAfter && MainWindow != IntPtr.Zero && IsWindowEnabled(MainWindow))
        {
            Log.Warning("MCP bridge: progress depth {Depth} reset after {Seconds:F0} s without a progress event", ProgressDepth, _sinceLastProgressEvent.Elapsed.TotalSeconds);
            Reset("stale");
        }

        return !ProgressActive && !ModalOpen && !(document?.IsActiveTransaction ?? false);
    }

    /// <summary>The bridge's own script is about to run or has just finished: whatever progress it caused is over.</summary>
    public void Reset(string why)
    {
        if (Interlocked.Exchange(ref _progressDepth, 0) != 0) Log.Debug("MCP bridge: progress depth reset ({Why})", why);
    }

    /// <summary>Short text for the status window and context: what the bridge thinks is going on.</summary>
    public string Describe(Document? document) =>
        ProgressActive ? "a file is loading or a clash test is running"
        : ModalOpen ? "a dialog is open"
        : document?.IsActiveTransaction == true ? "a transaction is in progress"
        : "idle";

    private void Bump(int delta, string what)
    {
        int depth;
        do
        {
            depth = Volatile.Read(ref _progressDepth);
        }
        while (Interlocked.CompareExchange(ref _progressDepth, Math.Max(0, depth + delta), depth) != depth);

        _sinceLastProgressEvent.Restart();
        Log.Debug("MCP bridge: {What} → progress depth {Depth}", what, Math.Max(0, depth + delta));
    }

    public void Dispose()
    {
        Application.ProgressBeginning -= _onBeginning;
        Application.ProgressEnded -= _onEnded;
        Application.ProgressSubOperationBegan -= _onSubBegan;
        Application.ProgressSubOperationEnded -= _onSubEnded;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowEnabled(IntPtr hWnd);

}
