namespace HPDynamo.Mcp.Server.Tools;

using System.ComponentModel;
using System.Text.Json;
using HPDynamo.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

/// <summary>
///     Core execution tool enabling AI agents to execute DesignScript code directly on the Dynamo engine,
///     creating geometric curves, solids, surfaces, or evaluating mathematical expressions with dryRun support.
/// </summary>
[McpServerToolType]
public sealed class ExecuteDesignScriptTool(ExecuteCodeService service)
{
    [McpServerTool(
        Name = DynamoHostProfile.ExecuteDesignScriptToolName,
        Title = "Execute DesignScript in Dynamo",
        ReadOnly = false,
        Destructive = true,
        Idempotent = false,
        OpenWorld = false)]
    [Description(
        "Executes DesignScript code directly within the Dynamo evaluation engine. " +
        "Supports standard DesignScript syntax: geometry creation (Point.ByCoordinates, Line.ByStartPointEndPoint, Curve, Solid), " +
        "code block logic, list operations, and math expressions. " +
        "transaction=auto commits created elements; none is read-only evaluation. " +
        "dryRun=true evaluates the code and rolls back all created Revit elements via TransactionGroup. " +
        "Requires the user to tick 'Allow AI Dynamo execution' in the HPDynamo MCP Bridge window.")]
    public Task<CallToolResult> ExecuteAsync(
        [Description("DesignScript code block to execute (e.g., 'pt = Point.ByCoordinates(0, 0, 0); line = Line.ByStartPointEndPoint(pt, Point.ByCoordinates(10, 0, 0));').")]
        string code,
        [Description("auto (default): commits geometry and elements. none: read-only evaluation. manual: manages own Transaction.")]
        string transaction = TransactionModes.Auto,
        [Description("Run the DesignScript and roll back all changes. Verifies geometry and output values without persisting.")]
        bool dryRun = false,
        [Description("Cooperative timeout in seconds, 5–600.")]
        int timeoutSeconds = 30,
        [Description("Short label for tracking in audit log. Max 64 characters.")]
        string? label = null,
        [Description("Optional JSON variables dictionary passed to the script execution environment.")]
        JsonElement? args = null,
        IProgress<ProgressNotificationValue>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var scriptLabel = string.IsNullOrWhiteSpace(label) ? "DesignScript" : $"DesignScript: {label.Trim()}";
        return service.ExecuteAsync(code, transaction, dryRun, timeoutSeconds, scriptLabel, args, progress, cancellationToken);
    }
}
