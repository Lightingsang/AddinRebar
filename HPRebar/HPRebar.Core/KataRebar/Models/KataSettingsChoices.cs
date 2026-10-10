using System;
using System.Collections.Generic;
using System.Linq;

namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// The fixed lists of Kata's "Detail thép" combo boxes. The dialog only offers these values; a saved value outside a
/// list is shown as the default until the user picks one, while the rules keep reading the number the file holds.
/// </summary>
public static class KataSettingsChoices
{
    /// <summary>"Bẻ cổ chai cho thép có phi từ" (mm).</summary>
    public static IReadOnlyList<double> CrankMinDiameters { get; } = new[] { 10.0, 12, 14, 16, 18, 20, 22, 25, 28, 30, 32, 36, 40 };

    /// <summary>"Tỷ lệ đoạn nhấn cổ chai" as the n of 1/n.</summary>
    public static IReadOnlyList<double> CrankSlopes { get; } = new[] { 4.0, 6, 10, 12 };

    /// <summary>"Coupler cho thép có phi từ" (mm).</summary>
    public static IReadOnlyList<double> CouplerMinDiameters { get; } = new[] { 16.0, 18, 20, 22, 25, 28, 30, 32, 36, 40, 50 };

    /// <summary>
    /// The list entry equal to <paramref name="value"/> (the same boxed number a combo box compares by), or
    /// <paramref name="fallback"/> with <paramref name="inList"/> false.
    /// </summary>
    public static double Pick(IReadOnlyList<double> options, double value, double fallback, out bool inList)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));
        foreach (var option in options.Where(o => Math.Abs(o - value) < 1e-6))
        {
            inList = true;
            return option;
        }

        inList = false;
        return fallback;
    }
}
