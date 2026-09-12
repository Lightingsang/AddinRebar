using System;
using System.Windows;

namespace HPRebar.BeamRebar.Service;

/// <summary>
/// Synchronizes WPF window styling with Revit's Light/Dark mode via DynamicResource replacement.
/// </summary>
public static class ThemeSwitcher
{
    private const string DarkUri = "pack://application:,,,/HPRebar;component/Resources/Themes/ThemeDark.xaml";
    private const string LightUri = "pack://application:,,,/HPRebar;component/Resources/Themes/ThemeLight.xaml";

    public static void ApplyFromRevit(FrameworkElement target) => 
        Apply(target, RevitPrefersDark());

    public static void Apply(FrameworkElement target, bool dark)
    {
        var wanted = new Uri(dark ? DarkUri : LightUri);
        var merged = target.Resources.MergedDictionaries;

        for (int i = 0; i < merged.Count; i++)
        {
            if (!IsColourDictionary(merged[i])) continue;
            if (merged[i].Source == wanted) return;
            merged[i] = new ResourceDictionary { Source = wanted };
            return;
        }

        foreach (var dict in merged)
        {
            for (int i = 0; i < dict.MergedDictionaries.Count; i++)
            {
                if (!IsColourDictionary(dict.MergedDictionaries[i])) continue;
                if (dict.MergedDictionaries[i].Source == wanted) return;
                dict.MergedDictionaries[i] = new ResourceDictionary { Source = wanted };
                return;
            }
        }
    }

    private static bool IsColourDictionary(ResourceDictionary dict)
    {
        var src = dict.Source?.OriginalString;
        return src is not null && (src.EndsWith("ThemeDark.xaml", StringComparison.OrdinalIgnoreCase)
                                   || src.EndsWith("ThemeLight.xaml", StringComparison.OrdinalIgnoreCase));
    }

    private static bool RevitPrefersDark()
    {
#if REVIT2024_OR_GREATER
        return Autodesk.Revit.UI.UIThemeManager.CurrentTheme == Autodesk.Revit.UI.UITheme.Dark;
#else
        return true;
#endif
    }
}
