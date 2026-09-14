using System.ComponentModel;
using System.Text.Json;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPRebar.Mcp.Server.Tools.Registry;

/// <summary>
///     Runs a stored tool by name. Published tools are also exposed directly as MCP tools; this is the
///     fallback for a client whose tool list is stale and the only door for unpublished tools during
///     testing.
/// </summary>
[McpServerToolType]
public sealed class RunToolTool(ToolManager manager, ResultFormatter formatter)
{
    [McpServerTool(Name = "run_tool", Title = "Run a registry tool", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false)]
    [Description(
        "Run a stored tool from the registry with the given args (must match its inputSchema from search_tools / get_tool). " +
        "Same result shape as execute_revit_code. Draft / tested / pending tools need allowUnpublished=true. Use dryRun=true first for tools that modify the model.")]
    public Task<CallToolResult> Run(
        [Description("Tool name")] string name,
        [Description("Arguments object for the tool")] JsonElement? args = null,
        [Description("Run then roll back")] bool dryRun = false,
        [Description("Allow running a tool that is not published yet")] bool allowUnpublished = false,
        CancellationToken cancellationToken = default)
    {
        return formatter.RunAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(name)) return formatter.Error("name is required.");
            if (args is { ValueKind: not (JsonValueKind.Object or JsonValueKind.Null or JsonValueKind.Undefined) }) return formatter.Error("args must be a JSON object.");

            try
            {
                var result = await manager.RunAsync(name.Trim(), args, dryRun, allowUnpublished, RunRecord.KindTool, cancellationToken).ConfigureAwait(false);
                return formatter.FromExecute(result);
            }
            catch (ToolNotFoundException exception)
            {
                return formatter.Error(exception.Message);
            }
            catch (ToolNotRunnableException exception)
            {
                return formatter.Error(exception.Message);
            }
        });
    }
}
