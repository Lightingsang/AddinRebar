using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using ETABSv1;
using Serilog;

namespace HPEtabs.McpBridge.Service;

/// <summary>
///     The one COM attachment to the running ETABS. Attach and Detach are called on the bridge's STA worker
///     (the proxy is apartment-bound and every OAPI call goes through that thread); the state is read from any
///     thread. Liveness is eager: the attached process's <c>Exited</c> event drops the attachment at once, so
///     a request after ETABS closed fails fast with "not attached" instead of waiting on a dead window handle.
///     Only the active instance is used (<c>Helper.GetObject</c>); with more than one ETABS running the bridge
///     says so and leaves the choice to the user (ETABS: Tools › Active Instance for API).
/// </summary>
public sealed class EtabsAttachment : IDisposable
{
    public const string ProgId = "CSI.ETABS.API.ETABSObject";
    public const string ProcessName = "ETABS";

    private readonly object _gate = new();
    private cOAPI? _etabs;
    private cSapModel? _sapModel;
    private Process? _process;
    private volatile bool _attached;
    private volatile string? _warning;

    /// <summary>True between a successful <see cref="Attach"/> and a <see cref="Detach"/> or the process exiting.</summary>
    public bool Attached => _attached;

    /// <summary>Process id of the attached ETABS; 0 while detached or when it could not be told apart from a second instance.</summary>
    public int Pid { get; private set; }

    /// <summary>Main window of the attached process, for the quiescence pre-check (a modal dialog disables it).</summary>
    public IntPtr MainWindowHandle { get; private set; }

    /// <summary>Wrapper file version (2.10.0.0) — what `GetOAPIVersionNumber` returns is a double and less telling.</summary>
    public string? OapiVersion { get; private set; }

    /// <summary>Set when more than one ETABS is running: which one <c>GetObject</c> returned is ETABS's decision, not ours.</summary>
    public string? Warning => _warning;

    public event Action? StateChanged;

    /// <summary>STA worker only. Throws with a message the window can show when no ETABS instance is registered.</summary>
    public cSapModel Attach()
    {
        lock (_gate)
        {
            if (_attached && _sapModel is not null) return _sapModel;

            var processes = Process.GetProcessesByName(ProcessName);
            if (processes.Length == 0) throw new InvalidOperationException("No ETABS process is running. Start ETABS 22 and open a model, then click Attach.");

            cOAPI etabs;
            try
            {
                // Helper implements cHelper explicitly: the members are only reachable through the interface.
                cHelper helper = new Helper();
                // Null = nothing under the ProgID in this session's running-object table: ETABS is still loading, or it was started
                // "as administrator" — an elevated process registers in a table a non-elevated app cannot see.
                etabs = helper.GetObject(ProgId) ?? throw new InvalidOperationException(
                    "ETABS is running but not registered for the API in this session. If it was started as administrator, close it and start it normally (an elevated ETABS is invisible to this app); otherwise wait for it to finish loading and click Attach again.");
            }
            catch (COMException exception)
            {
                throw new InvalidOperationException($"ETABS is running but its API object is not registered yet (0x{exception.HResult:X8}). Wait for the start screen to finish loading, then click Attach.", exception);
            }

            var sapModel = etabs.SapModel ?? throw new InvalidOperationException("ETABS answered GetObject but returned no SapModel.");

            _etabs = etabs;
            _sapModel = sapModel;
            OapiVersion = EtabsAssemblyResolver.WrapperFileVersion;
            _warning = processes.Length > 1
                ? $"{processes.Length} ETABS instances are running; the bridge attached to the one ETABS calls active. Pick it in ETABS: Tools › Active Instance for API, then Detach and Attach again."
                : null;

            // Attached before the watch goes up: an ETABS that exits during the watch then detaches cleanly instead of
            // leaving a half-built attachment behind.
            _attached = true;
            var target = processes.Length == 1 ? processes[0] : null;
            if (target is not null) Watch(target);
            else foreach (var p in processes) p.Dispose();
            if (!_attached) throw new InvalidOperationException("ETABS exited while the bridge was attaching. Start it again and click Attach.");

            var oapiNumber = Safe(() => etabs.GetOAPIVersionNumber().ToString(CultureInfo.InvariantCulture));
            Log.Information("Attached to ETABS pid {Pid} (GetOAPIVersionNumber {Number}, wrapper {Version}){Warning}",
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
            if (!_attached && _etabs is null) return;

            _attached = false;
            _etabs = null;
            _sapModel = null;
            Pid = 0;
            MainWindowHandle = IntPtr.Zero;
            _warning = null;
            Unwatch();
            Log.Information("Detached from ETABS: {Reason}", reason);
        }

        StateChanged?.Invoke();
    }

    /// <summary>STA worker only; throws the "not attached" refusal the dispatcher turns into -32003.</summary>
    public (cOAPI etabs, cSapModel sapModel) Require()
    {
        lock (_gate)
        {
            if (_attached && _etabs is not null && _sapModel is not null) return (_etabs, _sapModel);
        }

        throw NotAttached();
    }

    public static HPRebar.McpBridge.Core.Pipe.BridgeRequestException NotAttached() =>
        new(HPRebar.Mcp.Contracts.JsonRpc.BridgeErrorCode.NoActiveDocument, "ETABS not attached — click Attach in the HPEtabs MCP Bridge window (start ETABS 22 first).");

    /// <summary>
    ///     The COM channel to ETABS is gone: the process exited or the proxy was disconnected. With more than one
    ///     ETABS running no process is watched (which one answered GetObject is not knowable), so this is the
    ///     liveness check that always works — the first call after ETABS closed drops the attachment.
    /// </summary>
    public static bool IsDisconnectError(Exception exception) => exception is COMException com && com.HResult is
        unchecked((int)0x800706BA)   // RPC_S_SERVER_UNAVAILABLE
        or unchecked((int)0x800706BE) // RPC_S_CALL_FAILED
        or unchecked((int)0x800706BF) // RPC_S_CALL_FAILED_DNE
        or unchecked((int)0x80010108) // RPC_E_DISCONNECTED
        or unchecked((int)0x80010012) // RPC_E_SERVER_DIED_DNE
        or unchecked((int)0x80010007); // RPC_E_SERVER_DIED

    /// <summary>Drops the attachment when the exception says ETABS is gone and returns the refusal to throw instead; null otherwise.</summary>
    public HPRebar.McpBridge.Core.Pipe.BridgeRequestException? DetachIfGone(Exception exception)
    {
        if (!IsDisconnectError(exception)) return null;
        Detach($"ETABS gone: {exception.GetType().Name} 0x{exception.HResult:X8}");
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
            Log.Warning(exception, "Could not watch ETABS pid {Pid} for exit; liveness falls back to the next failed call", Pid);
        }
    }

    private void Unwatch()
    {
        if (_process is null) return;
        try { _process.Exited -= OnProcessExited; } catch { /* already gone */ }
        _process.Dispose();
        _process = null;
    }

    private void OnProcessExited(object? sender, EventArgs e) => Detach("ETABS exited");

    private static T? Safe<T>(Func<T?> read)
    {
        try { return read(); }
        catch { return default; }
    }

    public void Dispose() => Detach("bridge closing");
}
