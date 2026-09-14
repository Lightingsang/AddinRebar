using System.ComponentModel;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Server;

namespace HPRebar.Mcp.Server.Hosts.Revit;

/// <summary>
///     The same session snapshot as get_revit_context, exposed as resources for hosts that let the user
///     attach context explicitly (Claude Desktop resource picker, VS Code "Add context").
/// </summary>
[McpServerResourceType]
public sealed class RevitDocumentResources(ContextService service)
{
    [McpServerResource(UriTemplate = "revit://document/info", Name = "revit_document_info", Title = "Revit document", MimeType = "application/json")]
    [Description("Active Revit document: version, title, path, family/read-only flags, display units, active view, open documents.")]
    public Task<string> DocumentInfoAsync(CancellationToken cancellationToken) => service.ReadAsync(includeSelection: false, cancellationToken);

    [McpServerResource(UriTemplate = "revit://selection", Name = "revit_selection", Title = "Revit selection", MimeType = "application/json")]
    [Description("Elements currently selected in Revit as {id, category, name}, plus the document snapshot.")]
    public Task<string> SelectionAsync(CancellationToken cancellationToken) => service.ReadAsync(includeSelection: true, cancellationToken);
}
