using System;
using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using HPRebar.RebarPartition.Service;
using HPRebar.RebarPartition.View;
using HPRebar.RebarPartition.ViewModel;
using JetBrains.Annotations;
using Nice3point.Revit.Toolkit.External;
using Serilog;

namespace HPRebar.RebarPartition;

/// <summary>
/// External command entry point for the Rebar Partition management tool.
/// Opens the modeless WPF dialog styled with MaterialDesignInXamlToolkit.
/// </summary>
[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public sealed class RebarPartitionCommand : ExternalCommand
{
    private static RebarPartitionView? _window;

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
            var service = new RebarPartitionService();
            var handler = new RebarPartitionExternalEventHandler(document, service);
            var viewModel = new RebarPartitionViewModel(handler);
            var view = new RebarPartitionView(viewModel);

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
            Log.Error(ex, "Không thể khởi động Rebar Partition");
        }
    }
}
