using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Windows;
using System.Windows.Media;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;

namespace HPKata.UI.Common
{
    /// <summary>
    /// Manages Dark/Light palette switching synchronized with AutoCAD/Revit host palettes and MaterialDesignThemes.
    /// </summary>
    public static class ThemeManager
    {
        private const string PaletteFolder = "Themes";
        private static readonly string AssemblyName = typeof(ThemeManager).Assembly.GetName().Name!;
        private static readonly string DarkUri = $"pack://application:,,,/{AssemblyName};component/{PaletteFolder}/ThemeDark.xaml";
        private static readonly string LightUri = $"pack://application:,,,/{AssemblyName};component/{PaletteFolder}/ThemeLight.xaml";
        private const string OverlayMarker = "HPKata.ThemeOverlay";

        private static int _initialized;

        public static void EnsureAssembliesLoaded()
        {
            KataUiAssemblyResolver.Initialize();
            if (System.Threading.Interlocked.Exchange(ref _initialized, 1) != 0)
            {
                return;
            }

            WarmUpMaterialDesignAssemblies();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WarmUpMaterialDesignAssemblies()
        {
            _ = typeof(ThemeManager).Assembly;
            _ = typeof(PaletteHelper).Assembly;
            _ = typeof(SwatchesProvider).Assembly;
        }

        public static void Attach(Window window, IHostTheme host, Func<bool, ImageSource>? icon = null)
        {
            Apply(window, host.IsDark, icon);

            void OnChanged() => window.Dispatcher.BeginInvoke(() => Apply(window, host.IsDark, icon));
            host.Changed += OnChanged;
            window.Closed += (_, _) => host.Changed -= OnChanged;
        }

        public static void Apply(Window window, bool dark, Func<bool, ImageSource>? icon = null)
        {
            using var scope = AssemblyLoadContext.GetLoadContext(typeof(ThemeManager).Assembly)?.EnterContextualReflection();

            if (icon is not null)
            {
                window.Icon = icon(dark);
            }

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
            if (existing >= 0)
            {
                merged[existing] = overlay;
            }
            else
            {
                merged.Add(overlay);
            }
        }

        private static ColorReference Token(ResourceDictionary palette, string key, ColorReference fallback)
        {
            if (palette[key] is Color color)
            {
                return color;
            }
            if (palette[key] is SolidColorBrush brush)
            {
                return brush.Color;
            }
            string brushKey = "Brush." + key["Color.".Length..];
            if (palette[brushKey] is SolidColorBrush fromBrush)
            {
                return fromBrush.Color;
            }
            return fallback;
        }

        private static int IndexOfOverlay(IList<ResourceDictionary> merged)
        {
            for (var i = 0; i < merged.Count; i++)
            {
                if (merged[i].Contains(OverlayMarker))
                {
                    return i;
                }
            }
            return -1;
        }

        private static ResourceDictionary? FindThemeDictionary(ResourceDictionary root)
        {
            foreach (var dictionary in Walk(root))
            {
                if (dictionary is IMaterialDesignThemeDictionary)
                {
                    return dictionary;
                }
            }
            return null;
        }

        private static IEnumerable<ResourceDictionary> Walk(ResourceDictionary root)
        {
            yield return root;
            foreach (var child in root.MergedDictionaries)
            {
                foreach (var descendant in Walk(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}
