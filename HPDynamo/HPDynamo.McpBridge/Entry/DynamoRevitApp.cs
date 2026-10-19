namespace HPDynamo.McpBridge.Entry;

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using Autodesk.Windows;
using HPDynamo.McpBridge.Coordinator;
using HPDynamo.McpBridge.Resources.Icons;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Serilog;

/// <summary>
///     Revit 2026 External Application entry point.
///     Installs the Dynamo assembly resolver, starts the Named Pipe listener,
///     and registers the Revit Ribbon controls.
/// </summary>
public sealed class DynamoRevitApp : IExternalApplication
{
    private McpBridgeHost? _bridgeHost;
    private DynamoBridgeCoordinator? _coordinator;

    public static McpBridgeHost? BridgeHost { get; private set; }
    public static IntPtr MainWindowHandle { get; private set; }

    public Result OnStartup(UIControlledApplication application)
    {
        DynamoAssemblyResolver.Install();
        InitializeLogger();
        Log.Information("HPDynamo MCP Bridge initializing: DynamoAssemblyResolver installed and logger ready");

        try
        {
            MainWindowHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            var settingsStore = new BridgeSettingsStore("HPDynamo", "McpBridge");
            var settings = settingsStore.Load();

            _coordinator = DynamoBridgeCoordinator.Instance;
            var handler = new DynamoExternalEventHandler(_coordinator);
            var externalEvent = ExternalEvent.Create(handler);
            _coordinator.Initialize(settings, new RevitExternalEventRaiser(externalEvent));

            var pipeName = PipeNaming.For(PipeNaming.DynamoHost, 2026);
            const string executionDisabledMsg =
                "Code execution is disabled. Ask the user to tick 'Allow AI Dynamo execution' in the HPDynamo MCP Bridge window inside Revit.";

            _bridgeHost = new McpBridgeHost(
                _coordinator,
                settings,
                settingsStore,
                "2026",
                pipeName,
                "Dynamo",
                JsonRpcMethods.DynamoPrefix,
                executionDisabledMsg,
                HandleCustomRpcAsync);

            BridgeHost = _bridgeHost;

            CreateRibbonPanel(application);
            SubscribeApplicationEvents(application, settings);

            Log.Information("HPDynamo MCP Bridge initialized on pipe {Pipe}", pipeName);
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "HPDynamo MCP Bridge failed during OnStartup");
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

            DynamoAssemblyResolver.Uninstall();
            Log.Information("HPDynamo MCP Bridge shut down cleanly");
            Log.CloseAndFlush();
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error occurred during HPDynamo MCP Bridge OnShutdown");
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
                Log.Information("HPDynamo MCP Named Pipe listener auto-started");
            }
        };

        application.ViewActivated += (_, e) =>
        {
            _coordinator?.SetActiveDocumentTitle(e.Document?.Title);
        };
    }

    private static void CreateRibbonPanel(UIControlledApplication application)
    {
        Autodesk.Revit.UI.RibbonPanel panel;
        try
        {
            application.CreateRibbonTab("HPDynamo");
            panel = application.CreateRibbonPanel("HPDynamo", "Dynamo MCP");
        }
        catch
        {
            // If custom tab exists or creation fails, fallback to default Add-Ins panel
            panel = application.CreateRibbonPanel("Dynamo MCP");
        }

        var isDark = DynamoRibbonIcons.RevitIsDark();
        var icon = DynamoRibbonIcons.CreateMcpIcon(isDark);

        var pushButtonData = new PushButtonData(
            "HPDynamo_Status",
            "Dynamo MCP",
            typeof(DynamoRevitApp).Assembly.Location,
            "HPDynamo.McpBridge.Entry.ShowStatusDialogCommand")
        {
            LargeImage = icon,
            Image = icon,
            ToolTip = "HPDynamo MCP Bridge: toggle named pipe listener and authorize AI Dynamo execution."
        };

        var button = panel.AddItem(pushButtonData) as PushButton;
        if (button != null)
        {
            application.ThemeChanged += (_, _) =>
            {
                var newIcon = DynamoRibbonIcons.CreateMcpIcon(DynamoRibbonIcons.RevitIsDark());
                button.LargeImage = newIcon;
                button.Image = newIcon;
            };
        }

        TryMoveToManageTab();
    }

    private static void TryMoveToManageTab()
    {
        try
        {
            var ribbon = ComponentManager.Ribbon;
            if (ribbon is null) return;

            var hpTab = ribbon.Tabs.FirstOrDefault(t => t.Id == "HPDynamo" || t.Title == "HPDynamo");
            var manageTab = ribbon.Tabs.FirstOrDefault(t => t.Id == "Manage" || t.Title == "Manage");
            if (hpTab is null || manageTab is null) return;

            var mcpPanel = hpTab.Panels.FirstOrDefault(p =>
                p.Source?.Title == "Dynamo MCP" || p.Source?.Id == "Dynamo MCP");
            if (mcpPanel is not null)
            {
                hpTab.Panels.Remove(mcpPanel);
                manageTab.Panels.Add(mcpPanel);
                ribbon.Tabs.Remove(hpTab);
                Log.Information("Dynamo MCP ribbon panel repositioned onto Manage tab");
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Could not reposition Dynamo MCP panel to Manage tab via ComponentManager");
        }
    }

    private static async Task<JsonRpcEnvelope?> HandleCustomRpcAsync(
        long id,
        JsonRpcEnvelope envelope,
        NdjsonPipeWriter writer,
        CancellationToken cancellationToken)
    {
        var method = envelope.Method;
        if (string.IsNullOrEmpty(method)) return null;

        if (method.Equals("dynamo.show_window", StringComparison.OrdinalIgnoreCase) ||
            method.Equals("revit.show_window", StringComparison.OrdinalIgnoreCase))
        {
            ShowStatusDialogCommand.ShowWindow();
            return JsonRpcEnvelope.Success(id, new { shown = true });
        }

        if (method.Equals("dynamo.activate_tab", StringComparison.OrdinalIgnoreCase))
        {
            var app = System.Windows.Application.Current;
            var dispatcher = app?.Dispatcher ?? System.Windows.Threading.Dispatcher.CurrentDispatcher;
            await dispatcher.InvokeAsync(() =>
            {
                try
                {
                    var ribbon = ComponentManager.Ribbon;
                    if (ribbon != null)
                    {
                        var manageTab = ribbon.Tabs.FirstOrDefault(t => t.Id == "Manage" || t.Title == "Manage");
                        if (manageTab != null)
                        {
                            ribbon.ActiveTab = manageTab;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to switch active ribbon tab to Manage");
                }
            });
            return JsonRpcEnvelope.Success(id, new { tab = "Manage" });
        }

        if (method.Equals("dynamo.launch_dynamo", StringComparison.OrdinalIgnoreCase))
        {
            var app = System.Windows.Application.Current;
            var dispatcher = app?.Dispatcher ?? System.Windows.Threading.Dispatcher.CurrentDispatcher;
            await dispatcher.InvokeAsync(() =>
            {
                try
                {
                    var ribbon = ComponentManager.Ribbon;
                    if (ribbon != null)
                    {
                        var manageTab = ribbon.Tabs.FirstOrDefault(t => t.Id == "Manage" || t.Title == "Manage");
                        if (manageTab != null)
                        {
                            ribbon.ActiveTab = manageTab;
                            foreach (var panel in manageTab.Panels)
                            {
                                foreach (var item in panel.Source.Items)
                                {
                                    if (item is Autodesk.Windows.RibbonButton btn &&
                                        item.Id != "HPDynamo_Status" &&
                                        item.Text != "Dynamo MCP" &&
                                        item.Text != "Dynamo Player" &&
                                        (item.Id == "ID_VISUAL_PROGRAMMING_DYNAMO" ||
                                         item.Text?.IndexOf("Dynamo", StringComparison.OrdinalIgnoreCase) >= 0))
                                    {
                                        if (btn.CommandHandler != null && btn.CommandHandler.CanExecute(btn.CommandParameter))
                                        {
                                            btn.CommandHandler.Execute(btn.CommandParameter);
                                            Log.Information("Launched Dynamo via Ribbon button CommandHandler");
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to launch Dynamo via ribbon button");
                }
            });
            return JsonRpcEnvelope.Success(id, new { launched = true });
        }

        return null;
    }

    private static void InitializeLogger()
    {
        var logFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HPDynamo", "McpBridge", "logs");
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
