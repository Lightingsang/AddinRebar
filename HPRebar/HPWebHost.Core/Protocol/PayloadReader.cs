using System.Text.Json;

namespace HPWebHost.Protocol;

/// <summary>Reads a request payload as a typed record; a payload that does not fit is the caller's error (BAD_REQUEST).</summary>
public static class PayloadReader
{
    public static T Read<T>(JsonElement? payload)
    {
        try
        {
            var value = payload is { } element
                ? element.Deserialize<T>(BridgeJson.Options)
                : JsonSerializer.Deserialize<T>("{}", BridgeJson.Options);
            return value ?? throw new BridgeException(BridgeErrorCodes.BadRequest, $"Payload must be an object ({typeof(T).Name}).");
        }
        catch (JsonException exception)
        {
            throw new BridgeException(BridgeErrorCodes.BadRequest, $"Payload does not fit {typeof(T).Name}: {exception.Message}");
        }
    }
}
