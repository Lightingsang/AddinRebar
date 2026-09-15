using System.Diagnostics;
using System.Runtime.InteropServices;
using Autodesk.Navisworks.Api;
using HPNavis.McpBridge.Service;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.Core.Scripting;
using Serilog;

namespace HPNavis.McpBridge;

/// <summary>
///     Moves work from the pipe thread onto Navisworks' main thread. The dispatcher calls the
///     <see cref="IBridgeExecutor"/> side; the heavy pre-pass, the guard and the compile happen right there
///     on the pipe thread and only the run is queued. Navisworks has no external event, so the bridge
///     listens to <c>Application.Idle</c> for the whole session and lets <see cref="MainThreadQueue"/>
///     decide on each tick whether the host is quiescent. A context request while a script runs is
///     answered "busy" at once — otherwise the server's 15 s context timeout would fire a cancel into the
///     running script. Heavy runs are audited twice: a "started" line before the work is queued, so a
///     Navisworks killed mid-clash still leaves a trace.
/// </summary>
public sealed partial class NavisMainThreadExecutor : IBridgeExecutor, IDisposable
{
    private const string HostName = "Navisworks";
    private const uint WmNull = 0x0000;

    private readonly BridgeSettings _settings;
    private readonly ScriptCompiler _compiler;
    private readonly NavisScriptRunner _runner;
    private readonly NavisHeavyGate _heavy;
    private readonly NavisQuiescence _quiescence;
    private readonly TypeInspector _inspector;
    private readonly AuditLogger _audit;
    private readonly string _hostVersion;
    private readonly MainThreadQueue _queue;
    private readonly EventHandler<EventArgs> _onIdle;
    private readonly EventHandler<EventArgs> _onGuiCreated;
    private readonly EventHandler<EventArgs> _onActiveDocumentChanged;
    private int _busy;
    private volatile CancellationTokenSource? _currentCancel;
    private string? _activeDocumentTitle;

    /// <summary>Must be constructed on Navisworks' main thread (the plugin's OnLoaded): it subscribes to application events.</summary>
    public NavisMainThreadExecutor(BridgeSettings settings, ScriptCompiler compiler, NavisScriptRunner runner, NavisHeavyGate heavy,
        NavisQuiescence quiescence, TypeInspector inspector, AuditLogger audit, string hostVersion, TimeSpan busyGrace)
    {
        _settings = settings;
        _compiler = compiler;
        _runner = runner;
        _heavy = heavy;
        _quiescence = quiescence;
        _inspector = inspector;
        _audit = audit;
        _hostVersion = hostVersion;
        // expireWithoutTicks: Navisworks raises no Idle at all while a native modal (file dialog) runs, so the
        // busy answer must come from a timer or the request would wait until the user closes the dialog.
        _queue = new MainThreadQueue(() => _quiescence.IsQuiescent(Application.ActiveDocument), HostName, busyGrace, WakeMainThread, expireWithoutTicks: true);

        _onIdle = (_, _) =>
        {
            // Runs on every Navisworks idle pass; nothing may escape into Roamer's message loop.
            try { _queue.OnTick(); }
            catch (Exception exception) { Log.Error(exception, "MCP bridge idle tick failed"); }
        };
        _onGuiCreated = (_, _) => ReadMainWindow();
        _onActiveDocumentChanged = (_, _) => SetActiveDocumentTitle(SafeTitle(Application.ActiveDocument));

        Application.Idle += _onIdle;
        Application.GuiCreated += _onGuiCreated;
        Application.ActiveDocumentChanged += _onActiveDocumentChanged;
        ReadMainWindow();
        _activeDocumentTitle = SafeTitle(Application.ActiveDocument);
    }

    /// <summary>
    ///     The second per-session opt-in: heavy file/clash operations. Off on every start, never persisted, and
    ///     never on while code execution itself is off — the view model mirrors that rule, the executor owns it.
    /// </summary>
    public bool HeavyOperationsEnabled
    {
        get => _heavy.Enabled;
        set
        {
            if (value && !_settings.ExecutionEnabled) value = false;
            if (_heavy.Enabled == value) return;
            _heavy.Enabled = value;
            Log.Information("MCP bridge heavy operations {State}", value ? "ENABLED" : "disabled");
            StateChanged?.Invoke();
        }
    }

    public NavisQuiescence Quiescence => _quiescence;

    public bool IsBusy => Volatile.Read(ref _busy) == 1;

    public int CompiledScriptCount => _compiler.CompiledCount;

    public string? ActiveDocumentTitle => _activeDocumentTitle;

    public event Action? StateChanged;

    public event Action<LastRunInfo>? RunCompleted;

    public async Task<ExecuteResult> ExecuteAsync(ExecuteRequest request, IProgress<ScriptProgress>? progress, CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
            return ExecuteResult.Failure($"Another script is still running in {HostName}. Wait for it to finish or call cancel_execution.");

        var stopwatch = Stopwatch.StartNew();
        var hasHeavyCalls = false;

        try
        {
            StateChanged?.Invoke();

            // Heavy pre-pass first (its message tells the AI which checkbox to ask for), then the shared guard. The
            // timeout ceiling is read at the same moment so a heavy toggle mid-request cannot change the clamp.
            var heavy = _heavy.Check(request.Code, out hasHeavyCalls);
            var maxTimeoutSeconds = _heavy.MaxTimeoutSeconds;
            var guard = ScriptGuard.Check(request.Code, GuardProfile.Navis);
            var refused = heavy.Concat(guard).ToArray();
            if (refused.Length > 0) return Finish(request, Diagnostics(heavy.Count > 0 ? "heavy" : "guard", refused), stopwatch, hasHeavyCalls);

            var compiled = _compiler.GetOrCompile(request.Code);
            if (!compiled.Succeeded) return Finish(request, Diagnostics("compile", compiled.Diagnostics), stopwatch, hasHeavyCalls);

            if (hasHeavyCalls) AuditStarted(request);

            var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _currentCancel = cancel;

            try
            {
                var work = new MainThreadWorkItem("execute: " + (request.Label ?? "script"),
                    _ =>
                    {
                        _quiescence.Reset("run start");
                        try { return _runner.Run(RequireDocument(), request, compiled.Script!, hasHeavyCalls, maxTimeoutSeconds, progress, cancel); }
                        finally { _quiescence.Reset("run end"); }
                    },
                    cancel.Token);

                var result = await _queue.RunAsync(work).ConfigureAwait(false) as ExecuteResult
                             ?? ExecuteResult.Failure($"{HostName} returned no result for the script.");

                return Finish(request, result, stopwatch, hasHeavyCalls);
            }
            finally
            {
                _currentCancel = null;
                cancel.Dispose();
            }
        }
        catch (BridgeRequestException exception)
        {
            Finish(request, ExecuteResult.Failure(exception.Message), stopwatch, hasHeavyCalls);
            throw;
        }
        catch (OperationCanceledException)
        {
            return Finish(request, ExecuteResult.Failure("Script was cancelled before it started. Nothing ran."), stopwatch, hasHeavyCalls);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "MCP execute pipeline failed");
            return Finish(request, ExecuteResult.Failure(SafeText.StripPaths($"{exception.GetType().Name}: {exception.Message}")), stopwatch, hasHeavyCalls);
        }
        finally
        {
            Volatile.Write(ref _busy, 0);
            StateChanged?.Invoke();
        }
    }

    public async Task<ContextResult> GetContextAsync(bool includeSelection, CancellationToken cancellationToken)
    {
        // Answer at once instead of parking behind a running script: the server would time out and cancel it.
        if (IsBusy) throw BridgeRequestException.Busy(HostName);

        var work = new MainThreadWorkItem("context",
            _ => NavisContextReader.Read(includeSelection, _settings.ExecutionEnabled, _heavy.Enabled, _hostVersion, _quiescence),
            cancellationToken);

        return (ContextResult)await _queue.RunAsync(work).ConfigureAwait(false);
    }

    public InspectResult Inspect(InspectRequest request) => _inspector.Inspect(request);

    /// <summary>Analysis always screens with heavy operations OFF: a stored tool must not depend on a per-session checkbox.</summary>
    public AnalyzeResult Analyze(AnalyzeRequest request)
    {
        var result = ScriptAnalyzer.Run(_compiler, request.Code, GuardProfile.Navis, AnalyzerProfile.Navis);
        var heavy = new NavisHeavyGate().Check(request.Code, out _);
        if (heavy.Count > 0) result.GuardViolations = heavy.Concat(result.GuardViolations).ToArray();
        return result;
    }

    public CancelResult Cancel()
    {
        var current = _currentCancel;
        if (current is null) return new CancelResult(false, false);

        try
        {
            current.Cancel();
            return new CancelResult(true, true);
        }
        catch (ObjectDisposedException)
        {
            return new CancelResult(false, false);
        }
    }

    private void ReadMainWindow()
    {
        try
        {
            var handle = Application.Gui?.MainWindow?.Handle ?? IntPtr.Zero;
            if (handle != IntPtr.Zero) _quiescence.MainWindow = handle;
        }
        catch (Exception exception)
        {
            Log.Debug(exception, "MCP bridge could not read the main window handle yet");
        }
    }

    private void SetActiveDocumentTitle(string? title)
    {
        if (_activeDocumentTitle == title) return;
        _activeDocumentTitle = title;
        StateChanged?.Invoke();
    }

    private static string? SafeTitle(Document? document)
    {
        try { return document is null || document.IsClear ? null : document.Title; }
        catch { return null; }
    }

    /// <summary>Main thread only. Navisworks always has a document; "no document" means an untitled, empty one.</summary>
    private static Document RequireDocument()
    {
        var document = Application.ActiveDocument;
        if (document is null || document.IsClear) throw BridgeRequestException.NoActiveDocument(HostName, "model (.nwd/.nwf/.nwc)");
        return document;
    }

    /// <summary>Navisworks raises Idle after it processes a message; an empty message wakes the loop without moving the mouse.</summary>
    private void WakeMainThread()
    {
        var main = _quiescence.MainWindow;
        if (main != IntPtr.Zero) PostMessage(main, WmNull, IntPtr.Zero, IntPtr.Zero);
    }

    [DllImport("user32.dll", SetLastError = false)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    public void Dispose()
    {
        try { _currentCancel?.Cancel(); }
        catch (ObjectDisposedException) { /* the run finished and disposed its source between the read and the call */ }
        Application.Idle -= _onIdle;
        Application.GuiCreated -= _onGuiCreated;
        Application.ActiveDocumentChanged -= _onActiveDocumentChanged;
        _queue.FailAll(new ObjectDisposedException("The MCP bridge is shutting down."));
        _quiescence.Dispose();
        _audit.Dispose();
    }
}
