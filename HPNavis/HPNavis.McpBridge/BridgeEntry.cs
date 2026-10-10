using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Autodesk.Navisworks.Api;
using NavisApplication = Autodesk.Navisworks.Api.Application; // System.Windows.Application is also in scope
using HPNavis.McpBridge.Model;
using HPNavis.McpBridge.Service;
using HPNavis.McpBridge.View;
using HPNavis.McpBridge.ViewModel;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Scripting;
using Serilog;
using Serilog.Events;

namespace HPNavis.McpBridge;

/// <summary>
///     Wires logging, settings, the Roslyn compiler, the main-thread executor and the pipe host when the
///     plugin loads, runs the scripting self-check, and owns the status window. Runs on Navisworks' main
///     thread, before any model is open.
/// </summary>
public static class BridgeEntry
{
    public const string VendorFolder = "HPNavis";
    public const string ProductFolder = "McpBridge";
    private const string HostName = "Navisworks";

    /// <summary>
    ///     How long a request waits for Navisworks to finish a load, a clash run or a dialog before it fails as
    ///     busy. Below the server's shortest wait (timeout 5 s + 5 s extra) so the AI sees the actionable busy
    ///     code rather than a generic timeout.
    /// </summary>
    private static readonly TimeSpan BusyGrace = TimeSpan.FromSeconds(8);

    /// <summary>A progress operation that reports nothing for this long while the main window is enabled again is considered over.</summary>
    private static readonly TimeSpan ProgressStaleAfter = TimeSpan.FromSeconds(120);

    public static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), VendorFolder, ProductFolder, "logs");

    private static McpBridgeHost? _host;
    private static NavisMainThreadExecutor? _executor;
    private static bool _selfCheckOk;
    private static NavisBridgeStatusView? _window;
    private static Dispatcher? _mainDispatcher;

    public static bool SelfCheckOk => _selfCheckOk;

    public static void Start(string pluginFolder)
    {
        CreateLogger();

        var year = NavisVersion.YearFor(NavisVersion.RuntimeMajor, out var knownYear);
        Log.Information("HPNavis MCP bridge starting from {Directory}; Navisworks runtime {Runtime} → {Year}{Note}; CLR {Clr} ({Framework})",
            pluginFolder, NavisVersion.Runtime, year, knownYear ? "" : " (unknown series, using the build target)", Environment.Version, RuntimeInformation.FrameworkDescription);

        _mainDispatcher = Dispatcher.CurrentDispatcher;

        var store = new BridgeSettingsStore(VendorFolder, ProductFolder);
        var settings = store.Load();
        var app = new NavisApp(() => NavisClashModule.IsAvailable);
        var navisApi = NavisApiAssemblies();

        var compiler = CreateScriptCompiler(settings.ScriptCacheSize);
        _selfCheckOk = ScriptingSelfCheck.Run(compiler, app, pluginFolder);
        if (CoordinationEngine(navisApi).Any()) LogCoordinationEngine();

        var heavy = new NavisHeavyGate();
        var quiescence = new NavisQuiescence(ProgressStaleAfter);
        var runner = new NavisScriptRunner(settings, new NavisResultSerializer(settings.MaxOutputBytes), app);
        var inspector = new TypeInspector(navisApi, HostName);
        var audit = new AuditLogger(store.AuditDirectory);
        var hostVersion = year.ToString();

        _executor = new NavisMainThreadExecutor(settings, compiler, runner, heavy, quiescence, inspector, audit, hostVersion, BusyGrace);
        _host = new McpBridgeHost(_executor, settings, store, hostVersion, PipeNaming.For(PipeNaming.NavisHost, year), HostName, JsonRpcMethods.NavisPrefix);
        McpBridgeHost.Install(_host);

        if (!_selfCheckOk)
            Log.Error("MCP bridge: the listener will not start automatically because the scripting self-check failed; see the log above");
        else if (settings.AutoStartListener)
            _host.Start();

        Log.Information("MCP bridge ready on pipe {Pipe}; auto-start listener = {AutoStart}; self-check {SelfCheck}", _host.PipeName, settings.AutoStartListener, _selfCheckOk ? "OK" : "FAILED");

        if (Environment.GetEnvironmentVariable(ShowWindowOnStartVariable) == "1") ShowWindowWhenGuiIsReady();
    }

    /// <summary>Start threw before the host existed: log it (the logger is created first, so this usually works) and stay inert.</summary>
    public static void ReportStartupFailure(Exception exception)
    {
        try { Log.Error(exception, "HPNavis MCP bridge failed to start; the plugin stays loaded but inert. Log folder: {Directory}", LogDirectory); }
        catch { /* nothing else to fall back to */ }
    }

    /// <summary>
    ///     Process-scoped hook for the unattended harness: Roamer.exe started with this variable set opens the status
    ///     window by itself, so UI Automation can tick the opt-ins without navigating the Ribbon. Only the window —
    ///     "Allow AI code execution" still starts OFF and is never set from the environment.
    /// </summary>
    public const string ShowWindowOnStartVariable = "HPNAVIS_MCP_BRIDGE_SHOW_WINDOW";

    private static void ShowWindowWhenGuiIsReady()
    {
        EventHandler<EventArgs>? once = null;
        once = (_, _) =>
        {
            NavisApplication.GuiCreated -= once;
            // GuiCreated fires before the main window is fully up; one idle pass later it can own our window.
            _mainDispatcher?.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                try { ShowWindow(); }
                catch (Exception exception) { Log.Error(exception, "MCP bridge: could not open the status window on start"); }
            }));
        };

        if (NavisApplication.Gui is not null) once(null, EventArgs.Empty);
        else NavisApplication.GuiCreated += once;
        Log.Information("MCP bridge: {Variable}=1, the status window opens once the GUI is up", ShowWindowOnStartVariable);
    }

    /// <summary>
    ///     The script compiler exactly as the bridge runs it (imports, references, globals type). Public so the
    ///     seed compile-check compiles every stored tool through the same configuration — a seed that passes
    ///     there compiles inside Roamer too.
    /// </summary>
    public static ScriptCompiler CreateScriptCompiler(int cacheSize)
    {
        var navisApi = NavisApiAssemblies();
        return new ScriptCompiler(CompilerReferences(navisApi), ImportsFor(navisApi), typeof(NavisScriptGlobals), cacheSize);
    }

    /// <summary>The Navisworks assemblies Roamer already loaded; Clash is optional (Manage only).</summary>
    private static Assembly[] NavisApiAssemblies()
    {
        var list = new List<Assembly> { typeof(Document).Assembly };
        if (NavisClashModule.Assembly is { } clash) list.Add(clash);
        try { list.Add(TimelinerAssembly()); }
        catch (Exception exception) { Log.Information("Timeliner module not available: {Reason}", exception.GetType().Name); }
        return list.ToArray();
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static Assembly TimelinerAssembly() => typeof(Autodesk.Navisworks.Api.Timeliner.DocumentTimeliner).Assembly;

    /// <summary>The contract's imports, minus the namespaces whose assembly this install does not have (a Simulate has no Clash).</summary>
    private static string[] ImportsFor(Assembly[] navisApi)
    {
        var names = navisApi.Select(a => a.GetName().Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return HostScriptContracts.NavisImports
            .Where(ns => ns != "Autodesk.Navisworks.Api.Clash" || names.Contains("Autodesk.Navisworks.Clash"))
            .Where(ns => ns != "Autodesk.Navisworks.Api.Timeliner" || names.Contains("Autodesk.Navisworks.Timeliner"))
            .ToArray();
    }

    /// <summary>
    ///     The Navisworks API plus the .NET Framework BCL the script runs on (mscorlib, System, System.Core —
    ///     not the net10 facades the Revit/AutoCAD bridges reference) and the Core assembly that defines `args`/`units`.
    /// </summary>
    private static Assembly[] CompilerReferences(Assembly[] navisApi) => navisApi.Concat(
    [
        typeof(object).Assembly, typeof(Enumerable).Assembly, typeof(List<>).Assembly, typeof(Uri).Assembly,
        typeof(ScriptArgs).Assembly, typeof(System.Text.Json.JsonElement).Assembly,
    ]).Concat(CoordinationEngine(navisApi)).Distinct().ToArray();

    /// <summary>
    ///     The BIM-coordination engine the Coordination seeds call (fully qualified, so no import is added). It is built
    ///     on the Clash API, so a Navisworks without Clash Detective (Simulate) does not get it.
    /// </summary>
    /// <summary>Loads the embedded matrix once at start so a broken engine shows in the log, not in the first Coordination call.</summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void LogCoordinationEngine()
    {
        try
        {
            var matrix = HPNavis.BIMCoordinator.Rules.ClashMatrix.Default;
            Log.Information("BIM coordinator engine OK: {Rules} matrix rules from {Workbook}, {Sets} base sets",
                matrix.Rules.Count, matrix.Document.Source.Workbook, HPNavis.BIMCoordinator.SearchSets.BaseSetCatalog.Default.Sets.Count);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "BIM coordinator engine failed to load; the Coordination tools will fail");
        }
    }

    private static IEnumerable<Assembly> CoordinationEngine(Assembly[] navisApi) =>
        navisApi.Any(a => a.GetName().Name == "Autodesk.Navisworks.Clash")
            ? [typeof(HPNavis.BIMCoordinator.CoordinatorTools).Assembly]
            : [];

    public static void ShowWindow()
    {
        if (_window is not null)
        {
            _window.Activate();
            return;
        }

        var host = _host ?? throw new InvalidOperationException("The bridge did not start; see " + LogDirectory);
        var executor = _executor!;
        var dispatcher = _mainDispatcher ?? Dispatcher.CurrentDispatcher;

        var viewModel = new NavisBridgeStatusViewModel(host, executor, action => dispatcher.InvokeAsync(action), Clipboard.SetText, _selfCheckOk, LogDirectory);
        var view = new NavisBridgeStatusView(viewModel);

        var main = executor.Quiescence.MainWindow;
        if (main != IntPtr.Zero) new WindowInteropHelper(view) { Owner = main };

        view.Closed += (_, _) =>
        {
            viewModel.Detach();
            _window = null;
        };

        _window = view;
        view.Show();
        Log.Information("MCP bridge status window opened");
    }

    public static string Status()
    {
        var host = _host;
        if (host is null) return "[HPNavis MCP] bridge not started; see " + LogDirectory;

        var last = host.LastRun is { } run
            ? $"last run '{run.Label}' {(run.IsError ? "failed" : "ok")} at {run.Timestamp:HH:mm:ss}"
            : "no script has run yet";

        return $"[HPNavis MCP] {host.Status} on pipe {host.PipeName}; execution {(host.ExecutionEnabled ? "ENABLED" : "disabled")}; " +
               $"heavy {(_executor?.HeavyOperationsEnabled == true ? "ENABLED" : "disabled")}; self-check {(_selfCheckOk ? "OK" : "FAILED")}; " +
               $"compiled scripts {host.CompiledScriptCount}; {last}. Logs: {LogDirectory}";
    }

    public static void Dispose()
    {
        try { _window?.Close(); }
        catch (Exception exception) { Log.Debug(exception, "MCP bridge window close failed during unload"); }
        _host?.Dispose();
        _executor?.Dispose();
        Log.Information("HPNavis MCP bridge stopped");
        Log.CloseAndFlush();
    }

    private static void CreateLogger()
    {
        const string outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(Path.Combine(LogDirectory, "mcpbridge-.log"),
                restrictedToMinimumLevel: LogEventLevel.Debug,
                outputTemplate: outputTemplate,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                shared: true) // a second Navisworks must be able to log why its listener refused to start
            .MinimumLevel.Debug()
            .CreateLogger();
    }
}
