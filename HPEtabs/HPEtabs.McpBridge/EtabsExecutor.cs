using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using HPEtabs.McpBridge.Service;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.Core.Scripting;
using Serilog;

namespace HPEtabs.McpBridge;

/// <summary>
///     Moves work from the pipe thread onto the bridge's own STA worker, the one thread that holds the COM
///     attachment to ETABS. The dispatcher calls the <see cref="IBridgeExecutor"/> side; the guard, the compile
///     and the tier verdict happen on the pipe thread and only the run is queued. There is no host event to
///     hook: the worker loop itself ticks the <see cref="MainThreadQueue"/>, and drains a separate control
///     lane first so Attach/Detach never wait behind a busy grace. Liveness is the attachment's: a request
///     while ETABS is closed fails at once with "not attached" instead of waiting on a dead window handle.
/// </summary>
public sealed partial class EtabsExecutor : IBridgeExecutor, IDisposable
{
    public const string HostName = "ETABS";

    /// <summary>The engine's timeout ceiling for read-only and writing runs; destructive runs get <see cref="HPRebar.Mcp.Contracts.HostScriptContracts.EtabsHeavyMaxTimeoutSeconds"/>.</summary>
    public const int DefaultMaxTimeoutSeconds = 120;

    private readonly BridgeSettings _settings;
    private readonly ScriptCompiler _compiler;
    private readonly EtabsTierAnalyzer _analyzer;
    private readonly EtabsScriptRunner _runner;
    private readonly EtabsAttachment _attachment;
    private readonly TypeInspector _inspector;
    private readonly AuditLogger _audit;
    private readonly string _hostVersion;
    private readonly MainThreadQueue _queue;
    private readonly AutoResetEvent _wake = new(false);
    private readonly ConcurrentQueue<(string name, Func<object?> work, TaskCompletionSource<object?> completion)> _control = new();
    private readonly Thread _worker;
    private volatile bool _stop;
    private volatile bool _running;
    private int _busy;
    private volatile CancellationTokenSource? _currentCancel;
    private volatile bool _destructiveEnabled;

    public EtabsExecutor(BridgeSettings settings, ScriptCompiler compiler, EtabsTierAnalyzer analyzer, EtabsScriptRunner runner, EtabsAttachment attachment,
        TypeInspector inspector, AuditLogger audit, string hostVersion, TimeSpan busyGrace)
    {
        _settings = settings;
        _compiler = compiler;
        _analyzer = analyzer;
        _runner = runner;
        _attachment = attachment;
        _inspector = inspector;
        _audit = audit;
        _hostVersion = hostVersion;
        // expireWithoutTicks: while a COM call blocks inside ETABS (a modal dialog, a long analysis) the worker
        // cannot tick, so the busy answer must come from the queue's own timer.
        _queue = new MainThreadQueue(IsQuiescent, HostName, busyGrace, () => _wake.Set(), expireWithoutTicks: true);
        _attachment.StateChanged += () => StateChanged?.Invoke();

        // Foreground: a call in flight is drained before the process ends; Dispose stops the loop.
        _worker = new Thread(WorkerLoop) { Name = "HPEtabs COM worker", IsBackground = false };
        _worker.SetApartmentState(ApartmentState.STA);
        _worker.Start();
    }

    /// <summary>
    ///     The second per-session opt-in: unlock, analysis, file and delete operations. Off on every start, never
    ///     persisted, and never on while code execution itself is off — settable only from the window's view model.
    /// </summary>
    public bool DestructiveOperationsEnabled
    {
        get => _destructiveEnabled;
        set
        {
            if (value && !_settings.ExecutionEnabled) value = false;
            if (_destructiveEnabled == value) return;
            _destructiveEnabled = value;
            Log.Information("MCP bridge destructive operations {State}", value ? "ENABLED" : "disabled");
            StateChanged?.Invoke();
        }
    }

    public EtabsAttachment Attachment => _attachment;

    public bool IsBusy => Volatile.Read(ref _busy) == 1;

    public int CompiledScriptCount => _compiler.CompiledCount;

    public string? ActiveDocumentTitle => _attachment.Attached ? "attached (pid " + _attachment.Pid + ")" : null;

    public event Action? StateChanged;

    public event Action<LastRunInfo>? RunCompleted;

    public async Task<ExecuteResult> ExecuteAsync(ExecuteRequest request, IProgress<ScriptProgress>? progress, CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
            return ExecuteResult.Failure($"Another script is still running in {HostName}. Wait for it to finish or call cancel_execution.");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            StateChanged?.Invoke();

            var guard = ScriptGuard.Check(request.Code, GuardProfile.Etabs);
            if (guard.Count > 0) return Finish(request, Diagnostics("guard", guard), stopwatch);

            var compiled = _compiler.GetOrCompile(request.Code);
            if (!compiled.Succeeded) return Finish(request, Diagnostics("compile", compiled.Diagnostics), stopwatch);

            var verdict = _analyzer.Inspect(compiled.Script!);
            if (verdict.Refusals.Count > 0) return Finish(request, Diagnostics("path", verdict.Refusals), stopwatch);

            // `none` or dryRun on a writing script is a static preview: the AI sees which members it would call, nothing runs —
            // so it needs no opt-in, destructive members included.
            var mode = TransactionModes.Normalize(request.Transaction) ?? TransactionModes.Auto;
            if (verdict.Tier >= EtabsTier.Write && (request.DryRun || mode == TransactionModes.None)) return Finish(request, Preview(verdict.Hits), stopwatch);

            // A destructive member with the second opt-in off is refused with the opt-in code, never counted as a run of the tool.
            if (verdict.Tier == EtabsTier.Destructive && !_destructiveEnabled)
                throw new BridgeRequestException(BridgeErrorCode.ExecutionDisabled,
                    "Destructive operations are disabled. Ask the user to tick 'Allow destructive operations' in the HPEtabs MCP Bridge window.");

            if (!_attachment.Attached) throw EtabsAttachment.NotAttached();

            // Destructive runs may take minutes (RunAnalysis); everything else keeps the engine's ceiling.
            var maxTimeoutSeconds = verdict.Tier == EtabsTier.Destructive ? HPRebar.Mcp.Contracts.HostScriptContracts.EtabsHeavyMaxTimeoutSeconds : DefaultMaxTimeoutSeconds;


            var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _currentCancel = cancel;

            try
            {
                var work = new MainThreadWorkItem("execute: " + (request.Label ?? "script"),
                    _ => OnWorker(() =>
                    {
                        var (etabs, sapModel) = _attachment.Require();
                        _running = true;
                        // The audit "started" line goes in right before the forced save overwrites the user's file — after the run-time path check, so it never announces a save that did not happen.
                        try { return _runner.Run(etabs, sapModel, request, compiled.Script!, verdict, maxTimeoutSeconds, progress, cancel, () => AuditStarted(request, verdict.Tier)); }
                        finally { _running = false; }
                    }),
                    cancel.Token);

                var result = await _queue.RunAsync(work).ConfigureAwait(false) as ExecuteResult
                             ?? ExecuteResult.Failure($"{HostName} returned no result for the script.");

                return Finish(request, result, stopwatch, verdict.Tier);
            }
            finally
            {
                _currentCancel = null;
                cancel.Dispose();
            }
        }
        catch (BridgeRequestException exception)
        {
            Finish(request, ExecuteResult.Failure(exception.Message), stopwatch);
            throw;
        }
        catch (OperationCanceledException)
        {
            return Finish(request, ExecuteResult.Failure("Script was cancelled before it started. Nothing ran."), stopwatch);
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
        // Answer at once instead of parking behind a running script: the server would time out and cancel it.
        if (IsBusy) throw BridgeRequestException.Busy(HostName);

        var work = new MainThreadWorkItem("context",
            _ => OnWorker(() => EtabsContextReader.Read(_attachment, includeSelection, _settings.ExecutionEnabled, _destructiveEnabled, _hostVersion, IsQuiescent())),
            cancellationToken);

        return (ContextResult)await _queue.RunAsync(work).ConfigureAwait(false);
    }

    public InspectResult Inspect(InspectRequest request) => _inspector.Inspect(request);

    /// <summary>Analysis never depends on the per-session opt-ins: a destructive member makes a proposal invalid, a `none` declaration on a writing script too.</summary>
    public AnalyzeResult Analyze(AnalyzeRequest request)
    {
        var result = ScriptAnalyzer.Run(_compiler, request.Code, GuardProfile.Etabs, AnalyzerProfile.Etabs);
        if (!result.Compiles) return result;

        var compiled = _compiler.GetOrCompile(request.Code);
        if (compiled.Script is null) return result;

        var verdict = _analyzer.Inspect(compiled.Script);
        var extra = new List<ScriptDiagnostic>(verdict.Refusals);

        if (verdict.Tier == EtabsTier.Destructive)
            extra.AddRange(verdict.Hits.Where(h => h.Tier == EtabsTier.Destructive)
                .Select(h => new ScriptDiagnostic(h.Line, h.Column, "DESTRUCTIVE", $"{h.Member} is destructive (unlock, analysis, file or delete): it needs the user's second opt-in on every run and cannot be stored as a tool.")));

        if (verdict.Tier >= EtabsTier.Write && string.Equals(TransactionModes.Normalize(request.Transaction), TransactionModes.None, StringComparison.Ordinal))
            extra.AddRange(EtabsTierAnalyzer.Preview(verdict.Hits, EtabsTier.Write)
                .Select(d => d with { Message = $"declared transaction: none, but {d.Message} writes — declare auto" }));

        if (extra.Count > 0) result.GuardViolations = extra.Concat(result.GuardViolations).ToArray();
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

    public void Dispose()
    {
        try { _currentCancel?.Cancel(); }
        catch (ObjectDisposedException) { /* the run finished and disposed its source between the read and the call */ }

        _stop = true;
        _wake.Set();
        if (Thread.CurrentThread != _worker && !_worker.Join(TimeSpan.FromSeconds(10)))
            Log.Warning("HPEtabs COM worker did not stop within 10 s (a call into ETABS is still blocking)");

        _queue.FailAll(new ObjectDisposedException("The MCP bridge is shutting down."));
        _attachment.Dispose();
        _audit.Dispose();
        _wake.Dispose();
    }
}
