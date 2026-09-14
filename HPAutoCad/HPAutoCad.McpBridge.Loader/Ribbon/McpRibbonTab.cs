using System.IO;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.Windows;

namespace HPAutoCad.McpBridge.Loader.Ribbon;

/// <summary>
///     The "MCP AutoCAD" Ribbon tab. Every button forwards to the same bridge entry points the HPMCP*
///     commands use, so a click needs no document and never edits the drawing. The tab is created at most
///     once per Ribbon: creation waits for the Ribbon to exist, a workspace switch (which rebuilds the
///     Ribbon from the CUI and drops tabs added in code) schedules a re-check on the next idle tick, and
///     <c>FindTab</c> guards every attempt — repeated Initialize calls cannot produce duplicates.
/// </summary>
internal static class McpRibbonTab
{
    public const string TabId = "HPAUTOCAD_MCP_TAB";
    public const string TabTitle = "MCP AutoCAD";

    private static bool _installed;
    private static bool _recreatePending;
    private static RibbonStatusPresenter? _status;

    public static void Install()
    {
        if (_installed) return;
        _installed = true;

        Application.SystemVariableChanged += OnSystemVariableChanged;
        if (ComponentManager.Ribbon is null) ComponentManager.ItemInitialized += OnRibbonItemInitialized; // Ribbon not built yet at startup
        else EnsureCreated();
    }

    public static void Uninstall()
    {
        if (!_installed) return;
        _installed = false;

        Application.SystemVariableChanged -= OnSystemVariableChanged;
        ComponentManager.ItemInitialized -= OnRibbonItemInitialized;
        Application.Idle -= OnIdleRecreate;
        _status?.Dispose();
        _status = null;

        var ribbon = ComponentManager.Ribbon;
        if (ribbon?.FindTab(TabId) is { } tab) ribbon.Tabs.Remove(tab);
    }

    /// <summary>Adds the tab when the Ribbon exists and does not already show it.</summary>
    public static void EnsureCreated()
    {
        try
        {
            var ribbon = ComponentManager.Ribbon;
            if (ribbon is null || ribbon.FindTab(TabId) is not null) return;

            _status?.Dispose();
            ribbon.Tabs.Add(Build());
            LoaderLog.Write($"ribbon tab {TabId} created (bridge {(BridgeActions.BridgeAvailable ? "available" : "unavailable")})");
        }
        catch (System.Exception exception)
        {
            // The Ribbon is a convenience: a failure here must never take the bridge or its commands down.
            LoaderLog.Write("ribbon tab creation failed", exception);
        }
    }

    private static void OnRibbonItemInitialized(object? sender, RibbonItemEventArgs e)
    {
        if (ComponentManager.Ribbon is null) return;
        ComponentManager.ItemInitialized -= OnRibbonItemInitialized;
        EnsureCreated();
    }

    private static void OnSystemVariableChanged(object? sender, Autodesk.AutoCAD.ApplicationServices.SystemVariableChangedEventArgs e)
    {
        if (!string.Equals(e.Name, "WSCURRENT", StringComparison.OrdinalIgnoreCase) || _recreatePending) return;
        _recreatePending = true;
        Application.Idle += OnIdleRecreate; // the new workspace's Ribbon is complete by the next idle tick
    }

    private static void OnIdleRecreate(object? sender, EventArgs e)
    {
        Application.Idle -= OnIdleRecreate;
        _recreatePending = false;
        EnsureCreated();
    }

    private static RibbonTab Build()
    {
        var tab = new RibbonTab { Id = TabId, Title = TabTitle, IsVisible = true };
        var bridge = BridgeActions.BridgeAvailable;

        var connection = Panel("HPAUTOCAD_MCP_PANEL_CONNECTION", "Kết nối");
        connection.Source.Items.Add(Button("HPAUTOCAD_MCP_SHOW", "Bảng\nđiều khiển", "Mở cửa sổ trạng thái bridge: bật/tắt listener, tick \"Allow AI code execution\" cho phiên này, script cuối.", "HPMCPBRIDGE", RibbonIcons.Panel, () => BridgeActions.Run("show"), bridge));
        connection.Source.Items.Add(Button("HPAUTOCAD_MCP_START", "Bật\nlistener", "Mở named pipe để MCP server (do Claude Code chạy) kết nối. Không khởi động server.", "HPMCPSTART", RibbonIcons.Start, () => BridgeActions.Run("start"), bridge));
        connection.Source.Items.Add(Button("HPAUTOCAD_MCP_STOP", "Tắt\nlistener", "Đóng named pipe; MCP server sẽ báo \"bridge not connected\".", "HPMCPSTOP", RibbonIcons.Stop, () => BridgeActions.Run("stop"), bridge));
        connection.Source.Items.Add(Button("HPAUTOCAD_MCP_STATUS", "Trạng\nthái", "In trạng thái bridge (pipe, listener, opt-in, self-check, script cuối) ra dòng lệnh — hoặc hộp thoại khi chưa mở bản vẽ.", "HPMCPSTATUS", RibbonIcons.Info, () => BridgeActions.Run("status"), bridge));
        var label = new RibbonLabel { Id = "HPAUTOCAD_MCP_STATUS_TEXT", Text = "…" };
        connection.Source.Items.Add(new RibbonRowBreak());
        connection.Source.Items.Add(label);
        _status = new RibbonStatusPresenter(label);
        tab.Panels.Add(connection);

        var tools = Panel("HPAUTOCAD_MCP_PANEL_TOOLS", "Công cụ");
        tools.Source.Items.Add(Button("HPAUTOCAD_MCP_COPY", "Sao chép\nscript cuối", "Sao chép mã C# của lần chạy gần nhất vào clipboard để xem lại hoặc gửi cho AI.", null, RibbonIcons.Clipboard, () => BridgeActions.Run("copyLastScript"), bridge));
        tools.Source.Items.Add(Button("HPAUTOCAD_MCP_LIBRARY", "Thư viện\ntool", "Mở thư mục tools-library của MCP server: mỗi tool đã lưu là một thư mục tool.json + code.cs + examples.json.", null, RibbonIcons.Library, () => BridgeActions.OpenPath(BridgeActions.Query<string>("path", "library"), "thư viện tool"), bridge));
        tab.Panels.Add(tools);

        var settings = Panel("HPAUTOCAD_MCP_PANEL_SETTINGS", "Thiết lập");
        settings.Source.Items.Add(Button("HPAUTOCAD_MCP_LOGS", "Mở\nnhật ký", "Mở thư mục log của bridge (loader.log, mcpbridge-*.log).", null, RibbonIcons.Logs, () => BridgeActions.OpenPath(LoaderLog.LogDirectory, "thư mục nhật ký"), true));
        settings.Source.Items.Add(Button("HPAUTOCAD_MCP_AUDIT", "Mở\naudit", "Mở thư mục audit: mỗi script AI đã chạy là một dòng JSON.", null, RibbonIcons.Audit, () => BridgeActions.OpenPath(BridgeActions.Query<string>("path", "audit"), "thư mục audit"), bridge));
        settings.Source.Items.Add(AutoStartToggle(bridge));
        settings.Source.Items.Add(Button("HPAUTOCAD_MCP_GUIDE", "Hướng\ndẫn", "Mở hướng dẫn sử dụng (README đóng gói cùng bundle).", null, RibbonIcons.Guide, () => BridgeActions.OpenPath(GuidePath(), "hướng dẫn"), true));
        tab.Panels.Add(settings);

        return tab;
    }

    private static RibbonPanel Panel(string id, string title) => new RibbonPanel { Source = new RibbonPanelSource { Id = id, Title = title } };

    private static RibbonButton Button(string id, string text, string tooltip, string? command, ImageSource icon, Action action, bool enabled)
    {
        var button = new RibbonButton
        {
            Id = id,
            Text = text,
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Large,
            Orientation = Orientation.Vertical,
            Image = icon,
            LargeImage = icon,
            CommandHandler = new RibbonCommandHandler(id, action),
            IsEnabled = enabled,
        };
        button.ToolTip = new RibbonToolTip
        {
            Title = text.Replace('\n', ' '),
            Content = enabled ? tooltip : tooltip + "\n\nBridge chưa khởi động — xem loader.log (nút Mở nhật ký).",
            Command = command ?? string.Empty,
            IsHelpEnabled = false,
        };
        return button;
    }

    /// <summary>Reads and writes the persisted AutoStartListener setting; the button never guesses its own state.</summary>
    private static RibbonToggleButton AutoStartToggle(bool enabled)
    {
        var toggle = new RibbonToggleButton
        {
            Id = "HPAUTOCAD_MCP_AUTOSTART",
            Text = "Tự khởi động\nlistener",
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Large,
            Orientation = Orientation.Vertical,
            Image = RibbonIcons.AutoStart,
            LargeImage = RibbonIcons.AutoStart,
            IsEnabled = enabled,
            IsChecked = enabled && BridgeActions.Query<bool>("autoStart.get"),
        };
        toggle.ToolTip = new RibbonToolTip
        {
            Title = "Tự khởi động listener",
            Content = "Bật: listener mở pipe ngay khi AutoCAD khởi động (lưu trong settings.json). Không ảnh hưởng opt-in \"Allow AI code execution\", luôn tắt khi mở AutoCAD.",
            IsHelpEnabled = false,
        };
        toggle.CommandHandler = new RibbonCommandHandler(toggle.Id, () =>
        {
            var value = !BridgeActions.Query<bool>("autoStart.get");
            BridgeActions.Run("autoStart.set", value);
            toggle.IsChecked = value;
        });
        return toggle;
    }

    private static string GuidePath() => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "README.md");
}
