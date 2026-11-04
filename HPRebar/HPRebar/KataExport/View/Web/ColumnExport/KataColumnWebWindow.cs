using System.IO;
using System.Windows;
using System.Windows.Threading;
using HPRebar.Core.KataExport.ColumnStack;
using HPRebar.KataExport.Service;
using HPRebar.Resources.Icons;
using HPRebar.Resources.Themes;
using HPWebHost.Hosting;
using HPWebHost.Protocol;
using Serilog;

namespace HPRebar.KataExport.View.Web.ColumnExport;

/// <summary>
///     The Kata Column Rebar window (Revit 2026 only): modal over Revit, its content is the column page of
///     HPBimToolkit.Frontend in WebView2, on the Kata Export page's virtual host and profile.
/// </summary>
public sealed class KataColumnWebWindow : Window
{
    private const string ProductName = "Kata Column Rebar";
    private const string VirtualHost = "kata.hprebar.local";
    private const string StartPage = "column.html";

    /// <summary>The Kata Export page's dev server (…/kata.html): this window opens column.html beside it.</summary>
    private const string DevServerVariable = "HPREBAR_KATA_DEV_URL";
    private const string DebugPortVariable = "HPREBAR_KATA_DEBUG_PORT";

    private static readonly string DataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HPRebar");

    private readonly WebViewHost _host;
    private readonly WebMessageTransport _transport;
    private readonly KataColumnWebBridge _bridge;

    /// <param name="rebar">Reads sheet Cot back and creates the bars; it runs on Revit's thread because the window is modal.</param>
    internal KataColumnWebWindow(KataColumnStackInput input, KataColumnSheet sheet, KataColumnRebarSession rebar)
    {
        Title = ProductName;
        Width = 980;
        Height = Math.Min(720, SystemParameters.WorkArea.Height - 20);
        MinWidth = 640;
        MinHeight = 460;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        MaterialThemeBridge.Attach(this, RevitHostTheme.Instance, dark => new RibbonIcons(dark).KataColumnRebar);

        var origins = PageOrigins.Resolve(VirtualHost, DevServerUrl(), StartPage);
        var assets = new EmbeddedAssetServer(typeof(KataColumnWebWindow).Assembly, origins, ProductName, Log.Logger);
        var options = new WebViewHostOptions(Path.Combine(DataRoot, "WebView2"), DebugPortVariable, ProductName, Path.Combine(DataRoot, "logs"));

        var dispatcher = new CommandDispatcher(TimeSpan.FromSeconds(30));
        _host = new WebViewHost(options, assets.Attach, Log.Logger);
        _transport = new WebMessageTransport(_host, dispatcher, origins, Log.Logger);
        // Closed after the pass that answers the close command, so the page gets its answer first.
        _bridge = new KataColumnWebBridge(input, sheet, rebar, RevitHostTheme.Instance, Dispatcher, _transport.Publish,
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

    /// <summary>column.html on the Kata Export page's dev server, or null when there is none.</summary>
    private static string? DevServerUrl()
    {
        var kata = Environment.GetEnvironmentVariable(DevServerVariable);
        return Uri.TryCreate(kata, UriKind.Absolute, out var uri) ? new Uri(uri, StartPage).AbsoluteUri : null;
    }
}
