using System.Globalization;
using System.Text.RegularExpressions;

namespace HPRebar.Core.KataRebar.Parsers;

/// <summary>
/// The notations of Kata's tab "Thép mặc định": joint stirrups "5f8a50" (count, diameter, spacing) and hanger bars
/// "2f16" (count, diameter). A count of 0 is allowed and means none. The diameter is 4…50 mm, a stirrup spacing at least
/// twice the diameter and the count at most 50, so a typo ("5f8a5") never lays stirrups on top of each other.
/// </summary>
public static class KataJointNotation
{
    private static readonly Regex StirrupRegex = new(
        @"^(?<count>\d+)\s*(?:f|d|phi|ø|Ø|Φ)\s*(?<dia>\d+(?:\.\d+)?)\s*(?:a|@)\s*(?<step>\d+(?:\.\d+)?)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex BarRegex = new(
        @"^(?<count>\d+)\s*(?:f|d|phi|ø|Ø|Φ)\s*(?<dia>\d+(?:\.\d+)?)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static bool TryParseStirrups(string? text, out int count, out double diameter, out double spacing)
    {
        count = 0;
        diameter = spacing = 0.0;
        var m = StirrupRegex.Match(text?.Trim() ?? "");
        if (!m.Success
            || !int.TryParse(m.Groups["count"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out count)
            || !double.TryParse(m.Groups["dia"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out diameter)
            || !double.TryParse(m.Groups["step"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out spacing))
            return false;
        return count <= MaxCount && diameter >= MinDiameter && diameter <= MaxDiameter && spacing >= 2.0 * diameter;
    }

    public static bool TryParseBars(string? text, out int count, out double diameter)
    {
        count = 0;
        diameter = 0.0;
        var m = BarRegex.Match(text?.Trim() ?? "");
        if (!m.Success
            || !int.TryParse(m.Groups["count"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out count)
            || !double.TryParse(m.Groups["dia"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out diameter))
            return false;
        return count <= MaxCount && diameter >= MinDiameter && diameter <= MaxDiameter;
    }

    private const int MaxCount = 50;
    private const double MinDiameter = 4.0;
    private const double MaxDiameter = 50.0;
}
