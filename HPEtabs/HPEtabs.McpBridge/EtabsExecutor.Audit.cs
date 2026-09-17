using System.Diagnostics;
using HPEtabs.McpBridge.Service;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Scripting;
using Serilog;

namespace HPEtabs.McpBridge;

/// <summary>
///     The reporting half of the executor: the refusal results for the pre-run stages, the static preview a
///     writing script gets instead of a run, and the audit trail — one JSON line per request in a file the
///     user owns, whatever the outcome.
/// </summary>
public sealed partial class EtabsExecutor
{
    private static ExecuteResult Diagnostics(string stage, IReadOnlyList<ScriptDiagnostic> diagnostics) => new ExecuteResult
    {
        IsError = true,
        Message = stage switch
        {
            "guard" => "The script uses APIs the bridge blocks. Fix the listed lines and retry.",
            _ => "The script does not compile. Fix the listed errors and retry.",
        },
        Diagnostics = diagnostics,
    };

    /// <summary>
    ///     Nothing ran. An error result on purpose: a preview must never count as a passed test of a stored tool.
    ///     `rolledBack` is true in the only sense ETABS allows — no change was made.
    /// </summary>
    private static ExecuteResult Preview(IReadOnlyList<TierHit> hits)
    {
        var diagnostics = EtabsTierGate.Preview(hits, EtabsTier.Write);
        var members = string.Join(", ", diagnostics.Select(d => d.Message).Distinct());
        return new ExecuteResult
        {
            IsError = true,
            RolledBack = true,
            Message = $"static preview: the script would call {members} — nothing ran. Writing runs (save + .EDB snapshot first) are not available in this bridge build yet.",
            Diagnostics = diagnostics,
        };
    }

    private ExecuteResult Finish(ExecuteRequest request, ExecuteResult result, Stopwatch stopwatch)
    {
        if (result.DurationMs == 0) result.DurationMs = stopwatch.ElapsedMilliseconds;

        var outcome = !result.IsError ? "ok" : result.TimedOut ? "timeout" : result.Diagnostics.Count > 0 ? "rejected" : "error";

        try
        {
            _audit.Write(new AuditEntry(DateTimeOffset.Now, Environment.UserName, ActiveDocumentTitle, null, ScriptCompiler.Hash(request.Code), request.Code,
                request.Transaction, request.DryRun, outcome, result.DurationMs, result.Changed.Added, result.Changed.Modified, result.Changed.Deleted, result.Message));
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
