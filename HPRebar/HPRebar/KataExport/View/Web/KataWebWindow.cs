using System.IO;
using System.Windows;
using HPRebar.KataExport.ViewModel;
using HPRebar.Resources.Icons;
using HPRebar.Resources.Themes;
using HPWebHost.Hosting;
using HPWebHost.Protocol;
using Serilog;

namespace HPRebar.KataExport.View.Web;

/// <summary>
///     The Kata Export window (Revit 2026 only, ADR-0011): modeless, bound to <see cref="KataExportViewModel" />, its
///     content is the Kata page of HPBimToolkit.Frontend in WebView2. The page draws the elevation, sections and 3D view
///     itself from the records the bridge sends; nothing is painted on this side.
/// </summary>
public sealed class KataWebWindow : Window
{
    private const string ProductName = "Kata Export";
    private const string VirtualHost = "kata.hprebar.local";
    private const string StartPage = "kata.html";
    private const string DevServerVariable = "HPREBAR_KATA_DEV_URL";
    private const string DebugPortVariable = "HPREBAR_KATA_DEBUG_PORT";

    private static readonly string DataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HPRebar");

    private readonly WebViewHost _host;
    private readonly WebMessageTransport _transport;
    private readonly KataWebBridge _bridge;

    public KataWebWindow(KataExportViewModel viewModel)
    {
        Title = "HPRebar Kata Pro — Xuất dầm sang Kata Excel";
        Width = 1280;
        Height = Math.Min(860, SystemParameters.WorkArea.Height - 20);
        MinWidth = 720;
        MinHeight = 480;
        DataContext = viewModel;
        MaterialThemeBridge.Attach(this, RevitHostTheme.Instance, dark => new RibbonIcons(dark).KataExport);

        var origins = PageOrigins.Resolve(VirtualHost, Environment.GetEnvironmentVariable(DevServerVariable), StartPage);
        var assets = new EmbeddedAssetServer(typeof(KataWebWindow).Assembly, origins, ProductName, Log.Logger);
        var options = new WebViewHostOptions(Path.Combine(DataRoot, "WebView2"), DebugPortVariable, ProductName, Path.Combine(DataRoot, "logs"));

        // The handlers only touch the view model, on this thread: no Revit queue, short timeout.
        var dispatcher = new CommandDispatcher(TimeSpan.FromSeconds(30));
        _host = new WebViewHost(options, assets.Attach, Log.Logger);
        _transport = new WebMessageTransport(_host, dispatcher, origins, Log.Logger);
        _bridge = new KataWebBridge(viewModel, Dispatcher, RevitHostTheme.Instance, _transport.Publish);
        _bridge.Register(dispatcher);

        Content = _host;

        viewModel.CloseRequested += Close;
        // Out of the way while beams are picked in Revit, back in front afterwards.
        viewModel.PickStarted += Hide;
        viewModel.PickEnded += () =>
        {
            if (IsVisible) return;
            Show();
            Activate();
        };

        Closed += (_, _) =>
        {
            _bridge.Dispose();
            _transport.Dispose();
            _host.Dispose();
        };
    }
}
