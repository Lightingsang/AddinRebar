using System.Text.RegularExpressions;

namespace HPAutoCad.Core.HPGeoLink.Kml;

/// <summary>
/// KML colours are 8 hex digits in <b>aabbggrr</b> order (alpha, blue, green, red) — the reverse of CSS.
/// Defaults are the reference tool's: yellow points, red boundary lines.
/// </summary>
public static partial class KmlColor
{
    public const string DefaultPoint = "ff00ffff";
    public const string DefaultLine = "ff0000ff";

    [GeneratedRegex("^[0-9a-fA-F]{8}$")]
    private static partial Regex Pattern();

    public static bool IsValid(string? value) => value is not null && Pattern().IsMatch(value);

    /// <summary>Lower-case aabbggrr, or throws <see cref="ArgumentException"/> with a Vietnamese message.</summary>
    public static string Normalize(string? value, string what)
    {
        if (!IsValid(value))
            throw new ArgumentException($"Màu {what} phải là 8 ký tự hex theo thứ tự aabbggrr (vd ff00ffff), nhận được '{value}'.");
        return value!.ToLowerInvariant();
    }

    /// <summary>The fill the reference tool derives from a line colour: alpha 0x18 over the same bbggrr.</summary>
    public static string PolygonFillFrom(string lineColor) => "18" + Normalize(lineColor, "đường").Substring(2);
}
