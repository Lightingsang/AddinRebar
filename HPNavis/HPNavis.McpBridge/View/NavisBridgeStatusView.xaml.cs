using System.Windows;
using HPNavis.McpBridge.ViewModel;

namespace HPNavis.McpBridge.View;

public partial class NavisBridgeStatusView : Window
{
    public NavisBridgeStatusView(NavisBridgeStatusViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Modeless: there is no DialogResult to hand back, the window just goes away.
        viewModel.CloseRequested += Close;
    }
}
