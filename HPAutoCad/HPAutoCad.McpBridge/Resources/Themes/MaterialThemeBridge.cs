using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
#if NETCOREAPP
using System.Runtime.Loader;
#endif

namespace HPAutoCad.McpBridge.Resources.Themes;

/// <summary>
///     Keeps a window's two theme layers in step: the hand-tuned HP palette (ThemeDark/ThemeLight.xaml, the source
///     of every <c>Brush.*</c> token) and the MaterialDesign theme that styles the controls. Each apply builds one
///     overlay dictionary — the palette plus the MaterialDesign brushes re-derived from it (window, card, text and
///     validation colours come from the <c>Color.*</c> tokens) — and swaps it in at the top of the window's merged
///     dictionaries. Only a top-level replacement re-evaluates every <c>{DynamicResource}</c> in a shown window;
///     mutating a nested dictionary changes the lookup result but not what is on screen. Never uses
///     <c>PaletteHelper</c>: an add-in has no <c>Application.Current</c> resources.
/// </summary>
public static class MaterialThemeBridge
{
    // Copy of HPRebar/HPRebar/Resources/Themes/MaterialThemeBridge.cs (namespace + PaletteFolder may differ): HP folders never reference each other.
    // Palette files live beside this file in whichever assembly holds it.
    private const string PaletteFolder = "Resources/Themes";
    private static readonly string AssemblyName = typeof(MaterialThemeBridge).Assembly.GetName().Name!;
    private static readonly string DarkUri = $"pack://application:,,,/{AssemblyName};component/{PaletteFolder}/ThemeDark.xaml";
    private static readonly string LightUri = $"pack://application:,,,/{AssemblyName};component/{PaletteFolder}/ThemeLight.xaml";
    private const string OverlayMarker = "HPRebar.ThemeOverlay";

    /// <summary>
    ///     Applies the host's current theme and follows its changes until the window closes. <paramref name="icon" />
    ///     builds the window's title-bar glyph for a dark or light theme (HPRebar passes its vector RibbonIcons), so no
    ///     PNG resource is needed.
    /// </summary>
    public static void Attach(Window window, IHostTheme host, Func<bool, ImageSource>? icon = null)
    {
        Apply(window, host.IsDark, icon);

        void OnChanged() => window.Dispatcher.BeginInvoke(() => Apply(window, host.IsDark, icon));
        host.Changed += OnChanged;
        window.Closed += (_, _) => host.Changed -= OnChanged;
    }

    /// <summary>One-shot: palette + MaterialDesign brushes for <paramref name="dark" />, swapped in at top level.</summary>
    public static void Apply(Window window, bool dark, Func<bool, ImageSource>? icon = null)
    {
#if NETCOREAPP
        // A pack URI is resolved through Assembly.Load, which searches the default load context; an add-in living in its
        // own AssemblyLoadContext (the AutoCAD / Civil 3D / HPGeo bundles) must run the lookup inside that context. A no-op
        // for assemblies in the default context.
        using var reflectionScope = AssemblyLoadContext.GetLoadContext(typeof(MaterialThemeBridge).Assembly)?.EnterContextualReflection();
#endif
        if (icon is not null) window.Icon = icon(dark);

        var palette = new ResourceDictionary { Source = new Uri(dark ? DarkUri : LightUri) };
        var overlay = new ResourceDictionary { [OverlayMarker] = true };
        overlay.MergedDictionaries.Add(palette);

        // The CustomColorTheme merged by MaterialBridge.xaml only lends its primary/secondary colours; the brushes the
        // templates read are written into the overlay, which sits above it in lookup order.
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

    /// <summary>A palette token as a colour: the HPRebar palettes define <c>Color.X</c>, the bridge palettes only <c>Brush.X</c>.</summary>
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
        foreach (var child in root.MergedDictionaries)
            foreach (var descendant in Walk(child))
                yield return descendant;
    }
}
