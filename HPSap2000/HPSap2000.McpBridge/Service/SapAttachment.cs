using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using SAP2000v1;
using Serilog;

namespace HPSap2000.McpBridge.Service;

/// <summary>
///     The one COM attachment to the running SAP2000. Attach and Detach are called on the bridge's STA worker
///     (the proxy is apartment-bound and every OAPI call goes through that thread); the state is read from any
///     thread. Liveness is eager: the attached process's <c>Exited</c> event drops the attachment at once, so
///     a request after SAP2000 closed fails fast with "not attached" instead of waiting on a dead window handle.
///     Only the active instance is used (<c>Helper.GetObject</c>); with more than one SAP2000 running the bridge
///     warns the user.
/// </summary>
public sealed class SapAttachment : IDisposable
{
    public const string ProgId = "CSI.SAP2000.API.SapObject";
    public const string ProcessName = "SAP2000";

    private readonly object _gate = new();
    private cOAPI? _sap;
    private cSapModel? _sapModel;
    private Process? _process;
    private volatile bool _attached;
    private volatile string? _warning;

    /// <summary>True between a successful <see cref="Attach"/> and a <see cref="Detach"/> or the process exiting.</summary>
    public bool Attached => _attached;

    /// <summary>Process id of the attached SAP2000; 0 while detached.</summary>
    public int Pid { get; private set; }

    /// <summary>Main window of the attached process, for the quiescence pre-check (a modal dialog disables it).</summary>
    public IntPtr MainWindowHandle { get; private set; }

    /// <summary>Wrapper file version — what `GetOAPIVersionNumber` returns is a double and less telling.</summary>
    public string? OapiVersion { get; private set; }

    /// <summary>Set when more than one SAP2000 is running: which one <c>GetObject</c> returned is CSI's decision, not ours.</summary>
    public string? Warning => _warning;

    public event Action? StateChanged;

    /// <summary>STA worker only. Throws with a message the window can show when no SAP2000 instance is registered.</summary>
    public cSapModel Attach()
    {
        lock (_gate)
        {
            if (_attached && _sapModel is not null) return _sapModel;

            EnsureDefaultDesktop();

            var processes = Process.GetProcessesByName(ProcessName);
            if (processes.Length == 0) throw new InvalidOperationException("No SAP2000 process is running. Start SAP2000 and open a model, then click Attach.");

            cOAPI? sap = null;
            try
            {
                // Helper implements cHelper explicitly: members are reachable through the interface.
                cHelper helper = new Helper();
                sap = helper.GetObject(ProgId);
                if (sap is null && processes.Length > 0)
                {
                    Log.Information("GetObject returned null; trying GetObjectProcess for pid {Pid}", processes[0].Id);
                    sap = helper.GetObjectProcess(ProgId, processes[0].Id);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Helper.GetObject threw exception");
            }

            if (sap is null)
            {
                int hrClsid = CLSIDFromProgID(ProgId, out var clsid);
                object? punk = null;
                int hrActive = hrClsid == 0 ? GetActiveObject(ref clsid, IntPtr.Zero, out punk) : hrClsid;
                Log.Information("Direct GetActiveObject: CLSID={Clsid} (hr=0x{HrClsid:X8}), GAO hr=0x{HrActive:X8}, punk={Punk}",
                    clsid, hrClsid, hrActive, punk is not null);
                if (hrActive == 0 && punk is not null)
                {
                    sap = (cOAPI)punk;
                }
            }

            if (sap is null)
            {
                throw new InvalidOperationException(
                    "SAP2000 is running but not registered for the API in this session. If it was started as administrator, close it and start it normally (an elevated SAP2000 is invisible to this app); otherwise wait for it to finish loading and click Attach again.");
            }

            var sapModel = sap.SapModel ?? throw new InvalidOperationException("SAP2000 answered GetObject but returned no SapModel.");

            _sap = sap;
            _sapModel = sapModel;
            OapiVersion = SapAssemblyResolver.WrapperFileVersion;
            _warning = processes.Length > 1
                ? $"{processes.Length} SAP2000 instances are running; the bridge attached to the active instance. Pick it in SAP2000: Tools › Active Instance for API, then Detach and Attach again."
                : null;

            _attached = true;
            var target = processes.Length == 1 ? processes[0] : null;
            if (target is not null) Watch(target);
            else foreach (var p in processes) p.Dispose();
            if (!_attached) throw new InvalidOperationException("SAP2000 exited while the bridge was attaching. Start it again and click Attach.");

            var oapiNumber = Safe(() => sap.GetOAPIVersionNumber().ToString(CultureInfo.InvariantCulture));
            Log.Information("Attached to SAP2000 pid {Pid} (GetOAPIVersionNumber {Number}, wrapper {Version}){Warning}",
                Pid, oapiNumber ?? "?", OapiVersion, _warning is null ? string.Empty : " — " + _warning);
            StateChanged?.Invoke();
            return sapModel;
        }
    }

    /// <summary>Any thread. Drops the proxies; a request after this fails fast with "not attached".</summary>
    public void Detach(string reason)
    {
        lock (_gate)
        {
            if (!_attached && _sap is null) return;

            _attached = false;
            _sap = null;
            _sapModel = null;
            Pid = 0;
            MainWindowHandle = IntPtr.Zero;
            _warning = null;
            Unwatch();
            Log.Information("Detached from SAP2000: {Reason}", reason);
        }

        StateChanged?.Invoke();
    }

    /// <summary>STA worker only; throws the "not attached" refusal the dispatcher turns into -32003.</summary>
    public (cOAPI sap, cSapModel sapModel) Require()
    {
        lock (_gate)
        {
            if (_attached && _sap is not null && _sapModel is not null) return (_sap, _sapModel);
        }

        throw NotAttached();
    }

    public static HPRebar.McpBridge.Core.Pipe.BridgeRequestException NotAttached() =>
        new(HPRebar.Mcp.Contracts.JsonRpc.BridgeErrorCode.NoActiveDocument, "SAP2000 not attached — click Attach in the HPSap2000 MCP Bridge window (start SAP2000 first).");

    /// <summary>
    ///     The COM channel to SAP2000 is gone: process exited or proxy disconnected.
    /// </summary>
    public static bool IsDisconnectError(Exception exception) => exception is COMException com && com.HResult is
        unchecked((int)0x800706BA)   // RPC_S_SERVER_UNAVAILABLE
        or unchecked((int)0x800706BE) // RPC_S_CALL_FAILED
        or unchecked((int)0x800706BF) // RPC_S_CALL_FAILED_DNE
        or unchecked((int)0x80010108) // RPC_E_DISCONNECTED
        or unchecked((int)0x80010012) // RPC_E_SERVER_DIED_DNE
        or unchecked((int)0x80010007); // RPC_E_SERVER_DIED

    /// <summary>Drops the attachment when exception says SAP2000 is gone and returns refusal to throw; null otherwise.</summary>
    public HPRebar.McpBridge.Core.Pipe.BridgeRequestException? DetachIfGone(Exception exception)
    {
        if (!IsDisconnectError(exception)) return null;
        Detach($"SAP2000 gone: {exception.GetType().Name} 0x{exception.HResult:X8}");
        return NotAttached();
    }

    private void Watch(Process process)
    {
        _process = process;
        Pid = process.Id;
        MainWindowHandle = Safe(() => process.MainWindowHandle);
        try
        {
            process.EnableRaisingEvents = true;
            process.Exited += OnProcessExited;
            if (process.HasExited) OnProcessExited(process, EventArgs.Empty);
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Could not watch SAP2000 pid {Pid} for exit; liveness falls back to the next failed call", Pid);
        }
    }

    private void Unwatch()
    {
        if (_process is null) return;
        try { _process.Exited -= OnProcessExited; } catch { /* already gone */ }
        _process.Dispose();
        _process = null;
    }

    private void OnProcessExited(object? sender, EventArgs e) => Detach("SAP2000 exited");

    private static T? Safe<T>(Func<T?> read)
    {
        try { return read(); }
        catch { return default; }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr OpenDesktop(string lpszDesktop, uint dwFlags, bool fInherit, uint dwDesiredAccess);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetThreadDesktop(IntPtr hDesktop);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetThreadDesktop(uint dwThreadId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool GetUserObjectInformationW(IntPtr hObj, int nIndex, [Out] byte[] pvInfo, uint nLength, out uint lpnLengthNeeded);

    [DllImport("ole32.dll", CharSet = CharSet.Unicode)]
    private static extern int CLSIDFromProgID(string lpszProgID, out Guid lpclsid);

    [DllImport("oleaut32.dll", PreserveSig = true)]
    private static extern int GetActiveObject(ref Guid rclsid, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);

    private const int UOI_NAME = 2;

    public static string GetCurrentDesktopName()
    {
        try
        {
            var hDesk = GetThreadDesktop(GetCurrentThreadId());
            if (hDesk == IntPtr.Zero) return "<null>";
            var buf = new byte[256];
            if (GetUserObjectInformationW(hDesk, UOI_NAME, buf, (uint)buf.Length, out _))
            {
                return System.Text.Encoding.Unicode.GetString(buf).TrimEnd('\0');
            }
        }
        catch { }
        return "<unknown>";
    }

    public static void EnsureDefaultDesktop()
    {
        try
        {
            var currentName = GetCurrentDesktopName();
            var hDesk = OpenDesktop("Default", 0, false, 0x01FF);
            if (hDesk != IntPtr.Zero)
            {
                var ok = SetThreadDesktop(hDesk);
                var err = Marshal.GetLastWin32Error();
                Log.Information("EnsureDefaultDesktop: Current='{Prev}', hDesk=0x{HDesk:X}, SetThreadDesktop={Ok}, err={Err}", currentName, (long)hDesk, ok, err);
            }
            else
            {
                var err = Marshal.GetLastWin32Error();
                Log.Warning("EnsureDefaultDesktop: OpenDesktop failed from '{Prev}', err={Err}", currentName, err);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "EnsureDefaultDesktop exception");
        }
    }

    public void Dispose() => Detach("bridge closing");
}
