using System.Diagnostics;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Scripting;
using Serilog;

namespace HPNavis.McpBridge;

/// <summary>
///     The reporting half of the executor: the refusal results for the three pre-run stages and the audit
///     trail — one line per run, two for a heavy run ("started" when it is queued, since a file append or a
///     clash run cannot be interrupted and the outcome line may never come).
/// </summary>
public sealed partial class NavisMainThreadExecutor
{
    private static ExecuteResult Diagnostics(string stage, IReadOnlyList<ScriptDiagnostic> diagnostics) => new ExecuteResult
    {
        IsError = true,
        Message = stage switch
        {
            "heavy" => "The script calls heavy operations the user has not allowed in this session. See the listed lines.",
            "guard" => "The script uses APIs the bridge blocks. Fix the listed lines and retry.",
            _ => "The script does not compile. Fix the listed errors and retry.",
        },
        Diagnostics = diagnostics,
    };

    private void AuditStarted(ExecuteRequest request)
    {
        try
        {
            _audit.Write(new AuditEntry(DateTimeOffset.Now, Environment.UserName, _activeDocumentTitle, null, ScriptCompiler.Hash(request.Code),
                request.Code, request.Transaction, request.DryRun, "started", 0, 0, 0, 0, "[heavy] queued"));
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "MCP audit write failed");
        }
    }

    private ExecuteResult Finish(ExecuteRequest request, ExecuteResult result, Stopwatch stopwatch, bool hasHeavyCalls)
    {
        if (result.DurationMs == 0) result.DurationMs = stopwatch.ElapsedMilliseconds;

        var outcome = !result.IsError ? "ok" : result.TimedOut ? "timeout" : result.Diagnostics.Count > 0 ? "rejected" : "error";
        var message = hasHeavyCalls ? "[heavy] " + (result.Message ?? "ok") : result.Message;

        try
        {
            _audit.Write(new AuditEntry(DateTimeOffset.Now, Environment.UserName, _activeDocumentTitle, null, ScriptCompiler.Hash(request.Code), request.Code,
                request.Transaction, request.DryRun, outcome, result.DurationMs, result.Changed.Added, result.Changed.Modified, result.Changed.Deleted, message));
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "MCP audit write failed");
        }

        RunCompleted?.Invoke(new LastRunInfo(DateTimeOffset.Now, request.Label ?? "script", request.Code, result.IsError, result.Message,
            result.DurationMs, result.RolledBack, result.Changed.Added, result.Changed.Modified, result.Changed.Deleted));

        return result;
    }
}
