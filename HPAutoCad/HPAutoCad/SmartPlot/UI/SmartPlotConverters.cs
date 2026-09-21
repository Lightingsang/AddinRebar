using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HPAutoCad.SmartPlot.UI;

/// <summary>
/// Converts an Enum value to Boolean and back for RadioButton binding.
/// </summary>
public sealed class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || parameter is null) return false;
        return string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isChecked && isChecked && parameter is string paramString)
        {
            if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, paramString, true);
            }
        }
        return Binding.DoNothing;
    }
}

/// <summary>
/// Converts Boolean to Visibility with optional inversion.
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var b = value is true;
        if (Invert) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            var b = visibility == Visibility.Visible;
            return Invert ? !b : b;
        }
        return false;
    }
}

/// <summary>
/// Converts an Enum value to Visibility directly for conditional panel visibility.
/// </summary>
public sealed class EnumToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || parameter is null) return Invert ? Visibility.Visible : Visibility.Collapsed;
        bool match = string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
        if (Invert) match = !match;
        return match ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
