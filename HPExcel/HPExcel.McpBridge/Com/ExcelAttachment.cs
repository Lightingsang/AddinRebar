using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using HPExcel.McpBridge.Discovery;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Pipe;
using Serilog;

namespace HPExcel.McpBridge.Com;

/// <summary>
///     Manages out-of-process COM attachment to running Microsoft Excel instances.
///     Tracks active workbook, worksheet, selection, and connection state.
/// </summary>
public sealed class ExcelAttachment : IDisposable
{
    private readonly object _gate = new();
    private dynamic? _excelApp;
    private Process? _process;
    private bool _isAttached;
    private int? _attachedPid;
    private string? _excelVersion;
    private string? _activeWorkbookName;
    private string? _activeWorkbookPath;
    private string? _activeWorksheetName;
    private string? _selectionAddress;
    private int _openWorkbookCount;
    private int _worksheetCount;
    private List<string> _openWorkbookNames = new();
    private IntPtr _mainWindowHandle = IntPtr.Zero;

    public event Action? StateChanged;

    public bool IsAttached
    {
        get { lock (_gate) return _isAttached; }
    }

    public int? AttachedPid
    {
        get { lock (_gate) return _attachedPid; }
    }

    public string? ExcelVersion
    {
        get { lock (_gate) return _excelVersion; }
    }

    public string? ActiveWorkbookName
    {
        get { lock (_gate) return _activeWorkbookName; }
    }

    public string? ActiveWorkbookPath
    {
        get { lock (_gate) return _activeWorkbookPath; }
    }

    public string? ActiveWorksheetName
    {
        get { lock (_gate) return _activeWorksheetName; }
    }

    public string? SelectionAddress
    {
        get { lock (_gate) return _selectionAddress; }
    }

    public int OpenWorkbookCount
    {
        get { lock (_gate) return _openWorkbookCount; }
    }

    public int WorksheetCount
    {
        get { lock (_gate) return _worksheetCount; }
    }

    public IReadOnlyList<string> OpenWorkbookNames
    {
        get { lock (_gate) return _openWorkbookNames.ToArray(); }
    }

    public IntPtr MainWindowHandle
    {
        get { lock (_gate) return _mainWindowHandle; }
    }

    public bool HasActiveWorkbook
    {
        get { lock (_gate) return !string.IsNullOrEmpty(_activeWorkbookName); }
    }

    /// <summary>
    ///     Attaches to the active Excel process via ROT / GetActiveObject.
    ///     If targetPid is specified, ensures the attached process matches that PID.
    /// </summary>
    public void Attach(int? targetPid = null)
    {
        lock (_gate)
        {
            DetachInternal("re-attaching");

            var punk = ComInteropHelper.GetActiveExcelObject();
            if (punk == null)
            {
                // Check if Excel processes are running at all
                var instances = ExcelProcessDetector.DetectRunningInstances();
                if (instances.Count == 0)
                {
                    throw new InvalidOperationException(
                        "Microsoft Excel is not running. Please launch Microsoft Excel and open a workbook first.");
                }

                throw new InvalidOperationException(
                    "Excel is running but not registered in the Running Object Table (ROT). " +
                    "Try clicking inside Excel or switching sheets to ensure it registers with OLE.");
            }

            try
            {
                _excelApp = punk;
                _excelVersion = (string)_excelApp.Version;

                // Find process for PID
                if (targetPid.HasValue)
                {
                    _attachedPid = targetPid.Value;
                    try { _process = Process.GetProcessById(targetPid.Value); } catch { }
                }
                else
                {
                    var instances = ExcelProcessDetector.DetectRunningInstances();
                    if (instances.Count > 0)
                    {
                        _attachedPid = instances[0].ProcessId;
                        try { _process = Process.GetProcessById(instances[0].ProcessId); } catch { }
                    }
                }

                if (_process != null)
                {
                    _mainWindowHandle = _process.MainWindowHandle;
                    try
                    {
                        _process.EnableRaisingEvents = true;
                        _process.Exited += OnProcessExited;
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex, "Could not hook Process.Exited for Excel PID {Pid}", _attachedPid);
                    }
                }

                _isAttached = true;
                RefreshContextInternal();

                Log.Information("Attached to Excel PID {Pid} (Version {Version}, Active: '{Wb}')",
                    _attachedPid, _excelVersion, _activeWorkbookName ?? "<None>");
            }
            catch (Exception ex)
            {
                DetachInternal($"Attach initialization failed: {ex.Message}");
                throw;
            }
        }

        StateChanged?.Invoke();
    }

    /// <summary>
    ///     Refreshes the current context: active workbook, worksheet, selection, and open workbook count.
    /// </summary>
    public void RefreshContext()
    {
        lock (_gate)
        {
            if (!_isAttached || _excelApp == null) return;
            RefreshContextInternal();
        }
        StateChanged?.Invoke();
    }

    private void RefreshContextInternal()
    {
        try
        {
            if (_excelApp == null) return;

            dynamic? wb = null;
            try { wb = _excelApp.ActiveWorkbook; } catch { }

            if (wb != null)
            {
                try { _activeWorkbookName = (string)wb.Name; } catch { _activeWorkbookName = null; }
                try { _activeWorkbookPath = (string)wb.FullName; } catch { _activeWorkbookPath = null; }

                dynamic? ws = null;
                try { ws = wb.ActiveSheet; } catch { }

                if (ws != null)
                {
                    try { _activeWorksheetName = (string)ws.Name; } catch { _activeWorksheetName = null; }
                }
                else
                {
                    _activeWorksheetName = null;
                }

                try { _worksheetCount = (int)wb.Worksheets.Count; } catch { _worksheetCount = 0; }
            }
            else
            {
                _activeWorkbookName = null;
                _activeWorkbookPath = null;
                _activeWorksheetName = null;
                _worksheetCount = 0;
            }

            dynamic? sel = null;
            try { sel = _excelApp.Selection; } catch { }
            if (sel != null)
            {
                try { _selectionAddress = (string)sel.Address; } catch { _selectionAddress = null; }
            }
            else
            {
                _selectionAddress = null;
            }

            _openWorkbookNames.Clear();
            try
            {
                foreach (dynamic w in _excelApp.Workbooks)
                {
                    try { _openWorkbookNames.Add((string)w.Name); } catch { }
                }
            }
            catch { }
            _openWorkbookCount = _openWorkbookNames.Count;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Error refreshing Excel attachment context");
        }
    }

    /// <summary>
    ///     Detaches from the Excel COM instance.
    /// </summary>
    public void Detach(string reason)
    {
        lock (_gate)
        {
            DetachInternal(reason);
        }
        StateChanged?.Invoke();
    }

    private void DetachInternal(string reason)
    {
        if (!_isAttached && _excelApp == null) return;

        if (_process != null)
        {
            try { _process.Exited -= OnProcessExited; } catch { }
            _process.Dispose();
            _process = null;
        }

        if (_excelApp != null)
        {
            ComInteropHelper.ReleaseComObject(_excelApp);
            _excelApp = null;
        }

        _isAttached = false;
        _attachedPid = null;
        _excelVersion = null;
        _activeWorkbookName = null;
        _activeWorkbookPath = null;
        _activeWorksheetName = null;
        _selectionAddress = null;
        _openWorkbookCount = 0;
        _worksheetCount = 0;
        _openWorkbookNames.Clear();
        _mainWindowHandle = IntPtr.Zero;

        Log.Information("Detached from Excel: {Reason}", reason);
    }

    /// <summary>
    ///     Requires active attachment or throws a BridgeRequestException.
    /// </summary>
    public dynamic Require()
    {
        lock (_gate)
        {
            if (_isAttached && _excelApp != null)
            {
                return _excelApp!;
            }
        }

        throw new BridgeRequestException(BridgeErrorCode.NoActiveDocument,
            "Microsoft Excel is not attached. Click 'Attach' in the HPExcel MCP Bridge window (start Excel first).");
    }

    /// <summary>
    ///     Returns active application if attached, or null.
    /// </summary>
    public dynamic? GetApplication()
    {
        lock (_gate) return _excelApp;
    }

    /// <summary>
    ///     Checks if an exception represents a disconnected COM server.
    /// </summary>
    public static bool IsDisconnectError(Exception exception) => exception is COMException com && com.HResult is
        unchecked((int)0x800706BA)   // RPC_S_SERVER_UNAVAILABLE
        or unchecked((int)0x800706BE) // RPC_S_CALL_FAILED
        or unchecked((int)0x800706BF) // RPC_S_CALL_FAILED_DNE
        or unchecked((int)0x80010108) // RPC_E_DISCONNECTED
        or unchecked((int)0x80010012) // RPC_E_SERVER_DIED_DNE
        or unchecked((int)0x80010007); // RPC_E_SERVER_DIED

    /// <summary>
    ///     Drops the attachment if the exception indicates the Excel process terminated.
    /// </summary>
    public BridgeRequestException? DetachIfGone(Exception exception)
    {
        if (!IsDisconnectError(exception)) return null;
        Detach($"Excel disconnected: {exception.GetType().Name} 0x{exception.HResult:X8}");
        return new BridgeRequestException(BridgeErrorCode.NoActiveDocument,
            "Excel process disconnected or terminated unexpectedly. Please re-attach in the HPExcel MCP Bridge window.");
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        Detach("Excel process exited");
    }

    public void Dispose()
    {
        Detach("Disposing ExcelAttachment");
    }
}
