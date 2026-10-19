using System;

namespace HPKata.Core.CadUtilities.Calculators
{
    /// <summary>
    /// Formats architectural elevation values according to Vietnamese engineering conventions (TCVN 3986).
    /// Precision threshold around 0.000: values with |Z| &lt; 0.0005 are formatted as ±0.000 (or %%p0.000 for AutoCAD).
    /// </summary>
    public static class ElevationFormatter
    {
        public static string Format(double elevationM, bool forAutoCad = false)
        {
            if (Math.Abs(elevationM) < 0.0005)
            {
                return forAutoCad ? "%%p0.000" : "±0.000";
            }

            if (elevationM > 0.0)
            {
                return $"+{elevationM:0.000}";
            }

            return $"-{Math.Abs(elevationM):0.000}";
        }

        public static double Parse(string elevationText)
        {
            if (string.IsNullOrWhiteSpace(elevationText)) return 0.0;
            string clean = elevationText.Trim()
                .Replace("%%p", "")
                .Replace("±", "")
                .Replace("+", "");

            return double.TryParse(clean, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double val)
                ? val
                : 0.0;
        }
    }
}
