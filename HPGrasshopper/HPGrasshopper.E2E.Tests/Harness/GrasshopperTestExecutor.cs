namespace HPGrasshopper.E2E.Tests.Harness;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.Core.Scripting;

public sealed class GrasshopperTestExecutor : ICorrelatedCancelExecutor
{
    private readonly object _syncLock = new();
    private readonly List<long> _committedElements = new();
    private readonly ConcurrentDictionary<long, CancellationTokenSource> _correlatedRuns = new();
    private int _isBusy;

    public bool IsBusy => Volatile.Read(ref _isBusy) == 1;
    public int CompiledScriptCount { get; private set; }
    public string? ActiveDocumentTitle { get; set; } = "ParametricTower.rvt";
    public bool IsExecutionEnabled { get; set; } = true;
    public int CommittedElementCount { get { lock (_syncLock) { return _committedElements.Count; } } }

    public event Action? StateChanged;
    public event Action<LastRunInfo>? RunCompleted;

    public Task<ExecuteResult> ExecuteAsync(ExecuteRequest request, IProgress<ScriptProgress>? progress, CancellationToken ct) =>
        ExecuteAsync(0L, request, progress, ct);

    public async Task<ExecuteResult> ExecuteAsync(long requestId, ExecuteRequest request, IProgress<ScriptProgress>? progress, CancellationToken ct)
    {
        if (!IsExecutionEnabled)
        {
            return ExecuteResult.Failure("Execution disabled. Allow AI Grasshopper execution in Revit.", rolledBack: false);
        }

        if (Interlocked.CompareExchange(ref _isBusy, 1, 0) != 0)
        {
            return ExecuteResult.Failure("Bridge is currently busy executing another operation.", rolledBack: false);
        }

        StateChanged?.Invoke();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (requestId > 0) _correlatedRuns[requestId] = linkedCts;

        try
        {
            return await ExecuteInternalAsync(request, linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return ExecuteResult.Failure("Execution cancelled by token.", rolledBack: true);
        }
        finally
        {
            if (requestId > 0) _correlatedRuns.TryRemove(requestId, out _);
            Interlocked.Exchange(ref _isBusy, 0);
            StateChanged?.Invoke();
        }
    }

    private async Task<ExecuteResult> ExecuteInternalAsync(ExecuteRequest request, CancellationToken ct)
    {
        var guardViolations = ScriptGuard.Check(request.Code, GuardProfile.Grasshopper);
        if (guardViolations.Count > 0)
        {
            return new ExecuteResult { IsError = true, Diagnostics = guardViolations, Message = "Script rejected by Grasshopper security guard." };
        }

        if (ct.IsCancellationRequested)
        {
            return ExecuteResult.Failure("Execution cancelled by token.", rolledBack: true);
        }

        CompiledScriptCount++;
        await Task.Delay(5, ct).ConfigureAwait(false);

        lock (_syncLock)
        {
            if (request.DryRun)
            {
                return new ExecuteResult { IsError = false, RolledBack = true, Value = JsonSerializer.SerializeToElement(new { status = "rolled_back" }), DurationMs = 10 };
            }

            _committedElements.Add(100000L + _committedElements.Count + 1);
            RunCompleted?.Invoke(new LastRunInfo(DateTimeOffset.UtcNow, request.Label ?? "Script", "Grasshopper", false, null, 15, false, _committedElements.Count, 0, 0));
            return new ExecuteResult { IsError = false, RolledBack = false, Value = JsonSerializer.SerializeToElement(new { status = "committed" }), DurationMs = 15 };
        }
    }

    public CancelResult Cancel() => Cancel(null, null);

    public CancelResult Cancel(long? targetRequestId) => Cancel(targetRequestId, null);

    public CancelResult Cancel(long? targetRequestId, string? sessionId)
    {
        if (targetRequestId.HasValue && targetRequestId.Value > 0)
        {
            if (_correlatedRuns.TryGetValue(targetRequestId.Value, out var cts))
            {
                cts.Cancel();
                return new CancelResult(true, true);
            }
            return new CancelResult(IsBusy, false);
        }

        bool wasBusy = IsBusy;
        foreach (var run in _correlatedRuns.Values) run.Cancel();
        return new CancelResult(wasBusy, wasBusy);
    }

    public AnalyzeResult Analyze(AnalyzeRequest request)
    {
        var result = ScriptAnalyzer.Analyze(request.Code, AnalyzerProfile.Grasshopper);
        result.GuardViolations = ScriptGuard.Check(request.Code, GuardProfile.Grasshopper);
        result.Compiles = result.GuardViolations.Count == 0;
        return result;
    }

    public InspectResult Inspect(InspectRequest request) => new() { TypeName = request.TypeName, FullName = "Grasshopper.Kernel." + request.TypeName };

    public Task<ContextResult> GetContextAsync(bool includeSelection, CancellationToken ct) =>
        Task.FromResult(new ContextResult { DocTitle = ActiveDocumentTitle, Grasshopper = new GrasshopperInfo(true, "8.12", "8.12", ActiveDocumentTitle, null, 12, 18, 0, false, true) });
}
