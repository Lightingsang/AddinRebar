using System;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
#if NETCOREAPP
using System.Runtime.Loader;
#endif

namespace MyAddIn.Resources.Themes;

/// <summary>
///     MaterialThemeBridge — Cầu nối đồng bộ theme Dark / Light thời gian thực cho Add-in CAD/BIM.
///     Tự động ánh xạ màu sắc giao diện theo theme của phần mềm chủ (Revit, AutoCAD, Civil 3D, Windows).
///     Đảm bảo tương thích hoàn toàn khi chạy trong AssemblyLoadContext riêng hoặc ILRepack.
/// </summary>
public static class MaterialThemeBridge
{
    private const string PaletteFolder = "Resources/Themes";
    private static readonly string AssemblyName = typeof(MaterialThemeBridge).Assembly.GetName().Name!;
    private static readonly string DarkUri = $"pack://application:,,,/{AssemblyName};component/{PaletteFolder}/ThemeDark.xaml";
    private static readonly string LightUri = $"pack://application:,,,/{AssemblyName};component/{PaletteFolder}/ThemeLight.xaml";
    private const string OverlayMarker = "MyAddIn.ThemeOverlay";

    /// <summary>
    ///     Gắn tự động vào Window và theo dõi sự kiện đổi theme của phần mềm chủ cho đến khi đóng Window.
    /// </summary>
    public static void Attach(Window window, IHostTheme host, Func<bool, ImageSource>? icon = null)
    {
        Apply(window, host.IsDark, icon);

        void OnChanged() => window.Dispatcher.BeginInvoke(() => Apply(window, host.IsDark, icon));
        host.Changed += OnChanged;
        window.Closed += (_, _) => host.Changed -= OnChanged;
    }

    /// <summary>
    ///     Áp dụng theme một lần cho Window.
    /// </summary>
    public static void Apply(Window window, bool dark, Func<bool, ImageSource>? icon = null)
    {
#if NETCOREAPP
        // Pack URI resolution trong AssemblyLoadContext riêng (AutoCAD, Civil 3D, v.v.)
        using var reflectionScope = AssemblyLoadContext.GetLoadContext(typeof(MaterialThemeBridge).Assembly)?.EnterContextualReflection();
#endif
        if (icon is not null) window.Icon = icon(dark);

        var palette = new ResourceDictionary { Source = new Uri(dark ? DarkUri : LightUri) };
        var overlay = new ResourceDictionary { [OverlayMarker] = true };
        overlay.MergedDictionaries.Add(palette);

        if (FindThemeDictionary(window.Resources) is { } seed)
        {
            DeriveMaterialBrushes(seed, palette, overlay, dark);
        }

        // Hoán đổi overlay ở vị trí đầu tiên trong MergedDictionaries của Window
        var merged = window.Resources.MergedDictionaries;
        for (var i = 0; i < merged.Count; i++)
        {
            if (merged[i].Contains(OverlayMarker))
            {
                merged[i] = overlay;
                return;
            }
        }
        merged.Insert(0, overlay);
    }

    private static ResourceDictionary? FindThemeDictionary(ResourceDictionary root)
    {
        foreach (var dict in root.MergedDictionaries)
        {
            if (dict is CustomColorTheme) return dict;
            if (FindThemeDictionary(dict) is { } found) return found;
        }
        return null;
    }

    private static void DeriveMaterialBrushes(ResourceDictionary seed, ResourceDictionary palette, ResourceDictionary overlay, bool dark)
    {
        // Đồng bộ các brush nền tảng của Material Design với bảng màu palette của Add-in
        if (palette["Brush.Background"] is Brush bg)
        {
            overlay[MaterialDesignThemes.Wpf.Theme.CardBackground] = bg;
            overlay[MaterialDesignThemes.Wpf.Theme.Paper] = bg;
        }
        if (palette["Brush.Foreground.Primary"] is Brush fg)
        {
            overlay[MaterialDesignThemes.Wpf.Theme.Body] = fg;
        }
        if (palette["Brush.Border"] is Brush border)
        {
            overlay[MaterialDesignThemes.Wpf.Theme.ToolBarBackground] = border;
        }
    }
}

/// <summary>
///     Interface trừu tượng cho thông tin Theme của phần mềm chủ.
/// </summary>
public interface IHostTheme
{
    bool IsDark { get; }
    event Action? Changed;
}
