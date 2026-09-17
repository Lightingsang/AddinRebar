using System.Collections;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HPEtabs.McpBridge.Service;

/// <summary>
///     Turns what a script returned into the JSON the AI reads. Plain values, arrays, dictionaries and
///     anonymous objects serialise as they are; an OAPI proxy (any type from the ETABSv1 wrapper, or a raw COM
///     object) is summarised as its type name — walking it would call into ETABS property by property.
///     Output is capped so a script that returns every joint of a tower cannot swamp the pipe.
/// </summary>
public sealed class EtabsResultSerializer
{
    private const int MaxItems = 500;

    private readonly int _maxBytes;
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        MaxDepth = 16,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
    };

    public EtabsResultSerializer(int maxBytes)
    {
        _maxBytes = maxBytes;
    }

    public (JsonElement? value, string valueType, bool truncated) Serialize(object? value)
    {
        if (value is null) return (null, "null", false);

        var shaped = Shape(value, 0);
        var json = JsonSerializer.SerializeToUtf8Bytes(shaped, _options);
        var truncated = false;

        if (json.Length > _maxBytes)
        {
            truncated = true;
            var preview = Encoding.UTF8.GetString(json, 0, Math.Min(_maxBytes, json.Length));
            json = JsonSerializer.SerializeToUtf8Bytes(new { truncatedPreview = preview, originalBytes = json.Length }, _options);
        }

        using var document = JsonDocument.Parse(json);
        return (document.RootElement.Clone(), TypeName(value), truncated);
    }

    private static object? Shape(object? value, int depth)
    {
        if (value is null) return null;
        if (depth > 6) return TypeName(value);

        switch (value)
        {
            case string or bool or byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal or char or Guid or DateTime or DateTimeOffset or TimeSpan:
                return value;
            case Enum e:
                return e.ToString();
            case JsonElement element:
                return element;
            case IDictionary dictionary:
                return ShapeDictionary(dictionary, depth);
            case IEnumerable enumerable:
                return enumerable.Cast<object?>().Take(MaxItems).Select(item => Shape(item, depth + 1)).ToArray();
        }

        var type = value.GetType();
        if (IsOapiProxy(type)) return new { type = type.Name };

        // Anonymous objects, records and plain classes: their public readable properties, shaped recursively.
        return type.GetProperties().Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToDictionary(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name), p => Shape(SafeGet(p, value), depth + 1));
    }

    /// <summary>
    ///     Through the non-generic enumerator on purpose: `Dictionary&lt;K,V&gt;` hands `KeyValuePair` boxes to a plain
    ///     `IEnumerable` walk, so `Cast&lt;DictionaryEntry&gt;()` throws — `IDictionaryEnumerator` always yields Key/Value.
    /// </summary>
    private static Dictionary<string, object?> ShapeDictionary(IDictionary dictionary, int depth)
    {
        var shaped = new Dictionary<string, object?>();
        var enumerator = dictionary.GetEnumerator();
        while (shaped.Count < MaxItems && enumerator.MoveNext())
            shaped[enumerator.Key?.ToString() ?? "null"] = Shape(enumerator.Value, depth + 1);
        return shaped;
    }

    private static object? SafeGet(System.Reflection.PropertyInfo property, object target)
    {
        try { return property.GetValue(target); }
        catch (Exception exception) { return $"<{exception.GetType().Name}>"; }
    }

    private static bool IsOapiProxy(Type type) =>
        type.IsCOMObject || string.Equals(type.Assembly.GetName().Name, "ETABSv1", StringComparison.OrdinalIgnoreCase);

    private static string TypeName(object value) => value.GetType().IsCOMObject ? "COM object" : value.GetType().Name;
}
