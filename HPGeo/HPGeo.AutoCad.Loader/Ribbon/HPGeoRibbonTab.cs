using System.Windows.Controls;
using System.Windows.Input;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.Windows;

namespace HPGeo.AutoCad.Loader.Ribbon;

/// <summary>
/// HPGeo's place on the Ribbon: the panel "VN2000" with the button "KMZ" (runs HPGEO) on the <b>shared</b>
/// "HPAutoCad" tab — the rule for every HP add-in for AutoCAD is one tab, one panel per tool, never a new tab.
/// The tab is found by the Id the HPAutoCad MCP loader publishes (<see cref="TabId"/>); whichever add-in loads
/// first creates it, the others add their panel. This class touches nothing but its own panel: Uninstall removes
/// that panel and the tab only when it is left empty; a COLORTHEME change (the icon ink follows the theme)
/// rebuilds the panel, never the tab. Start-up, any Ribbon item initialising (RIBBON after RIBBONCLOSE), a
/// workspace switch and a theme change all go through <see cref="EnsureCreated"/> and its find-by-Id guards.
/// </summary>
internal static class HPGeoRibbonTab
{
    /// <summary>The shared tab of the HP AutoCAD add-ins — the value HPAutoCad.McpBridge.Loader creates it with.</summary>
    public const string TabId = "HPAUTOCAD_MCP_TAB";
    public const string TabTitle = "HPAutoCad";
    public const string PanelId = "HPGEO_VN2000_PANEL";
    public const string PanelTitle = "VN2000";
    public const string ButtonId = "HPGEO_KMZ";
    public const string ButtonText = "KMZ";
    public const string Command = "HPGEO";

    private static bool _installed;
    private static bool _idlePending;
    private static bool _rebuild;
    private static bool _building;

    public static void Install()
    {
        if (_installed) return;
        _installed = true;
        Application.SystemVariableChanged += OnSystemVariableChanged;
        ComponentManager.ItemInitialized += OnRibbonItemInitialized;
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
        RemoveOwnPanel(removeEmptyTab: true);
    }

    public static void EnsureCreated()
    {
        if (_building) return;
        try
        {
            var ribbon = ComponentManager.Ribbon;
            if (ribbon is null) return;
            _building = true;

            var tab = ribbon.FindTab(TabId);
            var createdTab = tab is null;
            if (tab is null)
            {
                tab = new RibbonTab { Id = TabId, Title = TabTitle, IsVisible = true };
                ribbon.Tabs.Add(tab);
            }
            if (FindOwnPanel(tab) is not null) return;

            tab.Panels.Add(BuildPanel());
            LoaderLog.Write($"ribbon panel {PanelId} added to tab {TabId} ({(createdTab ? "tab created" : "tab existing")}, add-in {(HPGeoLoaderApplication.App is null ? "unavailable" : "available")})");
        }
        catch (System.Exception exception)
        {
            // The Ribbon is a convenience: a failure here must never take the commands down.
            LoaderLog.Write("ribbon panel creation failed", exception);
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
        if (theme) _rebuild = true;
        if (_idlePending) return;
        _idlePending = true;
        Application.Idle += OnIdle;
    }

    private static void OnIdle(object? sender, EventArgs e)
    {
        Application.Idle -= OnIdle;
        _idlePending = false;
        if (_rebuild)
        {
            _rebuild = false;
            RemoveOwnPanel(removeEmptyTab: false);
        }
        EnsureCreated();
    }

    private static RibbonPanel? FindOwnPanel(RibbonTab tab) => tab.Panels.FirstOrDefault(p => p.Source?.Id == PanelId);

    private static void RemoveOwnPanel(bool removeEmptyTab)
    {
        try
        {
            var ribbon = ComponentManager.Ribbon;
            var tab = ribbon?.FindTab(TabId);
            if (tab is null) return;
            if (FindOwnPanel(tab) is { } panel) tab.Panels.Remove(panel);
            if (removeEmptyTab && tab.Panels.Count == 0) ribbon!.Tabs.Remove(tab);
        }
        catch (System.Exception exception)
        {
            LoaderLog.Write("ribbon panel removal failed", exception);
        }
    }

    private static RibbonPanel BuildPanel()
    {
        var icon = new RibbonIcons(IsDarkTheme()).Kmz;
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
            CommandHandler = new CommandRelay(ButtonId, RunCommand),
            IsEnabled = HPGeoLoaderApplication.App is not null,
        };
        button.ToolTip = new RibbonToolTip
        {
            Title = "VN-2000 → KMZ",
            Content = HPGeoLoaderApplication.App is not null
                ? "Chọn điểm/ranh VN-2000 trong bản vẽ, chọn tỉnh và kinh tuyến trục, xuất KMZ mở trong Google Earth."
                : "HPGeo không khởi động được (" + (HPGeoLoaderApplication.StartupError ?? "?") + "). Xem loader.log trong " + LoaderLog.LogDirectory,
            Command = Command,
            IsHelpEnabled = false,
        };
        var panel = new RibbonPanel { Source = new RibbonPanelSource { Id = PanelId, Title = PanelTitle } };
        panel.Source.Items.Add(button);
        return panel;
    }

    /// <summary>A Ribbon click arrives outside any command context; the command is queued like a typed one.</summary>
    private static void RunCommand()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        if (doc is null)
        {
            LoaderLog.Write("ribbon click with no drawing open");
            return;
        }
        doc.SendStringToExecute("_." + Command + " ", true, false, false);
    }

    /// <summary>COLORTHEME 0 = dark (AutoCAD's default), 1 = light; unreadable → dark.</summary>
    private static bool IsDarkTheme()
    {
        try { return Convert.ToInt32(Application.GetSystemVariable("COLORTHEME")) == 0; }
        catch (System.Exception) { return true; }
    }

    /// <summary>ICommand for a Ribbon button that never lets an exception reach the Ribbon (a silent dead button).</summary>
    private sealed class CommandRelay(string name, Action action) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            try { action(); }
            catch (System.Exception exception) { LoaderLog.Write($"ribbon '{name}' failed", exception); }
        }
    }
}
