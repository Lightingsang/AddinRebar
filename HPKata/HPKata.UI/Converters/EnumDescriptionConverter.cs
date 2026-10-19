using System;
using System.Globalization;
using System.Windows.Data;

namespace HPKata.UI.Converters
{
    public class EnumDescriptionConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null)
            {
                return string.Empty;
            }

            if (value is Enum e)
            {
                return FormatEnum(e);
            }

            return value.ToString() ?? string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static string FormatEnum(Enum e)
        {
            string name = e.ToString();
            // User-friendly names for Kata solver strategies and common enums
            return name switch
            {
                "Auto" => "Auto (Portfolio Winner)",
                "DuBang0" => "DuBang0 (Exact Zero Residual Subset-Sum)",
                "LapDayCanhDai" => "LapDayCanhDai (Best-Fit Decreasing BFD)",
                "BeamSearchTopK" => "BeamSearchTopK (State-Space Top-K)",
                "PatternCat" => "PatternCat (Modular 11.7m Dividers)",
                _ => name
            };
        }
    }
}
