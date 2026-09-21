using System.ComponentModel;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPPowerBi.Mcp.Server.Tools;

/// <summary>Tool to create or update DAX measures in the Power BI model.</summary>
[McpServerToolType]
public sealed class PowerBiCreateOrUpdateMeasureTool(IRevitBridgeClient bridge, ResultFormatter formatter)
{
    [McpServerTool(
        Name = "powerbi_create_or_update_measure",
        Title = "Create or Update DAX Measure",
        ReadOnly = false,
        Destructive = true,
        Idempotent = false,
        OpenWorld = false)]
    [Description(
        "Creates a new DAX measure or updates an existing measure in a specified table. " +
        "Validates DAX syntax, creates a metadata backup snapshot, and calls model.SaveChanges() to persist to Power BI Desktop. " +
        "Requires 'Allow Model Modifications / DAX Execution' checked on the Bridge UI.")]
    public Task<CallToolResult> CreateOrUpdateMeasureAsync(
        [Description("The target table name to place the measure in.")]
        string tableName,
        [Description("The name of the measure.")]
        string measureName,
        [Description("The DAX expression formula (e.g. 'SUM(Sales[Amount])').")]
        string expression,
        [Description("Optional description / docstring for the measure.")]
        string? description = null,
        [Description("Optional format string (e.g. '#,##0.00', '0.0%', '$#,##0').")]
        string? formatString = null,
        [Description("Optional display folder path within the table.")]
        string? displayFolder = null,
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

            if (string.IsNullOrWhiteSpace(expression))
            {
                return formatter.Error("DAX expression cannot be empty.");
            }

            var parameters = new
            {
                TableName = tableName,
                MeasureName = measureName,
                Expression = expression,
                Description = description,
                FormatString = formatString,
                DisplayFolder = displayFolder,
            };

            var result = await bridge.SendAsync<object>(
                bridge.Profile.MethodPrefix + "measure.upsert",
                parameters,
                TimeSpan.FromSeconds(30),
                null,
                cancellationToken).ConfigureAwait(false);

            return formatter.Text(result);
        });
    }
}
