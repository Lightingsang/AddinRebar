using System.Text.Json;

namespace HPCivil3d.Mcp.Server.Tests;

/// <summary>Reads a seed's inputSchema / code the way the structure tests need: every string, the numeric and array properties, the keys of nested item schemas, and the item keys the code reads off a list element.</summary>
internal static class SeedSchema
{
    /// <summary>Keys of the object schemas nested inside array items or object properties (the top level belongs to the analyzer).</summary>
    public static IEnumerable<string> NestedProperties(JsonElement schema)
    {
        if (!schema.TryGetProperty("properties", out var properties)) yield break;
        foreach (var property in properties.EnumerateObject())
        {
            var type = property.Value.TryGetProperty("type", out var t) ? t.GetString() : null;
            JsonElement nested = default;
            var hasNested = type == "object" ? property.Value.TryGetProperty("properties", out _) && (nested = property.Value).ValueKind == JsonValueKind.Object
                : type == "array" && property.Value.TryGetProperty("items", out nested) && nested.TryGetProperty("properties", out _);
            if (!hasNested) continue;
            foreach (var key in nested.GetProperty("properties").EnumerateObject().Select(p => p.Name)) yield return key;
            foreach (var deeper in NestedProperties(nested)) yield return deeper;
        }
    }

    /// <summary>The fallback literal of every `args.X("key", fallback)` read of the given top-level key (`args.Int("limit", 100)` → "100").</summary>
    public static IEnumerable<string> ArgReadsWithFallback(string code, string key) =>
        System.Text.RegularExpressions.Regex.Matches(code, @"(?<![A-Za-z0-9_.])args\.(?:Str|Double|Int|Long|Bool)\(\s*""" + System.Text.RegularExpressions.Regex.Escape(key) + @"""\s*,\s*(?<fallback>""[^""]*""|[-A-Za-z0-9_.]+)\s*\)")
            .Select(m => m.Groups["fallback"].Value);

    /// <summary>`receiver.Str("key")` / `.Double(` / … where the receiver is not the `args` global.</summary>
    public static IEnumerable<string> NestedArgReads(string code) =>
        System.Text.RegularExpressions.Regex.Matches(code, @"(?<![A-Za-z0-9_.])(?<receiver>[A-Za-z_][A-Za-z0-9_]*(?:\[[^\]]*\])?)\.(?:Str|Double|Int|Long|Bool|Has|Require|Obj|List|Strings|Doubles|Longs)\(\s*""(?<key>[^""]+)""")
            .Where(m => m.Groups["receiver"].Value.Split('[')[0] != "args")
            .Select(m => m.Groups["key"].Value);

    public static IEnumerable<string> Strings(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => [element.GetString()!],
        JsonValueKind.Object => element.EnumerateObject().SelectMany(p => Strings(p.Value)),
        JsonValueKind.Array => element.EnumerateArray().SelectMany(Strings),
        _ => [],
    };

    /// <summary>Every numeric property of the schema, nested object items included (points[].x …).</summary>
    public static IEnumerable<(string Name, JsonElement Property)> NumericProperties(JsonElement schema)
    {
        if (!schema.TryGetProperty("properties", out var properties)) yield break;
        foreach (var property in properties.EnumerateObject())
        {
            var type = property.Value.TryGetProperty("type", out var t) ? t.GetString() : null;
            if (type is "number" or "integer") yield return (property.Name, property.Value);
            if (type == "object") foreach (var nested in NumericProperties(property.Value)) yield return nested;
            if (type == "array" && property.Value.TryGetProperty("items", out var items)) foreach (var nested in NumericProperties(items)) yield return nested;
        }
    }

    public static IEnumerable<JsonElement> ArrayProperties(JsonElement schema)
    {
        if (!schema.TryGetProperty("properties", out var properties)) yield break;
        foreach (var property in properties.EnumerateObject())
            if (property.Value.TryGetProperty("type", out var t) && t.GetString() == "array") yield return property.Value;
    }
}
