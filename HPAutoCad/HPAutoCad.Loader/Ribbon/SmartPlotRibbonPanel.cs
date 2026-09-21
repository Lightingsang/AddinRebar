using System;
using System.Linq;
using System.Windows.Controls;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.Windows;

namespace HPAutoCad.Loader.Ribbon;

/// <summary>
/// Manages the Smart Plot Pro panel ("HPPLOT_PANEL") on the shared "HPAutoCad" Ribbon tab ("HPAUTOCAD_MCP_TAB").
/// Adheres strictly to the multi-add-in shared tab protocol:
/// 1. Finds or creates the shared tab without affecting sibling panels (e.g. MCP, HPGeoLink).
/// 2. Manages only its own panel ("HPPLOT_PANEL").
/// 3. Dynamically rebuilds icons when COLORTHEME flips.
/// 4. Recreates the panel after workspace switching (WSCURRENT).
/// 5. Automatically binds late Ribbon initialization via ComponentManager.ItemInitialized.
/// </summary>
internal static class SmartPlotRibbonPanel
{
    public const string TabId = "HPAUTOCAD_MCP_TAB";
    public const string TabTitle = "HPAutoCad";
    public const string PanelId = "HPPLOT_PANEL";
    public const string PanelTitle = "Plot";

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

            var isAvailable = HPAutoCadLoaderApplication.App is not null;
            var icons = new RibbonIcons(IsDarkTheme());
            tab.Panels.Add(BuildPanel(isAvailable, icons));

            var status = isAvailable ? "available" : "unavailable";
            if (createdTab) LoaderLog.Write($"ribbon tab {TabId} created (SmartPlot {status})");
            LoaderLog.Write($"ribbon panel {PanelId} added to tab {TabId} ({(createdTab ? "tab created" : "tab existing")}, SmartPlot {status})");
        }
        catch (Exception exception)
        {
            LoaderLog.Write("SmartPlot ribbon panel creation failed", exception);
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

    private static RibbonPanel? FindOwnPanel(RibbonTab tab) =>
        tab.Panels.FirstOrDefault(p => p.Source?.Id == PanelId);

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
        catch (Exception exception)
        {
            LoaderLog.Write("SmartPlot ribbon panel removal failed", exception);
        }
    }

    public static RibbonPanel BuildPanel(bool isAvailable, RibbonIcons icons)
    {
        var plotButton = new RibbonButton
        {
            Id = "HPSMARTPLOT_BUTTON",
            Text = "Smart Plot\nPro",
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Large,
            Orientation = Orientation.Vertical,
            Image = icons.Plot,
            LargeImage = icons.Plot,
            CommandHandler = new RibbonCommandHandler("HPSMARTPLOT_BUTTON", () => RunCommand("HPSMARTPLOT")),
            IsEnabled = isAvailable,
            ToolTip = new RibbonToolTip
            {
                Title = "Smart Plot Pro (HPSMARTPLOT)",
                Content = isAvailable
                    ? "Tự động quét khung tên, nhận diện khổ giấy và in hàng loạt ra PDF (đơn lẻ hoặc gộp file qua PdfSharp)."
                    : "HPAutoCad chưa khởi động (" + (HPAutoCadLoaderApplication.StartupError ?? "?") + ").",
                Command = "HPSMARTPLOT",
                IsHelpEnabled = false,
            }
        };

        var panel = new RibbonPanel { Source = new RibbonPanelSource { Id = PanelId, Title = PanelTitle } };
        panel.Source.Items.Add(plotButton);
        return panel;
    }

    private static void RunCommand(string command)
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        if (doc is null)
        {
            LoaderLog.Write($"ribbon click on '{command}' with no document open");
            return;
        }
        doc.SendStringToExecute("_." + command + " ", true, false, false);
    }

    private static bool IsDarkTheme()
    {
        try { return Convert.ToInt32(Application.GetSystemVariable("COLORTHEME")) == 0; }
        catch (Exception) { return true; }
    }
}
