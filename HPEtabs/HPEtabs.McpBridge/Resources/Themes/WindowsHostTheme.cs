using MaterialDesignThemes.Wpf;
using Microsoft.Win32;

namespace HPEtabs.McpBridge.Resources.Themes;

/// <summary>
///     Windows' app mode (Settings ▸ Personalization ▸ Colors) as an <see cref="IHostTheme" /> — ETABS exposes no theme
///     of its own. <c>HPETABS_MCP_BRIDGE_THEME=dark|light</c> pins the theme (harness runs, or a user who wants the bridge
///     to differ from Windows).
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

    public bool IsDark => Environment.GetEnvironmentVariable("HPETABS_MCP_BRIDGE_THEME")?.ToLowerInvariant() switch
    {
        "dark" => true,
        "light" => false,
        _ => Theme.GetSystemTheme() == BaseTheme.Dark,
    };

    public event Action? Changed;
}
