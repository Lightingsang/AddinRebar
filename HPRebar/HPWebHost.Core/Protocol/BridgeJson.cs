using System.Text.Json.Serialization;
using System.Text.Json;

namespace HPWebHost.Protocol;

/// <summary>The one serializer configuration for the wire: camelCase names and enums, nulls kept (the page tests for them).</summary>
public static class BridgeJson
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
        };
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
