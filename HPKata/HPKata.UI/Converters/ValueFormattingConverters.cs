using System;
using System.Globalization;
using System.Windows.Data;

namespace HPKata.UI.Converters
{
    public class NumberFormatConverter : IValueConverter
    {
        public string Format { get; set; } = "N0";

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null)
            {
                return string.Empty;
            }

            string fmt = parameter as string ?? Format;
            if (value is double d)
            {
                return d.ToString(fmt, CultureInfo.InvariantCulture);
            }
            if (value is int i)
            {
                return i.ToString(fmt, CultureInfo.InvariantCulture);
            }
            if (value is decimal m)
            {
                return m.ToString(fmt, CultureInfo.InvariantCulture);
            }

            return value.ToString() ?? string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string s && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }
            return 0.0;
        }
    }

    public class PercentageConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                return $"{d:F2}%";
            }
            return "0.00%";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class CutsFormattingConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is System.Collections.Generic.IEnumerable<double> cuts)
            {
                return string.Join(" + ", System.Linq.Enumerable.Select(cuts, c => c.ToString("F0", CultureInfo.InvariantCulture)));
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
