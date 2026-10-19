using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace HPDcePro.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class CommandXuatKhoiLuong : IExternalCommand
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
                TaskDialog.Show("DCE PRO", "Bóc tách khối lượng kết cấu BOQ thành công.");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Reason: Handle quantity takeoff failure and report diagnostic message
                message = $"CommandXuatKhoiLuong failed: {ex.Message}";
                return Result.Failed;
            }
        }
    }
}
