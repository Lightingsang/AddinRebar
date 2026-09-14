using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using HPAutoCad.McpBridge.Model;
using HPAutoCad.McpBridge.Service;
using HPAutoCad.McpBridge.View;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Scripting;
using HPRebar.McpBridge.Core.ViewModel;
using Serilog;
using Serilog.Events;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPAutoCad.McpBridge;

/// <summary>
///     Entry point the loader calls by reflection once this assembly sits in its own load context. Wires
///     logging, settings, the Roslyn compiler, the main-thread executor and the pipe host, runs the
///     scripting self-check, and hands back the entry points the loader's commands forward to — as plain
///     delegates, the only shape both load contexts agree on. Runs on AutoCAD's main thread, before any
///     drawing is open.
/// </summary>
public static partial class BridgeEntry
{
    public const string VendorFolder = "HPAutoCad";
    public const string ProductFolder = "McpBridge";
    private const string HostName = "AutoCAD";

    /// <summary>
    ///     How long a request waits for AutoCAD to finish a command or dialog before it fails as busy. Below
    ///     the server's shortest wait (timeout 5 s + 5 s extra) so the AI sees the actionable busy code
    ///     rather than a generic timeout.
    /// </summary>
    private static readonly TimeSpan BusyGrace = TimeSpan.FromSeconds(8);

    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), VendorFolder, ProductFolder, "logs");

    private static McpBridgeHost? _host;
    private static MainThreadExecutor? _executor;
    private static bool _selfCheckOk;
    private static AutocadBridgeStatusView? _window;

    /// <param name="bridgeDirectory">Contents\Bridge — where this assembly and its dependencies were loaded from.</param>
    /// <param name="loaderLog">The loader's file log, for anything worth recording beside the Serilog file.</param>
    public static IReadOnlyDictionary<string, Delegate> Start(string bridgeDirectory, Action<string> loaderLog)
    {
        CreateLogger();

        var version = AcadApp.Version;
        var year = AutocadVersionMap.YearFor(version, out var knownYear);
        Log.Information("HPAutoCad MCP bridge starting from {Directory}; AutoCAD {Version} → {Year}{Note}; runtime {Runtime}",
            bridgeDirectory, version, year, knownYear ? "" : " (unknown series, using the build target)", RuntimeInformation.FrameworkDescription);

        // AutoCAD 2026 Update 1.2 reports the same 25.1 series but runs on .NET 10; this net8 build is untested there.
        if (Environment.Version.Major != 8)
            Log.Warning("Host runtime is .NET {Major}, this bridge was built and verified for .NET 8", Environment.Version.Major);

        var store = new BridgeSettingsStore(VendorFolder, ProductFolder);
        var settings = store.Load();
        var autocadApi = AutocadApiAssemblies();

        var compiler = new ScriptCompiler(CompilerReferences(autocadApi), HostScriptContracts.AutocadImports, typeof(AutocadScriptGlobals), settings.ScriptCacheSize);
        _selfCheckOk = ScriptingSelfCheck.Run(compiler);
        loaderLog($"bridge self-check {(_selfCheckOk ? "OK" : "FAILED")} — see {LogDirectory}");

        var runner = new AutocadScriptRunner(settings, new AutocadResultSerializer(settings.MaxOutputBytes));
        var inspector = new TypeInspector(autocadApi, HostName);
        var audit = new AuditLogger(store.AuditDirectory);
        var hostVersion = year.ToString();

        _executor = new MainThreadExecutor(settings, compiler, runner, inspector, audit, hostVersion, BusyGrace);
        _host = new McpBridgeHost(_executor, settings, store, hostVersion, PipeNaming.For(PipeNaming.AutocadHost, year), HostName, JsonRpcMethods.AutocadPrefix);
        McpBridgeHost.Install(_host);

        if (settings.AutoStartListener) _host.Start();
        Log.Information("MCP bridge ready on pipe {Pipe}; auto-start listener = {AutoStart}", _host.PipeName, settings.AutoStartListener);

        var host = _host;
        var entries = new Dictionary<string, Delegate>(StringComparer.Ordinal)
        {
            ["show"] = new Func<string>(ShowWindow),
            ["start"] = new Func<string>(() => { host.Start(); return $"[HPAutoCad MCP] listener starting on {host.PipeName}"; }),
            ["stop"] = new Func<string>(() => { host.Stop(); return "[HPAutoCad MCP] listener stopping"; }),
            ["status"] = new Func<string>(Status),
            ["dispose"] = new Action(Dispose),
        };
        AddRibbonEntryPoints(entries, host, store);
        return entries;
    }

    /// <summary>The three assemblies acad.exe loaded; the load context never duplicates them, so these are the live ones.</summary>
    private static Assembly[] AutocadApiAssemblies() =>
    [
        typeof(Autodesk.AutoCAD.ApplicationServices.Application).Assembly, // AcMgd: Application.DocumentManager, DocumentExtension
        typeof(Document).Assembly,                                        // AcCoreMgd: Document, Editor, Core.Application
        typeof(Database).Assembly,                                        // AcDbMgd
    ];

    /// <summary>
    ///     The AutoCAD API plus the Core assembly that defines `args`/`units`. Imports come from
    ///     <see cref="HostScriptContracts.AutocadImports"/> so the server's tool description and the seed
    ///     compile checks describe the same environment; the seed tests must reference the same three
    ///     AutoCAD assemblies, or a script passes there and fails here.
    /// </summary>
    private static Assembly[] CompilerReferences(Assembly[] autocadApi) => autocadApi.Concat(
    [
        typeof(object).Assembly, typeof(Enumerable).Assembly, typeof(List<>).Assembly,
        Assembly.Load("netstandard"), Assembly.Load("System.Runtime"), Assembly.Load("System.Collections"),
        typeof(ScriptArgs).Assembly, typeof(System.Text.Json.JsonElement).Assembly,
    ]).ToArray();

    private static string ShowWindow()
    {
        if (_window is not null)
        {
            _window.Activate();
            return string.Empty;
        }

        var host = _host ?? throw new InvalidOperationException("The bridge did not start; see " + LogDirectory);

        // The command runs on AutoCAD's main thread, which owns the WPF dispatcher the window will live on.
        var dispatcher = Dispatcher.CurrentDispatcher;
        var viewModel = new McpBridgeStatusViewModel(host, action => dispatcher.InvokeAsync(action), Clipboard.SetText);
        var view = new AutocadBridgeStatusView(viewModel);

        view.Closed += (_, _) =>
        {
            viewModel.Detach();
            _window = null;
        };

        _window = view;
        AcadApp.ShowModelessWindow(view);
        Log.Information("MCP bridge status window opened (visible={Visible}, dispatcher thread {Thread})", view.IsVisible, view.Dispatcher.Thread.ManagedThreadId);
        return string.Empty;
    }

    private static string Status()
    {
        var host = _host;
        if (host is null) return "[HPAutoCad MCP] bridge not started; see " + LogDirectory;

        var last = host.LastRun is { } run
            ? $"last run '{run.Label}' {(run.IsError ? "failed" : "ok")} at {run.Timestamp:HH:mm:ss}"
            : "no script has run yet";

        return $"[HPAutoCad MCP] {host.Status} on pipe {host.PipeName}; execution {(host.ExecutionEnabled ? "ENABLED" : "disabled")}; " +
               $"self-check {(_selfCheckOk ? "OK" : "FAILED")}; compiled scripts {host.CompiledScriptCount}; {last}. Logs: {LogDirectory}";
    }

    private static void Dispose()
    {
        _window?.Close();
        _host?.Dispose();
        _executor?.Dispose();
        Log.Information("HPAutoCad MCP bridge stopped");
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
                shared: true) // a second AutoCAD must be able to log why its listener refused to start
            .MinimumLevel.Debug()
            .CreateLogger();
    }
}
