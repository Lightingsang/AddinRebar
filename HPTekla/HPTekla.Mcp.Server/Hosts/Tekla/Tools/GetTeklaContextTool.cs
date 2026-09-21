using System.ComponentModel;
using HPTekla.Mcp.Server.Hosts.Tekla;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPTekla.Mcp.Server.Hosts.Tekla.Tools;

/// <summary>Read-only snapshot of the active Tekla Structures session.</summary>
[McpServerToolType]
public sealed class GetTeklaContextTool(ContextService service)
{
    public const string ToolDescription =
        "Returns the active Tekla Structures session: hostVersion (2025), modelName, modelPath, projectName, " +
        "isModifiable, executionEnabled, heavyOperationsEnabled, partCount, rebarCount, and drawingCount. " +
        "With includeSelection=true returns selected objects in Tekla Structures. " +
        "Call this before execute_tekla_code to verify model state and readiness.";

    [McpServerTool(
        Name = TeklaHostProfile.ContextToolName,
        Title = "Get Tekla context",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description(ToolDescription)]
    public Task<CallToolResult> GetContextAsync(
        [Description("Include current selection (selected model objects).")]
        bool includeSelection = false,
        CancellationToken cancellationToken = default)
    {
        return service.GetAsync(includeSelection, cancellationToken);
    }
}
