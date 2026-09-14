using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;

namespace HPRebar.Mcp.Server.Tests.Fakes;

/// <summary>
///     Stands in for Revit behind the dispatcher. Scripted per test: what execute returns, how much
///     progress it reports, whether it is busy. Records what it was asked so tests can assert on the
///     request that crossed the pipe.
/// </summary>
public sealed class FakeRevitExecutor : IBridgeExecutor
{
    public bool IsBusy { get; set; }

    public int CompiledScriptCount { get; set; }

    public string? ActiveDocumentTitle { get; set; } = "Project1";

    public event Action? StateChanged;

    public event Action<LastRunInfo>? RunCompleted;

    public void RaiseState() => StateChanged?.Invoke();

    public void RaiseRun(LastRunInfo run) => RunCompleted?.Invoke(run);

    public ExecuteRequest? LastExecuteRequest { get; private set; }

    public int ProgressSteps { get; set; }

    public int ProgressDelayMs { get; set; } = 10;

    public Func<ExecuteRequest, ExecuteResult> ExecuteHandler { get; set; } = _ => new ExecuteResult { Value = System.Text.Json.JsonSerializer.SerializeToElement(42), ValueType = "System.Int32" };

    public int CancelCalls { get; private set; }

    public async Task<ExecuteResult> ExecuteAsync(ExecuteRequest request, IProgress<ScriptProgress>? progress, CancellationToken cancellationToken)
    {
        LastExecuteRequest = request;

        for (var step = 1; step <= ProgressSteps; step++)
        {
            progress?.Report(new ScriptProgress(step, ProgressSteps, $"step {step}"));
            if (ProgressDelayMs > 0) await Task.Delay(ProgressDelayMs, cancellationToken);
        }

        return ExecuteHandler(request);
    }

    public Task<ContextResult> GetContextAsync(bool includeSelection, CancellationToken cancellationToken) =>
        Task.FromResult(new ContextResult
        {
            RevitVersion = "2026",
            DocTitle = ActiveDocumentTitle,
            Selection = includeSelection ? [new ElementInfo(1001, "Walls", "Basic Wall")] : [],
        });

    public InspectResult Inspect(InspectRequest request) => new InspectResult
    {
        TypeName = request.TypeName,
        FullName = "Autodesk.Revit.DB." + request.TypeName,
        Members = [new MemberSignature("property", "ElementId Id { get; }")],
    };

    /// <summary>Real syntax analysis and guard; "compiles" unless the code carries the COMPILE_ERROR marker (no Roslyn compile in the fake).</summary>
    public AnalyzeResult Analyze(AnalyzeRequest request)
    {
        var result = HPRebar.McpBridge.Core.Scripting.ScriptAnalyzer.Analyze(request.Code);
        result.GuardViolations = HPRebar.McpBridge.Core.Scripting.ScriptGuard.Check(request.Code);
        var broken = request.Code.Contains("COMPILE_ERROR", StringComparison.Ordinal);
        result.Compiles = result.GuardViolations.Count == 0 && !broken;
        result.Diagnostics = broken ? [new ScriptDiagnostic(1, 1, "CS0103", "The name 'COMPILE_ERROR' does not exist")] : [];
        return result;
    }

    public CancelResult Cancel()
    {
        CancelCalls++;
        return new CancelResult(IsBusy, IsBusy);
    }
}
