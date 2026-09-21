using System.Windows;
using System.Windows.Threading;
using HPRobot.McpBridge.ViewModels;
using HPRobot.McpBridge.Views;
using Serilog;

namespace HPRobot.McpBridge;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private MainWindowViewModel? _viewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var (host, executor) = BridgeEntry.Start();
        var dispatcher = Dispatcher.CurrentDispatcher;

        _viewModel = new MainWindowViewModel(host, executor, action => dispatcher.InvokeAsync(action), Clipboard.SetText);
        var window = new MainWindow(_viewModel);
        window.Closing += (_, args) =>
        {
            if (!executor.IsBusy) return;
            var answer = MessageBox.Show(window,
                "A script is still running inside Robot. Close anyway? The running Robot call finishes first; the AI gets no answer.",
                "HPRobot MCP Bridge", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            args.Cancel = answer != MessageBoxResult.Yes;
        };

        MainWindow = window;
        window.Show();
        Log.Information("HPRobot MCP bridge main window opened");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        BridgeEntry.Dispose();
        base.OnExit(e);
    }
}
