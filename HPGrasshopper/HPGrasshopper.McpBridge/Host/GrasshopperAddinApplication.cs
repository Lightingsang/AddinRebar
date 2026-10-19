namespace HPGrasshopper.McpBridge.Host;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using HPGrasshopper.McpBridge.Coordinator;
using HPGrasshopper.McpBridge.Resources.Icons;
using HPGrasshopper.McpBridge.UI;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Serilog;

/// <summary>
///     Revit 2026 External Application entry point for HPGrasshopper MCP Bridge.
///     Starts the Named Pipe listener hpgrasshopper-mcp-2026 and registers Ribbon controls.
/// </summary>
public sealed class GrasshopperAddinApplication : IExternalApplication
{
    private McpBridgeHost? _bridgeHost;
    private GrasshopperBridgeCoordinator? _coordinator;
    private static GrasshopperRpcDispatcher? _rpcDispatcher;

    internal static GrasshopperRpcDispatcher RpcDispatcher => _rpcDispatcher ??= new GrasshopperRpcDispatcher(GrasshopperBridgeCoordinator.Instance);
    public static McpBridgeHost? BridgeHost { get; private set; }
    public static IntPtr MainWindowHandle { get; private set; }

    public Result OnStartup(UIControlledApplication application)
    {
        GrasshopperAssemblyResolver.Install();
        InitializeLogger();
        Log.Information("HPGrasshopper MCP Bridge initializing");

        return StartupInternal(application);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private Result StartupInternal(UIControlledApplication application)
    {
        try
        {
            MainWindowHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            var settingsStore = new BridgeSettingsStore("HPGrasshopper", "McpBridge");
            var settings = settingsStore.Load();

            _coordinator = GrasshopperBridgeCoordinator.Instance;
            var handler = new GrasshopperExternalEventHandler(_coordinator);
            var externalEvent = ExternalEvent.Create(handler);
            _coordinator.Initialize(settings, new RevitExternalEventRaiser(externalEvent));
            _rpcDispatcher = new GrasshopperRpcDispatcher(_coordinator, settings);

            var pipeName = PipeNaming.For(PipeNaming.GrasshopperHost, 2026);
            const string executionDisabledMsg =
                "Code execution is disabled. Ask the user to tick 'Allow AI Grasshopper execution' in the HPGrasshopper MCP Bridge window inside Revit.";

            _bridgeHost = new McpBridgeHost(
                _coordinator,
                settings,
                settingsStore,
                "2026",
                pipeName,
                "Grasshopper",
                JsonRpcMethods.GrasshopperPrefix,
                executionDisabledMsg,
                HandleCustomRpcAsync);

            BridgeHost = _bridgeHost;

            CreateRibbonPanel(application);
            SubscribeApplicationEvents(application, settings);

            Log.Information("HPGrasshopper MCP Bridge initialized on pipe {Pipe}", pipeName);
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "HPGrasshopper MCP Bridge failed during OnStartup");
            Log.CloseAndFlush();
            return Result.Failed;
        }
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        try
        {
            BridgeHost = null;
            _bridgeHost?.Stop();
            _bridgeHost?.Dispose();
            _bridgeHost = null;

            _coordinator?.Dispose();
            _rpcDispatcher = null;

            GrasshopperAssemblyResolver.Uninstall();
            Log.Information("HPGrasshopper MCP Bridge shut down cleanly");
            Log.CloseAndFlush();
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error occurred during HPGrasshopper MCP Bridge OnShutdown");
            return Result.Failed;
        }
    }

    private void SubscribeApplicationEvents(UIControlledApplication application, BridgeSettings settings)
    {
        application.ControlledApplication.ApplicationInitialized += (_, _) =>
        {
            if (settings.AutoStartListener)
            {
                _bridgeHost?.Start();
                Log.Information("HPGrasshopper MCP Named Pipe listener auto-started");
            }
        };

        application.ViewActivated += (_, e) =>
        {
            var ghDoc = Grasshopper.Instances.ActiveDocument;
            var rPath = string.IsNullOrWhiteSpace(e.Document?.PathName) ? null : e.Document?.PathName;
            var gPath = string.IsNullOrWhiteSpace(ghDoc?.FilePath) ? null : ghDoc?.FilePath;
            _coordinator?.UpdateActiveSnapshot(
                e.Document?.Title,
                rPath,
                ghDoc?.DocumentID,
                ghDoc?.DisplayName,
                gPath);
        };
    }

    private static RibbonPanel ResolveRhinoPanel(UIControlledApplication application)
    {
        string[] candidates = ["Rhinoceros", "Rhino.Inside", "Rhino"];
        var panel = TryFindExistingTabPanel(application, candidates);
        if (panel != null) return panel;

        foreach (var candidate in candidates)
        {
            try { return application.CreateRibbonPanel(candidate, "Grasshopper MCP"); }
            catch { /* candidate tab not registered yet */ }
        }

        try
        {
            application.CreateRibbonTab("Rhinoceros");
            return application.CreateRibbonPanel("Rhinoceros", "Grasshopper MCP");
        }
        catch
        {
            return application.CreateRibbonPanel("Grasshopper MCP");
        }
    }

    private static RibbonPanel? TryFindExistingTabPanel(UIControlledApplication application, string[] candidates)
    {
        try
        {
            var ribbon = Autodesk.Windows.ComponentManager.Ribbon;
            if (ribbon == null) return null;

            foreach (var tab in ribbon.Tabs)
            {
                var title = tab.Title ?? string.Empty;
                var id = tab.Id ?? string.Empty;
                foreach (var candidate in candidates)
                {
                    if (title.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) < 0 &&
                        id.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) < 0) continue;

                    if (!string.IsNullOrEmpty(title))
                    {
                        try { return application.CreateRibbonPanel(title, "Grasshopper MCP"); }
                        catch { /* Panel creation by title failed; probe by tab id or next candidate */ }
                    }
                    if (!string.IsNullOrEmpty(id))
                    {
                        try { return application.CreateRibbonPanel(id, "Grasshopper MCP"); }
                        catch { /* Panel creation by tab id failed; continue searching candidate tabs */ }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "ComponentManager tab probing skipped");
        }

        return null;
    }

    private static void CreateRibbonPanel(UIControlledApplication application)
    {
        var panel = ResolveRhinoPanel(application);
        var isDark = GrasshopperRibbonIcons.RevitIsDark();
        var icon = GrasshopperRibbonIcons.CreateMcpIcon(isDark);

        var pushButtonData = new PushButtonData(
            "HPGrasshopper_Status",
            "Grasshopper\nMCP",
            typeof(GrasshopperAddinApplication).Assembly.Location,
            "HPGrasshopper.McpBridge.UI.ShowStatusDialogCommand")
        {
            LargeImage = icon,
            Image = icon,
            ToolTip = "HPGrasshopper MCP Bridge: toggle named pipe listener and authorize AI Grasshopper execution."
        };

        if (panel.AddItem(pushButtonData) is PushButton button)
        {
            application.ThemeChanged += (_, _) =>
            {
                var newIcon = GrasshopperRibbonIcons.CreateMcpIcon(GrasshopperRibbonIcons.RevitIsDark());
                button.LargeImage = newIcon;
                button.Image = newIcon;
            };
        }
    }

    private static Task<JsonRpcEnvelope?> HandleCustomRpcAsync(
        long id,
        JsonRpcEnvelope envelope,
        NdjsonPipeWriter writer,
        CancellationToken cancellationToken)
    {
        return RpcDispatcher.DispatchAsync(id, envelope, writer, cancellationToken);
    }

    private static void InitializeLogger()
    {
        var logFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HPGrasshopper", "McpBridge", "logs");
        Directory.CreateDirectory(logFolder);

        var logPath = Path.Combine(logFolder, "mcpbridge-.log");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                shared: true)
            .CreateLogger();
    }
}
