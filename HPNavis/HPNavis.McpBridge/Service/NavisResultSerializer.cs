using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Autodesk.Navisworks.Api;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Scripting;
// The feature's own Model namespace shadows Autodesk.Navisworks.Api.Model inside HPNavis.McpBridge.*.
using NavisModel = Autodesk.Navisworks.Api.Model;

namespace HPNavis.McpBridge.Service;

/// <summary>
///     Turns whatever a script returned into JSON the AI can read. Navisworks objects are summarised,
///     never walked: a <see cref="ModelItem"/> reaches the whole tree through Parent/Children and its
///     property categories are hundreds of entries, so one line per item (name, class, model, guid,
///     bounding box in mm) is what the model actually uses. Lengths are converted with the document
///     units captured for the run.
/// </summary>
public sealed class NavisResultSerializer
{
    private const string TruncationMarker = "…[truncated]";

    private readonly int _maxOutputBytes;

    public NavisResultSerializer(int maxOutputBytes)
    {
        _maxOutputBytes = maxOutputBytes;
    }

    public (JsonElement? Value, string ValueType, bool Truncated) Serialize(object? value, ScriptUnits units)
    {
        if (value is null) return (null, "null", false);

        var options = new JsonSerializerOptions(BridgeJson.Options) { MaxDepth = 8, ReferenceHandler = ReferenceHandler.IgnoreCycles };
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new NavisObjectConverterFactory(units));

        var typeName = FriendlyName(value.GetType());
        byte[] bytes;

        try
        {
            bytes = JsonSerializer.SerializeToUtf8Bytes(value, value.GetType(), options);
        }
        catch (Exception exception)
        {
            var fallback = new { type = typeName, text = SafeToString(value), note = "value is not serializable: " + exception.GetType().Name };
            bytes = JsonSerializer.SerializeToUtf8Bytes(fallback, options);
        }

        if (bytes.Length <= _maxOutputBytes)
        {
            using var document = JsonDocument.Parse(bytes);
            return (document.RootElement.Clone(), typeName, false);
        }

        var head = Encoding.UTF8.GetString(bytes, 0, _maxOutputBytes).TrimEnd('�') + TruncationMarker;
        return (JsonSerializer.SerializeToElement(head, options), typeName, true);
    }

    /// <summary>`List<String>` / `ModelItem` / `object` (anonymous) instead of assembly-qualified generic soup.</summary>
    private static string FriendlyName(Type type)
    {
        if (type.Name.Contains("AnonymousType")) return "object";
        if (type.IsGenericType)
        {
            var name = type.Name.Substring(0, type.Name.IndexOf('`'));
            return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(FriendlyName))}>";
        }

        return type.Name;
    }

    private static bool IsNavisType(Type type) => type.Namespace is { } ns && ns.StartsWith("Autodesk.Navisworks", StringComparison.Ordinal);

    private static string SafeToString(object value)
    {
        try { return value.ToString() ?? string.Empty; }
        catch (Exception exception) { return $"<{value.GetType().Name}: {exception.GetType().Name}>"; }
    }

    private sealed class NavisObjectConverterFactory(ScriptUnits units) : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => !typeToConvert.IsEnum && !typeToConvert.IsPrimitive && IsNavisType(typeToConvert);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            (JsonConverter)Activator.CreateInstance(typeof(NavisObjectConverter<>).MakeGenericType(typeToConvert), units)!;
    }

    private sealed class NavisObjectConverter<T>(ScriptUnits units) : JsonConverter<T>
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotSupportedException("Navisworks objects are write-only in results.");

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case ModelItem item:
                    WriteModelItem(writer, item);
                    break;
                case NavisModel model:
                    writer.WriteStartObject();
                    writer.WriteString("type", "Model");
                    writer.WriteString("fileName", Safe(() => model.FileName));
                    writer.WriteString("sourceFileName", Safe(() => model.SourceFileName));
                    writer.WriteString("units", Safe(() => model.Units.ToString()));
                    writer.WriteString("guid", Safe(() => model.Guid.ToString()));
                    writer.WriteEndObject();
                    break;
                case SavedItem saved:
                    writer.WriteStartObject();
                    writer.WriteString("type", saved.GetType().Name);
                    writer.WriteString("displayName", Safe(() => saved.DisplayName));
                    writer.WriteString("guid", Safe(() => saved.Guid.ToString()));
                    writer.WriteBoolean("isGroup", saved.IsGroup);
                    if (saved is GroupItem group) writer.WriteNumber("childCount", Safe(() => (int?)group.Children.Count) ?? 0);
                    if (saved is SelectionSet set)
                    {
                        writer.WriteBoolean("hasSearch", Safe(() => (bool?)set.HasSearch) ?? false);
                        writer.WriteBoolean("hasExplicitItems", Safe(() => (bool?)set.HasExplicitModelItems) ?? false);
                    }
                    writer.WriteEndObject();
                    break;
                case Point3D point:
                    WritePoint(writer, point);
                    break;
                case BoundingBox3D box:
                    WriteBox(writer, box);
                    break;
                case DataProperty property:
                    writer.WriteStartObject();
                    writer.WriteString("name", Safe(() => property.DisplayName));
                    writer.WriteString("value", Safe(() => property.Value.ToDisplayString()));
                    writer.WriteEndObject();
                    break;
                case PropertyCategory category:
                    writer.WriteStartObject();
                    writer.WriteString("category", Safe(() => category.DisplayName));
                    writer.WriteNumber("propertyCount", Safe(() => (int?)category.Properties.Count) ?? 0);
                    writer.WriteEndObject();
                    break;
                default:
                    writer.WriteStartObject();
                    writer.WriteString("type", value?.GetType().Name);
                    writer.WriteString("text", value is null ? null : SafeToString(value));
                    writer.WriteEndObject();
                    break;
            }
        }

        private void WriteModelItem(Utf8JsonWriter writer, ModelItem item)
        {
            writer.WriteStartObject();
            writer.WriteString("type", "ModelItem");
            writer.WriteString("displayName", Safe(() => item.DisplayName));
            writer.WriteString("className", Safe(() => item.ClassDisplayName));
            writer.WriteString("model", Safe(() => item.Model?.FileName));
            writer.WriteString("guid", Safe(() => item.InstanceGuid == Guid.Empty ? null : item.InstanceGuid.ToString()));
            writer.WriteBoolean("hasGeometry", Safe(() => (bool?)item.HasGeometry) ?? false);
            writer.WriteBoolean("isHidden", Safe(() => (bool?)item.IsHidden) ?? false);
            writer.WriteNumber("childCount", Safe(() => (int?)item.Children.Count()) ?? 0);
            var box = Safe(() => item.BoundingBox());
            if (box is not null && !box.IsEmpty)
            {
                writer.WritePropertyName("bboxMm");
                WriteBox(writer, box);
            }
            writer.WriteEndObject();
        }

        private void WriteBox(Utf8JsonWriter writer, BoundingBox3D box)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("min");
            WritePoint(writer, box.Min);
            writer.WritePropertyName("max");
            WritePoint(writer, box.Max);
            writer.WriteEndObject();
        }

        private void WritePoint(Utf8JsonWriter writer, Point3D point)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(Math.Round(units.ToMm(point.X), 1));
            writer.WriteNumberValue(Math.Round(units.ToMm(point.Y), 1));
            writer.WriteNumberValue(Math.Round(units.ToMm(point.Z), 1));
            writer.WriteEndArray();
        }

        private static TValue? Safe<TValue>(Func<TValue?> read)
        {
            try { return read(); }
            catch { return default; }
        }
    }
}
