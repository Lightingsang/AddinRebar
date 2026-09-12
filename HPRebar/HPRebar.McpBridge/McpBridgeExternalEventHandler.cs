using System.Collections.Concurrent;
using System.Diagnostics;
using Autodesk.Revit.UI;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.Core.Scripting;
using HPRebar.McpBridge.Model;
using HPRebar.McpBridge.Service;
using Serilog;

namespace HPRebar.McpBridge;

/// <summary>
///     Moves work from the pipe thread onto Revit's API thread. The dispatcher calls the
///     <see cref="IRevitExecutor"/> side; guard and compile happen right there on the pipe thread, and
///     only the run is queued and raised. Revit calls <see cref="Execute"/> back on its own thread, where
///     touching the document is legal. Same shape as the ColumnRebar handler, minus the TaskDialog: the
///     caller is an AI, so results go back over the pipe instead of onto the screen.
/// </summary>
public sealed class McpBridgeExternalEventHandler : IExternalEventHandler, IRevitExecutor, IDisposable
{
    private readonly ConcurrentQueue<McpBridgeRequest> _pending = new ConcurrentQueue<McpBridgeRequest>();
    private readonly ExternalEvent _externalEvent;
    private readonly BridgeSettings _settings;
    private readonly ScriptCompiler _compiler;
    private readonly ScriptRunner _runner;
    private readonly TypeInspector _inspector;
    private readonly AuditLogger _audit;
    private int _busy;
    private volatile CancellationTokenSource? _currentCancel;
    private string? _activeDocumentTitle;

    public McpBridgeExternalEventHandler(BridgeSettings settings, ScriptCompiler compiler, ScriptRunner runner, TypeInspector inspector, AuditLogger audit)
    {
        _settings = settings;
        _compiler = compiler;
        _runner = runner;
        _inspector = inspector;
        _audit = audit;
        _externalEvent = ExternalEvent.Create(this);
    }

    public bool IsBusy => Volatile.Read(ref _busy) == 1;

    public int CompiledScriptCount => _compiler.CompiledCount;

    public string? ActiveDocumentTitle => _activeDocumentTitle;

    public event Action? StateChanged;

    public event Action<LastRunInfo>? RunCompleted;

    /// <summary>Called from Revit's ViewActivated / DocumentClosing events so the title is readable from any thread.</summary>
    public void SetActiveDocumentTitle(string? title)
    {
        if (_activeDocumentTitle == title) return;

        _activeDocumentTitle = title;
        StateChanged?.Invoke();
    }

    public async Task<ExecuteResult> ExecuteAsync(ExecuteRequest request, IProgress<ScriptProgress>? progress, CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
            return ExecuteResult.Failure("Another script is still running in Revit. Wait for it to finish or call cancel_execution.");

        var stopwatch = Stopwatch.StartNew();
        StateChanged?.Invoke();

        try
        {
            var guard = ScriptGuard.Check(request.Code);
            if (guard.Count > 0) return Finish(request, Diagnostics("guard", guard), stopwatch);

            var compiled = _compiler.GetOrCompile(request.Code);
            if (!compiled.Succeeded) return Finish(request, Diagnostics("compile", compiled.Diagnostics), stopwatch);

            var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _currentCancel = cancel;

            try
            {
                var work = new McpBridgeRequest("execute: " + (request.Label ?? "script"),
                    (uiapp, _) => _runner.Run(uiapp, request, compiled.Script!, progress, cancel),
                    cancel.Token);

                var result = await RunOnRevitThreadAsync(work).ConfigureAwait(false) as ExecuteResult
                             ?? ExecuteResult.Failure("Revit returned no result for the script.");

                return Finish(request, result, stopwatch);
            }
            finally
            {
                // Clear the reference before disposing so Cancel() on the pipe thread can never see a disposed source.
                _currentCancel = null;
                cancel.Dispose();
            }
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
        var work = new McpBridgeRequest("context",
            (uiapp, _) => RevitContextReader.Read(uiapp, includeSelection, _settings.ExecutionEnabled),
            cancellationToken);

        return (ContextResult)await RunOnRevitThreadAsync(work).ConfigureAwait(false);
    }

    public InspectResult Inspect(InspectRequest request) => _inspector.Inspect(request);

    public AnalyzeResult Analyze(AnalyzeRequest request) => ScriptAnalyzer.Run(_compiler, request.Code);

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

    /// <summary>Revit calls this on its API thread, once per <see cref="ExternalEvent.Raise"/>.</summary>
    public void Execute(UIApplication application)
    {
        SetActiveDocumentTitle(application.ActiveUIDocument?.Document?.Title);

        while (_pending.TryDequeue(out var request))
        {
            if (request.Completion.Task.IsCompleted) continue; // refused at Raise() time

            try
            {
                request.Completion.TrySetResult(request.Work(application, request.CancellationToken));
            }
            catch (Exception exception)
            {
                Log.Error(exception, "MCP bridge work '{Name}' threw on the Revit thread", request.Name);
                request.Completion.TrySetException(exception);
            }
        }
    }

    public string GetName() => "HPRebar MCP Bridge";

    private Task<object> RunOnRevitThreadAsync(McpBridgeRequest request)
    {
        _pending.Enqueue(request);

        var status = _externalEvent.Raise();
        if (status is not (ExternalEventRequest.Accepted or ExternalEventRequest.Pending))
        {
            request.Completion.TrySetException(new InvalidOperationException(
                $"Revit refused the request ({status}): it is in a modal dialog or another command. Close it and retry."));
        }

        return request.Completion.Task;
    }

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
        _externalEvent.Dispose();
        _audit.Dispose();
    }
}
