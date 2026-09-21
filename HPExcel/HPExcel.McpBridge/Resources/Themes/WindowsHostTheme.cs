using System;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;

namespace HPExcel.McpBridge.Resources.Themes;

/// <summary>
///     Windows' app mode (Settings ▸ Personalization ▸ Colors) as an <see cref="IHostTheme" />.
///     <c>HPEXCEL_MCP_BRIDGE_THEME=dark|light</c> pins the theme.
/// </summary>
public sealed class WindowsHostTheme : IHostTheme
{
    public static WindowsHostTheme Instance { get; } = new();

    private WindowsHostTheme()
    {
        SystemEvents.UserPreferenceChanged += (_, args) =>
        {
            if (args.Category == UserPreferenceCategory.General) Changed?.Invoke();
        };
    }

    public bool IsDark => Environment.GetEnvironmentVariable("HPEXCEL_MCP_BRIDGE_THEME")?.ToLowerInvariant() switch
    {
        "dark" => true,
        "light" => false,
        _ => Theme.GetSystemTheme() == BaseTheme.Dark,
    };

    public event Action? Changed;
}
