using System.ComponentModel;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Server;

namespace HPTekla.Mcp.Server.Hosts.Tekla.Resources;

/// <summary>
///     Exposes Tekla Structures model snapshot as resources (`tekla://...`) for MCP clients.
/// </summary>
[McpServerResourceType]
public sealed class TeklaResourceProvider(ContextService service)
{
    [McpServerResource(UriTemplate = "tekla://model/info", Name = "tekla_model_info", Title = "Tekla model info", MimeType = "application/json")]
    [Description("Active Tekla Structures model: version, file title and path, project name, object counts, and readiness.")]
    public Task<string> ModelInfoAsync(CancellationToken cancellationToken) => service.ReadAsync(includeSelection: false, cancellationToken);

    [McpServerResource(UriTemplate = "tekla://selection", Name = "tekla_selection", Title = "Tekla selection", MimeType = "application/json")]
    [Description("Objects currently selected in Tekla Structures, plus the model snapshot.")]
    public Task<string> SelectionAsync(CancellationToken cancellationToken) => service.ReadAsync(includeSelection: true, cancellationToken);
}
