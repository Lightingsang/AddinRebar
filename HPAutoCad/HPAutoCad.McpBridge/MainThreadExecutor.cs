using System.Diagnostics;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.ApplicationServices;
using HPAutoCad.McpBridge.Service;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.Core.Scripting;
using Serilog;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPAutoCad.McpBridge;

/// <summary>
///     Moves work from the pipe thread onto AutoCAD's main thread. The dispatcher calls the
///     <see cref="IBridgeExecutor"/> side; guard and compile happen right there on the pipe thread, and
///     only the run is queued. AutoCAD has no external event, so the bridge listens to
///     <c>Application.Idle</c> for the whole session and lets <see cref="MainThreadQueue"/> decide on
///     each tick whether the host is quiescent enough to run what is waiting. Same shape as the Revit
///     external-event handler, minus the TaskDialog: results go back over the pipe.
/// </summary>
public sealed class MainThreadExecutor : IBridgeExecutor, IDisposable
{
    private const string HostName = "AutoCAD";
    private const uint WmNull = 0x0000;

    private readonly BridgeSettings _settings;
    private readonly ScriptCompiler _compiler;
    private readonly AutocadScriptRunner _runner;
    private readonly TypeInspector _inspector;
    private readonly AuditLogger _audit;
    private readonly string _hostVersion;
    private readonly MainThreadQueue _queue;
    private readonly EventHandler _onIdle;
    private readonly DocumentCollectionEventHandler _onDocumentActivated;
    private readonly DocumentCollectionEventHandler _onDocumentToBeDestroyed;
    private int _busy;
    private volatile CancellationTokenSource? _currentCancel;
    private string? _activeDocumentTitle;

    /// <summary>Must be constructed on AutoCAD's main thread (the extension's Initialize): it subscribes to application events.</summary>
    public MainThreadExecutor(BridgeSettings settings, ScriptCompiler compiler, AutocadScriptRunner runner, TypeInspector inspector,
        AuditLogger audit, string hostVersion, TimeSpan busyGrace)
    {
        _settings = settings;
        _compiler = compiler;
        _runner = runner;
        _inspector = inspector;
        _audit = audit;
        _hostVersion = hostVersion;
        _queue = new MainThreadQueue(() => AcadApp.IsQuiescent, HostName, busyGrace, WakeMainThread);

        _onIdle = (_, _) => _queue.OnTick();
        _onDocumentActivated = (_, e) => SetActiveDocumentTitle(e.Document?.Name);
        _onDocumentToBeDestroyed = (_, e) =>
        {
            if (e.Document?.Name == _activeDocumentTitle) SetActiveDocumentTitle(null);
        };

        AcadApp.Idle += _onIdle;
        AcadApp.DocumentManager.DocumentActivated += _onDocumentActivated;
        AcadApp.DocumentManager.DocumentToBeDestroyed += _onDocumentToBeDestroyed;
        _activeDocumentTitle = AcadApp.DocumentManager.MdiActiveDocument?.Name;
    }

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

        try
        {
            // Inside the try: a subscriber (the status window) that throws must not leave _busy set forever.
            StateChanged?.Invoke();

            var guard = ScriptGuard.Check(request.Code, GuardProfile.Autocad);
            if (guard.Count > 0) return Finish(request, Diagnostics("guard", guard), stopwatch);

            var compiled = _compiler.GetOrCompile(request.Code);
            if (!compiled.Succeeded) return Finish(request, Diagnostics("compile", compiled.Diagnostics), stopwatch);

            var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _currentCancel = cancel;

            try
            {
                var work = new MainThreadWorkItem("execute: " + (request.Label ?? "script"),
                    _ => _runner.Run(RequireDocument(), request, compiled.Script!, progress, cancel),
                    cancel.Token);

                var result = await _queue.RunAsync(work).ConfigureAwait(false) as ExecuteResult
                             ?? ExecuteResult.Failure($"{HostName} returned no result for the script.");

                return Finish(request, result, stopwatch);
            }
            finally
            {
                // Clear the reference before disposing so Cancel() on the pipe thread can never see a disposed source.
                _currentCancel = null;
                cancel.Dispose();
            }
        }
        catch (BridgeRequestException)
        {
            throw; // busy past the grace period or no drawing: the dispatcher turns the code into the reply
        }
        catch (Exception exception)
        {
            Log.Error(exception, "MCP execute pipeline failed");
            return Finish(request, ExecuteResult.Failure(SafeText.StripPaths($"{exception.GetType().Name}: {exception.Message}")), stopwatch);
        }
        finally
        {
            Volatile.Write(ref _busy, 0);
            StateChanged?.Invoke();
        }
    }

    public async Task<ContextResult> GetContextAsync(bool includeSelection, CancellationToken cancellationToken)
    {
        var work = new MainThreadWorkItem("context",
            _ => AutocadContextReader.Read(includeSelection, _settings.ExecutionEnabled, _hostVersion),
            cancellationToken);

        return (ContextResult)await _queue.RunAsync(work).ConfigureAwait(false);
    }

    public InspectResult Inspect(InspectRequest request) => _inspector.Inspect(request);

    public AnalyzeResult Analyze(AnalyzeRequest request) => ScriptAnalyzer.Run(_compiler, request.Code, GuardProfile.Autocad, AnalyzerProfile.Autocad);

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
            return new CancelResult(false, false); // the script finished between the read and the cancel
        }
    }

    private void SetActiveDocumentTitle(string? title)
    {
        if (_activeDocumentTitle == title) return;

        _activeDocumentTitle = title;
        StateChanged?.Invoke();
    }

    /// <summary>Main thread only. The actionable code tells the AI to open a drawing instead of retrying.</summary>
    private static Document RequireDocument() =>
        AcadApp.DocumentManager.MdiActiveDocument ?? throw BridgeRequestException.NoActiveDocument(HostName, "drawing");

    /// <summary>
    ///     AutoCAD raises Idle after it processes a message; with nothing happening in the UI the queue
    ///     would wait for the next timer or mouse move. An empty message is enough to wake the loop.
    /// </summary>
    private static void WakeMainThread()
    {
        var handle = AcadApp.MainWindow?.Handle ?? IntPtr.Zero;
        if (handle != IntPtr.Zero) PostMessage(handle, WmNull, IntPtr.Zero, IntPtr.Zero);
    }

    [DllImport("user32.dll", SetLastError = false)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private static ExecuteResult Diagnostics(string stage, IReadOnlyList<ScriptDiagnostic> diagnostics) => new ExecuteResult
    {
        IsError = true,
        Message = stage == "guard"
            ? "The script uses APIs the bridge blocks. Fix the listed lines and retry."
            : "The script does not compile. Fix the listed errors and retry.",
        Diagnostics = diagnostics,
    };

    private ExecuteResult Finish(ExecuteRequest request, ExecuteResult result, Stopwatch stopwatch)
    {
        if (result.DurationMs == 0) result.DurationMs = stopwatch.ElapsedMilliseconds;

        var outcome = !result.IsError ? "ok" : result.TimedOut ? "timeout" : result.Diagnostics.Count > 0 ? "rejected" : "error";
        var title = _activeDocumentTitle;

        try
        {
            _audit.Write(new AuditEntry(
                DateTimeOffset.Now, Environment.UserName, title, null, ScriptCompiler.Hash(request.Code), request.Code,
                request.Transaction, request.DryRun, outcome, result.DurationMs,
                result.Changed.Added, result.Changed.Modified, result.Changed.Deleted, result.Message));
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "MCP audit write failed");
        }

        RunCompleted?.Invoke(new LastRunInfo(DateTimeOffset.Now, request.Label ?? "script", request.Code, result.IsError, result.Message,
            result.DurationMs, result.RolledBack, result.Changed.Added, result.Changed.Modified, result.Changed.Deleted));

        return result;
    }

    public void Dispose()
    {
        _currentCancel?.Cancel();
        AcadApp.Idle -= _onIdle;
        AcadApp.DocumentManager.DocumentActivated -= _onDocumentActivated;
        AcadApp.DocumentManager.DocumentToBeDestroyed -= _onDocumentToBeDestroyed;
        _queue.FailAll(new ObjectDisposedException("The MCP bridge is shutting down."));
        _audit.Dispose();
    }
}
