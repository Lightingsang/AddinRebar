using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HPRebar.Mcp.Contracts.JsonRpc;

/// <summary>
///     The one serializer configuration both processes use for the pipe, so a field name never differs
///     between what the bridge writes and what the server reads. camelCase matches the MCP wire format
///     the AI already sees, which keeps tool results readable without a second mapping.
/// </summary>
public static class BridgeJson
{
    public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        // The text is read by a model and by humans in logs, never embedded in HTML: keep '<', '>' and
        // non-ASCII (Vietnamese titles, unit symbols) readable instead of \uXXXX escapes.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static JsonElement? ToElement(object? value) =>
        value is null ? null : JsonSerializer.SerializeToElement(value, Options);

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);
}
