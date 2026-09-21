using System.Windows;
using System.Windows.Threading;
using HPSap2000.McpBridge.View;
using HPSap2000.McpBridge.ViewModel;
using Serilog;

namespace HPSap2000.McpBridge;

/// <summary>
///     The bridge is a plain WPF desktop app: start the engine, show the status window, stop engine on close.
/// </summary>
public partial class App : Application
{
    static App()
    {
        Service.SapAttachment.EnsureDefaultDesktop();
    }

    private SapBridgeStatusViewModel? _viewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var (host, executor) = BridgeEntry.Start();
        var dispatcher = Dispatcher.CurrentDispatcher;

        _viewModel = new SapBridgeStatusViewModel(host, executor, action => dispatcher.InvokeAsync(action), Clipboard.SetText,
            BridgeEntry.SelfCheckOk, BridgeEntry.ApiAvailable, BridgeEntry.LogDirectory, BridgeEntry.SnapshotDirectory);
        var window = new SapBridgeStatusView(_viewModel);
        window.Closing += (_, args) =>
        {
            if (!executor.IsBusy) return;
            var answer = MessageBox.Show(window, "A script is still running inside SAP2000. Close anyway? The running SAP2000 call finishes first; the AI gets no answer.",
                "HPSap2000 MCP Bridge", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            args.Cancel = answer != MessageBoxResult.Yes;
        };
        MainWindow = window;
        window.Show();
        Log.Information("MCP bridge status window opened");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _viewModel?.Detach();
        BridgeEntry.Dispose();
        base.OnExit(e);
    }
}
