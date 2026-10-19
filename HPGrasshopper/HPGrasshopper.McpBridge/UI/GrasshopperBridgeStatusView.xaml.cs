namespace HPGrasshopper.McpBridge.UI;

using System.Runtime.Loader;
using System.Windows;
using HPGrasshopper.McpBridge.Resources.Icons;
using HPGrasshopper.McpBridge.Resources.Themes;

/// <summary>
///     Modeless status window for the HPGrasshopper MCP bridge.
///     Displays connection lifecycle, opt-in execution toggle, and script audit logs.
/// </summary>
public partial class GrasshopperBridgeStatusView : Window
{
    public GrasshopperBridgeStatusView(GrasshopperBridgeStatusViewModel viewModel)
    {
        using (AssemblyLoadContext.GetLoadContext(typeof(GrasshopperBridgeStatusView).Assembly)?.EnterContextualReflection())
        {
            InitializeComponent();
            MaterialThemeBridge.Attach(this, GrasshopperHostTheme.Instance, dark => GrasshopperRibbonIcons.CreateMcpIcon(dark));
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
