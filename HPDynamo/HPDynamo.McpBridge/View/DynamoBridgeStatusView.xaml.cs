namespace HPDynamo.McpBridge.View;

using System.Runtime.Loader;
using System.Windows;
using HPDynamo.McpBridge.Resources.Icons;
using HPDynamo.McpBridge.Resources.Themes;
using HPRebar.McpBridge.Core.ViewModel;

/// <summary>
///     Modeless status window for the HPDynamo MCP bridge.
///     Displays connection lifecycle, opt-in execution toggle, and script audit logs.
/// </summary>
public partial class DynamoBridgeStatusView : Window
{
    public DynamoBridgeStatusView(McpBridgeStatusViewModel viewModel)
    {
        using (AssemblyLoadContext.GetLoadContext(typeof(DynamoBridgeStatusView).Assembly)?.EnterContextualReflection())
        {
            InitializeComponent();
            MaterialThemeBridge.Attach(this, DynamoHostTheme.Instance, dark => DynamoRibbonIcons.CreateMcpIcon(dark));
        }

        DataContext = viewModel;

        SourceInitialized += (_, _) =>
        {
            if (PresentationSource.FromVisual(this) is System.Windows.Interop.HwndSource source && source.CompositionTarget != null)
            {
                source.CompositionTarget.RenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
            }
        };

        viewModel.CloseRequested += Close;
    }
}
