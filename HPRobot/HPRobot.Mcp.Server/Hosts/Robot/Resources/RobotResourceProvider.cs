using System.ComponentModel;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Server;

namespace HPRobot.Mcp.Server.Hosts.Robot.Resources;

/// <summary>
///     Exposes Robot Structural Analysis model snapshot as resources (`robot://...`) for MCP clients.
/// </summary>
[McpServerResourceType]
public sealed class RobotResourceProvider(ContextService service)
{
    [McpServerResource(UriTemplate = "robot://model/info", Name = "robot_model_info", Title = "Robot model info", MimeType = "application/json")]
    [Description("Attached Robot Structural Analysis model: version, file title and path, structure type, calculation status, object counts.")]
    public Task<string> ModelInfoAsync(CancellationToken cancellationToken) => service.ReadAsync(includeSelection: false, cancellationToken);

    [McpServerResource(UriTemplate = "robot://selection", Name = "robot_selection", Title = "Robot selection", MimeType = "application/json")]
    [Description("Objects currently selected in Robot Structural Analysis, plus the model snapshot.")]
    public Task<string> SelectionAsync(CancellationToken cancellationToken) => service.ReadAsync(includeSelection: true, cancellationToken);
}
