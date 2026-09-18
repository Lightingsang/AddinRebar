using System.Windows;

namespace HPGeo.AutoCad.UI;

/// <summary>
/// Loads the resource dictionaries by component URI. Must be called inside the assembly's contextual
/// reflection scope (the add-in lives in the loader's AssemblyLoadContext).
/// </summary>
internal static class ThemeResources
{
    public static ResourceDictionary Brushes(bool dark) => Load(dark ? "UI/ThemeDark.xaml" : "UI/ThemeLight.xaml");

    public static ResourceDictionary Styles() => Load("UI/Theme.xaml");

    private static ResourceDictionary Load(string relativePath) =>
        new() { Source = new Uri("/HPGeo.AutoCad;component/" + relativePath, UriKind.Relative) };
}
