using System.Windows.Controls;
using System.Windows.Input;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.Windows;

namespace HPGeo.AutoCad.Loader.Ribbon;

/// <summary>
/// The "HPGeo" Ribbon tab: one panel "VN2000" with one button "KMZ" that runs the HPGEO command. The tab
/// exists at most once per Ribbon: start-up, any Ribbon item initialising (RIBBON after RIBBONCLOSE), a
/// workspace switch (rebuilds the Ribbon from the CUI) and a COLORTHEME change (the icon ink follows the
/// theme) all go through <see cref="EnsureCreated"/> and its FindTab guard.
/// </summary>
internal static class HPGeoRibbonTab
{
    public const string TabId = "HPGEO_TAB";
    public const string TabTitle = "HPGeo";
    public const string PanelId = "HPGEO_VN2000_PANEL";
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
        var ribbon = ComponentManager.Ribbon;
        if (ribbon?.FindTab(TabId) is { } tab) ribbon.Tabs.Remove(tab);
    }

    public static void EnsureCreated()
    {
        if (_building) return;
        try
        {
            var ribbon = ComponentManager.Ribbon;
            if (ribbon is null || ribbon.FindTab(TabId) is not null) return;
            _building = true;
            ribbon.Tabs.Add(Build());
            LoaderLog.Write($"ribbon tab {TabId} created (add-in {(HPGeoLoaderApplication.App is null ? "unavailable" : "available")})");
        }
        catch (System.Exception exception)
        {
            // The Ribbon is a convenience: a failure here must never take the commands down.
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
        var panel = new RibbonPanel { Source = new RibbonPanelSource { Id = PanelId, Title = "VN2000" } };
        panel.Source.Items.Add(button);
        tab.Panels.Add(panel);
        return tab;
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
