using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace HPRebar.Mcp.Server.Services;

/// <summary>
///     Turns bridge answers and failures into what the AI should see. Two channels, chosen by whether the
///     model can do something about it: a <see cref="CallToolResult"/> with IsError (fix the code, wait,
///     open a document) or an <see cref="McpException"/> (the conversation with Revit is broken).
/// </summary>
public sealed class ResultFormatter
{
    public CallToolResult Text(object payload) => new CallToolResult
    {
        IsError = false,
        Content = [new TextContentBlock { Text = BridgeJson.Serialize(payload) }],
    };

    public CallToolResult Error(string message) => new CallToolResult
    {
        IsError = true,
        Content = [new TextContentBlock { Text = StripPaths(message) }],
    };

    public CallToolResult FromExecute(ExecuteResult result)
    {
        if (result.Message is not null) result.Message = StripPaths(result.Message);
        // The snapshot is a file name by contract; a bridge that sends a full path anyway must not leak it here.
        if (result.Snapshot is not null) result.Snapshot = Path.GetFileName(result.Snapshot);

        if (result.Diagnostics.Count > 0)
        {
            result.Diagnostics = result.Diagnostics
                .Select(d => d with { Message = StripPaths(d.Message) })
                .ToArray();
        }

        return new CallToolResult
        {
            IsError = result.IsError,
            Content = [new TextContentBlock { Text = BridgeJson.Serialize(result) }],
        };
    }

    /// <summary>
    ///     Runs a bridge call and maps its failures. Anything the AI (or the user next to Revit) can fix —
    ///     Revit closed, bridge off, timeout, disabled / busy / no document — comes back as a tool error
    ///     with a plain message. Only a bridge bug becomes an <see cref="McpException"/>, which the SDK
    ///     also surfaces as a tool error but logs with its stack trace so it is not lost.
    /// </summary>
    public async Task<CallToolResult> RunAsync(Func<Task<CallToolResult>> action)
    {
        try
        {
            return await action().ConfigureAwait(false);
        }
        catch (BridgeUnavailableException exception)
        {
            return Error(exception.Message);
        }
        catch (BridgeTimeoutException exception)
        {
            return Error(exception.Message);
        }
        catch (BridgeErrorException exception) when (BridgeErrorCode.IsActionable(exception.Code))
        {
            return Error(exception.Message);
        }
        catch (BridgeErrorException exception)
        {
            throw new McpException($"Bridge error {exception.Code}: {StripPaths(exception.Message)}");
        }
    }

    /// <summary>Last line of defence against a machine path leaking into the model's context.</summary>
    public static string StripPaths(string text) => SafeText.StripPaths(text);
}
