using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using HPBimToolkit.Bridge;
using HPBimToolkit.Core.Contracts;
using HPBimToolkit.Core.Hosting;
using HPBimToolkit.Core.Protocol;
using HPBimToolkit.Core.Statistics;
using HPBimToolkit.ExternalEvents;
using HPBimToolkit.Infrastructure;
using HPBimToolkit.Modules;
using HPBimToolkit.Modules.Dashboard;
using Nice3point.Revit.Toolkit.External;
using Serilog.Core;

namespace HPBimToolkit.Host;

/// <summary>
///     Add-in entry point and composition root: builds the logger, request queue, WebView host, dispatcher, modules and
///     event publisher, registers the dockable pane (only legal during start-up) and the ribbon button that shows it.
/// </summary>
[UsedImplicitly]
public sealed class ToolkitApplication : ExternalApplication
{
    private const string DevServerVariable = "HPBIM_TOOLKIT_DEV_URL";

    /// <summary>Default for quick commands, waiting for Revit to become idle included; statistics have their own budget.</summary>
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    private Logger? _logger;
    private RevitRequestQueue? _queue;
    private WebViewHost? _host;
    private WebMessageTransport? _transport;
    private RevitEventPublisher? _publisher;
    private PushButton? _dashboardButton;

    public override void OnStartup()
    {
        _logger = ToolkitLogger.Create();
        try
        {
            DependencyPreloader.Preload(_logger, typeof(ToolkitApplication).Assembly, typeof(CommandDispatcher).Assembly);
            Compose(_logger);
            CreateRibbon();
            _logger.Information("HP BIM Toolkit started in Revit {Version}; pane {PaneId} registered",
                Application.ControlledApplication.SubVersionNumber, DashboardPaneProvider.PaneId.Guid);
        }
        catch (Exception exception)
        {
            // An exception escaping here makes Revit drop the add-in with only a journal entry: log it, and undo what
            // was wired so far — Revit never calls OnShutdown for an add-in whose start-up failed.
            _logger.Fatal(exception, "HP BIM Toolkit could not start");
            TearDown();
            throw;
        }
    }

    public override void OnShutdown()
    {
        _logger?.Information("HP BIM Toolkit stopping");
        TearDown();
    }

    private void TearDown()
    {
        Application.ThemeChanged -= OnThemeChanged;
        _publisher?.Dispose();
        _transport?.Dispose();
        _host?.Dispose();
        _queue?.Dispose();
        _logger?.Dispose();
    }

    private void Compose(Logger logger)
    {
        var origins = PageOrigins.Resolve(Environment.GetEnvironmentVariable(DevServerVariable));
        var assets = new EmbeddedAssetServer(typeof(ToolkitApplication).Assembly, origins, logger);
        var changes = new ModelChangeTally();
        var theme = new HostThemeState();
        theme.Refresh();
        var dispatcher = new CommandDispatcher(RequestTimeout, TranslateRevitException);

        _queue = new RevitRequestQueue(logger);
        _host = new WebViewHost(ToolkitPaths.WebViewProfile, assets.Attach, logger);
        _transport = new WebMessageTransport(_host, dispatcher, origins, logger);

        IToolkitModule[] modules =
        [
            new AppModule(Application.ControlledApplication.SubVersionNumber, theme),
            new DashboardModule(_queue, changes),
        ];
        foreach (var module in modules)
        {
            module.Register(dispatcher);
        }

        _publisher = new RevitEventPublisher(Application, _queue, _transport, changes, theme, logger);
        _publisher.Subscribe();

        Application.RegisterDockablePane(DashboardPaneProvider.PaneId, DashboardPaneProvider.Title, new DashboardPaneProvider(_host));
    }

    private void CreateRibbon()
    {
        _dashboardButton = Application.CreatePanel("BIM Toolkit", "HPRebar").AddPushButton<ShowDashboardCommand>("Dashboard");
        _dashboardButton.ToolTip = "Show the HP BIM Toolkit dashboard: document, view, element statistics and selection.";
        ApplyIcon();
        Application.ThemeChanged += OnThemeChanged;
    }

    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e) => ApplyIcon();

    private void ApplyIcon()
    {
        if (_dashboardButton is null)
        {
            return;
        }

        var image = DashboardRibbonIcon.Create(UIThemeManager.CurrentTheme == UITheme.Dark);
        _dashboardButton.Image = image;
        _dashboardButton.LargeImage = image;
    }

    /// <summary>Every Revit API exception derives from Autodesk's ApplicationException; Core cannot name the type.</summary>
    private static BridgeError? TranslateRevitException(Exception exception) =>
        exception is Autodesk.Revit.Exceptions.ApplicationException
            ? new BridgeError(BridgeErrorCodes.RevitApiError, exception.Message)
            : null;
}
