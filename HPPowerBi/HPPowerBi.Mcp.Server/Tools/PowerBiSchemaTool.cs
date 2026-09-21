using System.ComponentModel;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPPowerBi.Mcp.Server.Tools;

/// <summary>Tool to inspect the tabular schema: tables, columns, data types, measures, partitions, and relationships.</summary>
[McpServerToolType]
public sealed class PowerBiSchemaTool(IRevitBridgeClient bridge, ResultFormatter formatter)
{
    [McpServerTool(
        Name = "powerbi_get_schema",
        Title = "Get Power BI Model Schema",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description(
        "Inspects the Power BI tabular model schema. Returns list of tables, columns (name, dataType, isHidden), " +
        "measures (name, expression, formatString, displayFolder, description), partitions, and relationships (fromTable/Column -> toTable/Column, cardinality, crossFiltering). " +
        "Optionally filtered to a specific table.")]
    public Task<CallToolResult> GetSchemaAsync(
        [Description("Optional table name to filter schema for a single table.")]
        string? tableName = null,
        [Description("Whether to include detailed column metadata (default true).")]
        bool includeColumns = true,
        [Description("Whether to include measures (default true).")]
        bool includeMeasures = true,
        [Description("Whether to include table relationships (default true).")]
        bool includeRelationships = true,
        CancellationToken cancellationToken = default)
    {
        return formatter.RunAsync(async () =>
        {
            var parameters = new
            {
                TableName = tableName,
                IncludeColumns = includeColumns,
                IncludeMeasures = includeMeasures,
                IncludeRelationships = includeRelationships,
            };

            var result = await bridge.SendAsync<object>(
                bridge.Profile.Method("schema"),
                parameters,
                TimeSpan.FromSeconds(30),
                null,
                cancellationToken).ConfigureAwait(false);

            return formatter.Text(result);
        });
    }
}
