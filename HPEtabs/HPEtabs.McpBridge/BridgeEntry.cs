using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HPEtabs.McpBridge.Model;
using HPEtabs.McpBridge.Service;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Scripting;
using Serilog;
using Serilog.Events;

namespace HPEtabs.McpBridge;

/// <summary>
///     Wires the bridge app: logging, the ETABSv1 resolver (first, before anything that names a wrapper type is
///     JIT-compiled), settings, the Roslyn compiler, the STA executor and the pipe host, then the scripting
///     self-check. The window is created by <c>App</c> from what this class hands back. No public static
///     executor: scripts reference this assembly for their globals type and must not be able to reach the
///     opt-in flags through it.
/// </summary>
public static class BridgeEntry
{
    public const string VendorFolder = "HPEtabs";
    public const string ProductFolder = "McpBridge";
    public const string HostName = EtabsExecutor.HostName;
    public const int HostVersionNumber = 22;
    public const string HostVersion = "22";

    /// <summary>The opt-in refusal text: the generic one says "inside ETABS", which is exactly where this bridge is not.</summary>
    public const string ExecutionDisabledMessage =
        "Code execution is disabled. Ask the user to tick 'Allow AI code execution' in the HPEtabs MCP Bridge window (a separate app, not inside ETABS).";

    /// <summary>Below the server's shortest wait (timeout 5 s + 5 s extra) so the AI sees the busy code rather than a generic timeout.</summary>
    private static readonly TimeSpan BusyGrace = TimeSpan.FromSeconds(8);

    public static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), VendorFolder, ProductFolder, "logs");

    private static McpBridgeHost? _host;
    private static EtabsExecutor? _executor;

    public static bool SelfCheckOk { get; private set; }

    public static bool ApiAvailable { get; private set; }

    /// <summary>Called once from App.OnStartup on the UI thread. Returns what the window needs.</summary>
    public static (McpBridgeHost host, EtabsExecutor executor) Start()
    {
        CreateLogger();
        Log.Information("HPEtabs MCP bridge {Version} starting; runtime {Runtime}; base {Base}",
            typeof(BridgeEntry).Assembly.GetName().Version, RuntimeInformation.FrameworkDescription, AppContext.BaseDirectory);

        // Everything that names an ETABSv1 type lives behind this call; the resolver must be in place before it is JIT-compiled.
        ApiAvailable = EtabsAssemblyResolver.Install(HostVersionNumber);
        return Wire();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (McpBridgeHost host, EtabsExecutor executor) Wire()
    {
        var store = new BridgeSettingsStore(VendorFolder, ProductFolder);
        var settings = store.Load();
        var wrapper = ApiAvailable ? Assembly.Load("ETABSv1") : null;

        var compiler = new ScriptCompiler(CompilerReferences(wrapper), HostScriptContracts.EtabsImports, typeof(EtabsScriptGlobals), settings.ScriptCacheSize);
        SelfCheckOk = ApiAvailable && ScriptingSelfCheck.Run(compiler);

        var attachment = new EtabsAttachment();
        var runner = new EtabsScriptRunner(settings, new EtabsResultSerializer(settings.MaxOutputBytes));
        var inspector = new TypeInspector(wrapper is null ? [] : [wrapper], HostName);
        var audit = new AuditLogger(store.AuditDirectory);

        _executor = new EtabsExecutor(settings, compiler, runner, attachment, inspector, audit, HostVersion, BusyGrace);
        _host = new McpBridgeHost(_executor, settings, store, HostVersion, PipeNaming.For(PipeNaming.EtabsHost, HostVersionNumber), HostName,
            JsonRpcMethods.EtabsPrefix, ExecutionDisabledMessage);
        McpBridgeHost.Install(_host);

        if (settings.AutoStartListener && SelfCheckOk) _host.Start();
        Log.Information("MCP bridge ready on pipe {Pipe}; auto-start listener = {AutoStart}; self-check {SelfCheck}", _host.PipeName, settings.AutoStartListener, SelfCheckOk ? "OK" : "FAILED");

        return (_host, _executor);
    }

    /// <summary>
    ///     The wrapper plus the engine assembly that defines `args`/`units` and this assembly (the globals type).
    ///     Imports come from <see cref="HostScriptContracts.EtabsImports"/> so the server's tool description and
    ///     the seed compile checks describe the same environment.
    /// </summary>
    private static Assembly[] CompilerReferences(Assembly? wrapper)
    {
        var references = new List<Assembly>
        {
            typeof(object).Assembly, typeof(Enumerable).Assembly, typeof(List<>).Assembly,
            Assembly.Load("netstandard"), Assembly.Load("System.Runtime"), Assembly.Load("System.Collections"),
            typeof(ScriptArgs).Assembly, typeof(System.Text.Json.JsonElement).Assembly,
            typeof(EtabsScriptGlobals).Assembly,
        };
        if (wrapper is not null) references.Add(wrapper);
        return references.ToArray();
    }

    public static void Dispose()
    {
        _host?.Dispose();
        _executor?.Dispose();
        Log.Information("HPEtabs MCP bridge stopped");
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
                shared: true) // a second bridge instance must be able to log why its listener refused to start
            .MinimumLevel.Debug()
            .CreateLogger();
    }
}
