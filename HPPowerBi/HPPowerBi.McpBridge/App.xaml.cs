using System.Windows;
using System.Windows.Threading;
using HPPowerBi.McpBridge.ViewModels;
using HPPowerBi.McpBridge.Views;
using Serilog;

namespace HPPowerBi.McpBridge;

/// <summary>
///     Application entry and lifecycle host for the Power BI MCP Bridge standalone WPF app.
/// </summary>
public partial class App : Application
{
    private StatusViewModel? _viewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var container = BridgeEntry.Start();
        var dispatcher = Dispatcher.CurrentDispatcher;

        _viewModel = new StatusViewModel(
            container.ConnectionManager,
            container.Guard,
            container.SnapshotManager,
            container.Host,
            container.CloudClient,
            BridgeEntry.LogDirectory,
            action => dispatcher.InvokeAsync(action));

        _viewModel.RefreshInstances();

        var window = new StatusWindow(_viewModel);
        MainWindow = window;
        window.Show();

        Log.Information("HPPowerBi MCP bridge status window opened successfully");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        BridgeEntry.Dispose();
        base.OnExit(e);
    }
}
