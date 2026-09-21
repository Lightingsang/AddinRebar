using System.ComponentModel;
using HPRobot.Mcp.Server.Hosts.Robot;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPRobot.Mcp.Server.Hosts.Robot.Tools;

/// <summary>Read-only snapshot of the attached Robot Structural Analysis session.</summary>
[McpServerToolType]
public sealed class GetRobotContextTool(ContextService service)
{
    public const string ToolDescription =
        "Returns the Robot Structural Analysis session the bridge app is attached to: hostVersion (2026), docTitle and docPath of the model file (.rtd), " +
        "isModifiable (attached and idle), executionEnabled, and robot info " +
        "{isAttached, attachedPid, robotVersion, structureType, isCalculated, heavyOperationsEnabled, nodeCount, barCount, panelCount, loadCaseCount}. " +
        "With includeSelection=true returns selected nodes, bars, and panels. " +
        "Call this before execute_robot_code to verify model state and calculation readiness.";

    [McpServerTool(
        Name = RobotHostProfile.ContextToolName,
        Title = "Get Robot context",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description(ToolDescription)]
    public Task<CallToolResult> GetContextAsync(
        [Description("Include current selection (selected nodes, bars, and panels).")]
        bool includeSelection = false,
        CancellationToken cancellationToken = default)
    {
        return service.GetAsync(includeSelection, cancellationToken);
    }
}
