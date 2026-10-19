namespace HPDynamo.McpBridge.Entry;

using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HPDynamo.McpBridge.View;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.ViewModel;

/// <summary>
///     Ribbon entry: opens the modeless status window over the bridge that Application started.
///     The window never owns the bridge — the listener keeps serving with the window closed —
///     so closing it only drops the window reference.
/// </summary>
[Transaction(TransactionMode.Manual)]
public sealed class ShowStatusDialogCommand : IExternalCommand
{
    private static DynamoBridgeStatusView? _window;

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        ShowWindow(commandData.Application.MainWindowHandle);
        return Result.Succeeded;
    }

    public static void ShowWindow(IntPtr? ownerHwnd = null)
    {
        var app = System.Windows.Application.Current;
        var dispatcher = app?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        void OpenCore()
        {
            if (_window is not null)
            {
                _window.Activate();
                return;
            }

            var host = DynamoRevitApp.BridgeHost ?? McpBridgeHost.Current;
            if (host is null) return;

            var viewModel = new McpBridgeStatusViewModel(host, action => dispatcher.InvokeAsync(action), Clipboard.SetText);
            var view = new DynamoBridgeStatusView(viewModel);

            var owner = ownerHwnd ?? DynamoRevitApp.MainWindowHandle;
            if (owner != IntPtr.Zero)
            {
                new WindowInteropHelper(view).Owner = owner;
            }

            view.Closed += (_, _) =>
            {
                viewModel.Detach();
                _window = null;
            };

            _window = view;
            view.Show();
        }

        if (dispatcher.CheckAccess())
        {
            OpenCore();
        }
        else
        {
            dispatcher.InvokeAsync(OpenCore);
        }
    }
}
