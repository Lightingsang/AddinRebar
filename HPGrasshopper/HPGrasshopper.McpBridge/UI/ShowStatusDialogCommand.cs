namespace HPGrasshopper.McpBridge.UI;

using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HPGrasshopper.McpBridge.Host;
using HPRebar.McpBridge.Core.Host;

/// <summary>
///     Ribbon entry: opens the modeless status window over the bridge that Application started.
///     The window never owns the bridge — the listener keeps serving with the window closed.
/// </summary>
[Transaction(TransactionMode.Manual)] public sealed class ShowStatusDialogCommand : IExternalCommand
{
    private static GrasshopperBridgeStatusView? _window;

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

            var host = GrasshopperAddinApplication.BridgeHost ?? McpBridgeHost.Current;
            if (host is null) return;

            var viewModel = new GrasshopperBridgeStatusViewModel(host, action => dispatcher.InvokeAsync(action), Clipboard.SetText);
            var view = new GrasshopperBridgeStatusView(viewModel);

            var owner = ownerHwnd ?? GrasshopperAddinApplication.MainWindowHandle;
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
