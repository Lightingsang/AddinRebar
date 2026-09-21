using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
#if NETCOREAPP
using System.Runtime.Loader;
#endif

namespace HPSap2000.McpBridge.Resources.Themes;

/// <summary>
///     Keeps a window's two theme layers in step: HP palette and MaterialDesign.
/// </summary>
public static class MaterialThemeBridge
{
    private const string PaletteFolder = "Resources/Themes";
    private static readonly string AssemblyName = typeof(MaterialThemeBridge).Assembly.GetName().Name!;
    private static readonly string DarkUri = $"pack://application:,,,/{AssemblyName};component/{PaletteFolder}/ThemeDark.xaml";
    private static readonly string LightUri = $"pack://application:,,,/{AssemblyName};component/{PaletteFolder}/ThemeLight.xaml";
    private const string OverlayMarker = "HPRebar.ThemeOverlay";

    public static void Attach(Window window, IHostTheme host, Func<bool, ImageSource>? icon = null)
    {
        Apply(window, host.IsDark, icon);

        void OnChanged() => window.Dispatcher.BeginInvoke(() => Apply(window, host.IsDark, icon));
        host.Changed += OnChanged;
        window.Closed += (_, _) => host.Changed -= OnChanged;
    }

    public static void Apply(Window window, bool dark, Func<bool, ImageSource>? icon = null)
    {
#if NETCOREAPP
        using var reflectionScope = AssemblyLoadContext.GetLoadContext(typeof(MaterialThemeBridge).Assembly)?.EnterContextualReflection();
#endif
        if (icon is not null) window.Icon = icon(dark);

        var palette = new ResourceDictionary { Source = new Uri(dark ? DarkUri : LightUri) };
        var overlay = new ResourceDictionary { [OverlayMarker] = true };
        overlay.MergedDictionaries.Add(palette);

        if (FindThemeDictionary(window.Resources) is { } seed)
        {
            var seedTheme = seed.GetTheme();
            var theme = Theme.Create(dark ? BaseTheme.Dark : BaseTheme.Light, seedTheme.PrimaryMid.Color, seedTheme.SecondaryMid.Color);
            theme.Background = Token(palette, "Color.Background", theme.Background);
            theme.Foreground = Token(palette, "Color.Foreground.Primary", theme.Foreground);
            theme.ForegroundLight = Token(palette, "Color.Foreground.Secondary", theme.ForegroundLight);
            theme.ValidationError = Token(palette, "Color.Danger", theme.ValidationError);
            theme.Cards.Background = Token(palette, "Color.SurfaceElevated", theme.Cards.Background);
            theme.Cards.Border = Token(palette, "Color.Border", theme.Cards.Border);
            theme.ToolTips.Background = Token(palette, "Color.SurfaceElevated", theme.ToolTips.Background);
            overlay.SetTheme(theme);
        }

        var merged = window.Resources.MergedDictionaries;
        var existing = IndexOfOverlay(merged);
        if (existing >= 0) merged[existing] = overlay;
        else merged.Add(overlay);
    }

    private static ColorReference Token(ResourceDictionary palette, string key, ColorReference fallback)
        => palette[key] switch
        {
            System.Windows.Media.Color color => color,
            SolidColorBrush brush => brush.Color,
            _ => palette["Brush." + key["Color.".Length..]] is SolidColorBrush fromBrush ? fromBrush.Color : fallback,
        };

    private static int IndexOfOverlay(IList<ResourceDictionary> merged)
    {
        for (var i = 0; i < merged.Count; i++)
            if (merged[i].Contains(OverlayMarker)) return i;
        return -1;
    }

    private static ResourceDictionary? FindThemeDictionary(ResourceDictionary root)
    {
        foreach (var dictionary in Walk(root))
            if (dictionary is IMaterialDesignThemeDictionary) return dictionary;
        return null;
    }

    private static IEnumerable<ResourceDictionary> Walk(ResourceDictionary root)
    {
        yield return root;
        foreach (var merged in root.MergedDictionaries)
        foreach (var child in Walk(merged))
            yield return child;
    }
}
