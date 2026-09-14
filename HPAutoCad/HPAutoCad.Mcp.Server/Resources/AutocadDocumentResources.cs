using System.ComponentModel;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Server;

namespace HPAutoCad.Mcp.Server.Resources;

/// <summary>
///     The same session snapshot as get_autocad_context, exposed as resources for hosts that let the user
///     attach context explicitly (Claude Desktop resource picker, VS Code "Add context").
/// </summary>
[McpServerResourceType]
public sealed class AutocadDocumentResources(ContextService service)
{
    [McpServerResource(UriTemplate = "autocad://document/info", Name = "autocad_document_info", Title = "AutoCAD drawing", MimeType = "application/json")]
    [Description("Active AutoCAD drawing: version, title, path, read-only/quiescent flags, drawing units, current layout and layer, open drawings.")]
    public Task<string> DocumentInfoAsync(CancellationToken cancellationToken) => service.ReadAsync(includeSelection: false, cancellationToken);

    [McpServerResource(UriTemplate = "autocad://selection", Name = "autocad_selection", Title = "AutoCAD selection", MimeType = "application/json")]
    [Description("Entities currently selected in AutoCAD as {id = handle, category = layer, name = DXF name}, plus the drawing snapshot.")]
    public Task<string> SelectionAsync(CancellationToken cancellationToken) => service.ReadAsync(includeSelection: true, cancellationToken);
}
