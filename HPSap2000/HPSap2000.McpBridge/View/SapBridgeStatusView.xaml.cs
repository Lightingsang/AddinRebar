using System.Windows;
using HPSap2000.McpBridge.Resources.Themes;
using HPSap2000.McpBridge.ViewModel;

namespace HPSap2000.McpBridge.View;

public partial class SapBridgeStatusView : Window
{
    public SapBridgeStatusView(SapBridgeStatusViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        MaterialThemeBridge.Attach(this, WindowsHostTheme.Instance);

        viewModel.CloseRequested += Close;
    }
}
