using System.Windows;
using HPRobot.McpBridge.Resources.Themes;
using HPRobot.McpBridge.ViewModels;

namespace HPRobot.McpBridge.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        MaterialThemeBridge.Attach(this, WindowsHostTheme.Instance);
    }
}
