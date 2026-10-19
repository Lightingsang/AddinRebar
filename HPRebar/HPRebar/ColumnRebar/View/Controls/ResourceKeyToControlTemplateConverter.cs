using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace HPRebar.ColumnRebar.View.Controls;

/// <summary>
///     Resolves a resource key string into a <see cref="ControlTemplate"/> from application resources
///     or bundled ThemeIcons fallback dictionary when running in-process without Application.Current.
/// </summary>
public sealed class ResourceKeyToControlTemplateConverter : IValueConverter
{
    private static readonly Lazy<ResourceDictionary?> FallbackIcons = new(() =>
    {
        try
        {
            return new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/HPRebar;component/Resources/Themes/ThemeIcons.xaml", UriKind.Absolute)
            };
        }
        catch
        {
            return null;
        }
    });

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string key || string.IsNullOrEmpty(key))
        {
            return null;
        }

        if (System.Windows.Application.Current != null)
        {
            try
            {
                if (System.Windows.Application.Current.TryFindResource(key) is ControlTemplate template)
                {
                    return template;
                }
            }
            catch
            {
                // Fall back to direct dictionary lookup if Application.Current fails
            }
        }

        if (FallbackIcons.Value != null && FallbackIcons.Value.Contains(key) && FallbackIcons.Value[key] is ControlTemplate fallbackTemplate)
        {
            return fallbackTemplate;
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return System.Windows.Data.Binding.DoNothing;
    }
}
