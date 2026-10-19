namespace HPGrasshopper.McpBridge.Coordinator;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using HPGrasshopper.McpBridge.Execution;
using HPGrasshopper.McpBridge.Support;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.Core.Scripting;
using ScriptAnalyzer = HPGrasshopper.McpBridge.Execution.ScriptAnalyzer;
using Serilog;

/// <summary>
///     Coordinates execution between Revit and Grasshopper lifecycles.
///     Implements ICorrelatedCancelExecutor to serve JSON-RPC requests across named pipe.
/// </summary>
public sealed class GrasshopperBridgeCoordinator : ICorrelatedCancelExecutor, IDisposable
{
    private static readonly Lazy<GrasshopperBridgeCoordinator> LazyInstance =
        new(() => new GrasshopperBridgeCoordinator());

    private readonly GrasshopperExecutionEngine _executionEngine;
    private readonly GrasshopperCompilerLifecycle _compilerLifecycle = new();
    private readonly CoordinatorWorkQueue _workQueue = new();
    private ScriptCompiler? _compiler;
    private TypeInspector? _inspector;
    private BridgeSettings? _settings;
    private IExternalEventRaiser? _externalEvent;
    private int _busy;
    private string? _activeDocumentTitle;
    private volatile CancellationTokenSource? _currentCancel;
    private (string? SessionId, long RequestId) _activeRequest;
    private bool _isCancelling;
    private readonly object _cancelLock = new();

    private GrasshopperBridgeCoordinator() =>
        _executionEngine = new GrasshopperExecutionEngine(this);

    public static GrasshopperBridgeCoordinator Instance => LazyInstance.Value;

    internal ScriptCompiler? Compiler => _compiler = _compilerLifecycle.GetCompiler(_settings);
    internal Func<ExecuteRequest, Task<ExecuteResult>>? ExecutionHook { get; set; }
    public bool IsBusy => Volatile.Read(ref _busy) == 1;
    public int CompiledScriptCount => _compilerLifecycle.CompiledScriptCount;
    public string? ActiveDocumentTitle => _activeDocumentTitle;
    public string? ActiveDocumentPath { get; private set; }
    public Guid? ActiveDefinitionId { get; private set; }
    public string? ActiveDefinitionName { get; private set; }
    public string? ActiveDefinitionPath { get; private set; }
    public BridgeSettings? Settings => _settings;
    public ContextResult? CachedUiContext { get; private set; }
    public bool IsContextStale { get; private set; }
    public bool IsContextUnavailable { get; private set; }
    public DateTimeOffset? LastSnapshotTimestamp { get; private set; }
    internal long ActiveRequestId { get { lock (_cancelLock) return _activeRequest.RequestId; } }
    internal string? ActiveSessionId { get { lock (_cancelLock) return _activeRequest.SessionId; } }

    internal bool TryAcquireBusy()
    {
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) return false;
        StateChanged?.Invoke();
        return true;
    }

    internal void ReleaseBusy() { Volatile.Write(ref _busy, 0); StateChanged?.Invoke(); }

    public event Action? StateChanged;
    public event Action<LastRunInfo>? RunCompleted;

    public void Initialize(BridgeSettings settings, IExternalEventRaiser? externalEvent = null) { _settings = settings; _externalEvent = externalEvent; }

    public void InvalidateCompiler()
    {
        _compilerLifecycle.InvalidateCompiler();
        _compiler = null;
        _inspector = null;
    }

    public void MarkContextStale() => IsContextStale = true;
    public void MarkContextUnavailable() { IsContextUnavailable = true; IsContextStale = true; }
    public void MarkContextFresh() { IsContextStale = false; IsContextUnavailable = false; LastSnapshotTimestamp = DateTimeOffset.UtcNow; }

    public void UpdateCachedUiContext(ContextResult? context)
    {
        CachedUiContext = context;
        if (context != null)
        {
            LastSnapshotTimestamp = DateTimeOffset.UtcNow;
            IsContextStale = false;
            IsContextUnavailable = false;
        }
        else
        {
            IsContextUnavailable = true;
        }
    }

    public void SetActiveDocumentTitle(string? title) =>
        UpdateActiveSnapshot(title, ActiveDocumentPath, ActiveDefinitionId, ActiveDefinitionName, ActiveDefinitionPath);

    public void UpdateActiveSnapshot(string? revitTitle, string? revitPath, Guid? ghDefId, string? ghDefName = null, string? ghDefPath = null)
    {
        _activeDocumentTitle = string.IsNullOrWhiteSpace(revitTitle) ? null : revitTitle;
        ActiveDocumentPath = string.IsNullOrWhiteSpace(revitPath) ? null : revitPath;
        ActiveDefinitionId = ghDefId;
        ActiveDefinitionName = string.IsNullOrWhiteSpace(ghDefName) ? null : ghDefName;
        ActiveDefinitionPath = string.IsNullOrWhiteSpace(ghDefPath) ? null : ghDefPath;
        StateChanged?.Invoke();
    }

    public Task<ExecuteResult> ExecuteAsync(ExecuteRequest request, IProgress<ScriptProgress>? progress, CancellationToken ct) =>
        ExecuteAsync(0, request, progress, ct);

    public async Task<ExecuteResult> ExecuteAsync(long requestId, ExecuteRequest request, IProgress<ScriptProgress>? progress, CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
            return ExecuteResult.Failure("Another Grasshopper script or task is still running.");

        lock (_cancelLock) { _activeRequest = (request.SessionId, requestId); }
        var sw = Stopwatch.StartNew();
        StateChanged?.Invoke();
        try { return await ExecuteCoreAsync(request, progress, cancellationToken, sw).ConfigureAwait(false); }
        finally { Volatile.Write(ref _busy, 0); StateChanged?.Invoke(); }
    }

    private async Task<ExecuteResult> ExecuteCoreAsync(
        ExecuteRequest request,
        IProgress<ScriptProgress>? progress,
        CancellationToken cancellationToken,
        Stopwatch sw)
    {
        var timeoutSec = Math.Clamp(request.TimeoutSeconds > 0 ? request.TimeoutSeconds : 60, 5, 600);
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSec));
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        lock (_cancelLock) { _currentCancel = cancel; }

        try
        {
            var result = await DispatchExecutionAsync(request, progress, cancel).ConfigureAwait(false);
            return Finish(request, result, sw);
        }
        catch (OperationCanceledException)
        {
            var isTimeout = timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested;
            var res = ExecuteResult.Failure(isTimeout
                ? $"Execution timed out after {timeoutSec}s without host tick."
                : "Execution was cancelled.");
            res.TimedOut = isTimeout;
            res.RolledBack = true;
            return Finish(request, res, sw);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Execution failed in GrasshopperBridgeCoordinator");
            return Finish(request, ExecuteResult.Failure(SafeText.StripPaths($"{ex.GetType().Name}: {ex.Message}")), sw);
        }
        finally
        {
            lock (_cancelLock) { _currentCancel = null; _activeRequest = (null, 0); }
        }
    }

    private async Task<ExecuteResult> DispatchExecutionAsync(
        ExecuteRequest request,
        IProgress<ScriptProgress>? progress,
        CancellationTokenSource cancel)
    {
        if (ExecutionHook != null)
            return await ExecutionHook(request).ConfigureAwait(false);

        if (_settings?.ExecutionEnabled == false && !IsReadTool(request))
            return ExecuteResult.Failure("Execution is disabled in bridge settings.");

        return await _executionEngine.DispatchAsync(request, _settings ?? new BridgeSettings(), progress, cancel).ConfigureAwait(false);
    }

    private static bool IsReadTool(ExecuteRequest request) =>
        string.Equals(request.Label, "read_grasshopper_canvas", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(request.Label, "export_grasshopper_canvas_image", StringComparison.OrdinalIgnoreCase);

    private ExecuteResult Finish(ExecuteRequest request, ExecuteResult result, Stopwatch sw)
    {
        result.DurationMs = sw.ElapsedMilliseconds;
        RunCompleted?.Invoke(new LastRunInfo(DateTimeOffset.UtcNow, request.Label ?? "Script", "Grasshopper", result.IsError, result.Message, result.DurationMs, result.RolledBack, 0, 0, 0));
        return result;
    }

    public string GetName() => "HPGrasshopperBridgeCoordinator";

    internal Task<object?> RunOnRevitThreadAsync(McpBridgeWorkRequest work) =>
        _workQueue.RunOnRevitThreadAsync(work, _externalEvent, ExecutionHook);

    public void Execute(UIApplication application) => _workQueue.Execute(application);

    internal WorkItemState GetWorkItemState(McpBridgeWorkRequest request) => _workQueue.GetWorkItemState(request);

    public CancelResult Cancel() => Cancel(null, null);
    public CancelResult Cancel(long? targetRequestId) => Cancel(targetRequestId, null);
    public CancelResult Cancel(long? targetRequestId, string? sessionId)
    {
        lock (_cancelLock)
        {
            if (_isCancelling) return new CancelResult(false, IsBusy);
            if (!IsBusy) return new CancelResult(false, false);
            if (targetRequestId.HasValue && targetRequestId.Value != 0 && targetRequestId.Value != _activeRequest.RequestId) return new CancelResult(false, true);
            if (!string.IsNullOrEmpty(sessionId) && !string.IsNullOrEmpty(_activeRequest.SessionId) && sessionId != _activeRequest.SessionId) return new CancelResult(false, true);

            _isCancelling = true;
            try
            {
                var wasCancelled = _currentCancel != null;
                _currentCancel?.Cancel();
                return new CancelResult(wasCancelled, true);
            }
            finally { _isCancelling = false; }
        }
    }

    public async Task<ContextResult> GetContextAsync(bool includeSelection, CancellationToken cancellationToken)
    {
        if (_externalEvent == null)
            return GrasshopperContextExtractor.Extract(null, this, includeSelection, _settings?.ExecutionEnabled ?? true);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromMilliseconds(500));
        var work = new McpBridgeWorkRequest("get_context",
            (uiapp, _) => GrasshopperContextExtractor.Extract(uiapp, this, includeSelection, _settings?.ExecutionEnabled ?? true), cts.Token);

        try
        {
            var res = await RunOnRevitThreadAsync(work).ConfigureAwait(false);
            return res as ContextResult ?? GrasshopperContextExtractor.Extract(null, this, includeSelection, _settings?.ExecutionEnabled ?? true);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "GetContextAsync fell back to background context extraction");
            return GrasshopperContextExtractor.Extract(null, this, includeSelection, _settings?.ExecutionEnabled ?? true);
        }
    }

    public InspectResult Inspect(InspectRequest request)
    {
        _inspector = _compilerLifecycle.GetInspector();
        return _compilerLifecycle.Inspect(request);
    }

    public AnalyzeResult Analyze(AnalyzeRequest request) =>
        Compiler != null
            ? ScriptAnalyzer.Run(Compiler, request.Code, GuardProfile.Grasshopper, AnalyzerProfile.Grasshopper)
            : ScriptAnalyzer.Analyze(request.Code, AnalyzerProfile.Grasshopper);

    public void Dispose()
    {
        _workQueue.Dispose();
        _compilerLifecycle.Dispose();
    }
}
