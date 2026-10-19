namespace HPDynamo.McpBridge.Coordinator;

using System; using System.Collections.Concurrent; using System.Diagnostics; using System.Threading; using System.Threading.Tasks;
using Autodesk.Revit.UI; using Dynamo.Graph.Workspaces; using Dynamo.Models; using Dynamo.ViewModels; using Dynamo.Wpf.Extensions;
using HPDynamo.McpBridge.Execution; using HPDynamo.McpBridge.Support; using Serilog;
using HPRebar.Mcp.Contracts; using HPRebar.Mcp.Contracts.JsonRpc; using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model; using HPRebar.McpBridge.Core.Pipe; using HPRebar.McpBridge.Core.Scripting;

/// <summary>
///     Coordinates between Revit (headless) and Dynamo ViewExtension (live canvas) lifecycles.
///     Implements ICorrelatedCancelExecutor to serve JSON-RPC requests across the named pipe.
/// </summary>
public sealed class DynamoBridgeCoordinator : ICorrelatedCancelExecutor, IDisposable
{
    private static readonly Lazy<DynamoBridgeCoordinator> LazyInstance =
        new(() => new DynamoBridgeCoordinator());

    private readonly ConcurrentQueue<McpBridgeWorkRequest> _pending = new();
    private readonly ConcurrentDictionary<McpBridgeWorkRequest, WorkItemTracker> _workTrackers = new(ReferenceEqualityComparer.Instance);
    private readonly HeadlessGraphRunner _headlessRunner = new();
    private readonly CanvasGraphRunner _canvasRunner = new();
    private readonly DynamoExecutionEngine _executionEngine;
    private readonly TypeInspector _inspector = new(DynamoCompilerReferences.All);

    private BridgeSettings? _settings;
    private IExternalEventRaiser? _externalEvent;
    private ScriptCompiler? _compiler;
    private int _busy;
    private string? _activeDocumentTitle;
    private volatile CancellationTokenSource? _currentCancel;
    private (string? SessionId, long RequestId) _activeRequest;
    private bool _isCancelling;
    private readonly object _cancelLock = new();

    private DynamoViewModel? _liveViewModel;
    private WorkspaceModel? _liveWorkspace;
    private ViewLoadedParams? _liveViewParams;

    private DynamoBridgeCoordinator() =>
        _executionEngine = new DynamoExecutionEngine(this, _headlessRunner, _canvasRunner);

    public static DynamoBridgeCoordinator Instance => LazyInstance.Value;

    internal ScriptCompiler? Compiler => _compiler;
    internal Func<ExecuteRequest, Task<ExecuteResult>>? ExecutionHook { get; set; }

    public bool IsLiveCanvasAttached => _liveViewModel != null && _liveWorkspace != null;
    public bool IsBusy => Volatile.Read(ref _busy) == 1;
    public int CompiledScriptCount => _compiler?.CompiledCount ?? 0;
    public string? ActiveDocumentTitle => _activeDocumentTitle;
    public DynamoViewModel? ActiveViewModel => _liveViewModel;
    public WorkspaceModel? ActiveWorkspace => _liveWorkspace;
    internal long ActiveRequestId { get { lock (_cancelLock) return _activeRequest.RequestId; } }
    internal string? ActiveSessionId { get { lock (_cancelLock) return _activeRequest.SessionId; } }

    public event Action? StateChanged;
    public event Action<LastRunInfo>? RunCompleted;

    public void Initialize(BridgeSettings settings, IExternalEventRaiser? externalEvent = null)
    {
        _settings = settings; _externalEvent = externalEvent;
        var refs = DynamoCompilerReferences.All;
        _compiler = new ScriptCompiler(refs, DynamoCompilerReferences.ImportsFor(refs), typeof(DynamoScriptGlobals), settings.ScriptCacheSize);
    }

    public void RegisterLiveCanvas(ViewLoadedParams loadedParams, DynamoViewModel? vm, WorkspaceModel? ws)
    {
        _liveViewParams = loadedParams; _liveViewModel = vm; _liveWorkspace = ws;
        StateChanged?.Invoke();
        Log.Information("DynamoBridgeCoordinator: Live canvas registered (File: {File})", ws?.FileName);
    }

    public void UnregisterLiveCanvas()
    {
        _liveViewParams = null; _liveViewModel = null; _liveWorkspace = null;
        StateChanged?.Invoke();
        Log.Information("DynamoBridgeCoordinator: Live canvas unregistered");
    }

    public void UpdateActiveWorkspace(WorkspaceModel? ws) { _liveWorkspace = ws; StateChanged?.Invoke(); }
    public void SetActiveDocumentTitle(string? title) { if (_activeDocumentTitle != title) { _activeDocumentTitle = title; StateChanged?.Invoke(); } }
    public void ShowStatusDialog() { Log.Information("DynamoBridgeCoordinator: Requesting status window display"); Entry.ShowStatusDialogCommand.ShowWindow(); }

    public Task<ExecuteResult> ExecuteAsync(ExecuteRequest request, IProgress<ScriptProgress>? progress, CancellationToken ct) =>
        ExecuteAsync(0, request, progress, ct);

    public async Task<ExecuteResult> ExecuteAsync(
        long requestId,
        ExecuteRequest request,
        IProgress<ScriptProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
            return ExecuteResult.Failure("Another Dynamo script or graph is still running.");

        lock (_cancelLock) { _activeRequest = (request.SessionId, requestId); }
        var sw = Stopwatch.StartNew();
        StateChanged?.Invoke();

        try
        {
            using var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            lock (_cancelLock) { _currentCancel = cancel; }
            try
            {
                var result = await DispatchExecutionAsync(request, progress, cancel).ConfigureAwait(false);
                return Finish(request, result, sw);
            }
            finally
            {
                lock (_cancelLock)
                {
                    _currentCancel = null;
                    _activeRequest = (null, 0);
                }
            }
        }
        catch (OperationCanceledException)
        {
            var res = ExecuteResult.Failure("Execution was cancelled.");
            res.RolledBack = true;
            return Finish(request, res, sw);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Execution failed in DynamoBridgeCoordinator");
            return Finish(request, ExecuteResult.Failure(SafeText.StripPaths($"{ex.GetType().Name}: {ex.Message}")), sw);
        }
        finally
        {
            try { DrainDynamoScheduler(); } catch { /* best effort */ }
            Volatile.Write(ref _busy, 0);
            StateChanged?.Invoke();
        }
    }

    private async Task<ExecuteResult> DispatchExecutionAsync(
        ExecuteRequest request,
        IProgress<ScriptProgress>? progress,
        CancellationTokenSource cancel)
    {
        if (ExecutionHook != null)
            return await ExecutionHook(request).ConfigureAwait(false);

        if (_settings?.ExecutionEnabled == false)
            return ExecuteResult.Failure("Execution is disabled in bridge settings.");

        return await _executionEngine.DispatchAsync(request, _settings ?? new BridgeSettings(), progress, cancel).ConfigureAwait(false);
    }

    private ExecuteResult Finish(ExecuteRequest request, ExecuteResult result, Stopwatch sw)
    {
        result.DurationMs = sw.ElapsedMilliseconds;
        RunCompleted?.Invoke(new LastRunInfo(DateTimeOffset.UtcNow, request.Label ?? "Script", "Dynamo", result.IsError, result.Message, result.DurationMs, result.RolledBack, 0, 0, 0));
        return result;
    }

    private void DrainDynamoScheduler()
    {
        if (ExecutionHook != null) return;
        try
        {
            var model = _liveViewModel?.Model ?? TryGetHeadlessModel();
            if (model?.Scheduler == null || (!model.Scheduler.HasTaskInProgress && !model.Scheduler.HasPendingTasks)) return;

            var sw = Stopwatch.StartNew();
            while ((model.Scheduler.HasTaskInProgress || model.Scheduler.HasPendingTasks) && sw.ElapsedMilliseconds < 500)
                Thread.Sleep(10);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to drain Dynamo scheduler in coordinator cleanup");
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static DynamoModel? TryGetHeadlessModel()
    {
        try { return Dynamo.Applications.DynamoRevit.RevitDynamoModel as DynamoModel; }
        catch { return null; }
    }

    public string GetName() => "HPDynamoBridgeCoordinator";

    internal async Task<object?> RunOnRevitThreadAsync(McpBridgeWorkRequest work)
    {
        if (ExecutionHook != null)
            return await ExecutionHook(new ExecuteRequest(work.Name, "none")).ConfigureAwait(false);

        var tracker = new WorkItemTracker();
        _workTrackers[work] = tracker;
        _pending.Enqueue(work);

        try
        {
            _externalEvent?.Raise();
            return await work.Completion.Task.ConfigureAwait(false);
        }
        finally
        {
            _workTrackers.TryRemove(work, out _);
        }
    }

    public void Execute(UIApplication application)
    {
        while (_pending.TryDequeue(out var work))
        {
            if (work.CancellationToken.IsCancellationRequested)
            {
                work.Completion.TrySetCanceled(work.CancellationToken);
                continue;
            }

            if (_workTrackers.TryGetValue(work, out var tracker))
                tracker.TryTransitionToRunning();

            try
            {
                var result = work.Work(application, work.CancellationToken);
                work.Completion.TrySetResult(result);
            }
            catch (OperationCanceledException oce)
            {
                work.Completion.TrySetCanceled(oce.CancellationToken);
            }
            catch (Exception ex)
            {
                work.Completion.TrySetException(ex);
            }
            finally
            {
                tracker?.TryTransitionToCompleted();
            }
        }
    }

    internal WorkItemState GetWorkItemState(McpBridgeWorkRequest request) =>
        _workTrackers.TryGetValue(request, out var tracker) ? tracker.State : WorkItemState.Completed;

    public CancelResult Cancel() => Cancel(null, null);
    public CancelResult Cancel(long? targetRequestId) => Cancel(targetRequestId, null);
    public CancelResult Cancel(long? targetRequestId, string? sessionId)
    {
        lock (_cancelLock)
        {
            if (_isCancelling) return new CancelResult(false, IsBusy);
            if (!IsBusy) return new CancelResult(false, false);

            if (targetRequestId.HasValue && targetRequestId.Value != 0 && targetRequestId.Value != _activeRequest.RequestId)
                return new CancelResult(false, true);

            if (!string.IsNullOrEmpty(sessionId) && !string.IsNullOrEmpty(_activeRequest.SessionId) && sessionId != _activeRequest.SessionId)
                return new CancelResult(false, true);

            _isCancelling = true;
            try
            {
                _currentCancel?.Cancel();
                return new CancelResult(true, true);
            }
            finally
            {
                _isCancelling = false;
            }
        }
    }

    public Task<ContextResult> GetContextAsync(bool includeSelection, CancellationToken cancellationToken)
    {
        var context = DynamoContextExtractor.Extract(null, this, includeSelection, _settings?.ExecutionEnabled ?? true);
        return Task.FromResult(context);
    }

    public InspectResult Inspect(InspectRequest request) => _inspector.Inspect(request);

    public AnalyzeResult Analyze(AnalyzeRequest request) =>
        _compiler != null
            ? ScriptAnalyzer.Run(_compiler, request.Code, GuardProfile.Dynamo, AnalyzerProfile.Dynamo)
            : ScriptAnalyzer.Analyze(request.Code, AnalyzerProfile.Dynamo);

    public void Dispose()
    {
        while (_pending.TryDequeue(out var item))
        {
            if (_workTrackers.TryRemove(item, out var tracker))
                tracker.TryCancelIfQueued(item);
            else
                item.Completion.TrySetCanceled();
        }
        _workTrackers.Clear();
    }
}
