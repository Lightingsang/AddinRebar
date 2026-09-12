using System.ComponentModel;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPRebar.Mcp.Server.Tools;

/// <summary>Asks the bridge to cancel the script currently running. Cooperative: the script must observe `ct`.</summary>
[McpServerToolType]
public sealed class CancelExecutionTool(IRevitBridgeClient bridge, ResultFormatter formatter)
{
    [McpServerTool(
        Name = "cancel_execution",
        Title = "Cancel running script",
        ReadOnly = false,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description(
        "Signals cancellation to the script currently running in Revit. Cancellation is cooperative: the script stops at its next `ct` check, " +
        "and its transaction group is rolled back. Returns whether anything was running.")]
    public Task<CallToolResult> CancelAsync(CancellationToken cancellationToken = default)
    {
        return formatter.RunAsync(async () =>
        {
            var result = await bridge.SendAsync<CancelResult>(
                JsonRpcMethods.Cancel,
                null,
                RevitContextTool.Timeout,
                null,
                cancellationToken).ConfigureAwait(false);

            return formatter.Text(result);
        });
    }
}
