using System.ComponentModel;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Server;

namespace HPPowerBi.Mcp.Server.Resources;

/// <summary>
///     Exposes Power BI schema and document state as MCP resources.
/// </summary>
[McpServerResourceType]
public sealed class PowerBiSchemaResource(IRevitBridgeClient bridge, ContextService context)
{
    [McpServerResource(UriTemplate = "powerbi://schema", Name = "powerbi_schema", Title = "Power BI Tabular Schema", MimeType = "application/json")]
    [Description("Full tabular schema of the active Power BI Desktop model: tables, columns, data types, measures, and relationships.")]
    public async Task<string> GetSchemaAsync(CancellationToken cancellationToken)
    {
        var result = await bridge.SendAsync<object>(
            bridge.Profile.Method("schema"),
            new { IncludeColumns = true, IncludeMeasures = true, IncludeRelationships = true },
            TimeSpan.FromSeconds(30),
            null,
            cancellationToken).ConfigureAwait(false);

        return BridgeJson.Serialize(result);
    }

    [McpServerResource(UriTemplate = "powerbi://document/info", Name = "powerbi_document_info", Title = "Power BI Document Info", MimeType = "application/json")]
    [Description("Active Power BI Desktop session snapshot: version, document title, port, database, table and measure counts.")]
    public Task<string> DocumentInfoAsync(CancellationToken cancellationToken) =>
        context.ReadAsync(includeSelection: false, cancellationToken);
}
