namespace HPGrasshopper.Mcp.Server.Tools;

using System.ComponentModel;
using System.Text.Json;
using HPGrasshopper.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

/// <summary>
///     Core inspection tool returning graph structure, components, parameters, wires,
///     coordinates, solver state, warnings, errors, and volatile preview values from the active Grasshopper canvas.
/// </summary>
[McpServerToolType]
public sealed class ReadGrasshopperCanvasTool(IRevitBridgeClient client, ResultFormatter formatter)
{
    public const string ToolDescription =
        "Inspects the active Grasshopper canvas and returns a structured JSON model of all components, " +
        "parameters, wires, coordinates, solver state, warnings, errors, and optional data values. " +
        "Use this tool to discover existing component Guids and wiring topology before calling batch_edit_grasshopper_canvas.";

    [McpServerTool(
        Name = GrasshopperHostProfile.ReadCanvasToolName,
        Title = "Read Grasshopper Canvas",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description(ToolDescription)]
    public Task<CallToolResult> ReadCanvasAsync(
        [Description("Whether to extract summary preview values from volatile data. Default false.")]
        bool includeValues = false,
        [Description("Only return selected components. Default false.")]
        bool selectedOnly = false,
        [Description("Optional category filter (e.g. 'Params', 'Math', 'Revit').")]
        string? categoryFilter = null,
        CancellationToken cancellationToken = default)
    {
        return formatter.RunAsync(async () =>
        {
            var requestParams = new
            {
                includeValues,
                selectedOnly,
                categoryFilter
            };

            var result = await client.SendAsync<JsonElement>(
                client.Profile.Method(GrasshopperHostProfile.ReadCanvasToolName),
                requestParams,
                TimeSpan.FromSeconds(30),
                null,
                cancellationToken).ConfigureAwait(false);

            return GrasshopperToolResult.From(result, formatter);
        });
    }
}
