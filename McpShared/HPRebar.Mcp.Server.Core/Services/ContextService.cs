using System.Text.Json;
using System.Text.Json.Nodes;
using HPRebar.Mcp.Contracts;
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

            return formatter.Text(Shape(context));
        });
    }

    /// <summary>For resources: JSON text, or an <see cref="McpException"/> without machine paths.</summary>
    public async Task<string> ReadAsync(bool includeSelection, CancellationToken cancellationToken)
    {
        try
        {
            var context = await FetchAsync(includeSelection, cancellationToken).ConfigureAwait(false);

            return BridgeJson.Serialize(Shape(context));
        }
        catch (Exception exception) when (exception is BridgeUnavailableException or BridgeTimeoutException or BridgeErrorException)
        {
            throw new McpException(ResultFormatter.StripPaths(exception.Message));
        }
    }

    /// <summary>
    ///     The wire keeps the historical <c>revitVersion</c> and <c>isFamily</c> fields for older bridges;
    ///     another host's AI should not see Revit-named fields, so they are dropped and <c>hostVersion</c>
    ///     stands alone. Revit output is byte-for-byte what it was.
    /// </summary>
    private object Shape(ContextResult context)
    {
        if (bridge.Profile.HostId == PipeNaming.RevitHost) return context;

        var node = JsonSerializer.SerializeToNode(context, BridgeJson.Options) as JsonObject ?? new JsonObject();
        foreach (var field in RevitOnlyFields) node.Remove(field);
        return node;
    }

    private static readonly string[] RevitOnlyFields =
    [
        WireName(nameof(ContextResult.RevitVersion)),
        WireName(nameof(ContextResult.IsFamily)),
    ];

    private static string WireName(string property) => BridgeJson.Options.PropertyNamingPolicy?.ConvertName(property) ?? property;

    private Task<ContextResult> FetchAsync(bool includeSelection, CancellationToken cancellationToken) =>
        bridge.SendAsync<ContextResult>(
            bridge.Profile.Method(JsonRpcMethods.ContextSuffix),
            new ContextRequest(includeSelection),
            Timeout,
            null,
            cancellationToken);
}
