namespace HPGrasshopper.E2E.Tests.Harness;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPGrasshopper.E2E.Tests.Contracts;
using HPGrasshopper.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.Core.Scripting;

public class GrasshopperE2EBridge : ICorrelatedCancelExecutor, IDisposable
{
    private readonly ScriptCompiler _compiler;
    private readonly object _syncLock = new();
    private int _busy;
    private readonly ConcurrentCancellationMap _cancelMap = new();

    public BridgeSettings Settings { get; } = new() { ExecutionEnabled = false };
    public GrasshopperStateLedger Ledger { get; } = new();
    public GrasshopperCanvasSpec ActiveCanvas { get; private set; }
    public bool IsExecutionEnabled { get => Settings.ExecutionEnabled; set => Settings.ExecutionEnabled = value; }
    public bool IsLiveCanvasAttached { get; private set; } = true;
    public bool IsRhinoInsideActive { get; set; } = true;
    public bool IsBusy => Volatile.Read(ref _busy) == 1;
    public int CompiledScriptCount => _compiler.CompiledCount;
    public string? ActiveDocumentTitle { get; set; } = "ParametricTower.gh";

    public event Action? StateChanged;
    public event Action<LastRunInfo>? RunCompleted;

    public GrasshopperE2EBridge()
    {
        ActiveCanvas = new GrasshopperCanvasSpec(
            Guid.NewGuid(), "ParametricTower.gh", "Active Workspace",
            new List<GrasshopperComponentSpec>(), new List<GrasshopperWireSpec>());

        _compiler = new ScriptCompiler(
            new[] { typeof(object).Assembly, typeof(Enumerable).Assembly, typeof(List<>).Assembly, typeof(JsonElement).Assembly, typeof(ScriptArgs).Assembly, typeof(GrasshopperTestGlobals).Assembly },
            new[] { "System", "System.Linq", "System.Collections.Generic", "System.Text.Json", "HPRebar.McpBridge.Core.Scripting", "HPGrasshopper.E2E.Tests.Harness" },
            typeof(GrasshopperTestGlobals),
            cacheSize: 50);
    }

    public void AttachLiveCanvas(string name = "ParametricTower.gh")
    {
        lock (_syncLock)
        {
            IsLiveCanvasAttached = true;
            ActiveCanvas = new GrasshopperCanvasSpec(Guid.NewGuid(), name, "Live Workspace", new List<GrasshopperComponentSpec>(), new List<GrasshopperWireSpec>());
        }
    }

    public void DetachLiveCanvas() { lock (_syncLock) { IsLiveCanvasAttached = false; } }

    public int GetCommittedElementCount() => Ledger.CommittedCount;

    public GrasshopperInfoRecord GetContext()
    {
        lock (_syncLock)
        {
            return new GrasshopperInfoRecord(
                true, ActiveCanvas.Name, "C:\\Grasshopper\\" + ActiveCanvas.Name,
                ActiveCanvas.Components.Count, ActiveCanvas.Wires.Count, "Solved",
                "8.12.24275.14001", "2026.0", ActiveCanvas.HasUnsavedChanges,
                IsExecutionEnabled, IsLiveCanvasAttached, IsRhinoInsideActive);
        }
    }

    public GrasshopperScriptQualityResult ValidateScript(string code)
    {
        var trimmed = code.Trim();
        int lineCount = trimmed.Length == 0 ? 0 : trimmed.Split('\n').Length;
        var errors = new List<string>();
        var warnings = new List<string>();
        foreach (var d in ScriptGuard.Check(code, GuardProfile.Grasshopper)) errors.Add(d.Message);
        foreach (var f in ScriptQuality.Find(code))
        {
            if (f.Severity == QualityFinding.Error) errors.Add($"{f.RuleId}: {f.Message}");
            else warnings.Add($"{f.RuleId}: {f.Message}");
        }
        return new GrasshopperScriptQualityResult(errors.Count == 0, lineCount, errors, warnings);
    }

    public Task<ExecuteResult> ExecuteAsync(ExecuteRequest req, IProgress<ScriptProgress>? p, CancellationToken ct) => ExecuteAsync(0L, req, p, ct);

    public async Task<ExecuteResult> ExecuteAsync(long requestId, ExecuteRequest request, IProgress<ScriptProgress>? progress, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return new ExecuteResult { IsError = true, RolledBack = true, TimedOut = true, Message = "Execution cancelled." };
        if (!Settings.ExecutionEnabled) return ExecuteResult.Failure(GrasshopperE2EContracts.OptInDisabledMessageFragment + " in the HP MCP Bridge window.", rolledBack: false);

        var guardViolations = ScriptGuard.Check(request.Code, GuardProfile.Grasshopper);
        if (guardViolations.Count > 0) return new ExecuteResult { IsError = true, Diagnostics = guardViolations, RolledBack = true, Message = "Guard violation." };

        var findings = ScriptQuality.Find(request.Code);
        var errs = findings.Where(f => f.Severity == QualityFinding.Error).Select(f => f.Message).ToList();
        if (errs.Count > 0) return new ExecuteResult { IsError = true, RolledBack = true, Message = string.Join("; ", errs) };

        return await RunRoslynAsync(requestId, request, progress, ct).ConfigureAwait(false);
    }

    private async Task<ExecuteResult> ExecuteCompiledScriptAsync(ExecuteRequest request, IProgress<ScriptProgress>? progress, CancellationToken ct, Stopwatch sw)
    {
        var outcome = _compiler.GetOrCompile(request.Code);
        if (!outcome.Succeeded)
        {
            Ledger.RollbackTransaction();
            return new ExecuteResult { IsError = true, Diagnostics = outcome.Diagnostics, RolledBack = true, Message = "Compilation failed." };
        }

        var globals = CreateGlobals(request, progress, ct);
        var state = await outcome.Script!.RunAsync(globals, ct).ConfigureAwait(false);
        sw.Stop();
        return FinalizeSuccess(request, globals, state.ReturnValue, sw.ElapsedMilliseconds);
    }

    private async Task<ExecuteResult> RunRoslynAsync(long requestId, ExecuteRequest request, IProgress<ScriptProgress>? progress, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        Volatile.Write(ref _busy, 1);
        StateChanged?.Invoke();
        Ledger.BeginTransaction();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (requestId > 0) _cancelMap.Register(requestId, linkedCts);

        if (linkedCts.Token.IsCancellationRequested)
        {
            Ledger.RollbackTransaction();
            return new ExecuteResult { IsError = true, RolledBack = true, TimedOut = true, Message = "Execution cancelled.", DurationMs = sw.ElapsedMilliseconds };
        }

        try
        {
            return await ExecuteCompiledScriptAsync(request, progress, linkedCts.Token, sw).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Ledger.RollbackTransaction();
            return new ExecuteResult { IsError = true, RolledBack = true, TimedOut = true, Message = "Execution cancelled.", DurationMs = sw.ElapsedMilliseconds };
        }
        catch (Exception ex)
        {
            Ledger.RollbackTransaction();
            return new ExecuteResult { IsError = true, RolledBack = true, Message = ex.InnerException?.Message ?? ex.Message, DurationMs = sw.ElapsedMilliseconds };
        }
        finally
        {
            if (requestId > 0) _cancelMap.Unregister(requestId);
            Volatile.Write(ref _busy, 0);
            StateChanged?.Invoke();
        }
    }

    private GrasshopperTestGlobals CreateGlobals(ExecuteRequest req, IProgress<ScriptProgress>? prog, CancellationToken ct)
    {
        var g = new GrasshopperTestGlobals { ct = ct, dryRun = req.DryRun, args = new ScriptArgs(req.Args), Ledger = Ledger, doc = ActiveCanvas, canvas = ActiveCanvas };
        g.log = msg => { if (msg != null) g.Logs.Add(msg); };
        g.progress = (cur, tot, msg) => prog?.Report(new ScriptProgress(cur, tot ?? 100, msg));
        return g;
    }

    private ExecuteResult FinalizeSuccess(ExecuteRequest request, GrasshopperTestGlobals globals, object? returnValue, long durationMs)
    {
        bool isRollback = request.DryRun || string.Equals(request.Transaction, TransactionModes.None, StringComparison.OrdinalIgnoreCase);
        if (isRollback)
        {
            Ledger.RollbackTransaction();
            return new ExecuteResult { IsError = false, RolledBack = true, DurationMs = durationMs, Logs = globals.Logs, Value = JsonSerializer.SerializeToElement(returnValue), Changed = new ChangedCounts(0, 0, 0) };
        }

        var committed = Ledger.CommitTransaction();
        var result = new ExecuteResult { IsError = false, RolledBack = false, DurationMs = durationMs, Logs = globals.Logs, Value = JsonSerializer.SerializeToElement(returnValue), Changed = new ChangedCounts(committed.Count, 0, 0) };
        RunCompleted?.Invoke(new LastRunInfo(DateTimeOffset.UtcNow, request.Label ?? "Script", "Grasshopper", false, null, durationMs, false, committed.Count, 0, 0));
        return result;
    }

    public async Task<GrasshopperExecutionResult> ExecuteAsync(GrasshopperExecutionRequest request, CancellationToken ct = default)
    {
        JsonElement? argsElem = null;
        if (!string.IsNullOrWhiteSpace(request.ArgsJson))
        {
            using var parsed = JsonDocument.Parse(request.ArgsJson);
            argsElem = parsed.RootElement.Clone();
        }

        var execReq = new ExecuteRequest(request.Code, request.Transaction, request.DryRun, request.TimeoutSeconds, Args: argsElem);
        var result = await ExecuteAsync(0L, execReq, null, ct).ConfigureAwait(false);

        if (result.IsError)
        {
            int errCode = !Settings.ExecutionEnabled ? GrasshopperE2EContracts.OptInDisabledErrorCode : -32000;
            if (result.TimedOut) errCode = -32003;
            else if (result.Diagnostics.Count > 0) errCode = -32002;

            return new GrasshopperExecutionResult(false, null, result.RolledBack, Array.Empty<long>(), result.Logs, result.DurationMs, result.Message, errCode);
        }

        return new GrasshopperExecutionResult(true, result.Value?.GetRawText() ?? "{}", result.RolledBack, Ledger.CommittedElements, result.Logs, result.DurationMs);
    }

    public Task<ContextResult> GetContextAsync(bool includeSelection, CancellationToken ct) =>
        Task.FromResult(new ContextResult
        {
            Host = PipeNaming.GrasshopperHost,
            RevitVersion = "2026",
            DocTitle = ActiveDocumentTitle,
            Grasshopper = new GrasshopperInfo(true, "8.12.24275.14001", "8.12.24275.14001", ActiveDocumentTitle, "C:\\Grasshopper\\" + ActiveDocumentTitle, ActiveCanvas.Components.Count, ActiveCanvas.Wires.Count, includeSelection ? 1 : 0, false, IsRhinoInsideActive)
        });

    public AnalyzeResult Analyze(AnalyzeRequest request) => ScriptAnalyzer.Run(_compiler, request.Code, GuardProfile.Grasshopper, AnalyzerProfile.Grasshopper);

    public InspectResult Inspect(InspectRequest request) => new() { TypeName = request.TypeName, FullName = "Grasshopper.Kernel." + request.TypeName, Members = Array.Empty<MemberSignature>() };

    public CancelResult Cancel() => Cancel(null, null);

    public CancelResult Cancel(long? targetRequestId) => Cancel(targetRequestId, null);

    public CancelResult Cancel(long? targetRequestId, string? sessionId)
    {
        bool wasBusy = IsBusy;
        if (targetRequestId.HasValue && targetRequestId.Value > 0)
        {
            return new CancelResult(wasBusy, _cancelMap.Cancel(targetRequestId.Value));
        }
        _cancelMap.CancelAll();
        return new CancelResult(wasBusy, wasBusy);
    }

    public void Dispose() { StateChanged = null; RunCompleted = null; }
}
