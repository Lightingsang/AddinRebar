using System.Windows;

namespace HPAutoCad.Resources.Themes;

/// <summary>
/// Loads the style dictionary (Theme.xaml, which merges the MaterialDesign bootstrap) by component URI. Must be
/// called inside the assembly's contextual reflection scope (the add-in lives in the loader's AssemblyLoadContext).
/// The Dark/Light palette is swapped in by <see cref="MaterialThemeBridge"/>.
/// </summary>
internal static class ThemeResources
{
    public static ResourceDictionary Styles() =>
        new() { Source = new Uri("/HPAutoCad;component/Resources/Themes/Theme.xaml", UriKind.Relative) };
}
