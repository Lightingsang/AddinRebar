using System;
using System.Collections.Generic;
using System.Runtime.Loader;
using System.Windows;
using System.Windows.Media;

namespace HPDcePro.UI.Common
{
    /// <summary>
    /// Manages Dark/Light palette switching synchronized with AutoCAD/Revit host palettes and MahApps.Metro.
    /// </summary>
    public static class ThemeManager
    {
        private const string PaletteFolder = "Themes";
        private static readonly string AssemblyName = typeof(ThemeManager).Assembly.GetName().Name!;
        private static readonly string DarkUri = $"pack://application:,,,/{AssemblyName};component/{PaletteFolder}/ThemeDark.xaml";
        private static readonly string LightUri = $"pack://application:,,,/{AssemblyName};component/{PaletteFolder}/ThemeLight.xaml";
        private const string OverlayMarker = "HPDcePro.ThemeOverlay";

        /// <summary>
        /// Forces MahApps and ControlzEx assemblies into the current load context before XAML parsing.
        /// </summary>
        public static void EnsureAssembliesLoaded()
        {
            _ = typeof(MahApps.Metro.Controls.MetroWindow).Assembly;
            _ = typeof(ControlzEx.Theming.ThemeManager).Assembly;
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
            if (icon is not null)
            {
                window.Icon = icon(dark);
            }

            ApplyTheme(window, dark);
        }

        public static void ApplyTheme(Window window, bool dark = true)
        {
            EnsureAssembliesLoaded();
            using var scope = AssemblyLoadContext.GetLoadContext(typeof(ThemeManager).Assembly)?.EnterContextualReflection();

            ControlzEx.Theming.ThemeManager.Current.ChangeTheme(window, dark ? "Dark.Blue" : "Light.Blue");

            window.FontFamily = new FontFamily("Segoe UI");

            var palette = new ResourceDictionary { Source = new Uri(dark ? DarkUri : LightUri) };
            var overlay = new ResourceDictionary { [OverlayMarker] = true };
            overlay.MergedDictionaries.Add(palette);

            if (palette["Brush.Surface"] is SolidColorBrush surfaceBrush)
            {
                overlay["Brush.Card"] = surfaceBrush;
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
    }
}
