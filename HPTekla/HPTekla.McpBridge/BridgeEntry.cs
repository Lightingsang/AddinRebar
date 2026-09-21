using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using HPTekla.McpBridge.ViewModels;
using HPTekla.McpBridge.Views;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;
using Serilog;
using Tekla.Structures.Model;
using RefAssembly = System.Reflection.Assembly;

namespace HPTekla.McpBridge;

/// <summary>
///     Lifecycle coordinator for the HPTekla MCP Bridge inside Tekla Structures 2025.0.
///     Initializes assembly resolution, logging, settings, the Tekla executor,
///     the named pipe listener on 'hptekla-mcp-2025', and owns the WPF modeless status window.
/// </summary>
public static class BridgeEntry
{
    public const string VendorFolder = "HPTekla";
    public const string ProductFolder = "McpBridge";
    public const string HostName = "Tekla Structures";
    public const string HostVersion = "2025";

    public static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), VendorFolder, ProductFolder, "logs");

    private static McpBridgeHost? _host;
    private static TeklaBridgeExecutor? _executor;
    private static TeklaThreadDispatcher? _dispatcher;
    private static BridgeStatusWindow? _window;
    private static Dispatcher? _mainDispatcher;
    private static bool _started;

    public static bool IsStarted => _started;

    /// <summary>
    ///     Initializes the bridge on Tekla's main thread.
    /// </summary>
    public static void Start(string? pluginFolder = null)
    {
        if (_started) return;

        pluginFolder ??= Path.GetDirectoryName(RefAssembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory;
        PluginAssemblyResolver.Install(pluginFolder);

        CreateLogger();
        Log.Information("HPTekla MCP Bridge starting from '{Folder}' on Tekla Structures 2025.0 (.NET Framework 4.8)", pluginFolder);

        try
        {
            _mainDispatcher = Dispatcher.CurrentDispatcher;

            var store = new BridgeSettingsStore(VendorFolder, ProductFolder);
            var settings = store.Load();

            var model = new Model();
            _dispatcher = new TeklaThreadDispatcher(TimeSpan.FromSeconds(8));
            var snapshots = new TeklaSnapshotManager();

            _executor = new TeklaBridgeExecutor(model, _dispatcher, settings, snapshots, HostVersion);

            var pipeName = PipeNaming.For(PipeNaming.TeklaHost, 2025);
            _host = new McpBridgeHost(_executor, settings, store, HostVersion, pipeName, HostName, JsonRpcMethods.TeklaPrefix);
            McpBridgeHost.Install(_host);

            if (settings.AutoStartListener)
            {
                _host.Start();
                Log.Information("HPTekla named pipe listener started on '{Pipe}'", pipeName);
            }
            else
            {
                Log.Information("HPTekla named pipe listener ready on '{Pipe}' (stopped until enabled)", pipeName);
            }

            _started = true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize HPTekla MCP Bridge");
        }
    }

    /// <summary>
    ///     Displays or activates the modeless WPF Status Window.
    /// </summary>
    public static void ShowStatusWindow()
    {
        if (!_started)
        {
            Start();
        }

        var dispatcher = _mainDispatcher ?? Dispatcher.CurrentDispatcher;
        dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                if (_window != null && _window.IsLoaded)
                {
                    _window.Activate();
                    if (_window.WindowState == WindowState.Minimized)
                        _window.WindowState = WindowState.Normal;
                    return;
                }

                if (_host == null || _executor == null)
                {
                    Log.Error("Cannot show HPTekla status window: bridge is not initialized");
                    return;
                }

                var vm = new BridgeStatusViewModel(
                    runner: _host,
                    executor: _executor,
                    onUiThread: act => dispatcher.BeginInvoke(act),
                    copyToClipboard: text =>
                    {
                        try { Clipboard.SetText(text); }
                        catch (Exception ex) { Log.Warning(ex, "Failed to copy text to clipboard"); }
                    },
                    selfCheckOk: true,
                    logDirectory: LogDirectory);

                _window = new BridgeStatusWindow(vm);
                _window.Closed += (_, _) => _window = null;
                _window.Show();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to display HPTekla status window");
            }
        }));
    }

    /// <summary>
    ///     Stops the pipe listener and cleans up resources.
    /// </summary>
    public static void Stop()
    {
        try
        {
            _window?.Close();
            _host?.Stop();
            _executor?.Dispose();
            _dispatcher?.Dispose();
            _started = false;
            Log.Information("HPTekla MCP Bridge stopped");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error while stopping HPTekla bridge");
        }
    }

    private static void CreateLogger()
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            var logPath = Path.Combine(LogDirectory, "hptekla_bridge.log");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
                .CreateLogger();
        }
        catch
        {
            // Ignore logging initialization failure
        }
    }
}
