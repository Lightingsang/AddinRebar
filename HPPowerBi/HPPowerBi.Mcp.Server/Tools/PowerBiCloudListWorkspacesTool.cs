using System.ComponentModel;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPPowerBi.Mcp.Server.Tools;

/// <summary>Tool to list Power BI Service workspaces.</summary>
[McpServerToolType]
public sealed class PowerBiCloudListWorkspacesTool(IRevitBridgeClient bridge, ResultFormatter formatter)
{
    [McpServerTool(
        Name = "powerbi_cloud_list_workspaces",
        Title = "List Cloud Workspaces",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Lists Power BI Service / Fabric workspaces accessible by the configured user or Service Principal.")]
    public Task<CallToolResult> ListWorkspacesAsync(CancellationToken cancellationToken = default)
    {
        return formatter.RunAsync(async () =>
        {
            var result = await bridge.SendAsync<object>(
                bridge.Profile.MethodPrefix + "cloud.workspaces",
                null,
                TimeSpan.FromSeconds(30),
                null,
                cancellationToken).ConfigureAwait(false);

            return formatter.Text(result);
        });
    }
}
