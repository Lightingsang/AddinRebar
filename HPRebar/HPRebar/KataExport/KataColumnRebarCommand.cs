using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using HPRebar.Core.KataExport.ColumnStack;
using HPRebar.KataExport.Service;
using HPRebar.KataExport.View.Web.ColumnExport;
using HPRebar.Shared.Revit;
using JetBrains.Annotations;
using Nice3point.Revit.Toolkit.External;

namespace HPRebar.KataExport;

/// <summary>
/// Ribbon entry for Kata Column Rebar (HPRebar ▸ Rebar ▸ Kata Column Rebar, Revit 2026 only: a web page, ADR-0014),
/// the column counterpart of Kata Export: the user picks the columns of one stack once; the page writes their geometry
/// into sheet Cot of the Kata workbook open in Excel, reads the bars typed there back and creates them on the Revit
/// faces. The stack is measured here, on Revit's API thread, before the window opens. Modal: its handlers run inside
/// this command on Revit's thread, so the creation needs no external event and the model cannot change under the plan.
/// </summary>
[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public sealed class KataColumnRebarCommand : ExternalCommand
{
    private const string Title = "Kata Column Rebar";

    public override void Execute()
    {
        var uiDocument = Application.ActiveUIDocument;
        if (uiDocument is null)
        {
            return;
        }

        var columns = PreselectionPicker.PickElements(uiDocument, new KataColumnSelectionFilter(),
                "Chọn các cột của một trục cột, từ móng lên mái (nhấn Finish khi xong)")
            .OfType<FamilyInstance>()
            .ToList();
        if (columns.Count == 0)
        {
            return;
        }

        var document = uiDocument.Document;
        var reading = KataColumnStackReader.Read(document, columns);
        if (reading.Input is null)
        {
            RevitDialogs.Error(Title, reading.Error ?? "Không đọc được chồng cột.");
            return;
        }

        KataColumnSheet sheet;
        try
        {
            sheet = KataColumnSheetBuilder.Build(reading.Input);
            sheet = sheet with { Warnings = reading.Warnings.Concat(sheet.Warnings).ToList() };
        }
        catch (ArgumentException ex)
        {
            RevitDialogs.Error(Title, $"Chồng cột không ghi được sang Kata: {ex.Message}");
            return;
        }

        var window = new KataColumnWebWindow(reading.Input, sheet, new KataColumnRebarSession(document, reading));
        new WindowInteropHelper(window).Owner = Application.MainWindowHandle;
        window.ShowDialog();
    }
}
