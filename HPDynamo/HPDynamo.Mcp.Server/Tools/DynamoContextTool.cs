namespace HPDynamo.Mcp.Server.Tools;

using System.ComponentModel;
using HPDynamo.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

/// <summary>
///     Read-only snapshot of the Dynamo session and active graph state so the AI can inspect
///     nodes, wires, run type, and environment before authoring or executing code.
/// </summary>
[McpServerToolType]
public sealed class DynamoContextTool(ContextService service)
{
    [McpServerTool(
        Name = DynamoHostProfile.ContextToolName,
        Title = "Get Dynamo context",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description(
        "Returns the current Dynamo and Revit session status: hostVersion, active document title and path, isReadOnly, " +
        "isModifiable, units, openDocs, executionEnabled, and dynamo facts {isLive, dynamoVersion, activeWorkspaceName, " +
        "activeWorkspacePath, runType (Automatic, Manual, Periodic), nodeCount, wireCount, selectedNodeCount, hasUnsavedChanges}. " +
        "With includeSelection, returns currently selected nodes and elements. Call this before script execution.")]
    public Task<CallToolResult> GetContextAsync(
        [Description("Include currently selected nodes or elements in Dynamo canvas (id, category, name).")]
        bool includeSelection = false,
        CancellationToken cancellationToken = default)
    {
        return service.GetAsync(includeSelection, cancellationToken);
    }
}
