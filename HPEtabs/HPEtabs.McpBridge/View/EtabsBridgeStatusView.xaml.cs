using System.Windows;
using HPEtabs.McpBridge.ViewModel;

namespace HPEtabs.McpBridge.View;

public partial class EtabsBridgeStatusView : Window
{
    public EtabsBridgeStatusView(EtabsBridgeStatusViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Modeless: there is no DialogResult to hand back, the window just goes away (and the app with it).
        viewModel.CloseRequested += Close;
    }
}
