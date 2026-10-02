using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Parsers;

/// <summary>
/// Reads and writes <see cref="KataSettings"/> as one flat JSON object of numbers, booleans and strings.
/// Hand-rolled because the .NET Framework build of the add-in has no JSON library to lean on. A key the file
/// lacks keeps the Kata default and an unknown key is ignored, so an older file survives a newer add-in.
/// </summary>
public static class KataSettingsJson
{
    private static readonly PropertyInfo[] Properties = typeof(KataSettings)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.CanRead && p.CanWrite && IsSupported(p.PropertyType))
        .ToArray();

    public static string Write(KataSettings settings)
    {
        if (settings is null) throw new ArgumentNullException(nameof(settings));

        var sb = new StringBuilder("{\n");
        for (int i = 0; i < Properties.Length; i++)
        {
            var p = Properties[i];
            sb.Append("  \"").Append(p.Name).Append("\": ").Append(Format(p.GetValue(settings)));
            sb.Append(i < Properties.Length - 1 ? ",\n" : "\n");
        }

        return sb.Append('}').ToString();
    }

    /// <exception cref="FormatException">The text is not a flat JSON object.</exception>
    public static KataSettings Read(string json)
    {
        var values = ParseObject(json ?? throw new ArgumentNullException(nameof(json)));
        var settings = KataSettings.Default with { };
        foreach (var p in Properties)
        {
            if (!values.TryGetValue(p.Name, out var raw)) continue;
            var value = Convert(raw, p.PropertyType);
            if (value is not null) p.SetValue(settings, value);
        }

        return Sanitize(Migrate(settings, values.ContainsKey(nameof(KataSettings.SettingsVersion))));
    }

    /// <summary>
    /// A file saved before the Kata-drawing defaults still holds the old ones (15 d legs, 2 h dense zone, a 0.15 L
    /// cut, which now caps the cut instead of setting it): those values move to the new defaults, anything the user
    /// changed stays.
    /// </summary>
    private static KataSettings Migrate(KataSettings s, bool versioned)
    {
        if (versioned && s.SettingsVersion >= KataSettings.Default.SettingsVersion) return s;
        var d = KataSettings.Default;
        static bool Is(double v, double old) => Math.Abs(v - old) < 1e-9;
        return s with
        {
            SettingsVersion = d.SettingsVersion,
            MinimumLegFactor = Is(s.MinimumLegFactor, 15.0) ? d.MinimumLegFactor : s.MinimumLegFactor,
            DenseZoneHeightFactor = Is(s.DenseZoneHeightFactor, 2.0) ? d.DenseZoneHeightFactor : s.DenseZoneHeightFactor,
            BottomExtraCutFraction = Is(s.BottomExtraCutFraction, 0.15) ? d.BottomExtraCutFraction : s.BottomExtraCutFraction
        };
    }

    /// <summary>
    /// Every value out of range (negative, NaN, infinite, a fraction past half the span) replaced by the default,
    /// as the dialog would refuse it. Applied to a hand-edited file, to what the dialog saves and to what the rules use.
    /// </summary>
    public static KataSettings Sanitize(KataSettings s)
    {
        if (s is null) throw new ArgumentNullException(nameof(s));
        var d = KataSettings.Default;
        static bool Positive(double v) => v > 0.0 && !double.IsInfinity(v);
        static bool NonNegative(double v) => v >= 0.0 && !double.IsInfinity(v);
        static bool HalfSpan(double v) => v >= 0.0 && v <= 0.5;
        static bool Angle(int a) => a is 90 or 135 or 180;
        return s with
        {
            ClosedStirrupHookAngle = Angle(s.ClosedStirrupHookAngle) ? s.ClosedStirrupHookAngle : d.ClosedStirrupHookAngle,
            ClosedStirrupHookFactor = Positive(s.ClosedStirrupHookFactor) ? s.ClosedStirrupHookFactor : d.ClosedStirrupHookFactor,
            CrossTieHookAngle = Angle(s.CrossTieHookAngle) ? s.CrossTieHookAngle : d.CrossTieHookAngle,
            CrossTieHookFactor = Positive(s.CrossTieHookFactor) ? s.CrossTieHookFactor : d.CrossTieHookFactor,
            RoundCutExtraMm = s.RoundCutExtraMm >= 0.0 && !double.IsInfinity(s.RoundCutExtraMm) ? s.RoundCutExtraMm : d.RoundCutExtraMm,
            SideBarAnchorageFactor = Positive(s.SideBarAnchorageFactor) ? s.SideBarAnchorageFactor : d.SideBarAnchorageFactor,
            LayerTieMinBarCount = s.LayerTieMinBarCount >= 2 ? s.LayerTieMinBarCount : d.LayerTieMinBarCount,
            CrankMinDiameter = NonNegative(s.CrankMinDiameter) ? s.CrankMinDiameter : d.CrankMinDiameter,
            CurtailedExtensionMm = NonNegative(s.CurtailedExtensionMm) ? s.CurtailedExtensionMm : d.CurtailedExtensionMm,
            DenseZoneHeightFactor = NonNegative(s.DenseZoneHeightFactor) ? s.DenseZoneHeightFactor : d.DenseZoneHeightFactor,
            EndZoneFraction = HalfSpan(s.EndZoneFraction) ? s.EndZoneFraction : d.EndZoneFraction,
            BottomExtraCutFraction = HalfSpan(s.BottomExtraCutFraction) && s.BottomExtraCutFraction < 0.5 ? s.BottomExtraCutFraction : d.BottomExtraCutFraction,
            MinimumLegFactor = NonNegative(s.MinimumLegFactor) ? s.MinimumLegFactor : d.MinimumLegFactor,
            LayerClearGap = NonNegative(s.LayerClearGap) ? s.LayerClearGap : d.LayerClearGap,
            RoundLegMm = NonNegative(s.RoundLegMm) ? s.RoundLegMm : d.RoundLegMm,
            SideBarRequiredHeight = NonNegative(s.SideBarRequiredHeight) ? s.SideBarRequiredHeight : d.SideBarRequiredHeight
        };
    }

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
