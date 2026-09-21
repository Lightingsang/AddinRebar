using System;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;

namespace HPRobot.McpBridge.Resources.Themes;

/// <summary>
///     Windows' app mode as an <see cref="IHostTheme" />.
///     <c>HPROBOT_MCP_BRIDGE_THEME=dark|light</c> overrides it.
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

    public bool IsDark => Environment.GetEnvironmentVariable("HPROBOT_MCP_BRIDGE_THEME")?.ToLowerInvariant() switch
    {
        "dark" => true,
        "light" => false,
        _ => Theme.GetSystemTheme() == BaseTheme.Dark,
    };

    public event Action? Changed;
}
