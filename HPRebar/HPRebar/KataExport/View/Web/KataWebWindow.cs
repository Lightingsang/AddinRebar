using System.IO;
using System.Windows;
using System.Windows.Data;
using HPRebar.KataExport.View.Controls;
using HPRebar.KataExport.ViewModel;
using HPRebar.Resources.Icons;
using HPRebar.Resources.Themes;
using HPWebHost.Hosting;
using HPWebHost.Protocol;
using Serilog;

// WPF types, not the Revit ones the SDK imports globally.
using Binding = System.Windows.Data.Binding;
using Grid = System.Windows.Controls.Grid;

namespace HPRebar.KataExport.View.Web;

/// <summary>
///     The Kata Export window of Revit 2026 (ADR-0010): modeless like the WPF one and bound to the same view model, its
///     content is the Kata page of HPBimToolkit.Frontend in WebView2. The elevation canvas of the WPF window lives here
///     too, bound exactly as in KataExportView.xaml but hidden behind the browser: it paints, the bridge sends its
///     drawing to the page as SVG and hands the page's clicks back to it.
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
        var canvas = HiddenCanvas();

        // The handlers only touch the view model and the canvas, both on this thread: no Revit queue, short timeout.
        var dispatcher = new CommandDispatcher(TimeSpan.FromSeconds(30));
        _host = new WebViewHost(options, assets.Attach, Log.Logger);
        _transport = new WebMessageTransport(_host, dispatcher, origins, Log.Logger);
        _bridge = new KataWebBridge(viewModel, canvas, RevitHostTheme.Instance, _transport.Publish);
        _bridge.Register(dispatcher);

        var root = new Grid();
        root.Children.Add(canvas);
        root.Children.Add(_host);
        Content = root;

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

    /// <summary>
    ///     The WPF elevation canvas with KataExportView.xaml's bindings. Under the browser's window (WebView2 is a native
    ///     child window, always on top of WPF content) and transparent, so the user never sees or clicks it; the page sets
    ///     its size to the drawing area's.
    /// </summary>
    private static KataElevationCanvas HiddenCanvas()
    {
        var canvas = new KataElevationCanvas
        {
            Width = 800,
            Height = 400,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Opacity = 0,
            IsHitTestVisible = false,
            Focusable = false,
        };

        canvas.SetBinding(KataElevationCanvas.ElevationProperty, new Binding(nameof(KataExportViewModel.Elevation)));
        canvas.SetBinding(KataElevationCanvas.SelectedColumnIndexProperty, new Binding(nameof(KataExportViewModel.SelectedColumnIndex)) { Mode = BindingMode.TwoWay });
        canvas.SetBinding(KataElevationCanvas.FocusRequestProperty, new Binding(nameof(KataExportViewModel.FocusRequest)));
        canvas.SetBinding(KataElevationCanvas.RebarPlanProperty, new Binding(nameof(KataExportViewModel.RebarPlan)));
        canvas.SetBinding(KataElevationCanvas.RebarStationMapProperty, new Binding(nameof(KataExportViewModel.RebarStationMap)));
        canvas.SetBinding(KataElevationCanvas.ShowRebarProperty, new Binding(nameof(KataExportViewModel.ShowRebar)));
        canvas.SetBinding(KataElevationCanvas.ShowSectionProperty, new Binding(nameof(KataExportViewModel.ShowSection)));
        canvas.SetBinding(KataElevationCanvas.ShowBarTagsProperty, new Binding(nameof(KataExportViewModel.ShowBarTags)));
        canvas.SetBinding(KataElevationCanvas.RemoveBarsCommandProperty, new Binding(nameof(KataExportViewModel.RemoveBarsCommand)));
        canvas.SetBinding(KataElevationCanvas.UndoRemoveBarsCommandProperty, new Binding(nameof(KataExportViewModel.UndoRemoveBarsCommand)));
        canvas.SetResourceReference(KataElevationCanvas.SurfaceBrushProperty, "Brush.Canvas.Fill");
        return canvas;
    }
}
