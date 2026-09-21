using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HPSap2000.McpBridge.Model;
using HPSap2000.McpBridge.Service;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Scripting;
using Serilog;
using Serilog.Events;

namespace HPSap2000.McpBridge;

/// <summary>
///     Wires the SAP2000 bridge app: logging, SAP2000v1 resolver, settings, compiler, STA executor and pipe host.
/// </summary>
public static class BridgeEntry
{
    public const string VendorFolder = "HPSap2000";
    public const string ProductFolder = "McpBridge";
    public const string HostName = SapExecutor.HostName;
    public const int HostVersionNumber = 27;
    public const string HostVersion = "27";

    public const string ExecutionDisabledMessage =
        "Code execution is disabled. Ask the user to tick 'Allow AI code execution' in the HPSap2000 MCP Bridge window (a separate app, not inside SAP2000).";

    private static readonly TimeSpan BusyGrace = TimeSpan.FromSeconds(8);

    public static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), VendorFolder, ProductFolder, "logs");

    public static readonly string SnapshotDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), VendorFolder, ProductFolder, "snapshots");

    private static McpBridgeHost? _host;
    private static SapExecutor? _executor;

    public static bool SelfCheckOk { get; private set; }

    public static bool ApiAvailable { get; private set; }

    public static (McpBridgeHost host, SapExecutor executor) Start()
    {
        CreateLogger();
        Log.Information("HPSap2000 MCP bridge {Version} starting; runtime {Runtime}; base {Base}",
            typeof(BridgeEntry).Assembly.GetName().Version, RuntimeInformation.FrameworkDescription, AppContext.BaseDirectory);

        ApiAvailable = SapAssemblyResolver.Install(HostVersionNumber);
        return Wire();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (McpBridgeHost host, SapExecutor executor) Wire()
    {
        var store = new BridgeSettingsStore(VendorFolder, ProductFolder);
        var settings = store.Load();
        var wrapper = ApiAvailable ? Assembly.Load("SAP2000v1") : null;

        var compiler = CreateScriptCompiler(settings, wrapper);
        SelfCheckOk = ApiAvailable && ScriptingSelfCheck.Run(compiler);

        var attachment = new SapAttachment();
        var analyzer = new SapTierAnalyzer(SapTierTable.Embedded);
        var runner = new SapScriptRunner(settings, new SapResultSerializer(settings.MaxOutputBytes), new SapSnapshotManager(SnapshotDirectory));
        var inspector = new TypeInspector(wrapper is null ? [] : [wrapper], HostName);
        var audit = new AuditLogger(store.AuditDirectory);

        _executor = new SapExecutor(settings, compiler, analyzer, runner, attachment, inspector, audit, HostVersion, BusyGrace);
        _host = new McpBridgeHost(_executor, settings, store, HostVersion, PipeNaming.For(PipeNaming.Sap2000Host, HostVersionNumber), HostName,
            JsonRpcMethods.Sap2000Prefix, ExecutionDisabledMessage);
        McpBridgeHost.Install(_host);

        if (settings.AutoStartListener && SelfCheckOk) _host.Start();
        Log.Information("MCP bridge ready on pipe {Pipe}; auto-start listener = {AutoStart}; self-check {SelfCheck}", _host.PipeName, settings.AutoStartListener, SelfCheckOk ? "OK" : "FAILED");

        return (_host, _executor);
    }

    internal static ScriptCompiler CreateScriptCompiler(BridgeSettings settings, Assembly? wrapper) =>
        new(CompilerReferences(wrapper), HostScriptContracts.Sap2000Imports, typeof(SapScriptGlobals), settings.ScriptCacheSize);

    private static Assembly[] CompilerReferences(Assembly? wrapper)
    {
        var references = new List<Assembly>
        {
            typeof(object).Assembly, typeof(Enumerable).Assembly, typeof(List<>).Assembly,
            Assembly.Load("netstandard"), Assembly.Load("System.Runtime"), Assembly.Load("System.Collections"),
            typeof(ScriptArgs).Assembly, typeof(System.Text.Json.JsonElement).Assembly,
            typeof(SapScriptGlobals).Assembly,
        };
        if (wrapper is not null) references.Add(wrapper);
        return references.ToArray();
    }

    public static void Dispose()
    {
        _host?.Dispose();
        _executor?.Dispose();
        Log.Information("HPSap2000 MCP bridge stopped");
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
                shared: true)
            .MinimumLevel.Debug()
            .CreateLogger();
    }
}
