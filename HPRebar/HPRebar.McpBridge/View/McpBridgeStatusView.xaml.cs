using System.Windows;
using HPRebar.McpBridge.Service;
using HPRebar.McpBridge.ViewModel;

namespace HPRebar.McpBridge.View;

public partial class McpBridgeStatusView : Window
{
    public McpBridgeStatusView(McpBridgeStatusViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
        ThemeSwitcher.ApplyFromRevit(this);

        // Modeless: there is no DialogResult to hand back, the window just goes away.
        viewModel.CloseRequested += Close;
    }
}
