using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using ETABSv1;
using Serilog;

namespace HPEtabs.McpBridge.Service;

/// <summary>
///     The one COM attachment to ETABS. Attach, EnsureConnected and Detach are called on the bridge's STA worker
///     (the proxy is apartment-bound and every OAPI call goes through that thread); the state is read from any
///     thread. Liveness is eager: the attached process's <c>Exited</c> event drops the attachment at once, so
///     a request after ETABS closed fails fast with "not attached" instead of waiting on a dead window handle.
///     Only the active instance is used (<c>Helper.GetObject</c>) when attaching to existing; with AutoStart=true,
///     it can also launch a new instance via <c>Helper.CreateObject</c> and initialize a new blank model.
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

    /// <summary>Active connection configuration.</summary>
    public EtabsConnectionConfig Config { get; set; } = new() { AutoStart = false };

    /// <summary>True between a successful <see cref="Attach"/> / <see cref="EnsureConnected"/> and a <see cref="Detach"/> or the process exiting.</summary>
    public bool Attached => _attached;

    /// <summary>Process id of the attached ETABS; 0 while detached or when it could not be told apart from a second instance.</summary>
    public int Pid { get; private set; }

    /// <summary>Main window of the attached process, for the quiescence pre-check (a modal dialog disables it).</summary>
    public IntPtr MainWindowHandle { get; private set; }

    /// <summary>True if the ETABS application window is currently visible on desktop.</summary>
    public bool IsVisible
    {
        get
        {
            lock (_gate)
            {
                try { return _etabs?.Visible() ?? false; }
                catch { return false; }
            }
        }
    }

    /// <summary>Wrapper file version (2.10.0.0) — what `GetOAPIVersionNumber` returns is a double and less telling.</summary>
    public string? OapiVersion { get; private set; }

    /// <summary>Set when more than one ETABS is running: which one <c>GetObject</c> returned is ETABS's decision, not ours.</summary>
    public string? Warning => _warning;

    public event Action? StateChanged;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_RESTORE = 9;

    /// <summary>Controls visibility of the ETABS application window on desktop.</summary>
    public bool SetVisible(bool visible)
    {
        lock (_gate)
        {
            if (_etabs is null) return false;
            try
            {
                if (visible)
                {
                    _etabs.Unhide();
                    BringToFront(_process);
                }
                else
                {
                    _etabs.Hide();
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to set ETABS visibility to {Visible}", visible);
                return false;
            }
        }
    }

    /// <summary>
    ///     Ensures the ETABS window is unhidden and brought to front on desktop if hidden.
    /// </summary>
    public void EnsureVisibleOnDesktop()
    {
        try
        {
            if (_etabs is not null && !_etabs.Visible())
            {
                Log.Information("ETABS window is hidden; calling Unhide()...");
                _etabs.Unhide();
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to call Unhide() on ETABS object.");
        }

        BringToFront(_process);
    }

    /// <summary>
    ///     Brings the ETABS process's main window to the front and activates it.
    /// </summary>
    public void BringToFront(Process? process = null)
    {
        process ??= _process;
        if (process is null) return;

        try
        {
            var sw = Stopwatch.StartNew();
            IntPtr hwnd = IntPtr.Zero;

            // Wait up to 3 seconds for MainWindowHandle to be populated once unhidden
            while (sw.Elapsed < TimeSpan.FromSeconds(3))
            {
                process.Refresh();
                if (process.HasExited) return;

                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    hwnd = process.MainWindowHandle;
                    break;
                }
                Thread.Sleep(200);
            }

            if (hwnd != IntPtr.Zero)
            {
                MainWindowHandle = hwnd;
                ShowWindow(hwnd, SW_RESTORE);
                SetForegroundWindow(hwnd);
                Log.Information("Brought ETABS main window {Hwnd} to foreground", hwnd);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not bring ETABS window to front");
        }
    }

    /// <summary>
    ///     Checks if the current COM proxy is still alive and responsive using <c>SapModel.GetVersion</c>.
    /// </summary>
    public bool IsHealthy()
    {
        lock (_gate)
        {
            if (!_attached || _sapModel is null || _etabs is null) return false;

            try
            {
                string version = string.Empty;
                double versionNumber = 0.0;
                int ret = _sapModel.GetVersion(ref version, ref versionNumber);
                if (ret != 0)
                {
                    Log.Warning("ETABS health check: SapModel.GetVersion returned {Ret}", ret);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "ETABS health check failed: {Message}; dropping connection", ex.Message);
                Detach($"Health check failed: {ex.GetType().Name}");
                return false;
            }
        }
    }

    /// <summary>
    ///     Central connection manager: checks health, attaches to running ETABS, or launches a new instance.
    ///     STA worker only.
    /// </summary>
    public EtabsConnectionResult EnsureConnected(EtabsConnectionConfig? config = null)
    {
        lock (_gate)
        {
            config ??= Config;

            // 1. Health check existing connection
            if (_attached && _sapModel is not null && _etabs is not null)
            {
                if (IsHealthy())
                {
                    if (config.StartVisible)
                    {
                        EnsureVisibleOnDesktop();
                    }
                    var (ver, num, title) = GetModelInfo(_sapModel);
                    return new EtabsConnectionResult(
                        EtabsConnectionState.already_connected,
                        ver,
                        num,
                        title,
                        Pid,
                        null,
                        _warning);
                }

                Detach("Health check failed on existing attachment");
            }

            var processes = Process.GetProcessesByName(ProcessName);

            // 2. Try attaching to existing running ETABS if preferred and running
            if (config.PreferExistingInstance && processes.Length > 0)
            {
                try
                {
                    var sapModel = Attach();
                    if (config.StartVisible)
                    {
                        EnsureVisibleOnDesktop();
                    }
                    var (ver, num, title) = GetModelInfo(sapModel);
                    return new EtabsConnectionResult(
                        EtabsConnectionState.attached_existing,
                        ver,
                        num,
                        title,
                        Pid,
                        null,
                        _warning);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to attach to existing ETABS instance");
                    if (!config.AutoStart)
                    {
                        return new EtabsConnectionResult(
                            EtabsConnectionState.failed,
                            ErrorMessage: $"Failed to attach to running ETABS: {ex.Message}");
                    }
                }
            }

            // 3. Auto-start if allowed
            if (!config.AutoStart)
            {
                return new EtabsConnectionResult(
                    EtabsConnectionState.failed,
                    ErrorMessage: "No ETABS process is running and AutoStart is disabled in configuration.");
            }

            return StartNewInstance(config);
        }
    }

    private EtabsConnectionResult StartNewInstance(EtabsConnectionConfig config)
    {
        try
        {
            string? exePath = config.EtabsExePath;
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            {
                var installDir = EtabsApiLocator.Find();
                if (installDir is not null)
                {
                    var candidate = Path.Combine(installDir, "ETABS.exe");
                    if (File.Exists(candidate)) exePath = candidate;
                }
            }

            cHelper helper = new Helper();
            cOAPI etabs;

            var beforePids = Process.GetProcessesByName(ProcessName).Select(p => p.Id).ToHashSet();

            if (!string.IsNullOrWhiteSpace(exePath) && File.Exists(exePath))
            {
                Log.Information("Starting ETABS via CreateObject({Path})", exePath);
                etabs = helper.CreateObject(exePath);
            }
            else
            {
                Log.Information("Starting ETABS via CreateObjectProgID({ProgId})", ProgId);
                etabs = helper.CreateObjectProgID(ProgId);
            }

            if (etabs is null)
            {
                return new EtabsConnectionResult(
                    EtabsConnectionState.failed,
                    ErrorMessage: "helper.CreateObject returned null.");
            }

            Log.Information("Starting ETABS application via ApplicationStart()...");
            int ret = etabs.ApplicationStart();
            if (ret != 0)
            {
                return new EtabsConnectionResult(
                    EtabsConnectionState.failed,
                    ErrorMessage: $"ApplicationStart() failed with return code {ret}.");
            }

            // Wait for ETABS to be ready
            var timeout = TimeSpan.FromSeconds(Math.Max(10, config.StartupTimeoutSeconds));
            var sw = Stopwatch.StartNew();
            cSapModel? sapModel = null;

            while (sw.Elapsed < timeout)
            {
                try
                {
                    sapModel = etabs.SapModel;
                    if (sapModel is not null)
                    {
                        string v = string.Empty;
                        double vn = 0.0;
                        if (sapModel.GetVersion(ref v, ref vn) == 0)
                        {
                            break;
                        }
                    }
                }
                catch
                {
                    // still starting up
                }
                Thread.Sleep(500);
            }

            if (sapModel is null)
            {
                return new EtabsConnectionResult(
                    EtabsConnectionState.failed,
                    ErrorMessage: $"ETABS started but SapModel was not ready within {config.StartupTimeoutSeconds}s.");
            }

            // If a default model path was specified, open it; otherwise create a New Blank Model
            if (!string.IsNullOrWhiteSpace(config.DefaultModelPath) && File.Exists(config.DefaultModelPath))
            {
                Log.Information("Opening default model {Path}...", config.DefaultModelPath);
                ret = sapModel.File.OpenFile(config.DefaultModelPath);
                if (ret != 0) Log.Warning("File.OpenFile({Path}) returned {Ret}", config.DefaultModelPath, ret);
            }
            else
            {
                Log.Information("Initializing New Blank Model (kN_m_C)...");
                ret = sapModel.InitializeNewModel(eUnits.kN_m_C);
                if (ret == 0)
                {
                    ret = sapModel.File.NewBlank();
                    if (ret != 0) Log.Warning("File.NewBlank() returned {Ret}", ret);
                }
                else
                {
                    Log.Warning("InitializeNewModel() returned {Ret}", ret);
                }
            }

            if (config.StartVisible)
            {
                try
                {
                    Log.Information("Unhiding ETABS application window...");
                    etabs.Unhide();
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to call Unhide() on ETABS object.");
                }
            }

            _etabs = etabs;
            _sapModel = sapModel;
            OapiVersion = EtabsAssemblyResolver.WrapperFileVersion;
            _attached = true;

            var afterProcesses = Process.GetProcessesByName(ProcessName);
            var newProcess = afterProcesses.FirstOrDefault(p => !beforePids.Contains(p.Id)) ?? afterProcesses.FirstOrDefault();
            if (newProcess is not null)
            {
                Watch(newProcess);
                if (config.StartVisible)
                {
                    BringToFront(newProcess);
                }
            }

            var (ver, num, title) = GetModelInfo(sapModel);
            Log.Information("Successfully started and connected to new ETABS instance pid {Pid} (Version {Ver})", Pid, ver);
            StateChanged?.Invoke();

            return new EtabsConnectionResult(
                EtabsConnectionState.started_new,
                ver,
                num,
                title,
                Pid,
                null,
                null);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to start ETABS instance");
            Detach("Start ETABS failed");
            return new EtabsConnectionResult(
                EtabsConnectionState.failed,
                ErrorMessage: $"Failed to start ETABS: {ex.Message}");
        }
    }

    private static (string? version, double? versionNumber, string? modelTitle) GetModelInfo(cSapModel sapModel)
    {
        string version = string.Empty;
        double versionNumber = 0.0;
        try
        {
            sapModel.GetVersion(ref version, ref versionNumber);
        }
        catch { }

        string? title = null;
        try
        {
            var fileName = sapModel.GetModelFilename(true);
            title = EtabsContextReader.ModelFile(fileName);
            if (title is not null) title = Path.GetFileName(title);
        }
        catch { }

        return (version, versionNumber, title);
    }

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

    public static HPRebar.McpBridge.Core.Pipe.BridgeRequestException NotAttached(string? reason = null) =>
        new(HPRebar.Mcp.Contracts.JsonRpc.BridgeErrorCode.NoActiveDocument,
            reason is null
                ? "ETABS not attached — click Attach in the HPEtabs MCP Bridge window (start ETABS 22 first)."
                : $"ETABS not attached ({reason}) — click Attach in the HPEtabs MCP Bridge window (start ETABS 22 first).");

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
