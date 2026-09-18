using System.ComponentModel;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Server;

namespace HPCivil3d.Mcp.Server.Resources;

/// <summary>
///     The same session snapshot as get_civil3d_context, exposed as resources for hosts that let the user
///     attach context explicitly (Claude Desktop resource picker, VS Code "Add context").
/// </summary>
[McpServerResourceType]
public sealed class Civil3dDocumentResources(ContextService service)
{
    [McpServerResource(UriTemplate = "civil3d://document/info", Name = "civil3d_document_info", Title = "Civil 3D drawing", MimeType = "application/json")]
    [Description("Active Civil 3D drawing: version, title, path, read-only/quiescent flags, the Civil drawing unit and zone, insunitsMismatch, object counts (alignments, surfaces, corridors, pipe networks, COGO points), current layout and layer, open drawings.")]
    public Task<string> DocumentInfoAsync(CancellationToken cancellationToken) => service.ReadAsync(includeSelection: false, cancellationToken);

    [McpServerResource(UriTemplate = "civil3d://selection", Name = "civil3d_selection", Title = "Civil 3D selection", MimeType = "application/json")]
    [Description("Entities currently selected in Civil 3D as {id = handle, category = layer, name = DXF name}, plus the drawing snapshot.")]
    public Task<string> SelectionAsync(CancellationToken cancellationToken) => service.ReadAsync(includeSelection: true, cancellationToken);
}
