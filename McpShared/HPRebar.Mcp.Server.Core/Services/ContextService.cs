using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace HPRebar.Mcp.Server.Services;

/// <summary>
///     The body of every host's `get_*_context` tool and `{scheme}://document/info|selection` resources:
///     one `*.context` round trip, formatted for the channel that asked. Tools get a
///     <see cref="CallToolResult"/> (errors are tool errors the AI can act on); resources have no error
///     channel, so a failure there becomes an <see cref="McpException"/> the host shows the user.
/// </summary>
public sealed class ContextService(IRevitBridgeClient bridge, ResultFormatter formatter)
{
    /// <summary>Context, inspect and cancel are quick; a bridge that takes longer than this is stuck.</summary>
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    public Task<CallToolResult> GetAsync(bool includeSelection, CancellationToken cancellationToken)
    {
        return formatter.RunAsync(async () =>
        {
            var context = await FetchAsync(includeSelection, cancellationToken).ConfigureAwait(false);

            return formatter.Text(context);
        });
    }

    /// <summary>For resources: JSON text, or an <see cref="McpException"/> without machine paths.</summary>
    public async Task<string> ReadAsync(bool includeSelection, CancellationToken cancellationToken)
    {
        try
        {
            var context = await FetchAsync(includeSelection, cancellationToken).ConfigureAwait(false);

            return BridgeJson.Serialize(context);
        }
        catch (Exception exception) when (exception is BridgeUnavailableException or BridgeTimeoutException or BridgeErrorException)
        {
            throw new McpException(ResultFormatter.StripPaths(exception.Message));
        }
    }

    private Task<ContextResult> FetchAsync(bool includeSelection, CancellationToken cancellationToken) =>
        bridge.SendAsync<ContextResult>(
            bridge.Profile.Method(JsonRpcMethods.ContextSuffix),
            new ContextRequest(includeSelection),
            Timeout,
            null,
            cancellationToken);
}
