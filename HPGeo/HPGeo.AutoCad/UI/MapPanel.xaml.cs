using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;

namespace HPGeo.AutoCad.UI;

/// <summary>
/// Leaflet map in a WebView2, fed by the <see cref="Data"/> dependency property (the view model's JSON).
/// The browser is created on Loaded with a user-data folder under %LocalAppData% (acad.exe's folder is
/// read-only) and torn down on Unloaded. Every failure — no runtime, no internet, blocked tiles — is logged
/// and turns the panel into a one-line explanation; the Google Earth / Maps buttons stay the fail-safe.
/// </summary>
public partial class MapPanel : UserControl
{
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(string), typeof(MapPanel), new PropertyMetadata("", (d, _) => ((MapPanel)d).PushData()));

    public static readonly DependencyProperty DarkThemeProperty = DependencyProperty.Register(
        nameof(DarkTheme), typeof(bool), typeof(MapPanel), new PropertyMetadata(true));

    private static readonly string UserDataFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HPGeo", "webview2");

    private bool _ready;
    private bool _tileLogged;
    private bool _disposed;

    public MapPanel()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public string Data
    {
        get => (string)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public bool DarkTheme
    {
        get => (bool)GetValue(DarkThemeProperty);
        set => SetValue(DarkThemeProperty, value);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        try
        {
            var version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            Directory.CreateDirectory(UserDataFolder);
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: UserDataFolder);
            if (_disposed) return; // the dialog was closed before the browser came up
            await Browser.EnsureCoreWebView2Async(environment);
            if (_disposed) return;
            HPGeoLog.Information($"WebView2 {version} initialised (user data {UserDataFolder})");

            Browser.CoreWebView2.Settings.UserAgent = "HPGeo/" + Entry.Version + " (AutoCAD add-in; WebView2 " + version + ")";
            Browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            Browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
            Browser.CoreWebView2.WebMessageReceived += OnWebMessage;
            Browser.NavigateToString(MapHtml.Page(DarkTheme));
            Browser.Visibility = Visibility.Visible;
        }
        catch (Exception exception)
        {
            if (_disposed) return; // closed mid-start-up: not an error worth a log line
            // WebView2Runtime missing (WebView2RuntimeNotFoundException), a locked user-data folder, an old runtime —
            // the map is a convenience and must never take the dialog down.
            HPGeoLog.Error("WebView2 could not start; map panel disabled", exception);
            ShowFallback("Bản đồ không khả dụng (" + exception.GetType().Name + "). Dùng nút Mở Google Earth / Google Maps.");
        }
    }

    private void OnWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var message = e.TryGetWebMessageAsString();
        switch (message)
        {
            case "page-ready":
                _ready = true;
                Fallback.Visibility = Visibility.Collapsed;
                PushData();
                break;
            case "tile-loaded":
                if (!_tileLogged) HPGeoLog.Information("map tile loaded (Esri World Imagery via Leaflet " + MapHtml.LeafletVersion + ")");
                _tileLogged = true;
                break;
            case "tile-error":
                HPGeoLog.Warning("map tile failed to load (no internet or tile server unreachable); points are still drawn");
                break;
            case "leaflet-missing":
                HPGeoLog.Warning("Leaflet could not be loaded from the CDN (no internet); map panel disabled");
                ShowFallback("Không tải được thư viện bản đồ (cần Internet). Dùng nút Mở Google Earth / Google Maps.");
                break;
            default:
                if (message is not null && message.StartsWith("update-error", StringComparison.Ordinal))
                    HPGeoLog.Warning("map page rejected the data: " + message);
                break;
        }
    }

    private void PushData()
    {
        if (_disposed || !_ready || Browser.CoreWebView2 is null) return;
        // An empty document clears the map: stale points must not outlive an invalid conversion.
        var json = string.IsNullOrEmpty(Data) ? "{\"points\":[],\"boundaries\":[]}" : Data;
        try
        {
            Browser.CoreWebView2.PostWebMessageAsString(json);
        }
        catch (Exception exception)
        {
            HPGeoLog.Warning("map data could not be posted: " + exception.Message);
        }
    }

    private void ShowFallback(string text)
    {
        Browser.Visibility = Visibility.Collapsed;
        Fallback.Text = text;
        Fallback.Visibility = Visibility.Visible;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Unloaded -= OnUnloaded;
        _disposed = true;
        try
        {
            if (Browser.CoreWebView2 is not null) Browser.CoreWebView2.WebMessageReceived -= OnWebMessage;
            Browser.Dispose();
        }
        catch (Exception exception)
        {
            HPGeoLog.Warning("WebView2 dispose failed: " + exception.Message);
        }
    }
}
