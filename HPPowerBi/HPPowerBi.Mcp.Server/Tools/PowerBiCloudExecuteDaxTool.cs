using System.ComponentModel;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPPowerBi.Mcp.Server.Tools;

/// <summary>Tool to execute DAX queries against a cloud dataset via Power BI REST API.</summary>
[McpServerToolType]
public sealed class PowerBiCloudExecuteDaxTool(IRevitBridgeClient bridge, ResultFormatter formatter)
{
    [McpServerTool(
        Name = "powerbi_cloud_execute_dax",
        Title = "Execute DAX on Cloud Dataset",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Executes a DAX query against a Power BI Service cloud dataset via the Power BI REST API executeQueries endpoint.")]
    public Task<CallToolResult> ExecuteCloudDaxAsync(
        [Description("The dataset ID (GUID) to query.")]
        string datasetId,
        [Description("The DAX query to execute (e.g. 'EVALUATE TOPN(5, Sales)').")]
        string query,
        [Description("Optional workspace ID (GUID). If omitted, assumes 'My workspace'.")]
        string? workspaceId = null,
        CancellationToken cancellationToken = default)
    {
        return formatter.RunAsync(async () =>
        {
            var parameters = new
            {
                DatasetId = datasetId,
                Query = query,
                WorkspaceId = workspaceId,
            };

            var result = await bridge.SendAsync<object>(
                bridge.Profile.MethodPrefix + "cloud.dax",
                parameters,
                TimeSpan.FromSeconds(60),
                null,
                cancellationToken).ConfigureAwait(false);

            return formatter.Text(result);
        });
    }
}
