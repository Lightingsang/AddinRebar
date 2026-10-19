using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HPKata.UI.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }
        public bool CollapseWhenHidden { get; set; } = true;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool flag = value is bool b && b;
            if (Invert)
            {
                flag = !flag;
            }

            if (flag)
            {
                return Visibility.Visible;
            }
            return CollapseWhenHidden ? Visibility.Collapsed : Visibility.Hidden;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Visibility vis)
            {
                bool b = vis == Visibility.Visible;
                return Invert ? !b : b;
            }
            return false;
        }
    }
}
