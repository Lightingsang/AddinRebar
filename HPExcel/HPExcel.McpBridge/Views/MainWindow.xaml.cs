using System.Windows;
using HPExcel.McpBridge.Resources.Themes;
using HPExcel.McpBridge.ViewModels;

namespace HPExcel.McpBridge.Views;

/// <summary>
///     Interaction logic for MainWindow.xaml.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        MaterialThemeBridge.Attach(this, WindowsHostTheme.Instance);
        Loaded += (_, _) =>
        {
            Activate();
            Focus();
        };
    }
}
