namespace HPGrasshopper.Mcp.Server.Tools;

using System.ComponentModel;
using System.Text.Json;
using HPGrasshopper.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

/// <summary>
///     Core automation tool running a Grasshopper definition (.gh or .ghx) in Player mode,
///     injecting input values, solving the definition, and returning evaluated terminal outputs.
/// </summary>
[McpServerToolType]
public sealed class RunGrasshopperPlayerTool(IRevitBridgeClient client, ResultFormatter formatter)
{
    public const string ToolDescription =
        "Opens and solves a Grasshopper definition file (.gh or .ghx) in Grasshopper Player mode with " +
        "user-provided JSON input values, extracting computed outputs without interactive UI prompts. " +
        "Supports numeric inputs, text, points, booleans, and Revit element identifiers.";

    [McpServerTool(
        Name = GrasshopperHostProfile.RunPlayerToolName,
        Title = "Run Grasshopper Player",
        ReadOnly = false,
        Destructive = true,
        Idempotent = false,
        OpenWorld = false)]
    [Description(ToolDescription)]
    public Task<CallToolResult> RunPlayerAsync(
        [Description("Absolute path to .gh or .ghx file.")]
        string filePath,
        [Description("Key-value pairs mapping parameter NickName or Guid to input values (numbers, strings, booleans, points [x,y,z], or Revit element IDs).")]
        JsonElement? inputs = null,
        [Description("List of output parameter NickNames to retrieve. If empty, retrieves all terminal outputs.")]
        string[]? outputs = null,
        [Description("Run in background document without docking to active canvas. Default true.")]
        bool headless = true,
        CancellationToken cancellationToken = default)
    {
        return formatter.RunAsync(async () =>
        {
            var requestParams = new
            {
                filePath,
                inputs,
                outputs,
                headless
            };

            var result = await client.SendAsync<JsonElement>(
                client.Profile.Method(GrasshopperHostProfile.RunPlayerToolName),
                requestParams,
                TimeSpan.FromSeconds(120),
                null,
                cancellationToken).ConfigureAwait(false);

            return GrasshopperToolResult.From(result, formatter);
        });
    }
}
