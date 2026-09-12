using System.ComponentModel;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Services;
using HPRebar.Mcp.Server.Tools;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace HPRebar.Mcp.Server.Resources;

/// <summary>
///     The same session snapshot as get_revit_context, exposed as resources for hosts that let the user
///     attach context explicitly (Claude Desktop resource picker, VS Code "Add context").
/// </summary>
[McpServerResourceType]
public sealed class RevitDocumentResources(IRevitBridgeClient bridge)
{
    [McpServerResource(UriTemplate = "revit://document/info", Name = "revit_document_info", Title = "Revit document", MimeType = "application/json")]
    [Description("Active Revit document: version, title, path, family/read-only flags, display units, active view, open documents.")]
    public Task<string> DocumentInfoAsync(CancellationToken cancellationToken) => FetchAsync(includeSelection: false, cancellationToken);

    [McpServerResource(UriTemplate = "revit://selection", Name = "revit_selection", Title = "Revit selection", MimeType = "application/json")]
    [Description("Elements currently selected in Revit as {id, category, name}, plus the document snapshot.")]
    public Task<string> SelectionAsync(CancellationToken cancellationToken) => FetchAsync(includeSelection: true, cancellationToken);

    private async Task<string> FetchAsync(bool includeSelection, CancellationToken cancellationToken)
    {
        try
        {
            var context = await bridge.SendAsync<ContextResult>(
                JsonRpcMethods.Context,
                new ContextRequest(includeSelection),
                RevitContextTool.Timeout,
                null,
                cancellationToken).ConfigureAwait(false);

            return BridgeJson.Serialize(context);
        }
        catch (Exception exception) when (exception is BridgeUnavailableException or BridgeTimeoutException or BridgeErrorException)
        {
            // Resources have no IsError channel; the exception message is what the host shows the user.
            throw new McpException(ResultFormatter.StripPaths(exception.Message));
        }
    }
}
