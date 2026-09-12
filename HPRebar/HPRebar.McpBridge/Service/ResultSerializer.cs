using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Autodesk.Revit.DB;
using HPRebar.Mcp.Contracts.JsonRpc;

namespace HPRebar.McpBridge.Service;

/// <summary>
///     Turns whatever a script returned into JSON the AI can read. Revit objects are summarised, never
///     walked: their property graphs are huge, cyclic, and many getters throw — one line per element
///     (id, name, category) is what the model actually uses.
/// </summary>
public sealed class ResultSerializer
{
    private const string TruncationMarker = "…[truncated]";

    private readonly JsonSerializerOptions _options;
    private readonly int _maxOutputBytes;

    public ResultSerializer(int maxOutputBytes)
    {
        _maxOutputBytes = maxOutputBytes;
        _options = new JsonSerializerOptions(BridgeJson.Options)
        {
            MaxDepth = 8,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
        };
        _options.Converters.Add(new JsonStringEnumConverter());
        _options.Converters.Add(new RevitObjectConverterFactory());
    }

    public (JsonElement? Value, string ValueType, bool Truncated) Serialize(object? value)
    {
        if (value is null) return (null, "null", false);

        var typeName = FriendlyName(value.GetType());
        byte[] bytes;

        try
        {
            bytes = JsonSerializer.SerializeToUtf8Bytes(value, value.GetType(), _options);
        }
        catch (Exception exception)
        {
            var fallback = new { type = typeName, text = SafeToString(value), note = "value is not serializable: " + exception.GetType().Name };
            bytes = JsonSerializer.SerializeToUtf8Bytes(fallback, _options);
        }

        if (bytes.Length <= _maxOutputBytes)
        {
            using var document = JsonDocument.Parse(bytes);
            return (document.RootElement.Clone(), typeName, false);
        }

        // Too big to hand to a model verbatim: keep the head as text so the shape is still recognisable.
        var head = Encoding.UTF8.GetString(bytes, 0, _maxOutputBytes).TrimEnd('�') + TruncationMarker;
        return (JsonSerializer.SerializeToElement(head, _options), typeName, true);
    }

    /// <summary>`List<String>` / `Wall` / `object` (anonymous) instead of assembly-qualified generic soup.</summary>
    private static string FriendlyName(Type type)
    {
        if (type.Name.Contains("AnonymousType")) return "object";
        if (type.IsGenericType)
        {
            var name = type.Name[..type.Name.IndexOf('`')];
            return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(FriendlyName))}>";
        }

        return type.Namespace is { } ns && ns.StartsWith("Autodesk.Revit", StringComparison.Ordinal) ? type.FullName! : type.Name;
    }

    private static string SafeToString(object value)
    {
        try { return value.ToString() ?? string.Empty; }
        catch (Exception exception) { return $"<{value.GetType().Name}: ToString threw {exception.GetType().Name}>"; }
    }

    /// <summary>Applies to every Autodesk.Revit.* reference type; enums and primitives fall through to the defaults.</summary>
    private sealed class RevitObjectConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) =>
            !typeToConvert.IsEnum
            && !typeToConvert.IsPrimitive
            && typeToConvert.Namespace is { } ns
            && ns.StartsWith("Autodesk.Revit", StringComparison.Ordinal);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            (JsonConverter)Activator.CreateInstance(typeof(RevitObjectConverter<>).MakeGenericType(typeToConvert))!;
    }

    private sealed class RevitObjectConverter<T> : JsonConverter<T>
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotSupportedException("Revit objects are output-only.");

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case ElementId id:
                    writer.WriteNumberValue(id.Value);
                    break;

                case XYZ point:
                    writer.WriteStartObject();
                    writer.WriteNumber("x", point.X);
                    writer.WriteNumber("y", point.Y);
                    writer.WriteNumber("z", point.Z);
                    writer.WriteEndObject();
                    break;

                case Element element:
                    var info = RevitContextReader.Describe(element);
                    writer.WriteStartObject();
                    writer.WriteNumber("id", info.Id);
                    writer.WriteString("name", info.Name);
                    writer.WriteString("category", info.Category);
                    writer.WriteString("type", element.GetType().Name);
                    writer.WriteEndObject();
                    break;

                case Category category:
                    writer.WriteStartObject();
                    writer.WriteNumber("id", category.Id.Value);
                    writer.WriteString("name", category.Name);
                    writer.WriteEndObject();
                    break;

                case Parameter parameter:
                    writer.WriteStartObject();
                    writer.WriteString("name", parameter.Definition?.Name);
                    writer.WriteString("value", parameter.AsValueString() ?? parameter.AsString());
                    writer.WriteString("storageType", parameter.StorageType.ToString());
                    writer.WriteEndObject();
                    break;

                default:
                    writer.WriteStartObject();
                    writer.WriteString("type", value?.GetType().FullName);
                    writer.WriteString("text", value is null ? null : SafeToString(value));
                    writer.WriteEndObject();
                    break;
            }
        }
    }
}
