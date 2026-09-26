using System.Globalization;
using System.Text.RegularExpressions;
using HPRebar.Core.KataExport.Models;

namespace HPRebar.Core.KataExport.Calculators;

/// <summary>
/// What each sheet value becomes in an Excel cell written through COM. Excel parses a string the way it
/// parses typing, so text that looks like a date ("1-2") or a number ("-0.050") would change type; text is
/// therefore sent with Excel's text prefix ('), which Kata reads back without it.
/// </summary>
public static class KataExcelCell
{
    private static readonly Regex PlainNumber = new(@"^-?(0|[1-9][0-9]*)(\.[0-9]+)?$", RegexOptions.CultureInvariant);

    public static object ToCellValue(object? value)
    {
        switch (value)
        {
            case null:
                return string.Empty;
            case double or int or long or bool:
                return value;
            case KataText text:
                // Always text, whatever it looks like: Kata keeps the beam elevation as "+3.300" / "-0.050".
                return text.Value.Length == 0 ? string.Empty : "'" + text.Value;
        }

        string s = value.ToString() ?? string.Empty;
        if (s.Length == 0) return string.Empty;

        // Plain numbers (a grid named "1", a count held in a text parameter) go as numbers, like Kata's sample;
        // ASCII digits only, and "01" stays text so a leading zero in a name is never lost.
        return PlainNumber.IsMatch(s) ? double.Parse(s, CultureInfo.InvariantCulture) : "'" + s;
    }
}
