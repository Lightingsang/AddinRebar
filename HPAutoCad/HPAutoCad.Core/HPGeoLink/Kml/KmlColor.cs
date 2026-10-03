using System.Text.RegularExpressions;

namespace HPAutoCad.Core.HPGeoLink.Kml;

public sealed record KmlColorPreset(int Stt, string Name, string HexRgb, string KmlHex)
{
    public string DisplayText => $"{Stt}. {Name} ({HexRgb})";
    public string ToolTipText => $"{Stt}. {Name}\nHEX: {HexRgb}\nKML: {KmlHex}";
}

/// <summary>
/// KML colours are 8 hex digits in <b>aabbggrr</b> order (alpha, blue, green, red) — the reverse of CSS.
/// Defaults are the reference tool's: yellow points, red boundary lines.
/// Also supports standard HEX #RRGGBB or #AARRGGBB formats.
/// </summary>
public static partial class KmlColor
{
    public const string DefaultPoint = "ff00ffff";
    public const string DefaultLine = "ff0000ff";

    public static readonly IReadOnlyList<KmlColorPreset> Presets = new KmlColorPreset[]
    {
        new(1, "Đỏ", "#FF0000", "ff0000ff"),
        new(2, "Xanh lá", "#00FF00", "ff00ff00"),
        new(3, "Xanh dương", "#0000FF", "ffff0000"),
        new(4, "Vàng", "#FFFF00", "ff00ffff"),
        new(5, "Trắng", "#FFFFFF", "ffffffff"),
        new(6, "Đen", "#000000", "ff000000"),
        new(7, "Xám", "#808080", "ff808080"),
        new(8, "Xám nhạt", "#D9D9D9", "ffd9d9d9"),
        new(9, "Xám đậm", "#404040", "ff404040"),
        new(10, "Xanh #0070C0", "#0070C0", "ffc07000"),
        new(11, "Xanh Office", "#4472C4", "ffc47244"),
        new(12, "Xanh đậm", "#1F4E78", "ff784e1f"),
        new(13, "Xanh navy", "#000080", "ff800000"),
        new(14, "Xanh trời", "#00B0F0", "fff0b000"),
        new(15, "Xanh cyan", "#00FFFF", "ffffff00"),
        new(16, "Xanh ngọc", "#00B050", "ff50b000"),
        new(17, "Xanh lá đậm", "#008000", "ff008000"),
        new(18, "Xanh lá nhạt", "#92D050", "ff50d092"),
        new(19, "Vàng cam", "#FFC000", "ff00c0ff"),
        new(20, "Cam", "#ED7D31", "ff317ded"),
        new(21, "Cam đậm", "#C65911", "ff1159c6"),
        new(22, "Đỏ đậm", "#C00000", "ff0000c0"),
        new(23, "Đỏ nâu", "#A52A2A", "ff2a2aa5"),
        new(24, "Hồng", "#FF69B4", "ffb469ff"),
    };

    [GeneratedRegex("^[0-9a-fA-F]{8}$")]
    internal static partial Regex Pattern8();

    [GeneratedRegex("^[0-9a-fA-F]{6}$")]
    internal static partial Regex Pattern6();

    public static bool TryNormalize(string? value, out string kmlColor)
    {
        kmlColor = "";
        if (string.IsNullOrWhiteSpace(value)) return false;
        var s = value.Trim();

        if (s.StartsWith("#", StringComparison.Ordinal))
        {
            var hex = s.Substring(1);
            if (hex.Length == 6 && Pattern6().IsMatch(hex))
            {
                var r = hex.Substring(0, 2);
                var g = hex.Substring(2, 2);
                var b = hex.Substring(4, 2);
                kmlColor = $"ff{b}{g}{r}".ToLowerInvariant();
                return true;
            }
            if (hex.Length == 8 && Pattern8().IsMatch(hex))
            {
                var a = hex.Substring(0, 2);
                var r = hex.Substring(2, 2);
                var g = hex.Substring(4, 2);
                var b = hex.Substring(6, 2);
                kmlColor = $"{a}{b}{g}{r}".ToLowerInvariant();
                return true;
            }
            return false;
        }

        if (s.Length == 8 && Pattern8().IsMatch(s))
        {
            kmlColor = s.ToLowerInvariant();
            return true;
        }

        return false;
    }

    public static bool IsValid(string? value) => TryNormalize(value, out _);

    /// <summary>Lower-case aabbggrr, or throws <see cref="ArgumentException"/> with a Vietnamese message.</summary>
    public static string Normalize(string? value, string what)
    {
        if (!TryNormalize(value, out var kml))
            throw new ArgumentException($"Màu {what} phải là 8 ký tự hex theo thứ tự aabbggrr (vd ff00ffff) hoặc mã HEX #RRGGBB (vd #FF0000), nhận được '{value}'.");
        return kml;
    }

    /// <summary>The fill the reference tool derives from a line colour: alpha 0x18 over the same bbggrr.</summary>
    public static string PolygonFillFrom(string lineColor) => "18" + Normalize(lineColor, "đường").Substring(2);

    /// <summary>Converts a KML aabbggrr or #RRGGBB hex string to standard #RRGGBB for display and CSS.</summary>
    public static string KmlToRgbHex(string? kmlOrRgbHex)
    {
        if (string.IsNullOrWhiteSpace(kmlOrRgbHex)) return "#000000";
        var s = kmlOrRgbHex.Trim();

        if (s.StartsWith("#", StringComparison.Ordinal))
        {
            var hex = s.Substring(1);
            if (hex.Length == 6 && Pattern6().IsMatch(hex))
                return $"#{hex.ToUpperInvariant()}";
            if (hex.Length == 8 && Pattern8().IsMatch(hex))
                return $"#{hex.Substring(2).ToUpperInvariant()}";
            return "#000000";
        }

        if (s.Length == 8 && Pattern8().IsMatch(s))
        {
            var b = s.Substring(2, 2);
            var g = s.Substring(4, 2);
            var r = s.Substring(6, 2);
            return $"#{r}{g}{b}".ToUpperInvariant();
        }

        if (s.Length == 6 && Pattern6().IsMatch(s))
        {
            return $"#{s.ToUpperInvariant()}";
        }

        return "#000000";
    }
}
