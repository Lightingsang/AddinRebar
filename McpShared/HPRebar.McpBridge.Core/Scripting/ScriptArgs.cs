using System.Globalization;
using System.Text.Json;

namespace HPRebar.McpBridge.Core.Scripting;

/// <summary>
///     What a script sees as `args`: a forgiving, typed view over the JSON object the tool call sent
///     beside the code. Lookups are case-insensitive and coerce the obvious cases (a number written as a
///     string, a bool written as 0/1) because the values were produced by a model, not a compiler. Missing
///     keys return the fallback; <see cref="Require"/> is the one accessor that throws, with a message the
///     model can act on.
/// </summary>
public sealed class ScriptArgs
{
    public static readonly ScriptArgs Empty = new ScriptArgs(null);

    private readonly JsonElement? _element;

    public ScriptArgs(JsonElement? element)
    {
        _element = element is { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined } ? null : element;
    }

    /// <summary>The underlying JSON, for the rare script that wants to walk it by hand.</summary>
    public JsonElement? Raw => _element;

    public bool IsEmpty => _element is null || (_element.Value.ValueKind == JsonValueKind.Object && !_element.Value.EnumerateObject().Any());

    public bool IsObject => _element is { ValueKind: JsonValueKind.Object };

    public bool IsArray => _element is { ValueKind: JsonValueKind.Array };

    public IReadOnlyList<string> Keys => IsObject ? _element!.Value.EnumerateObject().Select(p => p.Name).ToArray() : [];

    public bool Has(string key) => TryGet(key, out var value) && value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined);

    // ---- scalars by key -------------------------------------------------------------------------

    public string? Str(string key, string? fallback = null) => TryGet(key, out var v) ? ToStr(v) ?? fallback : fallback;

    public double Double(string key, double fallback = 0) => TryGet(key, out var v) && TryDouble(v, out var d) ? d : fallback;

    public double? DoubleOrNull(string key) => TryGet(key, out var v) && TryDouble(v, out var d) ? d : null;

    public int Int(string key, int fallback = 0) => TryGet(key, out var v) && TryDouble(v, out var d) ? checked((int)Math.Round(d)) : fallback;

    public int? IntOrNull(string key) => TryGet(key, out var v) && TryDouble(v, out var d) ? checked((int)Math.Round(d)) : null;

    public long Long(string key, long fallback = 0) => TryGet(key, out var v) && TryDouble(v, out var d) ? checked((long)Math.Round(d)) : fallback;

    public long? LongOrNull(string key) => TryGet(key, out var v) && TryDouble(v, out var d) ? checked((long)Math.Round(d)) : null;

    public bool Bool(string key, bool fallback = false) => TryGet(key, out var v) && TryBool(v, out var b) ? b : fallback;

    /// <summary>Non-null, non-empty string or an <see cref="ArgumentException"/> naming the key.</summary>
    public string Require(string key)
    {
        var value = Str(key);
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"args.{key} is required.");
        return value!;
    }

    public double RequireDouble(string key) => DoubleOrNull(key) ?? throw new ArgumentException($"args.{key} (number) is required.");

    // ---- structures by key ----------------------------------------------------------------------

    /// <summary>Nested object, or <see cref="Empty"/> when missing — so `args.Obj("p0").Double("x")` never throws.</summary>
    public ScriptArgs Obj(string key) => TryGet(key, out var v) && v.ValueKind == JsonValueKind.Object ? new ScriptArgs(v) : Empty;

    /// <summary>Array items, each wrapped so objects and scalars read the same way (`item.Double("x")` / `item.AsDouble()`).</summary>
    public IReadOnlyList<ScriptArgs> List(string key) =>
        TryGet(key, out var v) && v.ValueKind == JsonValueKind.Array ? v.EnumerateArray().Select(e => new ScriptArgs(e)).ToArray() : [];

    public IReadOnlyList<string> Strings(string key) => List(key).Select(a => a.AsString() ?? string.Empty).ToArray();

    public IReadOnlyList<double> Doubles(string key) => List(key).Select(a => a.AsDouble()).ToArray();

    public IReadOnlyList<long> Longs(string key) => List(key).Select(a => a.AsLong()).ToArray();

    // ---- this element as a scalar (for array items) ----------------------------------------------

    public string? AsString() => _element is { } e ? ToStr(e) : null;

    public double AsDouble(double fallback = 0) => _element is { } e && TryDouble(e, out var d) ? d : fallback;

    public long AsLong(long fallback = 0) => _element is { } e && TryDouble(e, out var d) ? checked((long)Math.Round(d)) : fallback;

    public int AsInt(int fallback = 0) => _element is { } e && TryDouble(e, out var d) ? checked((int)Math.Round(d)) : fallback;

    public bool AsBool(bool fallback = false) => _element is { } e && TryBool(e, out var b) ? b : fallback;

    public override string ToString() => _element?.GetRawText() ?? "{}";

    // ---- internals -----------------------------------------------------------------------------

    private bool TryGet(string key, out JsonElement value)
    {
        value = default;
        if (!IsObject) return false;

        var obj = _element!.Value;
        if (obj.TryGetProperty(key, out value)) return true;

        foreach (var property in obj.EnumerateObject())
        {
            if (!string.Equals(property.Name, key, StringComparison.OrdinalIgnoreCase)) continue;
            value = property.Value;
            return true;
        }

        return false;
    }

    private static string? ToStr(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.String => e.GetString(),
        JsonValueKind.Number => e.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        _ => e.GetRawText(),
    };

    private static bool TryDouble(JsonElement e, out double d)
    {
        switch (e.ValueKind)
        {
            case JsonValueKind.Number:
                return e.TryGetDouble(out d);
            case JsonValueKind.String:
                return double.TryParse(e.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out d);
            case JsonValueKind.True:
                d = 1;
                return true;
            case JsonValueKind.False:
                d = 0;
                return true;
            default:
                d = 0;
                return false;
        }
    }

    private static bool TryBool(JsonElement e, out bool b)
    {
        switch (e.ValueKind)
        {
            case JsonValueKind.True:
                b = true;
                return true;
            case JsonValueKind.False:
                b = false;
                return true;
            case JsonValueKind.Number:
                b = e.TryGetDouble(out var d) && d != 0;
                return true;
            case JsonValueKind.String:
                var s = e.GetString()?.Trim();
                if (bool.TryParse(s, out b)) return true;
                if (s is "1" or "0") { b = s == "1"; return true; }
                return false;
            default:
                b = false;
                return false;
        }
    }
}
