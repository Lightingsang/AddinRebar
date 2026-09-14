using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HPRebar.Mcp.Contracts.JsonRpc;

namespace HPAutoCad.McpBridge.Service;

/// <summary>
///     Turns whatever a script returned into JSON the AI can read. AutoCAD objects are summarised, never
///     walked: a Database is cyclic, most getters throw once the object is closed, and one line per
///     entity (handle, type, layer) is what the model actually uses. Must run while the bridge's
///     transaction is still open — entities the script fetched through it are disposed at Commit/Abort.
/// </summary>
public sealed class AutocadResultSerializer
{
    private const string TruncationMarker = "…[truncated]";

    private readonly JsonSerializerOptions _options;
    private readonly int _maxOutputBytes;

    public AutocadResultSerializer(int maxOutputBytes)
    {
        _maxOutputBytes = maxOutputBytes;
        _options = new JsonSerializerOptions(BridgeJson.Options)
        {
            MaxDepth = 8,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
        };
        _options.Converters.Add(new JsonStringEnumConverter());
        _options.Converters.Add(new AutocadObjectConverterFactory());
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

    /// <summary>`List<String>` / `Line` / `object` (anonymous) instead of assembly-qualified generic soup.</summary>
    private static string FriendlyName(Type type)
    {
        if (type.Name.Contains("AnonymousType")) return "object";
        if (type.IsGenericType)
        {
            var name = type.Name[..type.Name.IndexOf('`')];
            return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(FriendlyName))}>";
        }

        return IsAutocadType(type) ? type.FullName! : type.Name;
    }

    private static bool IsAutocadType(Type type) => type.Namespace is { } ns && ns.StartsWith("Autodesk.AutoCAD", StringComparison.Ordinal);

    private static string SafeToString(object value)
    {
        try { return value.ToString() ?? string.Empty; }
        catch (Exception exception) { return $"<{value.GetType().Name}: ToString threw {exception.GetType().Name}>"; }
    }

    /// <summary>Applies to every Autodesk.AutoCAD.* type except enums and primitives; structs included (ObjectId, Point3d are structs).</summary>
    private sealed class AutocadObjectConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => !typeToConvert.IsEnum && !typeToConvert.IsPrimitive && IsAutocadType(typeToConvert);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            (JsonConverter)Activator.CreateInstance(typeof(AutocadObjectConverter<>).MakeGenericType(typeToConvert))!;
    }

    private sealed class AutocadObjectConverter<T> : JsonConverter<T>
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotSupportedException("AutoCAD objects are output-only.");

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case ObjectId id:
                    writer.WriteStartObject();
                    writer.WriteString("handle", id.IsNull ? null : id.Handle.ToString());
                    writer.WriteString("class", id.IsNull ? null : SafeDxfName(id));
                    writer.WriteEndObject();
                    break;

                case Handle handle:
                    writer.WriteStringValue(handle.ToString());
                    break;

                case Point3d p:
                    WritePoint(writer, p.X, p.Y, p.Z);
                    break;

                case Point2d p:
                    WritePoint(writer, p.X, p.Y, null);
                    break;

                case Vector3d v:
                    WritePoint(writer, v.X, v.Y, v.Z);
                    break;

                case Vector2d v:
                    WritePoint(writer, v.X, v.Y, null);
                    break;

                case Extents3d extents:
                    writer.WriteStartObject();
                    writer.WritePropertyName("min");
                    WritePoint(writer, extents.MinPoint.X, extents.MinPoint.Y, extents.MinPoint.Z);
                    writer.WritePropertyName("max");
                    WritePoint(writer, extents.MaxPoint.X, extents.MaxPoint.Y, extents.MaxPoint.Z);
                    writer.WriteEndObject();
                    break;

                case Entity entity:
                    writer.WriteStartObject();
                    writer.WriteString("handle", entity.Handle.ToString());
                    writer.WriteString("type", entity.GetType().Name);
                    writer.WriteString("layer", Safe(() => entity.Layer));
                    writer.WriteString("dxfName", SafeDxfName(entity.ObjectId));
                    writer.WriteEndObject();
                    break;

                case DBObject dbObject:
                    writer.WriteStartObject();
                    writer.WriteString("handle", dbObject.Handle.ToString());
                    writer.WriteString("type", dbObject.GetType().Name);
                    if (dbObject is SymbolTableRecord record) writer.WriteString("name", Safe(() => record.Name));
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

        private static void WritePoint(Utf8JsonWriter writer, double x, double y, double? z)
        {
            writer.WriteStartObject();
            writer.WriteNumber("x", x);
            writer.WriteNumber("y", y);
            if (z is { } zz) writer.WriteNumber("z", zz);
            writer.WriteEndObject();
        }

        private static string? SafeDxfName(ObjectId id) => id.IsNull ? null : Safe(() => id.ObjectClass?.DxfName);

        private static string? Safe(Func<string?> read)
        {
            try { return read(); }
            catch { return null; } // closed object, erased object: no name to give
        }
    }
}
