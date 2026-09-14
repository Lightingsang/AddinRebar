using System.ComponentModel;
using System.Text.Json;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPRebar.Mcp.Server.Tools.Registry;

/// <summary>
///     Write side of the tool memory: package a successful run as a draft, test it, ask for publication,
///     retire it. The AI drives these; a person closes the loop with the CLI under the default policy.
/// </summary>
[McpServerToolType]
public sealed class ToolLifecycleTools(ToolLifecycleService lifecycle, ToolManager manager, ResultFormatter formatter)
{
    [McpServerTool(Name = "propose_tool", Title = "Propose a new registry tool", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description(
        "Package a working C# script as a reusable tool (status draft). Replace every literal that may change between calls with args.<Kind>(\"key\") " +
        "and declare each key in inputSchema (JSON Schema object: string/number/integer/boolean/array/object, required, default, description). " +
        "The code is guard-checked and compiled inside the host application but not run. Give ≥ 2 examples with different args; test_tool runs them. " +
        "Returns a validation report; fix errors and call again. Use newVersion=true to replace the code of an existing tool.")]
    public Task<CallToolResult> Propose(
        [Description("snake_case name, e.g. color_beams_by_type")] string name,
        [Description("What the tool does, for search and for the reviewer (≥ 20 chars)")] string description,
        [Description("Architecture | Structure | MEP | Annotation | View | Data | Generic")] string category,
        [Description("JSON Schema of the args object")] JsonElement inputSchema,
        [Description("Script body: same globals as the execute tool plus args; must end with return")] string code,
        [Description("Usage examples: [{\"title\": \"...\", \"args\": {...}}]")] JsonElement examples,
        [Description("Human title, e.g. 'Colour beams by type'")] string? title = null,
        [Description("Search tags")] string[]? tags = null,
        [Description("auto (bridge wraps a Transaction) | manual (code opens its own) | none (read-only)")] string transaction = "auto",
        [Description("5–120 seconds")] int timeoutSeconds = 60,
        [Description("runId of the execute run this comes from (see get_run)")] long? sourceRunId = null,
        [Description("Propose a new version of an existing tool name")] bool newVersion = false,
        CancellationToken cancellationToken = default)
    {
        return formatter.RunAsync(async () =>
        {
            List<ToolExample> parsedExamples;
            try
            {
                parsedExamples = examples.ValueKind == JsonValueKind.Array
                    ? examples.EnumerateArray().Select(e => new ToolExample
                    {
                        Title = e.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                        Args = e.TryGetProperty("args", out var a) ? a.Clone() : JsonSerializer.Deserialize<JsonElement>("{}"),
                        Expected = e.TryGetProperty("expected", out var x) ? x.Clone() : null,
                    }).ToList()
                    : [];
            }
            catch (Exception exception) when (exception is JsonException or InvalidOperationException)
            {
                return formatter.Error("examples must be an array of {title, args}: " + exception.Message);
            }

            var outcome = await lifecycle.ProposeAsync(new ProposeInput(name, title, description, category, tags ?? [], inputSchema, code, parsedExamples,
                transaction, timeoutSeconds, sourceRunId, newVersion), cancellationToken).ConfigureAwait(false);

            var payload = new
            {
                accepted = outcome.Accepted,
                name = outcome.Record?.Name ?? name,
                version = outcome.Record?.Version,
                status = outcome.Record is null ? null : ToolRegistryDb.StatusText(outcome.Record.Status),
                errors = outcome.Report.Errors,
                warnings = outcome.Report.Warnings,
                next = outcome.Next,
                folder = outcome.Record?.Folder,
            };
            return outcome.Accepted ? formatter.Text(payload) : new CallToolResult { IsError = true, Content = [new TextContentBlock { Text = HPRebar.Mcp.Contracts.JsonRpc.BridgeJson.Serialize(payload) }] };
        });
    }

    [McpServerTool(Name = "test_tool", Title = "Test a registry tool", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false)]
    [Description(
        "Run a tool's examples (or the given cases) inside the host application with dryRun — every change is rolled back — and record the outcomes. " +
        "All cases passing moves a draft to tested. realRun=true commits the changes (only on a scratch model).")]
    public Task<CallToolResult> Test(
        [Description("Tool name")] string name,
        [Description("Optional cases [{\"title\": \"...\", \"args\": {...}}]; default = the tool's examples")] JsonElement? cases = null,
        [Description("Commit instead of rolling back")] bool realRun = false,
        CancellationToken cancellationToken = default)
    {
        return formatter.RunAsync(async () =>
        {
            var parsed = cases is { ValueKind: JsonValueKind.Array } arr
                ? arr.EnumerateArray().Select((e, i) => new ToolExample
                {
                    Title = e.TryGetProperty("title", out var t) ? t.GetString() ?? $"case {i + 1}" : $"case {i + 1}",
                    Args = e.TryGetProperty("args", out var a) ? a.Clone() : JsonSerializer.Deserialize<JsonElement>("{}"),
                }).ToList()
                : null;

            try
            {
                var outcome = await lifecycle.TestAsync(name, parsed, realRun, cancellationToken).ConfigureAwait(false);
                var payload = new
                {
                    outcome.Name, outcome.Status, outcome.Passed, outcome.Failed,
                    cases = outcome.Cases.Select(c => new { c.Title, c.Success, c.DryRun, c.DurationMs, c.Error, c.RunId, c.Changed, value = c.Value }),
                    outcome.Next,
                };
                return outcome.Failed == 0 ? formatter.Text(payload) : new CallToolResult { IsError = true, Content = [new TextContentBlock { Text = HPRebar.Mcp.Contracts.JsonRpc.BridgeJson.Serialize(payload) }] };
            }
            catch (ToolNotFoundException exception) { return formatter.Error(exception.Message); }
            catch (ToolNotRunnableException exception) { return formatter.Error(exception.Message); }
        });
    }

    [McpServerTool(Name = "publish_tool", Title = "Publish a registry tool", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description(
        "Request publication of a tested tool. Policy manual (default): the tool becomes pending_approval, a review file is written and a human must run " +
        "`HPRebar.Mcp.Server.exe registry approve <name>` (or set status=published in tool.json). Policy auto: tested tools publish immediately. " +
        "Published tools appear in tools/list of every running server.")]
    public CallToolResult Publish([Description("Tool name")] string name)
    {
        try
        {
            var outcome = lifecycle.Publish(name);
            return formatter.Text(new { outcome.Name, outcome.Status, outcome.Message, outcome.ReviewFile, policy = manager.Options.PublishPolicy });
        }
        catch (ToolNotFoundException exception) { return formatter.Error(exception.Message); }
        catch (ToolNotRunnableException exception) { return formatter.Error(exception.Message); }
    }

    [McpServerTool(Name = "manage_tool", Title = "Deprecate, quarantine or restore a tool", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("deprecate: retire a tool (never runs again until restored). quarantine: pull it from tools/list. restore: back to draft so it can be tested and published again.")]
    public CallToolResult Manage(
        [Description("Tool name")] string name,
        [Description("deprecate | quarantine | restore")] string action,
        [Description("Why")] string? reason = null)
    {
        try
        {
            var record = lifecycle.Manage(name, action, reason, "ai");
            return formatter.Text(new { record.Name, status = ToolRegistryDb.StatusText(record.Status), record.Notes });
        }
        catch (ToolNotFoundException exception) { return formatter.Error(exception.Message); }
        catch (ToolNotRunnableException exception) { return formatter.Error(exception.Message); }
        catch (ArgumentException exception) { return formatter.Error(exception.Message); }
    }
}
