using System;
using System.Collections.Generic;
using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using HPRebar.BeamRebar.Service;
using HPRebar.KataRebar.View;
using HPRebar.KataRebar.ViewModel;
using JetBrains.Annotations;
using Nice3point.Revit.Toolkit.External;
using Serilog;

namespace HPRebar.KataRebar;

/// <summary>
/// External command entry point for Kata Rebar.
/// Opens the modeless WPF dialog for configuring and generating 3D beam reinforcement from Kata Excel.
/// </summary>
[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public sealed class KataRebarCommand : ExternalCommand
{
    private static KataRebarView? _window;

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

        var filter = new KataRebarSelectionFilter();
        var selectedIds = uiDocument.Selection.GetElementIds();
        var initialBeamIds = new List<ElementId>();

        foreach (var id in selectedIds)
        {
            var elem = document.GetElement(id);
            if (elem is not null && filter.AllowElement(elem))
            {
                initialBeamIds.Add(id);
            }
        }

        try
        {
            var handler = new KataRebarExternalEventHandler();
            var viewModel = new KataRebarViewModel(document, initialBeamIds, handler);
            var view = new KataRebarView(viewModel);

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
            Log.Error(ex, "Lỗi khởi chạy Kata Rebar");
            RevitDialogs.Error("Kata Rebar", $"Không thể mở cửa sổ Kata Rebar: {ex.Message}");
        }
    }
}
