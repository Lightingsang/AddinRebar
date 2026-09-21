using System.ComponentModel;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPPowerBi.Mcp.Server.Tools;

/// <summary>Tool to delete DAX measures from the Power BI model.</summary>
[McpServerToolType]
public sealed class PowerBiDeleteMeasureTool(IRevitBridgeClient bridge, ResultFormatter formatter)
{
    [McpServerTool(
        Name = "powerbi_delete_measure",
        Title = "Delete DAX Measure",
        ReadOnly = false,
        Destructive = true,
        Idempotent = false,
        OpenWorld = false)]
    [Description(
        "Deletes an existing DAX measure from a specified table. " +
        "Creates a backup snapshot before removing. Requires 'Allow Model Modifications / DAX Execution' checked on the Bridge UI.")]
    public Task<CallToolResult> DeleteMeasureAsync(
        [Description("The table name containing the measure.")]
        string tableName,
        [Description("The name of the measure to delete.")]
        string measureName,
        CancellationToken cancellationToken = default)
    {
        return formatter.RunAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return formatter.Error("TableName cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(measureName))
            {
                return formatter.Error("MeasureName cannot be empty.");
            }

            var parameters = new
            {
                TableName = tableName,
                MeasureName = measureName,
            };

            var result = await bridge.SendAsync<object>(
                bridge.Profile.MethodPrefix + "measure.delete",
                parameters,
                TimeSpan.FromSeconds(30),
                null,
                cancellationToken).ConfigureAwait(false);

            return formatter.Text(result);
        });
    }
}
