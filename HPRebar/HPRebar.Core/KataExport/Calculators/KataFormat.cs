using System;
using System.Globalization;

namespace HPRebar.Core.KataExport.Calculators;

/// <summary>
/// Rounding and text formats of the Kata sheet cells.
/// </summary>
public static class KataFormat
{
    /// <summary>Whole millimetres, halves away from zero, never -0.</summary>
    public static double Round(double valueMm) => Math.Round(valueMm, MidpointRounding.AwayFromZero) + 0.0;

    /// <summary>Tenths of a millimetre, for section sizes that may come from measured geometry.</summary>
    public static double Clean(double valueMm) => Math.Round(valueMm, 1, MidpointRounding.AwayFromZero) + 0.0;

    /// <summary>"b x h" of a supporting beam, e.g. "300x600".</summary>
    public static string Section(double widthMm, double heightMm) => $"{Whole(widthMm)}x{Whole(heightMm)}";

    /// <summary>"width;offset" of the column standing on a support, e.g. "400;-50".</summary>
    public static string UpperColumn(double widthMm, double offsetMm) => $"{Whole(widthMm)};{Whole(offsetMm)}";

    /// <summary>
    /// Beam elevation the way Kata writes it: metres, 3 decimals, explicit sign ("+3.300", "-1.200", "+0.000").
    /// The sign is always "+" or "-" so a VBA Val/CDbl reads it back; "±" would not parse.
    /// </summary>
    public static string Elevation(double elevationMm)
    {
        double metres = Round(elevationMm) / 1000.0;
        string sign = metres < 0 ? "-" : "+";
        return sign + Math.Abs(metres).ToString("0.000", CultureInfo.InvariantCulture);
    }

    private static string Whole(double valueMm) => ((long)Round(valueMm)).ToString(CultureInfo.InvariantCulture);
}
