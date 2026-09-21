using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using HPSap2000.McpBridge.Service;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.Core.Scripting;
using Serilog;

namespace HPSap2000.McpBridge;

/// <summary>
///     Moves work from the pipe thread onto the bridge's own STA worker, the one thread that holds the COM
///     attachment to SAP2000.
/// </summary>
public sealed partial class SapExecutor : IBridgeExecutor, IDisposable
{
    public const string HostName = "SAP2000";

    public const int DefaultMaxTimeoutSeconds = 120;

    private readonly BridgeSettings _settings;
    private readonly ScriptCompiler _compiler;
    private readonly SapTierAnalyzer _analyzer;
    private readonly SapScriptRunner _runner;
    private readonly SapAttachment _attachment;
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

    public SapExecutor(BridgeSettings settings, ScriptCompiler compiler, SapTierAnalyzer analyzer, SapScriptRunner runner, SapAttachment attachment,
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
        _queue = new MainThreadQueue(IsQuiescent, HostName, busyGrace, () => _wake.Set(), expireWithoutTicks: true);
        _attachment.StateChanged += () => StateChanged?.Invoke();

        _worker = new Thread(WorkerLoop) { Name = "HPSap2000 COM worker", IsBackground = false };
        _worker.Start();
    }

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

    public SapAttachment Attachment => _attachment;

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

            var guard = ScriptGuard.Check(request.Code, GuardProfile.Sap2000);
            if (guard.Count > 0) return Finish(request, Diagnostics("guard", guard), stopwatch);

            var compiled = _compiler.GetOrCompile(request.Code);
            if (!compiled.Succeeded) return Finish(request, Diagnostics("compile", compiled.Diagnostics), stopwatch);

            var verdict = _analyzer.Inspect(compiled.Script!);
            if (verdict.Refusals.Count > 0) return Finish(request, Diagnostics("path", verdict.Refusals), stopwatch);

            var mode = TransactionModes.Normalize(request.Transaction) ?? TransactionModes.Auto;
            if (verdict.Tier >= SapTier.Write && (request.DryRun || mode == TransactionModes.None)) return Finish(request, Preview(verdict.Hits), stopwatch);

            if (verdict.Tier == SapTier.Destructive && !_destructiveEnabled)
                throw new BridgeRequestException(BridgeErrorCode.ExecutionDisabled,
                    "Destructive operations are disabled. Ask the user to tick 'Allow destructive operations' in the HPSap2000 MCP Bridge window.");

            if (!_attachment.Attached) throw SapAttachment.NotAttached();

            var maxTimeoutSeconds = verdict.Tier == SapTier.Destructive ? HPRebar.Mcp.Contracts.HostScriptContracts.Sap2000HeavyMaxTimeoutSeconds : DefaultMaxTimeoutSeconds;

            var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _currentCancel = cancel;

            try
            {
                var work = new MainThreadWorkItem("execute: " + (request.Label ?? "script"),
                    _ => OnWorker(() =>
                    {
                        var (sap, sapModel) = _attachment.Require();
                        _running = true;
                        try { return _runner.Run(sap, sapModel, request, compiled.Script!, verdict, maxTimeoutSeconds, progress, cancel, () => AuditStarted(request, verdict.Tier)); }
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
        if (IsBusy) throw BridgeRequestException.Busy(HostName);

        var work = new MainThreadWorkItem("context",
            _ => OnWorker(() => SapContextReader.Read(_attachment, includeSelection, _settings.ExecutionEnabled, _destructiveEnabled, _hostVersion, IsQuiescent())),
            cancellationToken);

        return (ContextResult)await _queue.RunAsync(work).ConfigureAwait(false);
    }

    public InspectResult Inspect(InspectRequest request) => _inspector.Inspect(request);

    public AnalyzeResult Analyze(AnalyzeRequest request)
    {
        var result = ScriptAnalyzer.Run(_compiler, request.Code, GuardProfile.Sap2000, AnalyzerProfile.Sap2000);
        if (!result.Compiles) return result;

        var compiled = _compiler.GetOrCompile(request.Code);
        if (compiled.Script is null) return result;

        var verdict = _analyzer.Inspect(compiled.Script);
        var extra = new List<ScriptDiagnostic>(verdict.Refusals);

        if (verdict.Tier == SapTier.Destructive)
            extra.AddRange(verdict.Hits.Where(h => h.Tier == SapTier.Destructive)
                .Select(h => new ScriptDiagnostic(h.Line, h.Column, "DESTRUCTIVE", $"{h.Member} is destructive (unlock, analysis, file or delete): it needs the user's second opt-in on every run and cannot be stored as a tool.")));

        if (verdict.Tier >= SapTier.Write && string.Equals(TransactionModes.Normalize(request.Transaction), TransactionModes.None, StringComparison.Ordinal))
            extra.AddRange(SapTierAnalyzer.Preview(verdict.Hits, SapTier.Write)
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
        catch (ObjectDisposedException) { }

        _stop = true;
        _wake.Set();
        if (Thread.CurrentThread != _worker && !_worker.Join(TimeSpan.FromSeconds(10)))
            Log.Warning("HPSap2000 COM worker did not stop within 10 s");

        _queue.FailAll(new ObjectDisposedException("The MCP bridge is shutting down."));
        _attachment.Dispose();
        _audit.Dispose();
        _wake.Dispose();
    }
}
