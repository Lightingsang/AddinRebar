using System.Windows;
using HPPowerBi.McpBridge.Resources.Themes;
using HPPowerBi.McpBridge.ViewModels;

namespace HPPowerBi.McpBridge.Views;

/// <summary>
///     Code-behind for the Power BI MCP Bridge status window.
///     Initializes data context and attaches WindowsHostTheme for automatic dark/light synchronization.
/// </summary>
public partial class StatusWindow : Window
{
    public StatusWindow(StatusViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        MaterialThemeBridge.Attach(this, WindowsHostTheme.Instance);
    }
}
