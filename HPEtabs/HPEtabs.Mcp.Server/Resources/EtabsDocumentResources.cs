using System.ComponentModel;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Server;

namespace HPEtabs.Mcp.Server.Resources;

/// <summary>
///     The same session snapshot as get_etabs_context, exposed as resources for hosts that let the user
///     attach context explicitly (Claude Desktop resource picker, VS Code "Add context").
/// </summary>
[McpServerResourceType]
public sealed class EtabsDocumentResources(ContextService service)
{
    [McpServerResource(UriTemplate = "etabs://model/info", Name = "etabs_model_info", Title = "ETABS model", MimeType = "application/json")]
    [Description("Attached ETABS model: version, file title and path, lock state, present and database units, whether destructive operations are allowed, point/frame/area counts.")]
    public Task<string> ModelInfoAsync(CancellationToken cancellationToken) => service.ReadAsync(includeSelection: false, cancellationToken);

    [McpServerResource(UriTemplate = "etabs://selection", Name = "etabs_selection", Title = "ETABS selection", MimeType = "application/json")]
    [Description("Objects currently selected in ETABS (max 50) as {id, category = object type, name = unique name}, plus the model snapshot.")]
    public Task<string> SelectionAsync(CancellationToken cancellationToken) => service.ReadAsync(includeSelection: true, cancellationToken);
}
