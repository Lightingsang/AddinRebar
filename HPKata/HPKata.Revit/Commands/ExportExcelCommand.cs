using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HPKata.Revit.Interop;
using HPKata.Revit.Runners;

namespace HPKata.Revit.Commands;

/// <summary>
/// External command exporting rebar schedules to an Excel workbook.
/// </summary>
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public sealed class ExportExcelCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var context = new RevitDocumentContext(commandData.Application.ActiveUIDocument);
        RevitExportExcelRunner.Execute(context);
        return Result.Succeeded;
    }
}
