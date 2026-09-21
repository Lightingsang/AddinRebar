using System;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;

namespace HPPowerBi.McpBridge.Resources.Themes;

/// <summary>
///     Windows' app mode (Settings ▸ Personalization ▸ Colors, AppsUseLightTheme) as an <see cref="IHostTheme" />.
///     <c>HPPOWERBI_MCP_BRIDGE_THEME=dark|light</c> pins the theme (harness runs, or a user who wants the bridge
///     to differ from Windows).
/// </summary>
public sealed class WindowsHostTheme : IHostTheme
{
    public static WindowsHostTheme Instance { get; } = new();

    private WindowsHostTheme()
    {
        try
        {
            SystemEvents.UserPreferenceChanged += (_, args) =>
            {
                if (args.Category == UserPreferenceCategory.General)
                {
                    Changed?.Invoke();
                }
            };
        }
        catch
        {
            // Headless / non-interactive environment fallback
        }
    }

    public bool IsDark
    {
        get
        {
            var env = Environment.GetEnvironmentVariable("HPPOWERBI_MCP_BRIDGE_THEME")?.ToLowerInvariant();
            if (env == "dark") return true;
            if (env == "light") return false;

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key?.GetValue("AppsUseLightTheme") is int val)
                {
                    return val == 0;
                }
            }
            catch
            {
                // Fallback to MaterialDesignThemes Theme.GetSystemTheme()
            }

            return Theme.GetSystemTheme() == BaseTheme.Dark;
        }
    }

    public event Action? Changed;
}
