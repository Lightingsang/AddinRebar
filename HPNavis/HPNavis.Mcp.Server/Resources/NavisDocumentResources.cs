using System.ComponentModel;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Server;

namespace HPNavis.Mcp.Server.Resources;

/// <summary>
///     The same session snapshot as get_navis_context, exposed as resources for hosts that let the user
///     attach context explicitly (Claude Desktop resource picker, VS Code "Add context").
/// </summary>
[McpServerResourceType]
public sealed class NavisDocumentResources(ContextService service)
{
    [McpServerResource(UriTemplate = "navis://document/info", Name = "navis_document_info", Title = "Navisworks model", MimeType = "application/json")]
    [Description("Open Navisworks model: version, title, path, units, appended models with their units, saved viewpoint / selection set / clash test counts, busy and heavy-operation flags.")]
    public Task<string> DocumentInfoAsync(CancellationToken cancellationToken) => service.ReadAsync(includeSelection: false, cancellationToken);

    [McpServerResource(UriTemplate = "navis://selection", Name = "navis_selection", Title = "Navisworks selection", MimeType = "application/json")]
    [Description("Model items currently selected in Navisworks (max 50) as {id = instance-guid hash, category = class display name, name = display name}, plus the model snapshot.")]
    public Task<string> SelectionAsync(CancellationToken cancellationToken) => service.ReadAsync(includeSelection: true, cancellationToken);
}
