using System.Runtime.Loader;
using System.Windows;
using HPAutoCad.McpBridge.Service;
using HPRebar.McpBridge.Core.ViewModel;

namespace HPAutoCad.McpBridge.View;

public partial class AutocadBridgeStatusView : Window
{
    public AutocadBridgeStatusView(McpBridgeStatusViewModel viewModel)
    {
        // WPF resolves "/HPAutoCad.McpBridge;component/..." through Assembly.Load, which searches the default
        // load context — this assembly lives in the bridge's own context, so the lookup must run inside it.
        using (AssemblyLoadContext.GetLoadContext(typeof(AutocadBridgeStatusView).Assembly)!.EnterContextualReflection())
        {
            InitializeComponent();
            AutocadThemeSwitcher.ApplyFromAutocad(this);
        }

        DataContext = viewModel;

        // Modeless: there is no DialogResult to hand back, the window just goes away.
        viewModel.CloseRequested += Close;
    }
}
