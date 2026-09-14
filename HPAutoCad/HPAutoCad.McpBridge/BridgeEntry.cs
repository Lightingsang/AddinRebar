using System.IO;
using System.Reflection;
using System.Windows;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using HPAutoCad.McpBridge.Model;
using HPAutoCad.McpBridge.Service;
using HPRebar.Mcp.Contracts;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Scripting;
using Serilog;
using Serilog.Events;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPAutoCad.McpBridge;

/// <summary>
///     Entry point the loader calls by reflection once this assembly sits in its own load context. Wires
///     logging, settings and the Roslyn compiler, runs the scripting self-check, and hands back the
///     entry points the loader's commands forward to — as plain delegates, the only shape both load
///     contexts agree on. Phase 1: no pipe listener yet; that arrives with the executor in phase 2.
/// </summary>
public static class BridgeEntry
{
    public const string VendorFolder = "HPAutoCad";
    public const string ProductFolder = "McpBridge";

    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), VendorFolder, ProductFolder, "logs");

    private static ScriptCompiler? _compiler;
    private static BridgeSettings? _settings;
    private static bool _selfCheckOk;
    private static Window? _window;

    /// <param name="bridgeDirectory">Contents\Bridge — where this assembly and its dependencies were loaded from.</param>
    /// <param name="loaderLog">The loader's file log, for anything worth recording beside the Serilog file.</param>
    public static IReadOnlyDictionary<string, Delegate> Start(string bridgeDirectory, Action<string> loaderLog)
    {
        CreateLogger();

        var version = AcadApp.Version;
        var year = AutocadVersionMap.YearFor(version, out var knownYear);
        Log.Information("HPAutoCad MCP bridge starting from {Directory}; AutoCAD {Version} → {Year}{Note}; pipe would be {Pipe}",
            bridgeDirectory, version, year, knownYear ? "" : " (unknown series, using the build target)", PipeNaming.For(PipeNaming.AutocadHost, year));

        var store = new BridgeSettingsStore(VendorFolder, ProductFolder);
        _settings = store.Load();

        _compiler = CreateCompiler(_settings.ScriptCacheSize);
        _selfCheckOk = ScriptingSelfCheck.Run(_compiler);
        loaderLog($"bridge self-check {(_selfCheckOk ? "OK" : "FAILED")} — see {LogDirectory}");

        var spike = new SpikeRunner(_compiler, LogDirectory);

        return new Dictionary<string, Delegate>(StringComparer.Ordinal)
        {
            ["show"] = new Func<string>(ShowWindow),
            ["start"] = new Func<string>(() => "[HPAutoCad MCP] the pipe listener arrives with phase 2; nothing to start yet."),
            ["stop"] = new Func<string>(() => "[HPAutoCad MCP] the pipe listener arrives with phase 2; nothing to stop."),
            ["status"] = new Func<string>(Status),
            ["spike"] = new Func<bool, string>(quit => spike.Start(quit)),
            ["dispose"] = new Action(Dispose),
        };
    }

    /// <summary>
    ///     The compiler sees the three AutoCAD API assemblies exactly as acad.exe loaded them (the load
    ///     context never duplicates them) plus the Core assembly that defines `args`/`units`. Imports come
    ///     from <see cref="HostScriptContracts.AutocadImports"/> so the server's tool description and the
    ///     seed compile checks describe the same environment.
    /// </summary>
    private static ScriptCompiler CreateCompiler(int cacheSize)
    {
        Assembly[] references =
        [
            typeof(Document).Assembly,      // AcMgd
            typeof(AcadApp).Assembly,       // AcCoreMgd
            typeof(Database).Assembly,      // AcDbMgd
            typeof(object).Assembly, typeof(Enumerable).Assembly, typeof(List<>).Assembly,
            Assembly.Load("netstandard"), Assembly.Load("System.Runtime"), Assembly.Load("System.Collections"),
            typeof(ScriptArgs).Assembly, typeof(System.Text.Json.JsonElement).Assembly,
        ];

        return new ScriptCompiler(references, HostScriptContracts.AutocadImports, typeof(AutocadScriptGlobals), cacheSize);
    }

    private static string ShowWindow()
    {
        if (_window is { IsVisible: true })
        {
            _window.Activate();
            return string.Empty;
        }

        // Placeholder until phase 2 brings the real status window and its shared view model.
        _window = new Window
        {
            Title = "HPAutoCad MCP Bridge",
            Width = 420, Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Content = new System.Windows.Controls.TextBlock
            {
                Margin = new Thickness(16),
                TextWrapping = TextWrapping.Wrap,
                Text = Status(),
            },
        };
        _window.Closed += (_, _) => _window = null;
        AcadApp.ShowModelessWindow(_window);
        return string.Empty;
    }

    private static string Status() =>
        $"[HPAutoCad MCP] phase 1 — self-check {(_selfCheckOk ? "OK" : "FAILED")}, compiled scripts {_compiler?.CompiledCount ?? 0}, " +
        $"listener: not built yet (phase 2). Logs: {LogDirectory}";

    private static void Dispose()
    {
        _window?.Close();
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
                retainedFileCountLimit: 7)
            .MinimumLevel.Debug()
            .CreateLogger();
    }
}
