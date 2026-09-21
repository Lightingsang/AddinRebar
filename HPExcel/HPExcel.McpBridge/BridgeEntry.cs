using System;
using System.IO;
using System.Runtime.InteropServices;
using HPExcel.McpBridge.Com;
using HPExcel.McpBridge.Headless;
using HPExcel.McpBridge.Host;
using HPExcel.McpBridge.Safety;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;
using Serilog;
using Serilog.Events;

namespace HPExcel.McpBridge;

public sealed record BridgeContainer(
    McpBridgeHost Host,
    ExcelBridgeExecutor Executor,
    ExcelAttachment Attachment,
    ExcelStaWorker StaWorker,
    ExcelSafetyGuard Guard,
    ExcelSnapshotManager SnapshotManager,
    ClosedXmlWorkbookService ClosedXml,
    ExcelDispatcher Dispatcher);

/// <summary>
///     Entry point and container for HPExcel MCP Bridge services.
/// </summary>
public static class BridgeEntry
{
    public const string VendorFolder = "HPExcel";
    public const string ProductFolder = "McpBridge";
    public const string HostName = ExcelBridgeExecutor.HostName;
    public const int HostVersionNumber = 2026;
    public const string HostVersion = "2026";
    public const string PipeName = "hpexcel-mcp-2026";

    public static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), VendorFolder, ProductFolder, "logs");

    public static readonly string SnapshotDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), VendorFolder, "Snapshots");

    private static BridgeContainer? _container;

    public static BridgeContainer? Current => _container;

    public static BridgeContainer Start()
    {
        if (_container != null)
            return _container;

        CreateLogger();
        Log.Information("HPExcel MCP bridge {Version} starting; runtime {Runtime}; base {Base}",
            typeof(BridgeEntry).Assembly.GetName().Version, RuntimeInformation.FrameworkDescription, AppContext.BaseDirectory);

        var store = new BridgeSettingsStore(VendorFolder, ProductFolder);
        var settings = store.Load();

        var staWorker = new ExcelStaWorker();
        staWorker.Start();

        var attachment = new ExcelAttachment();
        var guard = new ExcelSafetyGuard();
        var snapshotManager = new ExcelSnapshotManager();
        var closedXml = new ClosedXmlWorkbookService();

        var executor = new ExcelBridgeExecutor(attachment, staWorker, guard, snapshotManager, closedXml, HostVersion);
        var dispatcher = new ExcelDispatcher(executor, settings, HostVersion);

        var pipe = PipeNaming.For(PipeNaming.ExcelHost, HostVersionNumber); // "hpexcel-mcp-2026"
        var host = new McpBridgeHost(executor, settings, store, HostVersion, pipe, HostName,
            JsonRpcMethods.ExcelPrefix, ExcelSafetyGuard.ExecutionDisabledMessage, dispatcher.DispatchCustomAsync);

        McpBridgeHost.Install(host);

        if (settings.AutoStartListener)
        {
            host.Start();
        }

        Log.Information("HPExcel MCP bridge ready on pipe {Pipe}; auto-start listener = {AutoStart}", pipe, settings.AutoStartListener);

        _container = new BridgeContainer(host, executor, attachment, staWorker, guard, snapshotManager, closedXml, dispatcher);
        return _container;
    }

    public static void Dispose()
    {
        _container?.Host.Dispose();
        _container?.Executor.Dispose();
        _container?.StaWorker.Dispose();
        _container = null;

        Log.Information("HPExcel MCP bridge stopped");
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
