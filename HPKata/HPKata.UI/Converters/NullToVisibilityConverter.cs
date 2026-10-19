using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HPKata.UI.Converters
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }
        public bool CollapseWhenHidden { get; set; } = true;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isNotNull = value != null;
            if (value is string s && string.IsNullOrWhiteSpace(s))
            {
                isNotNull = false;
            }

            if (Invert)
            {
                isNotNull = !isNotNull;
            }

            if (isNotNull)
            {
                return Visibility.Visible;
            }
            return CollapseWhenHidden ? Visibility.Collapsed : Visibility.Hidden;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
