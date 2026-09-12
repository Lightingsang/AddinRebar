using System;
using System.Windows;
using Serilog;

namespace HPRebar.ColumnRebar.Service;

/// <summary>
///     Keeps the window's colours in step with Revit's own light or dark setting by swapping the colour
///     dictionary. Everything else in the theme reads its colours through DynamicResource, so the swap
///     takes effect without recreating any window.
/// </summary>
public static class ThemeSwitcher
{
    private const string DarkUri = "pack://application:,,,/HPRebar;component/Resources/Themes/ThemeDark.xaml";
    private const string LightUri = "pack://application:,,,/HPRebar;component/Resources/Themes/ThemeLight.xaml";

    /// <summary>Applies Revit's current theme to a window's merged dictionaries.</summary>
    public static void ApplyFromRevit(FrameworkElement target)
    {
        Apply(target, RevitPrefersDark());
    }

    public static void Apply(FrameworkElement target, bool dark)
    {
        var wanted = new Uri(dark ? DarkUri : LightUri);
        var merged = target.Resources.MergedDictionaries;

        for (var i = 0; i < merged.Count; i++)
        {
            if (!IsColourDictionary(merged[i])) continue;

            if (merged[i].Source == wanted) return;

            merged[i] = new ResourceDictionary { Source = wanted };

            return;
        }

        // The theme file merges the colour dictionary one level down, so look there too.
        foreach (var dictionary in merged)
        {
            var nested = dictionary.MergedDictionaries;

            for (var i = 0; i < nested.Count; i++)
            {
                if (!IsColourDictionary(nested[i])) continue;

                if (nested[i].Source == wanted) return;

                nested[i] = new ResourceDictionary { Source = wanted };

                return;
            }
        }

        Log.Warning("Could not find the colour dictionary to swap; the window keeps its default theme");
    }

    private static bool IsColourDictionary(ResourceDictionary dictionary)
    {
        var source = dictionary.Source?.OriginalString;

        return source is not null
               && (source.EndsWith("ThemeDark.xaml", StringComparison.OrdinalIgnoreCase)
                   || source.EndsWith("ThemeLight.xaml", StringComparison.OrdinalIgnoreCase));
    }

    private static bool RevitPrefersDark()
    {
        // Multi-version: UIThemeManager arrived in Revit 2024. Earlier versions have no theme API,
        // and their UI is dark, so that is the safe default there.
#if REVIT2024_OR_GREATER
        return Autodesk.Revit.UI.UIThemeManager.CurrentTheme == Autodesk.Revit.UI.UITheme.Dark;
#else
        return true;
#endif
    }
}
