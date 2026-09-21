using System.ComponentModel;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPPowerBi.Mcp.Server.Tools;

/// <summary>Tool to trigger a refresh for a dataset in Power BI Service.</summary>
[McpServerToolType]
public sealed class PowerBiCloudTriggerRefreshTool(IRevitBridgeClient bridge, ResultFormatter formatter)
{
    [McpServerTool(
        Name = "powerbi_cloud_trigger_refresh",
        Title = "Trigger Cloud Dataset Refresh",
        ReadOnly = false,
        Destructive = true,
        Idempotent = false,
        OpenWorld = false)]
    [Description("Triggers a data refresh for a dataset / semantic model in Power BI Service.")]
    public Task<CallToolResult> TriggerRefreshAsync(
        [Description("The dataset ID (GUID) to refresh.")]
        string datasetId,
        [Description("Optional workspace ID (GUID). If omitted, assumes 'My workspace'.")]
        string? workspaceId = null,
        [Description("Optional notification option: 'NoNotification' (default) or 'MailOnFailure'.")]
        string notifyOption = "NoNotification",
        CancellationToken cancellationToken = default)
    {
        return formatter.RunAsync(async () =>
        {
            var parameters = new
            {
                DatasetId = datasetId,
                WorkspaceId = workspaceId,
                NotifyOption = notifyOption,
            };

            var result = await bridge.SendAsync<object>(
                bridge.Profile.MethodPrefix + "cloud.refresh",
                parameters,
                TimeSpan.FromSeconds(30),
                null,
                cancellationToken).ConfigureAwait(false);

            return formatter.Text(result);
        });
    }
}
