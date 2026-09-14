using System.ComponentModel;
using HPRebar.Mcp.Server.Registry;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace HPRebar.Mcp.Server.Prompts;

/// <summary>
///     Standardises the "turn this run into a tool" step: the model gets the code, the literals and the
///     exact contract propose_tool expects, so proposals come out parameterised and testable.
/// </summary>
[McpServerPromptType]
public sealed class ToolifyPrompts(ToolManager manager, ToolLifecycleService lifecycle)
{
    private const string Rules =
        "You are packaging a C# Revit script that already ran successfully into a reusable registry tool.\n" +
        "Rules:\n" +
        "1. Every value that could differ between calls (dimensions, names, counts, ids, categories, spacing) becomes a parameter: read it with args.Double/Int/Long/Str/Bool/Strings/Longs/List/Obj(\"key\", default). Keep sensible defaults in the code and in the schema.\n" +
        "2. Keep the script's globals contract: doc, uidoc, app, uiapp, ct, log, progress, args; end with `return <value>;` that summarises what happened (ids created, counts).\n" +
        "3. Units: accept millimetres in args, convert with UnitUtils inside.\n" +
        "4. Choose transaction: auto when the code modifies the model and opens no Transaction itself; none when it only reads; manual only if it opens its own Transaction.\n" +
        "5. Name: snake_case verb_object (create_column_grid, color_beams_by_type). Category: Architecture | Structure | MEP | Annotation | View | Data | Generic.\n" +
        "6. Write ≥ 2 examples with different args that will work on the current model; test_tool runs them with dryRun.\n" +
        "7. Then call propose_tool with sourceRunId; on errors fix and retry; then test_tool; then publish_tool.";

    [McpServerPrompt(Name = "toolify_run", Title = "Package a run as a tool")]
    [Description("Loads the code and literal analysis of a past execute_revit_code run and instructs the model to generalise it into a registry tool.")]
    public async Task<ChatMessage[]> ToolifyAsync(
        [Description("runId returned by execute_revit_code")] string runId,
        CancellationToken cancellationToken)
    {
        // McpException messages reach the client; any other exception is masked as "An error occurred".
        if (!long.TryParse(runId, out var id)) throw new McpException("runId must be a number.");
        var run = manager.Db.GetRun(id) ?? throw new McpException($"No run #{id} in the history.");
        var code = run.Code ?? (run.ToolName is not null && manager.TryGet(run.ToolName, out var tool) ? tool.Code : null)
                   ?? throw new McpException($"Run #{id} has no stored code (it failed or was pruned).");

        var analysis = await lifecycle.AnalyzeAsync(code, cancellationToken).ConfigureAwait(false);
        var literals = analysis is null
            ? "(Revit not reachable — literal analysis unavailable; read the code yourself.)"
            : analysis.Literals.Count == 0 ? "(no candidate literals found)"
            : string.Join("\n", analysis.Literals.Select(l => $"- line {l.Line}: {l.Kind} {l.Value}{(l.BoundTo is null ? "" : $" → variable '{l.BoundTo}'")} | {l.Context}"));

        return
        [
            new ChatMessage(ChatRole.System, Rules),
            new ChatMessage(ChatRole.User,
                $"Run #{run.Id} ({(run.Success ? "success" : "failed")}, {run.DurationMs} ms, document '{run.DocTitle}', Revit {run.RevitVersion}).\n" +
                (run.ArgsJson is null ? "" : $"Args used: {run.ArgsJson}\n") +
                $"\nCode:\n```csharp\n{code}\n```\n\nCandidate literals to parameterise:\n{literals}\n\n" +
                "Produce the propose_tool call: name, title, description, category, tags, inputSchema, code (parameterised), examples (≥ 2), transaction, timeoutSeconds, sourceRunId=" + run.Id + "."),
        ];
    }
}
