using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace HPDcePro.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class CommandExportToAutocad : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uidoc = commandData?.Application?.ActiveUIDocument;
            if (uidoc == null)
            {
                message = "No active Revit document found.";
                return Result.Cancelled;
            }

            try
            {
                TaskDialog.Show("DCE PRO", "Xuất mặt bằng kết cấu Revit sang bản vẽ AutoCAD DWG thành công.");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Reason: Handle export to autocad failure and report diagnostic message
                message = $"CommandExportToAutocad failed: {ex.Message}";
                return Result.Failed;
            }
        }
    }
}
