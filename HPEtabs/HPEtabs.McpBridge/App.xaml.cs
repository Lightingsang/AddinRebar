using System.Windows;
using System.Windows.Threading;
using HPEtabs.McpBridge.View;
using HPEtabs.McpBridge.ViewModel;
using Serilog;

namespace HPEtabs.McpBridge;

/// <summary>
///     The bridge is a plain WPF desktop app: start the engine, show the one status window, stop the engine
///     when that window closes. Closing while a script runs asks first — the call inside ETABS cannot be
///     interrupted and the worker drains it before the process ends.
/// </summary>
public partial class App : Application
{
    private EtabsBridgeStatusViewModel? _viewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (s, args) =>
        {
            Log.Error(args.Exception, "Unhandled dispatcher exception in bridge app");
        };
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            Log.Error(args.ExceptionObject as Exception, "Unhandled domain exception in bridge app");
        };

        base.OnStartup(e);

        var (host, executor) = BridgeEntry.Start();
        var dispatcher = Dispatcher.CurrentDispatcher;

        _viewModel = new EtabsBridgeStatusViewModel(host, executor, action => dispatcher.InvokeAsync(action), Clipboard.SetText,
            BridgeEntry.SelfCheckOk, BridgeEntry.ApiAvailable, BridgeEntry.LogDirectory, BridgeEntry.SnapshotDirectory);
        var window = new EtabsBridgeStatusView(_viewModel);
        window.Closing += (_, args) =>
        {
            if (!executor.IsBusy) return;
            var answer = MessageBox.Show(window, "A script is still running inside ETABS. Close anyway? The running ETABS call finishes first; the AI gets no answer.",
                "HPEtabs MCP Bridge", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            args.Cancel = answer != MessageBoxResult.Yes;
        };
        MainWindow = window;
        window.Show();
        Log.Information("MCP bridge status window opened");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Bridge application OnExit with code {Code}", e.ApplicationExitCode);
        _viewModel?.Detach();
        BridgeEntry.Dispose();
        base.OnExit(e);
    }
}
