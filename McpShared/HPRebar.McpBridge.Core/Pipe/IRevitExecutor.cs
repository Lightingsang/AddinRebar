using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;

namespace HPRebar.McpBridge.Core.Pipe;

/// <summary>
///     What the pipe side asks of Revit. Implemented by the external-event handler, which owns the only
///     legal path onto Revit's API thread; the dispatcher never touches a Revit object itself.
/// </summary>
public interface IRevitExecutor
{
    /// <summary>A script is running (or queued) on the Revit thread; one at a time, the rest get "busy".</summary>
    bool IsBusy { get; }

    int CompiledScriptCount { get; }

    /// <summary>Title of the active document, refreshed on Revit's own events so it is safe to read from any thread.</summary>
    string? ActiveDocumentTitle { get; }

    /// <summary>Busy flag or document title changed. Raised on an arbitrary thread.</summary>
    event Action? StateChanged;

    /// <summary>A script finished (any outcome). Raised on an arbitrary thread.</summary>
    event Action<LastRunInfo>? RunCompleted;

    Task<ExecuteResult> ExecuteAsync(ExecuteRequest request, IProgress<ScriptProgress>? progress, CancellationToken cancellationToken);

    Task<ContextResult> GetContextAsync(bool includeSelection, CancellationToken cancellationToken);

    /// <summary>Reflection only — needs no Revit thread, answers immediately.</summary>
    InspectResult Inspect(InspectRequest request);

    /// <summary>Guard + compile + syntax facts on the pipe thread; the script is not run.</summary>
    AnalyzeResult Analyze(AnalyzeRequest request);

    /// <summary>Cooperative: signals the running script's token. Returns whether anything was running.</summary>
    CancelResult Cancel();
}
