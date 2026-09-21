using System.ComponentModel;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPPowerBi.Mcp.Server.Tools;

/// <summary>Tool to list datasets in a Power BI Service workspace.</summary>
[McpServerToolType]
public sealed class PowerBiCloudListDatasetsTool(IRevitBridgeClient bridge, ResultFormatter formatter)
{
    [McpServerTool(
        Name = "powerbi_cloud_list_datasets",
        Title = "List Cloud Datasets",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Lists datasets / semantic models within a specific workspace or 'My workspace'.")]
    public Task<CallToolResult> ListDatasetsAsync(
        [Description("Optional workspace ID (GUID). If omitted, queries 'My workspace'.")]
        string? workspaceId = null,
        CancellationToken cancellationToken = default)
    {
        return formatter.RunAsync(async () =>
        {
            var parameters = new { WorkspaceId = workspaceId };

            var result = await bridge.SendAsync<object>(
                bridge.Profile.MethodPrefix + "cloud.datasets",
                parameters,
                TimeSpan.FromSeconds(30),
                null,
                cancellationToken).ConfigureAwait(false);

            return formatter.Text(result);
        });
    }
}
