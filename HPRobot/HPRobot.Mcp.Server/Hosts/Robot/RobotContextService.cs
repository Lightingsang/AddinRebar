using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;

namespace HPRobot.Mcp.Server.Hosts.Robot;

/// <summary>
///     Context service wrapper for Robot Structural Analysis providing typed access and shaping.
/// </summary>
public sealed class RobotContextService(ContextService contextService)
{
    public Task<CallToolResult> GetContextAsync(bool includeSelection, CancellationToken cancellationToken = default) =>
        contextService.GetAsync(includeSelection, cancellationToken);

    public Task<string> ReadAsync(bool includeSelection, CancellationToken cancellationToken = default) =>
        contextService.ReadAsync(includeSelection, cancellationToken);
}
