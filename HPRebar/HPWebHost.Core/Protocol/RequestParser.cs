using System.Text.Json;

namespace HPWebHost.Protocol;

/// <summary>Either a well-formed request or the failure response to send instead.</summary>
public sealed record RequestParseResult(RequestEnvelope? Request, ResponseEnvelope? Rejection)
{
    public bool IsAccepted => Request is not null;
}

/// <summary>
///     Turns a page message into a v1 request. Accepts the JSON object itself or that object posted as a JSON string
///     (<c>postMessage(JSON.stringify(…))</c>); anything else is rejected with <c>BAD_REQUEST</c>, keeping the request id
///     whenever one could be read so the page can settle the call instead of waiting for its timeout.
/// </summary>
public static class RequestParser
{
    /// <summary>Upper bound for one message; Dashboard requests are a few hundred characters.</summary>
    public const int MaxMessageChars = 256 * 1024;

    public const int MaxIdChars = 100;

    private const string NoId = "";

    public static RequestParseResult Parse(string message) => Parse(message, allowStringWrapper: true);

    private static RequestParseResult Parse(string message, bool allowStringWrapper)
    {
        if (message.Length > MaxMessageChars)
        {
            return Reject(NoId, $"Message exceeds {MaxMessageChars} characters.");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(message);
        }
        catch (JsonException)
        {
            return Reject(NoId, "Message is not valid JSON.");
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.String && allowStringWrapper)
            {
                return Parse(root.GetString() ?? string.Empty, allowStringWrapper: false);
            }

            return root.ValueKind == JsonValueKind.Object
                ? ReadRequest(root)
                : Reject(NoId, "Message must be a JSON object.");
        }
    }

    private static RequestParseResult ReadRequest(JsonElement root)
    {
        var id = ReadString(root, "id");
        if (string.IsNullOrEmpty(id) || id.Length > MaxIdChars)
        {
            return Reject(NoId, $"Request id must be a non-empty string of at most {MaxIdChars} characters.");
        }

        if (!root.TryGetProperty("v", out var version) || version.ValueKind != JsonValueKind.Number ||
            !version.TryGetInt32(out var versionNumber) || versionNumber != ProtocolVersion.Current)
        {
            return Reject(id, $"Unsupported protocol version; expected v = {ProtocolVersion.Current}.");
        }

        if (ReadString(root, "kind") != EnvelopeKinds.Request)
        {
            return Reject(id, "Only messages of kind \"request\" are accepted.");
        }

        var command = ReadString(root, "command");
        if (string.IsNullOrWhiteSpace(command))
        {
            return Reject(id, "Request has no command.");
        }

        JsonElement? payload = root.TryGetProperty("payload", out var element) && element.ValueKind != JsonValueKind.Null
            ? element.Clone()
            : null;
        return new RequestParseResult(new RequestEnvelope(id, command, payload), null);
    }

    private static string? ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String ? element.GetString() : null;

    private static RequestParseResult Reject(string id, string message) =>
        new(null, ResponseEnvelope.Failure(id, new BridgeError(BridgeErrorCodes.BadRequest, message)));
}
