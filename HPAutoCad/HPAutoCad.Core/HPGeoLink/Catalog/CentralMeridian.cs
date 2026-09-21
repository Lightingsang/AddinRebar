using System.Globalization;

namespace HPAutoCad.Core.HPGeoLink.Catalog;

/// <summary>
/// A VN-2000 central meridian ("kinh tuyến trục"). Vietnamese provinces use 17 of them, 103°00′ … 108°30′ in
/// quarter-degree steps. The EPSG code is optional metadata the engine never needs: the reference material
/// contradicts itself on those codes, so none is assigned until a verified registry table exists.
/// </summary>
public readonly record struct CentralMeridian(double Degrees)
{
    public string Label => Format(Degrees);

    /// <summary>EPSG CRS code for "VN-2000 / TM-3 &lt;meridian&gt;" — always null in this build (unverified).</summary>
    public int? EpsgCode => null;

    /// <summary>105.75 → "105°45′", exactly as the reference tool prints it.</summary>
    public static string Format(double degrees)
    {
        if (!double.IsFinite(degrees)) return "—";
        var deg = (int)Math.Floor(degrees);
        var min = (int)Math.Round((degrees - deg) * 60);
        if (min == 60)
        {
            deg += 1;
            min = 0;
        }
        return string.Create(CultureInfo.InvariantCulture, $"{deg}°{min:00}′");
    }

    /// <summary>Accepts "105.75", "105,75", "105°45′", "105-45", "105 45"; null when nothing parses.</summary>
    public static double? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var t = text.Trim().Replace(',', '.');
        if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var plain)) return plain;

        var parts = t.Split(new[] { '°', '′', '\'', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is 1 or 2 &&
            int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var d))
        {
            var m = 0;
            if (parts.Length == 2 && !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out m)) return null;
            if (m is < 0 or >= 60) return null;
            return d + m / 60.0;
        }
        return null;
    }
}
