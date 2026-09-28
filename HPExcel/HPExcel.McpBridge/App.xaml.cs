using System.Windows;
using HPExcel.McpBridge.ViewModels;
using HPExcel.McpBridge.Views;

namespace HPExcel.McpBridge;

/// <summary>
///     Application entry point and lifecycle manager.
/// </summary>
public partial class App : Application
{
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var container = BridgeEntry.Start();

            var viewModel = new MainWindowViewModel(
                container.Host,
                container.Executor,
                action => Dispatcher.InvokeAsync(action),
                text => Clipboard.SetText(text));

            if (System.Linq.Enumerable.Contains(e.Args, "--allow-execution") || 
                System.Environment.GetEnvironmentVariable("HPEXCEL_ALLOW_EXECUTION") == "1")
            {
                viewModel.IsExecutionEnabled = true;
            }
            if (System.Linq.Enumerable.Contains(e.Args, "--allow-write") ||
                System.Environment.GetEnvironmentVariable("HPEXCEL_ALLOW_WRITE") == "1")
            {
                viewModel.IsExecutionEnabled = true;
                viewModel.IsWriteEnabled = true;
            }
            if (System.Linq.Enumerable.Contains(e.Args, "--auto-attach") ||
                System.Environment.GetEnvironmentVariable("HPEXCEL_AUTO_ATTACH") == "1")
            {
                try
                {
                    viewModel.Attach();
                }
                catch (System.Exception ex)
                {
                    Serilog.Log.Warning(ex, "Auto-attach failed during startup");
                }
            }

            _mainWindow = new MainWindow(viewModel);
            MainWindow = _mainWindow;
            _mainWindow.Show();

            Serilog.Log.Information("HPExcel MCP bridge main window opened successfully");
        }
        catch (System.Exception ex)
        {
            Serilog.Log.Fatal(ex, "HPExcel MCP bridge crashed during startup");
            MessageBox.Show("Fatal error starting HPExcel MCP Bridge:\n" + ex.Message, "HPExcel Bridge Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        BridgeEntry.Dispose();
        base.OnExit(e);
    }
}
