using System.IO;
using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Scripting;
using HPRebar.McpBridge.Model;
using HPRebar.McpBridge.Service;
using Nice3point.Revit.Toolkit;
using Nice3point.Revit.Toolkit.External;
using Serilog;
using Serilog.Events;

namespace HPRebar.McpBridge;

/// <summary>
///     Application entry point for the MCP bridge add-in. Runs beside the main HPRebar add-in in its own
///     assembly load context so Roslyn and its dependencies never leak into the rebar features.
/// </summary>
[UsedImplicitly]
public class Application : ExternalApplication
{
    private static readonly string[] ScriptImports =
    [
        "System", "System.Linq", "System.Collections.Generic",
        "Autodesk.Revit.DB", "Autodesk.Revit.UI", "Autodesk.Revit.DB.Structure",
        // so a script can name the type of its `args` global in helper functions
        "HPRebar.McpBridge.Core.Scripting",
    ];

    private McpBridgeExternalEventHandler? _handler;
    private EventHandler<ViewActivatedEventArgs>? _onViewActivated;
    private EventHandler<DocumentClosingEventArgs>? _onDocumentClosing;
    private EventHandler<ApplicationInitializedEventArgs>? _onInitialized;

    public override void OnStartup()
    {
        CreateLogger();

        try
        {
            CreateRibbon();
            CreateBridge();
        }
        catch (Exception exception)
        {
            // An exception escaping here makes Revit drop the add-in with nothing but a journal entry
            // to go on. Log it to the file sink first so there is something to read, then let Revit
            // report the failure as it normally would.
            Log.Fatal(exception, "HPRebar MCP Bridge could not start");
            Log.CloseAndFlush();

            throw;
        }
    }

    public override void OnShutdown()
    {
        if (_onViewActivated is not null) Application.ViewActivated -= _onViewActivated;
        if (_onDocumentClosing is not null) Application.ControlledApplication.DocumentClosing -= _onDocumentClosing;
        if (_onInitialized is not null) Application.ControlledApplication.ApplicationInitialized -= _onInitialized;

        McpBridgeHost.Current?.Dispose();
        _handler?.Dispose();
        Log.CloseAndFlush();
    }

    private void CreateRibbon()
    {
        var panel = Application.CreatePanel("MCP", "HPRebar");

        panel.AddPushButton<McpBridgeCommand>("MCP Bridge")
            .SetImage("/HPRebar.McpBridge;component/Resources/Icons/McpBridge16.png")
            .SetLargeImage("/HPRebar.McpBridge;component/Resources/Icons/McpBridge32.png");
    }

    /// <summary>
    ///     Wires the pipe side to Revit. ExternalEvent.Create is only legal here, in a Revit API context,
    ///     which is why the handler is built at startup rather than when the listener starts.
    /// </summary>
    private void CreateBridge()
    {
        var settings = BridgeSettingsStore.Revit.Load();
        var revitApi = new[] { typeof(Document).Assembly, typeof(UIDocument).Assembly };

        var references = revitApi.Concat(
        [
            typeof(object).Assembly, typeof(Enumerable).Assembly, typeof(List<>).Assembly,
            Assembly.Load("netstandard"), Assembly.Load("System.Runtime"), Assembly.Load("System.Collections"),
            // `args` global: ScriptArgs lives in Core, its Raw view is a System.Text.Json element
            typeof(ScriptArgs).Assembly, typeof(System.Text.Json.JsonElement).Assembly,
        ]).ToArray();

        var compiler = new ScriptCompiler(references, ScriptImports, typeof(ScriptGlobals), settings.ScriptCacheSize);
        var runner = new ScriptRunner(settings, new ResultSerializer(settings.MaxOutputBytes));
        var inspector = new TypeInspector(revitApi);
        var audit = new AuditLogger(BridgeSettingsStore.Revit.AuditDirectory);

        _handler = new McpBridgeExternalEventHandler(settings, compiler, runner, inspector, audit);
        var host = new McpBridgeHost(_handler, settings, Application.ControlledApplication.VersionNumber);
        McpBridgeHost.Install(host);

        var handler = _handler;
        _onViewActivated = (_, e) => handler.SetActiveDocumentTitle(e.Document?.Title);
        _onDocumentClosing = (_, e) =>
        {
            if (e.Document?.Title == handler.ActiveDocumentTitle) handler.SetActiveDocumentTitle(null);
        };

        // Revit is fully up here; the self-check needs a UIApplication and the listener should not
        // accept requests before the ribbon and events exist.
        _onInitialized = (_, _) =>
        {
            ScriptingSelfCheck.Run(compiler, RevitContext.UiApplication);
            if (settings.AutoStartListener) host.Start();
        };

        Application.ViewActivated += _onViewActivated;
        Application.ControlledApplication.DocumentClosing += _onDocumentClosing;
        Application.ControlledApplication.ApplicationInitialized += _onInitialized;

        Log.Information("MCP bridge ready on pipe {Pipe}; auto-start listener = {AutoStart}", host.PipeName, settings.AutoStartListener);
    }

    private static void CreateLogger()
    {
        const string outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

        // Separate folder from the main add-in: two Serilog file sinks on one path would fight over the lock.
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HPRebar", "McpBridge", "logs", "mcpbridge-.log");

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Debug(LogEventLevel.Debug, outputTemplate)
            .WriteTo.File(logPath,
                restrictedToMinimumLevel: LogEventLevel.Debug,
                outputTemplate: outputTemplate,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .MinimumLevel.Debug()
            .CreateLogger();

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var exception = (Exception)args.ExceptionObject;
            Log.Fatal(exception, "Domain unhandled exception");
        };
    }
}
