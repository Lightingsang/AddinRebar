using System.Windows.Controls;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.Windows;

namespace HPAutoCad.McpBridge.Loader.Ribbon;

/// <summary>
///     The "HPAutoCad" Ribbon tab: one panel "MCP" with one button "MCP Bridge" — the same surface as the
///     Revit and Navisworks bridges. The button forwards to the bridge entry point HPMCPBRIDGE uses, so a
///     click needs no document and never edits the drawing; everything else (listener, opt-in, last script,
///     audit, logs) lives in the window it opens. The tab exists at most once per Ribbon: every path that could
///     add it — start-up, any Ribbon item initialising (covers the RIBBON command after RIBBONCLOSE), a
///     workspace switch (rebuilds the Ribbon from the CUI and drops tabs added in code), a COLORTHEME change
///     (the icon ink follows the theme) — goes through <see cref="EnsureCreated"/> and its <c>FindTab</c> guard.
/// </summary>
internal static class McpRibbonTab
{
    public const string TabId = "HPAUTOCAD_MCP_TAB";
    public const string TabTitle = "HPAutoCad";
    public const string PanelId = "HPAUTOCAD_MCP_PANEL";
    public const string ButtonId = "HPAUTOCAD_MCP_BRIDGE";
    public const string ButtonText = "MCP Bridge";

    private static bool _installed;
    private static bool _idlePending;
    private static bool _rebuild;
    private static bool _building;

    public static void Install()
    {
        if (_installed) return;
        _installed = true;

        Application.SystemVariableChanged += OnSystemVariableChanged;
        ComponentManager.ItemInitialized += OnRibbonItemInitialized; // stays hooked: the Ribbon can be rebuilt any time
        EnsureCreated();
    }

    public static void Uninstall()
    {
        if (!_installed) return;
        _installed = false;

        Application.SystemVariableChanged -= OnSystemVariableChanged;
        ComponentManager.ItemInitialized -= OnRibbonItemInitialized;
        Application.Idle -= OnIdle;
        _idlePending = false;
        _rebuild = false;

        var ribbon = ComponentManager.Ribbon;
        if (ribbon?.FindTab(TabId) is { } tab) ribbon.Tabs.Remove(tab);
    }

    /// <summary>Adds the tab when the Ribbon exists and does not already show it.</summary>
    public static void EnsureCreated()
    {
        if (_building) return;
        try
        {
            var ribbon = ComponentManager.Ribbon;
            if (ribbon is null || ribbon.FindTab(TabId) is not null) return;

            _building = true;
            ribbon.Tabs.Add(Build());
            LoaderLog.Write($"ribbon tab {TabId} created (bridge {(BridgeActions.BridgeAvailable ? "available" : "unavailable")})");
        }
        catch (System.Exception exception)
        {
            // The Ribbon is a convenience: a failure here must never take the bridge or its commands down.
            LoaderLog.Write("ribbon tab creation failed", exception);
        }
        finally
        {
            _building = false;
        }
    }

    private static void OnRibbonItemInitialized(object? sender, RibbonItemEventArgs e) => EnsureCreated();

    private static void OnSystemVariableChanged(object? sender, Autodesk.AutoCAD.ApplicationServices.SystemVariableChangedEventArgs e)
    {
        var workspace = string.Equals(e.Name, "WSCURRENT", StringComparison.OrdinalIgnoreCase);
        var theme = string.Equals(e.Name, "COLORTHEME", StringComparison.OrdinalIgnoreCase);
        if (!workspace && !theme) return;
        if (theme) _rebuild = true; // the icon ink is picked per theme, so the tab is rebuilt
        if (_idlePending) return;
        _idlePending = true;
        Application.Idle += OnIdle; // the new Ribbon is complete by the next idle tick
    }

    private static void OnIdle(object? sender, EventArgs e)
    {
        Application.Idle -= OnIdle;
        _idlePending = false;
        if (_rebuild)
        {
            _rebuild = false;
            try
            {
                if (ComponentManager.Ribbon?.FindTab(TabId) is { } tab) ComponentManager.Ribbon.Tabs.Remove(tab);
            }
            catch (System.Exception exception)
            {
                LoaderLog.Write("ribbon tab removal for rebuild failed", exception);
            }
        }
        EnsureCreated();
    }

    private static RibbonTab Build()
    {
        var tab = new RibbonTab { Id = TabId, Title = TabTitle, IsVisible = true };
        var bridge = BridgeActions.BridgeAvailable;
        var icon = new RibbonIcons(IsDarkTheme()).McpBridge;

        var button = new RibbonButton
        {
            Id = ButtonId,
            Text = ButtonText,
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Large,
            Orientation = Orientation.Vertical,
            Image = icon,
            LargeImage = icon,
            CommandHandler = new RibbonCommandHandler(ButtonId, () => BridgeActions.Run("show")),
            IsEnabled = bridge,
        };
        button.ToolTip = new RibbonToolTip
        {
            Title = ButtonText,
            Content = bridge
                ? "Opens the MCP bridge status window: start or stop the listener, allow AI code execution for this session, see the last script, open the audit and log folders."
                : "The bridge did not start, so the window cannot open. See loader.log in " + LoaderLog.LogDirectory,
            Command = "HPMCPBRIDGE",
            IsHelpEnabled = false,
        };

        var panel = new RibbonPanel { Source = new RibbonPanelSource { Id = PanelId, Title = "MCP" } };
        panel.Source.Items.Add(button);
        tab.Panels.Add(panel);
        return tab;
    }

    /// <summary>COLORTHEME 0 = dark (AutoCAD's default), 1 = light; unreadable → dark.</summary>
    private static bool IsDarkTheme()
    {
        try { return Convert.ToInt32(Application.GetSystemVariable("COLORTHEME")) == 0; }
        catch (System.Exception) { return true; }
    }
}
