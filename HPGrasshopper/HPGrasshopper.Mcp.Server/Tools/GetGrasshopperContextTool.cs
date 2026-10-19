namespace HPGrasshopper.Mcp.Server.Tools;

using System.ComponentModel;
using HPGrasshopper.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

/// <summary>
///     Read-only snapshot of Grasshopper and Revit session state including active definition path,
///     component and wire counts, solver status, and selection state.
/// </summary>
[McpServerToolType]
public sealed class GetGrasshopperContextTool(ContextService service)
{
    public const string ToolDescription =
        "Returns current Grasshopper and Revit session status: hostVersion, active definition name and path, " +
        "component count, wire count, selected component count, solver status, whether Rhino.Inside.Revit is active, " +
        "and execution settings. Call this before script execution.";

    [McpServerTool(
        Name = GrasshopperHostProfile.ContextToolName,
        Title = "Get Grasshopper Context",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description(ToolDescription)]
    public Task<CallToolResult> GetContextAsync(
        [Description("Include currently selected components or elements in Grasshopper canvas.")]
        bool includeSelection = false,
        CancellationToken cancellationToken = default)
    {
        return service.GetAsync(includeSelection, cancellationToken);
    }
}
