using System.ComponentModel;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Server;

namespace HPSap2000.Mcp.Server.Resources;

/// <summary>
///     The same session snapshot as get_sap2000_context, exposed as resources for hosts that let the user
///     attach context explicitly (Claude Desktop resource picker, VS Code "Add context").
/// </summary>
[McpServerResourceType]
public sealed class Sap2000DocumentResources(ContextService service)
{
    [McpServerResource(UriTemplate = "sap2000://model/info", Name = "sap2000_model_info", Title = "SAP2000 model", MimeType = "application/json")]
    [Description("Attached SAP2000 model: version, file title and path, lock state, present and database units, whether destructive operations are allowed, point/frame/area counts.")]
    public Task<string> ModelInfoAsync(CancellationToken cancellationToken) => service.ReadAsync(includeSelection: false, cancellationToken);

    [McpServerResource(UriTemplate = "sap2000://selection", Name = "sap2000_selection", Title = "SAP2000 selection", MimeType = "application/json")]
    [Description("Objects currently selected in SAP2000 (max 50) as {id, category = object type, name = unique name}, plus the model snapshot.")]
    public Task<string> SelectionAsync(CancellationToken cancellationToken) => service.ReadAsync(includeSelection: true, cancellationToken);
}
