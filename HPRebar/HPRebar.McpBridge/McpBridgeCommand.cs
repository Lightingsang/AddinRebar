using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using System.Windows;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.ViewModel;
using HPRebar.McpBridge.View;
using Nice3point.Revit.Toolkit.External;

namespace HPRebar.McpBridge;

/// <summary>
///     Ribbon entry: opens the modeless status window over the bridge that Application started. The
///     window never owns the bridge — the listener keeps serving with the window closed — so closing it
///     only drops the window reference; the host is disposed in Application.OnShutdown.
/// </summary>
[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public sealed class McpBridgeCommand : ExternalCommand
{
    // Modeless, so it outlives this command. Holding it here keeps it off the collector and makes a
    // second click focus the open window instead of opening a twin.
    private static McpBridgeStatusView? _window;

    public override void Execute()
    {
        if (_window is not null)
        {
            _window.Activate();
            return;
        }

        var host = McpBridgeHost.Current;
        if (host is null)
        {
            TaskDialog.Show("HPRebar MCP Bridge",
                "The bridge did not initialise when Revit started. See %LocalAppData%\\HPRebar\\McpBridge\\logs for the reason.");
            return;
        }

        var viewModel = new McpBridgeStatusViewModel(host, Clipboard.SetText);
        var view = new McpBridgeStatusView(viewModel);

        // Application is the inherited UIApplication of ExternalCommand, not HPRebar.McpBridge.Application.
        new WindowInteropHelper(view).Owner = Application.MainWindowHandle;

        view.Closed += (_, _) =>
        {
            viewModel.Detach();
            _window = null;
        };

        _window = view;
        view.Show();
    }
}
