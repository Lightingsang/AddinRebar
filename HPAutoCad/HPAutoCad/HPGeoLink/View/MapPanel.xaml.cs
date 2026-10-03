using System.IO;
using System.Windows;
using System.Windows.Controls;
using HPAutoCad.HPGeoLink.Support;
using Microsoft.Web.WebView2.Core;

namespace HPAutoCad.HPGeoLink.View;

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

    public static readonly DependencyProperty ZoomTextProperty = DependencyProperty.Register(
        nameof(ZoomText), typeof(string), typeof(MapPanel), new PropertyMetadata("Zoom 18 · Satellite"));

    private static readonly string UserDataFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HPAutoCad", "webview2");

    private static bool _loaderPathConfigured;

    static MapPanel()
    {
        EnsureWebView2LoaderPath();
    }

    private static void EnsureWebView2LoaderPath()
    {
        if (_loaderPathConfigured) return;

        try
        {
            var candidates = new List<string>();

            // 1. Executing assembly directory
            var asmLocation = typeof(MapPanel).Assembly.Location;
            if (!string.IsNullOrEmpty(asmLocation))
            {
                var asmDir = Path.GetDirectoryName(asmLocation);
                if (!string.IsNullOrEmpty(asmDir))
                {
                    candidates.Add(Path.Combine(asmDir, "runtimes", "win-x64", "native"));
                    candidates.Add(asmDir);
                }
            }

            // 2. Official bundle App directory in %AppData%
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var bundleNative = Path.Combine(appData, "Autodesk", "ApplicationPlugins", "HPAutoCad.bundle", "Contents", "App", "runtimes", "win-x64", "native");
            var bundleApp = Path.Combine(appData, "Autodesk", "ApplicationPlugins", "HPAutoCad.bundle", "Contents", "App");
            candidates.Add(bundleNative);
            candidates.Add(bundleApp);

            // 3. Dev build output directory
            var devNative = @"F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPAutoCad\HPAutoCad\bin\Debug\net8.0-windows\runtimes\win-x64\native";
            var devDir = @"F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPAutoCad\HPAutoCad\bin\Debug\net8.0-windows";
            candidates.Add(devNative);
            candidates.Add(devDir);

            // 4. Base directory
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(baseDir))
            {
                candidates.Add(Path.Combine(baseDir, "runtimes", "win-x64", "native"));
                candidates.Add(baseDir);
            }

            string? foundLoader = null;
            string? foundDir = null;
            foreach (var dir in candidates)
            {
                var loaderPath = Path.Combine(dir, "WebView2Loader.dll");
                if (File.Exists(loaderPath))
                {
                    foundLoader = loaderPath;
                    foundDir = dir;
                    break;
                }
            }

            if (foundLoader != null && foundDir != null)
            {
                try
                {
                    System.Runtime.InteropServices.NativeLibrary.SetDllImportResolver(
                        typeof(CoreWebView2Environment).Assembly,
                        (libraryName, assembly, searchPath) =>
                        {
                            if (libraryName.Equals("WebView2Loader.dll", StringComparison.OrdinalIgnoreCase) ||
                                libraryName.Equals("WebView2Loader", StringComparison.OrdinalIgnoreCase))
                            {
                                if (File.Exists(foundLoader))
                                {
                                    return System.Runtime.InteropServices.NativeLibrary.Load(foundLoader);
                                }
                            }
                            return IntPtr.Zero;
                        });
                    HPGeoLog.Information("Registered NativeLibrary DllImportResolver for WebView2Loader: " + foundLoader);
                }
                catch (Exception resEx)
                {
                    HPGeoLog.Warning("SetDllImportResolver: " + resEx.Message);
                }

                try
                {
                    CoreWebView2Environment.SetLoaderDllFolderPath(foundDir);
                    HPGeoLog.Information("Configured WebView2Loader folder: " + foundDir);
                }
                catch (Exception setEx)
                {
                    HPGeoLog.Warning("SetLoaderDllFolderPath: " + setEx.Message);
                }

                if (!string.IsNullOrEmpty(asmLocation))
                {
                    var asmDir = Path.GetDirectoryName(asmLocation);
                    if (!string.IsNullOrEmpty(asmDir) && !asmDir.Equals(foundDir, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var dest = Path.Combine(asmDir, "WebView2Loader.dll");
                            if (!File.Exists(dest) || new FileInfo(dest).Length != new FileInfo(foundLoader).Length)
                            {
                                File.Copy(foundLoader, dest, true);
                            }
                        }
                        catch { }
                    }
                }

                _loaderPathConfigured = true;
                return;
            }

            HPGeoLog.Warning("WebView2Loader.dll not found in any candidate directory.");
        }
        catch (Exception ex)
        {
            HPGeoLog.Warning("Could not set WebView2Loader DLL folder path: " + ex.Message);
        }
    }

    private Microsoft.Web.WebView2.Wpf.WebView2? _browser;
    private bool _ready;
    private bool _tileLogged;
    private bool _disposed;

    public MapPanel()
    {
        EnsureWebView2LoaderPath();
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

    public string ZoomText
    {
        get => (string)GetValue(ZoomTextProperty);
        set => SetValue(ZoomTextProperty, value);
    }

    public void FitBounds()
    {
        if (_disposed || !_ready || _browser?.CoreWebView2 is null) return;
        try
        {
            _browser.CoreWebView2.PostWebMessageAsString("fit-bounds");
        }
        catch (Exception ex)
        {
            HPGeoLog.Warning("fit-bounds message failed: " + ex.Message);
        }
    }

    public void RefreshMap() => PushData();

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        try
        {
            EnsureWebView2LoaderPath();
            var version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            Directory.CreateDirectory(UserDataFolder);
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: UserDataFolder);
            if (_disposed) return; // the dialog was closed before the browser came up

            _browser = new Microsoft.Web.WebView2.Wpf.WebView2 { Visibility = Visibility.Collapsed };
            BrowserContainer.Child = _browser;

            await _browser.EnsureCoreWebView2Async(environment);
            if (_disposed) return;
            HPGeoLog.Information($"WebView2 {version} initialised (user data {UserDataFolder})");

            _browser.CoreWebView2.Settings.UserAgent = "HPAutoCad/" + Entry.Version + " (AutoCAD add-in; WebView2 " + version + ")";
            _browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            _browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
            _browser.CoreWebView2.WebMessageReceived += OnWebMessage;
            _browser.NavigateToString(MapHtml.Page(DarkTheme));
            _browser.Visibility = Visibility.Visible;
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
                if (message is not null)
                {
                    if (message.StartsWith("zoom:", StringComparison.Ordinal))
                    {
                        var z = message.Substring(5);
                        ZoomText = $"Zoom {z} · Satellite";
                    }
                    else if (message.StartsWith("update-error", StringComparison.Ordinal))
                    {
                        HPGeoLog.Warning("map page rejected the data: " + message);
                    }
                }
                break;
        }
    }

    private void PushData()
    {
        if (_disposed || !_ready || _browser?.CoreWebView2 is null) return;
        // An empty document clears the map: stale points must not outlive an invalid conversion.
        var json = string.IsNullOrEmpty(Data) ? "{\"points\":[],\"boundaries\":[]}" : Data;
        try
        {
            _browser.CoreWebView2.PostWebMessageAsString(json);
        }
        catch (Exception exception)
        {
            HPGeoLog.Warning("map data could not be posted: " + exception.Message);
        }
    }

    private void ShowFallback(string text)
    {
        if (_browser != null) _browser.Visibility = Visibility.Collapsed;
        Fallback.Text = text;
        Fallback.Visibility = Visibility.Visible;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Unloaded -= OnUnloaded;
        _disposed = true;
        try
        {
            if (_browser?.CoreWebView2 is not null) _browser.CoreWebView2.WebMessageReceived -= OnWebMessage;
            _browser?.Dispose();
            _browser = null;
        }
        catch (Exception exception)
        {
            HPGeoLog.Warning("WebView2 dispose failed: " + exception.Message);
        }
    }
}
