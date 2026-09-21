using System;
using System.IO;
using System.Runtime.InteropServices;
using HPPowerBi.McpBridge.Cloud;
using HPPowerBi.McpBridge.Host;
using HPPowerBi.McpBridge.Safety;
using HPPowerBi.McpBridge.Tabular;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;
using Serilog;
using Serilog.Events;

namespace HPPowerBi.McpBridge;

public sealed record BridgeContainer(
    McpBridgeHost Host,
    PowerBiBridgeExecutor Executor,
    PbiConnectionManager ConnectionManager,
    PbiSafetyGuard Guard,
    PbiSnapshotManager SnapshotManager,
    PowerBiCloudClient CloudClient,
    PowerBiDispatcher Dispatcher);

/// <summary>
///     Wires the Power BI bridge application: logging, settings, tabular connection manager,
///     safety guard, pre-mutation snapshot manager, Roslyn script executor, custom dispatcher,
///     and named pipe listener on "hppowerbi-mcp-2026".
/// </summary>
public static class BridgeEntry
{
    public const string VendorFolder = "HPPowerBi";
    public const string ProductFolder = "McpBridge";
    public const string HostName = PowerBiBridgeExecutor.HostName;
    public const int HostVersionNumber = 2026;
    public const string HostVersion = "2026";
    public const string PipeName = "hppowerbi-mcp-2026";

    public static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), VendorFolder, ProductFolder, "logs");

    public static readonly string SnapshotDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), VendorFolder, "Snapshots");

    private static BridgeContainer? _container;

    public static BridgeContainer? Current => _container;

    /// <summary>
    ///     Initializes the bridge services and starts the named pipe listener.
    /// </summary>
    public static BridgeContainer Start()
    {
        if (_container != null)
            return _container;

        CreateLogger();
        Log.Information("HPPowerBi MCP bridge {Version} starting; runtime {Runtime}; base {Base}",
            typeof(BridgeEntry).Assembly.GetName().Version, RuntimeInformation.FrameworkDescription, AppContext.BaseDirectory);

        var store = new BridgeSettingsStore(VendorFolder, ProductFolder);
        var settings = store.Load();

        var connectionManager = new PbiConnectionManager();
        var guard = new PbiSafetyGuard();
        var snapshotManager = new PbiSnapshotManager(SnapshotDirectory);
        var cloudClient = new PowerBiCloudClient();

        var executor = new PowerBiBridgeExecutor(connectionManager, guard, snapshotManager, HostVersion);
        var dispatcher = new PowerBiDispatcher(executor, cloudClient, settings, HostVersion);

        var pipe = PipeNaming.For(PipeNaming.PowerBiHost, HostVersionNumber); // "hppowerbi-mcp-2026"
        var host = new McpBridgeHost(executor, settings, store, HostVersion, pipe, HostName,
            JsonRpcMethods.PowerBiPrefix, PbiSafetyGuard.ExecutionDisabledMessage, dispatcher.DispatchCustomAsync);

        McpBridgeHost.Install(host);

        if (settings.AutoStartListener)
        {
            host.Start();
        }

        Log.Information("HPPowerBi MCP bridge ready on pipe {Pipe}; auto-start listener = {AutoStart}", pipe, settings.AutoStartListener);

        _container = new BridgeContainer(host, executor, connectionManager, guard, snapshotManager, cloudClient, dispatcher);
        return _container;
    }

    /// <summary>
    ///     Disposes bridge services and closes log file sinks.
    /// </summary>
    public static void Dispose()
    {
        _container?.Host.Dispose();
        _container?.Executor.Dispose();
        _container?.ConnectionManager.Dispose();
        _container?.CloudClient.Dispose();
        _container = null;

        Log.Information("HPPowerBi MCP bridge stopped");
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
