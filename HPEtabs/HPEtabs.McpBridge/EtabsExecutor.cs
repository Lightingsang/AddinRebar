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

    private readonly BridgeSettings _settings;
    private readonly ScriptCompiler _compiler;
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

    public EtabsExecutor(BridgeSettings settings, ScriptCompiler compiler, EtabsScriptRunner runner, EtabsAttachment attachment,
        TypeInspector inspector, AuditLogger audit, string hostVersion, TimeSpan busyGrace)
    {
        _settings = settings;
        _compiler = compiler;
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

    /// <summary>Runs a piece of work on the STA worker ahead of any queued script: Attach, Detach and the window's probes.</summary>
    public Task<object?> RunOnWorkerAsync(string name, Func<object?> work)
    {
        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _control.Enqueue((name, work, completion));
        _wake.Set();
        return completion.Task;
    }

    public Task AttachAsync() => RunOnWorkerAsync("attach", () => _attachment.Attach());

    public Task DetachAsync() => RunOnWorkerAsync("detach", () => { _attachment.Detach("user clicked Detach"); return null; });

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

            var (tier, hits) = EtabsTierGate.Inspect(request.Code);
            var maxTimeoutSeconds = _destructiveEnabled ? HPRebar.Mcp.Contracts.HostScriptContracts.EtabsHeavyMaxTimeoutSeconds : 120;

            // A destructive member with the second opt-in off is refused with the opt-in code, never counted as a run of the tool.
            if (tier == EtabsTier.Destructive && !_destructiveEnabled)
                throw new BridgeRequestException(BridgeErrorCode.ExecutionDisabled,
                    "Destructive operations are disabled. Ask the user to tick 'Allow destructive operations' in the HPEtabs MCP Bridge window.");

            // Writing tiers are previewed, never run, until the save-and-snapshot path exists: the AI sees exactly which members it would call.
            if (tier >= EtabsTier.Write) return Finish(request, Preview(hits), stopwatch);

            if (!_attachment.Attached) throw EtabsAttachment.NotAttached();

            var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _currentCancel = cancel;

            try
            {
                var work = new MainThreadWorkItem("execute: " + (request.Label ?? "script"),
                    _ => OnWorker(() =>
                    {
                        var (etabs, sapModel) = _attachment.Require();
                        _running = true;
                        try { return _runner.Run(etabs, sapModel, request, compiled.Script!, maxTimeoutSeconds, progress, cancel); }
                        finally { _running = false; }
                    }),
                    cancel.Token);

                var result = await _queue.RunAsync(work).ConfigureAwait(false) as ExecuteResult
                             ?? ExecuteResult.Failure($"{HostName} returned no result for the script.");

                return Finish(request, result, stopwatch);
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

    /// <summary>Runs OAPI work on the STA worker; a COM disconnect drops the attachment and becomes the "not attached" refusal.</summary>
    private object OnWorker(Func<object> work)
    {
        try
        {
            return work();
        }
        catch (Exception exception)
        {
            var refusal = _attachment.DetachIfGone(exception);
            if (refusal is not null) throw refusal;
            throw;
        }
    }

    public InspectResult Inspect(InspectRequest request) => _inspector.Inspect(request);

    /// <summary>Analysis never depends on the per-session opt-ins: a destructive member makes a proposal invalid, a `none` declaration on a writing script too.</summary>
    public AnalyzeResult Analyze(AnalyzeRequest request)
    {
        var result = ScriptAnalyzer.Run(_compiler, request.Code, GuardProfile.Etabs, AnalyzerProfile.Etabs);
        var (tier, hits) = EtabsTierGate.Inspect(request.Code);
        var extra = new List<ScriptDiagnostic>();

        if (tier == EtabsTier.Destructive)
            extra.AddRange(hits.Where(h => h.Tier == EtabsTier.Destructive)
                .Select(h => new ScriptDiagnostic(h.Line, h.Column, "DESTRUCTIVE", $"{h.Member} is destructive (unlock, analysis, file or delete): it needs the user's second opt-in on every run and cannot be stored as a tool.")));

        if (tier >= EtabsTier.Write && string.Equals(TransactionModes.Normalize(request.Transaction), TransactionModes.None, StringComparison.Ordinal))
            extra.AddRange(EtabsTierGate.Preview(hits, EtabsTier.Write)
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

    /// <summary>
    ///     Not attached counts as quiescent so the queued work reaches the tick and fails fast; a disabled main window
    ///     means a modal dialog; a handle that no longer names a window (ETABS re-created it) must not read as busy forever.
    /// </summary>
    private bool IsQuiescent()
    {
        if (_running) return false;
        if (!_attachment.Attached) return true;
        var handle = _attachment.MainWindowHandle;
        return handle == IntPtr.Zero || !IsWindow(handle) || IsWindowEnabled(handle);
    }

    private void WorkerLoop()
    {
        Log.Debug("HPEtabs COM worker started (STA={Sta})", Thread.CurrentThread.GetApartmentState() == ApartmentState.STA);

        while (!_stop)
        {
            _wake.WaitOne(TimeSpan.FromMilliseconds(250));
            DrainControlLane();
            try { _queue.OnTick(); }
            catch (Exception exception) { Log.Error(exception, "MCP bridge worker tick failed"); }
        }

        DrainControlLane();
        Log.Debug("HPEtabs COM worker stopped");
    }

    private void DrainControlLane()
    {
        while (_control.TryDequeue(out var item))
        {
            try { item.completion.TrySetResult(item.work()); }
            catch (Exception exception)
            {
                Log.Warning(exception, "MCP bridge control lane '{Name}' failed", item.name);
                item.completion.TrySetException(exception);
            }
            StateChanged?.Invoke();
        }
    }

    [DllImport("user32.dll", SetLastError = false)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowEnabled(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = false)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(IntPtr hWnd);

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
