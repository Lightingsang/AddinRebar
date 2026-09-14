using System.ComponentModel;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPRebar.Mcp.Server.Tools.Registry;

/// <summary>
///     The bridge between "that script worked" and "make it a tool": returns the code of a past run
///     together with the literals worth turning into parameters.
/// </summary>
[McpServerToolType]
public sealed class RunHistoryTools(ToolManager manager, ToolLifecycleService lifecycle, ResultFormatter formatter)
{
    [McpServerTool(Name = "get_run", Title = "Get a past run", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Details of one run from the history (runId comes back from the execute tool, run_tool and test_tool): outcome, args, the code when it was a successful " +
        "ad-hoc script, and — when the host application is reachable — an analysis listing hard-coded literals (line, value, variable) and the args keys already read. " +
        "Use it before propose_tool to decide which literals become parameters.")]
    public Task<CallToolResult> Get(
        [Description("Run id")] long runId,
        [Description("Analyse the code (guard, compile, literals) through the host application")] bool analyze = true,
        CancellationToken cancellationToken = default)
    {
        return formatter.RunAsync(async () =>
        {
            var run = manager.Db.GetRun(runId);
            if (run is null) return formatter.Error($"No run #{runId} in the history.");

            var code = run.Code;
            if (code is null && run.ToolName is not null && manager.TryGet(run.ToolName, out var tool)) code = tool.Code;

            var analysis = analyze && code is not null ? await lifecycle.AnalyzeAsync(code, cancellationToken).ConfigureAwait(false) : null;

            return formatter.Text(new
            {
                run.Id,
                run.Kind,
                run.ToolName,
                run.Version,
                run.Success,
                run.DryRun,
                run.DurationMs,
                run.Error,
                run.RevitVersion,
                run.DocTitle,
                run.Timestamp,
                args = run.ArgsJson,
                code,
                codeAvailable = code is not null,
                analysis = analysis is null ? null : new
                {
                    analysis.Compiles,
                    analysis.LineCount,
                    analysis.HasLoops,
                    analysis.UsesTransaction,
                    literals = analysis.Literals,
                    argKeys = analysis.ArgKeys,
                    guardViolations = analysis.GuardViolations,
                    diagnostics = analysis.Diagnostics,
                },
                next = code is null
                    ? "No code stored for this run (failed run, or pruned)."
                    : "To package: rewrite literals as args.<Kind>(\"key\") with defaults, then propose_tool {name, description, category, inputSchema, code, examples, sourceRunId}.",
            });
        });
    }
}
