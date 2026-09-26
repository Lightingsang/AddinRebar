using System;
using System.Globalization;

namespace HPRebar.Core.KataExport.Calculators;

/// <summary>Excel names of the Kata data columns and the text a cell value is shown as.</summary>
public static class KataColumnLetters
{
    /// <summary>Data columns start at C.</summary>
    private const int FirstColumnNumber = 3;

    /// <summary>Excel letters of data column <paramref name="index"/> (0 → "C", 23 → "Z", 24 → "AA", 75 → "BZ").</summary>
    public static string Letter(int index)
    {
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), index, "Column index cannot be negative.");

        int number = index + FirstColumnNumber;
        string letters = string.Empty;
        while (number > 0)
        {
            int remainder = (number - 1) % 26;
            letters = (char)('A' + remainder) + letters;
            number = (number - 1) / 26;
        }

        return letters;
    }

    /// <summary>A sheet value as text, culture-invariant so 12.5 never shows as "12,5".</summary>
    public static string CellText(object? value) =>
        value is null ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
}
