using System.Windows;
using HPRebar.McpBridge.Core.ViewModel;
using HPRebar.Resources.Icons;
using HPRebar.Resources.Themes;

namespace HPRebar.McpBridge.View;

public partial class McpBridgeStatusView : Window
{
    public McpBridgeStatusView(McpBridgeStatusViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
        MaterialThemeBridge.Attach(this, RevitHostTheme.Instance, dark => new RibbonIcons(dark).McpBridge);

        // Modeless: there is no DialogResult to hand back, the window just goes away.
        viewModel.CloseRequested += Close;
    }
}
