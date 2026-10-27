using System.IO;
using System.Windows;
using System.Windows.Threading;
using HPRebar.Resources.Icons;
using HPRebar.Resources.Themes;
using HPWebHost.Hosting;
using HPWebHost.Protocol;
using Serilog;

namespace HPRebar.KataExport.View.Web.Settings;

/// <summary>
///     The Kata Settings window (Revit 2026 only): modal over Revit, its content is the settings page of
///     HPBimToolkit.Frontend in WebView2, served beside the Kata Export page on the same virtual host and profile.
/// </summary>
public sealed class KataSettingsWebWindow : Window
{
    private const string ProductName = "Kata Settings";
    private const string VirtualHost = "kata.hprebar.local";
    private const string StartPage = "settings.html";

    /// <summary>The Kata Export page's dev server (…/kata.html): this window opens settings.html beside it.</summary>
    private const string DevServerVariable = "HPREBAR_KATA_DEV_URL";
    private const string DebugPortVariable = "HPREBAR_KATA_DEBUG_PORT";

    private static readonly string DataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HPRebar");

    private readonly WebViewHost _host;
    private readonly WebMessageTransport _transport;
    private readonly KataSettingsWebBridge _bridge;

    public KataSettingsWebWindow()
    {
        Title = "Thiết lập thép Kata";
        Width = 860;
        Height = Math.Min(760, SystemParameters.WorkArea.Height - 20);
        MinWidth = 560;
        MinHeight = 480;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        MaterialThemeBridge.Attach(this, RevitHostTheme.Instance, dark => new RibbonIcons(dark).KataSettings);

        var origins = PageOrigins.Resolve(VirtualHost, DevServerUrl(), StartPage);
        var assets = new EmbeddedAssetServer(typeof(KataSettingsWebWindow).Assembly, origins, ProductName, Log.Logger);
        var options = new WebViewHostOptions(Path.Combine(DataRoot, "WebView2"), DebugPortVariable, ProductName, Path.Combine(DataRoot, "logs"));

        // The handlers read and write the settings file on this thread: no Revit queue, short timeout.
        var dispatcher = new CommandDispatcher(TimeSpan.FromSeconds(30));
        _host = new WebViewHost(options, assets.Attach, Log.Logger);
        _transport = new WebMessageTransport(_host, dispatcher, origins, Log.Logger);
        // Closed after the pass that answers the close command, so the page gets its answer first.
        _bridge = new KataSettingsWebBridge(RevitHostTheme.Instance, Dispatcher, _transport.Publish,
            () => Dispatcher.BeginInvoke(DispatcherPriority.Background, Close));
        _bridge.Register(dispatcher);

        Content = _host;

        Closed += (_, _) =>
        {
            _bridge.Dispose();
            _transport.Dispose();
            _host.Dispose();
        };
    }

    /// <summary>A form was accepted but the settings file could not be written (the settings apply to this session only).</summary>
    public bool FileFailed => _bridge.FileFailed;

    /// <summary>settings.html on the Kata Export page's dev server, or null when there is none.</summary>
    private static string? DevServerUrl()
    {
        var kata = Environment.GetEnvironmentVariable(DevServerVariable);
        return Uri.TryCreate(kata, UriKind.Absolute, out var uri) ? new Uri(uri, StartPage).AbsoluteUri : null;
    }
}
