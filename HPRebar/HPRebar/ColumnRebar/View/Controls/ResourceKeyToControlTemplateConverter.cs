using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace HPRebar.ColumnRebar.View.Controls;

/// <summary>
///     Resolves a resource key string into a <see cref="ControlTemplate"/> from application resources.
/// </summary>
public sealed class ResourceKeyToControlTemplateConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string key && System.Windows.Application.Current?.TryFindResource(key) is ControlTemplate template)
        {
            return template;
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return System.Windows.Data.Binding.DoNothing;
    }
}
