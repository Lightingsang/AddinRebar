using System.Diagnostics;
using HPSap2000.McpBridge.Service;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Scripting;
using Serilog;

namespace HPSap2000.McpBridge;

/// <summary>
///     The reporting half of the executor.
/// </summary>
public sealed partial class SapExecutor
{
    private static ExecuteResult Diagnostics(string stage, IReadOnlyList<ScriptDiagnostic> diagnostics) => new ExecuteResult
    {
        IsError = true,
        Message = stage switch
        {
            "guard" => "The script uses APIs the bridge blocks. Fix the listed lines and retry.",
            "path" => "A file path the script would pass to SAP2000 cannot be checked or is off limits. Fix the listed lines and retry.",
            _ => "The script does not compile. Fix the listed errors and retry.",
        },
        Diagnostics = diagnostics,
    };

    private static ExecuteResult Preview(IReadOnlyList<TierHit> hits)
    {
        var diagnostics = SapTierAnalyzer.Preview(hits, SapTier.Write);
        var members = string.Join("; ", diagnostics.Select(d => d.Message).Distinct());
        return new ExecuteResult
        {
            IsError = true,
            RolledBack = true,
            Message = $"static preview: the script would call {members} — nothing ran. Send transaction: auto with dryRun: false to run it (the bridge saves the model and copies a .SDB snapshot first).",
            Diagnostics = diagnostics,
        };
    }

    private void AuditStarted(ExecuteRequest request, SapTier tier)
    {
        try
        {
            var note = tier == SapTier.Destructive ? "[destructive] [tier:D] forced save + snapshot, then run" : "[tier:W] forced save + snapshot, then run";
            _audit.Write(new AuditEntry(DateTimeOffset.Now, Environment.UserName, ActiveDocumentTitle, null, ScriptCompiler.Hash(request.Code), request.Code,
                request.Transaction, request.DryRun, "started", 0, 0, 0, 0, note));
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "MCP audit write failed");
        }
    }

    private ExecuteResult Finish(ExecuteRequest request, ExecuteResult result, Stopwatch stopwatch, SapTier? tier = null)
    {
        if (result.DurationMs == 0) result.DurationMs = stopwatch.ElapsedMilliseconds;

        var outcome = !result.IsError ? "ok" : result.TimedOut ? "timeout" : result.Diagnostics.Count > 0 ? "rejected" : "error";
        var tags = tier switch { SapTier.Destructive => "[destructive] [tier:D] ", SapTier.Write => "[tier:W] ", SapTier.ReadOnly => "[tier:R] ", _ => "" };
        if (result.Snapshot is not null) tags += $"[snapshot:{result.Snapshot}] ";

        try
        {
            _audit.Write(new AuditEntry(DateTimeOffset.Now, Environment.UserName, ActiveDocumentTitle, null, ScriptCompiler.Hash(request.Code), request.Code,
                request.Transaction, request.DryRun, outcome, result.DurationMs, result.Changed.Added, result.Changed.Modified, result.Changed.Deleted,
                tags.Length > 0 ? tags + (result.Message ?? "ok") : result.Message));
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
