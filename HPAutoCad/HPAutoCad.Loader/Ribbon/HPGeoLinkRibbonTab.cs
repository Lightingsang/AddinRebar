using System;
using System.Linq;
using System.Windows.Controls;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.Windows;

namespace HPAutoCad.Loader.Ribbon;

/// <summary>
/// Manages the HPGeoLink panel on the shared "HPAutoCad" Ribbon tab (TabId: "HPAUTOCAD_MCP_TAB").
/// Adheres strictly to the multi-add-in shared tab protocol:
/// 1. Finds or creates the shared tab without affecting sibling panels (e.g. MCP).
/// 2. Manages only its own panel ("HPGEOLINK_PANEL").
/// 3. Dynamically rebuilds icons when COLORTHEME flips (without removing the shared tab).
/// 4. Recreates the panel after workspace switching (WSCURRENT).
/// 5. Automatically binds late Ribbon initialization via ComponentManager.ItemInitialized.
/// </summary>
internal static class HPGeoLinkRibbonTab
{
    public const string TabId = "HPAUTOCAD_MCP_TAB";
    public const string TabTitle = "HPAutoCad";
    public const string PanelId = "HPGEOLINK_PANEL";
    public const string PanelTitle = "HPGeoLink";

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

            CadAddinManagerIntegrator.EnsureRibbonTab(ribbon);

            var tab = ribbon.FindTab(TabId);
            var createdTab = tab is null;
            if (tab is null)
            {
                tab = new RibbonTab { Id = TabId, Title = TabTitle, IsVisible = true };
                ribbon.Tabs.Add(tab);
            }
            if (FindOwnPanel(tab) is not null) return;

            tab.Panels.Add(BuildPanel());
            var status = HPAutoCadLoaderApplication.App is not null ? "available" : "unavailable";
            if (createdTab) LoaderLog.Write($"ribbon tab {TabId} created (HPGeoLink {status})");
            LoaderLog.Write($"ribbon panel {PanelId} added to tab {TabId} ({(createdTab ? "tab created" : "tab existing")}, HPGeoLink {status})");
        }
        catch (Exception exception)
        {
            LoaderLog.Write("HPGeoLink ribbon panel creation failed", exception);
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
            LoaderLog.Write("HPGeoLink ribbon panel removal failed", exception);
        }
    }

    private static RibbonPanel BuildPanel()
    {
        var isAvailable = HPAutoCadLoaderApplication.App is not null;
        var icons = new RibbonIcons(IsDarkTheme());

        // 1. Primary Large Button: HPGEO_KMZ
        var kmzButton = new RibbonButton
        {
            Id = "HPGEO_KMZ",
            Text = "KMZ",
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Large,
            Orientation = Orientation.Vertical,
            Image = icons.Kmz,
            LargeImage = icons.Kmz,
            CommandHandler = new RibbonCommandHandler("HPGEO_KMZ", () => RunCommand("HPGEO")),
            IsEnabled = isAvailable,
            ToolTip = new RibbonToolTip
            {
                Title = "VN-2000 → KMZ (HPGEO)",
                Content = isAvailable
                    ? "Chọn điểm/ranh VN-2000 trong bản vẽ, chọn tỉnh và kinh tuyến trục, xuất KMZ mở trong Google Earth."
                    : GetUnavailableReason(),
                Command = "HPGEO",
                IsHelpEnabled = false,
            }
        };

        // 2. Dropdown item: HPGEO_IMPORT
        var importItem = new RibbonButton
        {
            Id = "HPGEO_IMPORT",
            Text = "Import KML/KMZ",
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Standard,
            Orientation = Orientation.Horizontal,
            Image = icons.Import,
            LargeImage = icons.Import,
            CommandHandler = new RibbonCommandHandler("HPGEO_IMPORT", () => RunCommand("HPGEOIMPORT")),
            IsEnabled = isAvailable,
            ToolTip = new RibbonToolTip
            {
                Title = "Import KML/KMZ (HPGEOIMPORT)",
                Content = isAvailable
                    ? "Nhập đối tượng địa lý từ KML/KMZ hoặc danh sách tọa độ vào CAD trên layer HPGEO-IMPORT theo hệ tọa độ VN-2000."
                    : GetUnavailableReason(),
                Command = "HPGEOIMPORT",
                IsHelpEnabled = false,
            }
        };

        // 3. Dropdown item: HPGEO_IMAGE
        var imageItem = new RibbonButton
        {
            Id = "HPGEO_IMAGE",
            Text = "Ảnh vệ tinh",
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Standard,
            Orientation = Orientation.Horizontal,
            Image = icons.Map,
            LargeImage = icons.Map,
            CommandHandler = new RibbonCommandHandler("HPGEO_IMAGE", () => RunCommand("-HPGEOIMAGE")),
            IsEnabled = isAvailable,
            ToolTip = new RibbonToolTip
            {
                Title = "Chèn ảnh vệ tinh (-HPGEOIMAGE)",
                Content = isAvailable
                    ? "Tải và chèn ảnh vệ tinh độ phân giải cao (Esri/Google/Bing) theo khung ranh đất VN-2000 vào CAD."
                    : GetUnavailableReason(),
                Command = "-HPGEOIMAGE",
                IsHelpEnabled = false,
            }
        };

        // 4. Dropdown item: HPGEO_INFO
        var infoItem = new RibbonButton
        {
            Id = "HPGEO_INFO",
            Text = "Thông tin & Chẩn đoán",
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Standard,
            Orientation = Orientation.Horizontal,
            Image = icons.Info,
            LargeImage = icons.Info,
            CommandHandler = new RibbonCommandHandler("HPGEO_INFO", () => RunCommand("HPGEOINFO")),
            IsEnabled = isAvailable,
            ToolTip = new RibbonToolTip
            {
                Title = "Thông tin & Chẩn đoán (HPGEOINFO)",
                Content = isAvailable
                    ? "Hiển thị thông tin hệ tọa độ, kinh tuyến trục bản vẽ, INSUNITS, danh mục tỉnh thành và log chẩn đoán."
                    : GetUnavailableReason(),
                Command = "HPGEOINFO",
                IsHelpEnabled = false,
            }
        };

        // 5. Dropdown item: HPGEO_KMZ_SCRIPT
        var kmzScriptItem = new RibbonButton
        {
            Id = "HPGEO_KMZ_SCRIPT",
            Text = "Xuất KMZ (Script)",
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Standard,
            Orientation = Orientation.Horizontal,
            Image = icons.KmzScript,
            LargeImage = icons.KmzScript,
            CommandHandler = new RibbonCommandHandler("HPGEO_KMZ_SCRIPT", () => RunCommand("-HPGEOKMZ")),
            IsEnabled = isAvailable,
            ToolTip = new RibbonToolTip
            {
                Title = "Xuất KMZ dòng lệnh (-HPGEOKMZ)",
                Content = isAvailable
                    ? "Thực thi xuất KMZ ở chế độ không mở hộp thoại với tham số dòng lệnh (hỗ trợ script tự động)."
                    : GetUnavailableReason(),
                Command = "-HPGEOKMZ",
                IsHelpEnabled = false,
            }
        };

        // 6. Dropdown item: HPGEO_IMPORT_SCRIPT
        var importScriptItem = new RibbonButton
        {
            Id = "HPGEO_IMPORT_SCRIPT",
            Text = "Nhập KML/KMZ (Script)",
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Standard,
            Orientation = Orientation.Horizontal,
            Image = icons.Import,
            LargeImage = icons.Import,
            CommandHandler = new RibbonCommandHandler("HPGEO_IMPORT_SCRIPT", () => RunCommand("-HPGEOIMPORT")),
            IsEnabled = isAvailable,
            ToolTip = new RibbonToolTip
            {
                Title = "Nhập KML/KMZ dòng lệnh (-HPGEOIMPORT)",
                Content = isAvailable
                    ? "Thực thi nhập KML/KMZ ở chế độ không mở hộp thoại với tham số dòng lệnh file=<path> cm=<ktt>."
                    : GetUnavailableReason(),
                Command = "-HPGEOIMPORT",
                IsHelpEnabled = false,
            }
        };

        // Secondary Large SplitButton
        var splitButton = new RibbonSplitButton
        {
            Id = "HPGEO_SECONDARY_SPLIT",
            Text = "Import",
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Large,
            Orientation = Orientation.Vertical,
            IsSplit = true,
            Image = icons.Import,
            LargeImage = icons.Import,
            CommandHandler = new RibbonCommandHandler("HPGEO_IMPORT", () => RunCommand("HPGEOIMPORT")),
            IsEnabled = isAvailable,
            ToolTip = new RibbonToolTip
            {
                Title = "Nhập dữ liệu & Công cụ địa lý",
                Content = isAvailable
                    ? "Nhấp nút để Import KML/KMZ vào CAD; nhấp mũi tên để mở menu các công cụ phụ trợ (Ảnh vệ tinh, Thông tin hệ tọa độ, Lệnh Script)."
                    : GetUnavailableReason(),
                Command = "HPGEOIMPORT",
                IsHelpEnabled = false,
            }
        };

        splitButton.Items.Add(importItem);
        splitButton.Items.Add(imageItem);
        splitButton.Items.Add(infoItem);
        splitButton.Items.Add(kmzScriptItem);
        splitButton.Items.Add(importScriptItem);

        var panel = new RibbonPanel { Source = new RibbonPanelSource { Id = PanelId, Title = PanelTitle } };
        panel.Source.Items.Add(kmzButton);
        panel.Source.Items.Add(splitButton);
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
        // SendStringToExecute queues command execution through AutoCAD's normal command loop
        doc.SendStringToExecute("_." + command + " ", true, false, false);
    }

    private static bool IsDarkTheme()
    {
        try { return Convert.ToInt32(Application.GetSystemVariable("COLORTHEME")) == 0; }
        catch (Exception) { return true; }
    }

    private static string GetUnavailableReason() =>
        "HPGeoLink không khởi động được (" + (HPAutoCadLoaderApplication.StartupError ?? "?") + "). Xem loader.log trong " + LoaderLog.LogDirectory;
}
