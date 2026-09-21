using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HPRobot.McpBridge.Com;
using HPRobot.McpBridge.Host;
using HPRobot.McpBridge.Safety;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Scripting;
using RobotOM;
using Serilog;
using Serilog.Events;

namespace HPRobot.McpBridge;

/// <summary>
///     Wires the Autodesk Robot MCP bridge desktop application:
///     logging, assembly resolver, settings, compiler, STA executor, dispatcher, and named pipe host.
/// </summary>
public static class BridgeEntry
{
    public const string VendorFolder = "HPRobot";
    public const string ProductFolder = "McpBridge";
    public const string HostName = RobotBridgeExecutor.HostName;
    public const int HostVersionNumber = 2026;
    public const string HostVersion = "2026";

    public const string ExecutionDisabledMessage =
        "Code execution is disabled. Ask the user to tick 'Allow AI execution' in the HPRobot MCP Bridge window.";

    public static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), VendorFolder, ProductFolder, "logs");

    public static readonly string SnapshotDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), VendorFolder, ProductFolder, "snapshots");

    private static McpBridgeHost? _host;
    private static RobotBridgeExecutor? _executor;

    public static bool ApiAvailable { get; private set; }

    public static (McpBridgeHost host, RobotBridgeExecutor executor) Start()
    {
        CreateLogger();
        Log.Information("HPRobot MCP bridge {Version} starting; runtime {Runtime}; base {Base}",
            typeof(BridgeEntry).Assembly.GetName().Version, RuntimeInformation.FrameworkDescription, AppContext.BaseDirectory);

        ApiAvailable = RobotAssemblyResolver.Install();
        return Wire();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (McpBridgeHost host, RobotBridgeExecutor executor) Wire()
    {
        var store = new BridgeSettingsStore(VendorFolder, ProductFolder);
        var settings = store.Load();

        var staWorker = new RobotStaWorker();
        staWorker.Start();

        var attachment = new RobotAttachment();
        var guard = new RobotSafetyGuard();
        var snapshots = new RobotSnapshotManager(SnapshotDirectory);

        var compiler = CreateScriptCompiler(settings);
        _executor = new RobotBridgeExecutor(attachment, staWorker, guard, snapshots, HostVersion, compiler);

        var customDispatcher = new RobotDispatcher(_executor, settings, HostVersion);

        _host = new McpBridgeHost(
            _executor,
            settings,
            store,
            HostVersion,
            PipeNaming.For(PipeNaming.RobotHost, HostVersionNumber),
            HostName,
            JsonRpcMethods.RobotPrefix,
            ExecutionDisabledMessage,
            customDispatcher.DispatchCustomAsync);

        McpBridgeHost.Install(_host);

        if (settings.AutoStartListener)
        {
            _host.Start();
        }

        Log.Information("HPRobot MCP bridge ready on pipe '{Pipe}'; auto-start = {AutoStart}; API available = {Api}",
            _host.PipeName, settings.AutoStartListener, ApiAvailable);

        return (_host, _executor);
    }

    internal static ScriptCompiler CreateScriptCompiler(BridgeSettings settings)
    {
        Assembly? interopAsm = null;
        try
        {
            interopAsm = typeof(IRobotApplication).Assembly;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Could not load IRobotApplication assembly for compiler");
        }

        var references = new System.Collections.Generic.List<Assembly>
        {
            typeof(object).Assembly,
            typeof(System.Linq.Enumerable).Assembly,
            typeof(System.Collections.Generic.List<>).Assembly,
            Assembly.Load("netstandard"),
            Assembly.Load("System.Runtime"),
            Assembly.Load("System.Collections"),
            typeof(ScriptArgs).Assembly,
            typeof(System.Text.Json.JsonElement).Assembly,
            typeof(RobotScriptGlobals).Assembly
        };

        if (interopAsm != null && !references.Contains(interopAsm))
        {
            references.Add(interopAsm);
        }

        return new ScriptCompiler(
            references,
            HostScriptContracts.RobotImports,
            typeof(RobotScriptGlobals),
            settings.ScriptCacheSize);
    }

    public static void Dispose()
    {
        _host?.Dispose();
        _executor?.Dispose();
        Log.Information("HPRobot MCP bridge stopped");
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
