using System.Runtime.Loader;
using System.Windows;
using HPCivil3d.McpBridge.Resources.Themes;
using HPCivil3d.McpBridge.Service;
using HPRebar.McpBridge.Core.ViewModel;

namespace HPCivil3d.McpBridge.View;

public partial class Civil3dBridgeStatusView : Window
{
    public Civil3dBridgeStatusView(McpBridgeStatusViewModel viewModel)
    {
        // WPF resolves "/HPCivil3d.McpBridge;component/..." through Assembly.Load, which searches the default
        // load context — this assembly lives in the bridge's own context, so the lookup must run inside it.
        using (AssemblyLoadContext.GetLoadContext(typeof(Civil3dBridgeStatusView).Assembly)!.EnterContextualReflection())
        {
            InitializeComponent();
            MaterialThemeBridge.Attach(this, Civil3dHostTheme.Instance);
        }

        DataContext = viewModel;

        // Modeless: there is no DialogResult to hand back, the window just goes away.
        viewModel.CloseRequested += Close;
    }
}
