using System;
using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using HPRebar.JoinElements.Service;
using HPRebar.JoinElements.View;
using HPRebar.JoinElements.ViewModel;
using JetBrains.Annotations;
using Nice3point.Revit.Toolkit.External;
using Serilog;

namespace HPRebar.JoinElements;

/// <summary>
/// External command entry point for the Auto Join Elements tool.
/// Opens the modeless WPF dialog styled with MaterialDesignInXamlToolkit.
/// </summary>
[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public sealed class JoinElementCommand : ExternalCommand
{
    private static JoinElementView? _window;

    public override void Execute()
    {
        var uiDocument = Application.ActiveUIDocument;
        if (uiDocument is null) return;
        var document = uiDocument.Document;

        if (_window is not null)
        {
            _window.Activate();
            return;
        }

        try
        {
            var service = new JoinElementService();
            var handler = new JoinElementExternalEventHandler(document, service);
            var viewModel = new JoinElementViewModel(handler);
            var view = new JoinElementView(viewModel);

            new WindowInteropHelper(view).Owner = Application.MainWindowHandle;

            view.Closed += (_, _) =>
            {
                handler.Dispose();
                _window = null;
            };

            _window = view;
            view.Show();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Không thể khởi động Auto Join Elements");
        }
    }
}
