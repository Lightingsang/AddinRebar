using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Parsers;

/// <summary>
/// Reads and writes the settings file (<see cref="KataSettingsFile"/>) as one flat JSON object of numbers, booleans and
/// strings: the drawing settings by name, the joint defaults as "JointBeam.X" / "JointColumn.X", the pending beam options
/// as "Pending.X" and the shop settings as "Shop.X". Hand-rolled because the .NET Framework build of the add-in has no
/// JSON library to lean on. A key the file lacks keeps the default and an unknown key is ignored, so an older file
/// survives a newer add-in.
/// </summary>
public static class KataSettingsJson
{
    private const string JointBeamPrefix = "JointBeam.";
    private const string JointColumnPrefix = "JointColumn.";
    private const string PendingPrefix = "Pending.";
    private const string ShopPrefix = "Shop.";

    /// <summary>The drawing settings in a file whose other groups hold their defaults.</summary>
    public static string Write(KataSettings settings) =>
        WriteFile(KataSettingsFile.Default with { Drawing = settings ?? throw new ArgumentNullException(nameof(settings)) });

    public static string WriteFile(KataSettingsFile file)
    {
        if (file is null) throw new ArgumentNullException(nameof(file));
        var entries = Entries("", file.Drawing)
            .Concat(Entries(JointBeamPrefix, file.Drawing.JointBeam))
            .Concat(Entries(JointColumnPrefix, file.Drawing.JointColumn))
            .Concat(Entries(PendingPrefix, file.Pending))
            .Concat(Entries(ShopPrefix, file.Shop))
            .ToList();

        var sb = new StringBuilder("{\n");
        for (int i = 0; i < entries.Count; i++)
        {
            sb.Append("  \"").Append(entries[i].Key).Append("\": ").Append(Format(entries[i].Value));
            sb.Append(i < entries.Count - 1 ? ",\n" : "\n");
        }

        return sb.Append('}').ToString();
    }

    /// <exception cref="FormatException">The text is not a flat JSON object.</exception>
    public static KataSettings Read(string json) => ReadFile(json).Drawing;

    /// <exception cref="FormatException">The text is not a flat JSON object.</exception>
    public static KataSettingsFile ReadFile(string json)
    {
        var values = ParseObject(json ?? throw new ArgumentNullException(nameof(json)));
        var drawing = Fill(KataSettings.Default with { }, "", values) with
        {
            JointBeam = Fill(KataJointRebarSettings.Default with { }, JointBeamPrefix, values),
            JointColumn = Fill(KataJointRebarSettings.Default with { }, JointColumnPrefix, values)
        };
        var file = new KataSettingsFile(
            Migrate(drawing, values.ContainsKey(nameof(KataSettings.SettingsVersion))),
            Fill(KataBeamOptions.Default with { }, PendingPrefix, values),
            Fill(KataShopSettings.Default with { }, ShopPrefix, values));
        return KataSettingsSanitizer.File(file);
    }

    /// <summary>
    /// A file saved before the Kata-drawing defaults (no version) still holds the old ones (15 d legs, 2 h dense zone,
    /// a 0.15 L cut, which now caps the cut instead of setting it): those values move to the new defaults, anything the
    /// user changed stays. Later versions only gain keys, which keep their defaults.
    /// </summary>
    private static KataSettings Migrate(KataSettings s, bool versioned)
    {
        var d = KataSettings.Default;
        if (versioned && s.SettingsVersion >= 2) return s with { SettingsVersion = d.SettingsVersion };
        static bool Is(double v, double old) => Math.Abs(v - old) < 1e-9;
        return s with
        {
            SettingsVersion = d.SettingsVersion,
            MinimumLegFactor = Is(s.MinimumLegFactor, 15.0) ? d.MinimumLegFactor : s.MinimumLegFactor,
            DenseZoneHeightFactor = Is(s.DenseZoneHeightFactor, 2.0) ? d.DenseZoneHeightFactor : s.DenseZoneHeightFactor,
            BottomExtraCutFraction = Is(s.BottomExtraCutFraction, 0.15) ? d.BottomExtraCutFraction : s.BottomExtraCutFraction
        };
    }

    /// <summary>The drawing settings with every value out of range replaced by the default (<see cref="KataSettingsSanitizer"/>).</summary>
    public static KataSettings Sanitize(KataSettings s) => KataSettingsSanitizer.Drawing(s);

    private static IEnumerable<KeyValuePair<string, object?>> Entries(string prefix, object record) =>
        Supported(record.GetType()).Select(p => new KeyValuePair<string, object?>(prefix + p.Name, p.GetValue(record)));

    /// <summary>A copy of <paramref name="target"/> (already a fresh copy) with the values the file holds under the prefix.</summary>
    private static T Fill<T>(T target, string prefix, Dictionary<string, object?> values) where T : class
    {
        foreach (var p in Supported(typeof(T)))
        {
            if (!values.TryGetValue(prefix + p.Name, out var raw)) continue;
            var value = Convert(raw, p.PropertyType);
            if (value is not null) p.SetValue(target, value);
        }

        return target;
    }

    private static IEnumerable<PropertyInfo> Supported(Type type) => type
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.CanRead && p.CanWrite && IsSupported(p.PropertyType));

    private static bool IsSupported(Type t) => t == typeof(double) || t == typeof(int) || t == typeof(bool) || t == typeof(string);

    private static string Format(object? value) => value switch
    {
        null => "null",
        bool b => b ? "true" : "false",
        double d => d.ToString("R", CultureInfo.InvariantCulture),
        int n => n.ToString(CultureInfo.InvariantCulture),
        string s => Quote(s),
        _ => "null"
    };

    private static string Quote(string s)
    {
        var sb = new StringBuilder("\"");
        foreach (char c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else sb.Append(c);
                    break;
            }
        }

        return sb.Append('"').ToString();
    }

    private static object? Convert(object? raw, Type target)
    {
        if (target == typeof(string)) return raw as string;
        if (target == typeof(bool)) return raw as bool?;
        if (raw is not double d) return null;
        if (target == typeof(double)) return d;
        return d >= int.MinValue && d <= int.MaxValue ? (int)Math.Round(d) : null;
    }

    /// <summary>Values are string, double, bool or null.</summary>
    private static Dictionary<string, object?> ParseObject(string json)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        int i = 0;
        Skip(json, ref i);
        Expect(json, ref i, '{');
        Skip(json, ref i);
        if (Peek(json, i) == '}') return result;

        while (true)
        {
            Skip(json, ref i);
            string key = ParseString(json, ref i);
            Skip(json, ref i);
            Expect(json, ref i, ':');
            Skip(json, ref i);
            result[key] = ParseValue(json, ref i);
            Skip(json, ref i);
            char c = Peek(json, i);
            i++;
            if (c == '}') return result;
            if (c != ',') throw new FormatException($"Expected ',' or '}}' at {i - 1}.");
        }
    }

    private static object? ParseValue(string s, ref int i)
    {
        char c = Peek(s, i);
        if (c == '"') return ParseString(s, ref i);
        if (Match(s, ref i, "true")) return true;
        if (Match(s, ref i, "false")) return false;
        if (Match(s, ref i, "null")) return null;

        int start = i;
        while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
        if (i == start || !double.TryParse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
            throw new FormatException($"Unsupported value at {start}.");
        return d;
    }

    private static string ParseString(string s, ref int i)
    {
        Expect(s, ref i, '"');
        var sb = new StringBuilder();
        while (i < s.Length)
        {
            char c = s[i++];
            if (c == '"') return sb.ToString();
            if (c != '\\') { sb.Append(c); continue; }
            if (i >= s.Length) break;
            char e = s[i++];
            switch (e)
            {
                case 'n': sb.Append('\n'); break;
                case 'r': sb.Append('\r'); break;
                case 't': sb.Append('\t'); break;
                case 'b': sb.Append('\b'); break;
                case 'f': sb.Append('\f'); break;
                case 'u' when i + 4 <= s.Length:
                    sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                    i += 4;
                    break;
                default: sb.Append(e); break;
            }
        }

        throw new FormatException("Unterminated string.");
    }

    private static bool Match(string s, ref int i, string word)
    {
        if (string.CompareOrdinal(s, i, word, 0, word.Length) != 0) return false;
        i += word.Length;
        return true;
    }

    private static void Skip(string s, ref int i)
    {
        while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
    }

    private static char Peek(string s, int i) => i < s.Length ? s[i] : '\0';

    private static void Expect(string s, ref int i, char c)
    {
        if (Peek(s, i) != c) throw new FormatException($"Expected '{c}' at {i}.");
        i++;
    }
}
