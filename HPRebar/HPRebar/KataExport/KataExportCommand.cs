using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using HPRebar.KataExport.Service;
using HPRebar.KataExport.View;
using HPRebar.KataExport.ViewModel;
using HPRebar.KataRebar.Service;
using JetBrains.Annotations;
using Nice3point.Revit.Toolkit.External;
using Serilog;

namespace HPRebar.KataExport;

/// <summary>
/// Entry point for the Kata Export tool.
/// Reads the selected continuous beam chain, opens the modeless preview window,
/// and exports geometric span/support data to KATA Excel.
/// </summary>
[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public sealed class KataExportCommand : ExternalCommand
{
    private static KataExportView? _window;

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

        var filter = new KataExportSelectionFilter();
        var selectedIds = uiDocument.Selection.GetElementIds();
        var beams = new List<Element>();

        foreach (var id in selectedIds)
        {
            var elem = document.GetElement(id);
            if (elem is not null && filter.AllowElement(elem))
            {
                beams.Add(elem);
            }
        }

        // If no framing elements are pre-selected, prompt user to pick them
        if (beams.Count == 0)
        {
            try
            {
                var refs = uiDocument.Selection.PickObjects(
                    ObjectType.Element,
                    filter,
                    "Chọn dải dầm liên tục trong Revit (nhấn Finish khi xong)");

                if (refs is null || refs.Count == 0) return;

                beams = refs.Select(r => document.GetElement(r)).Where(e => e is not null).ToList()!;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // User pressed Escape
                return;
            }
        }

        if (beams.Count == 0) return;

        try
        {
            var session = KataSessionReader.Read(document, uiDocument.ActiveView, beams);
            if (session.Pieces.Count == 0)
            {
                RevitDialogs.Warning("Kata Export", "Không trích xuất được đoạn dầm hợp lệ nào từ các phần tử đã chọn.");
                return;
            }

            var handler = new KataExportExternalEventHandler(document, uiDocument.ActiveView);
            var viewModel = new KataExportViewModel(session, handler, new KataRebarTypeResolver(document));
            var view = new KataExportView(viewModel);

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
            Log.Error(ex, "Kata Export: Lỗi khi đọc dữ liệu dầm");
            RevitDialogs.Error("Kata Export", $"Không thể đọc thông tin dải dầm đã chọn:{Environment.NewLine}{ex.Message}");
        }
    }
}
