using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace HPDcePro.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class CommandDceSetup : IExternalCommand
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
                TaskDialog.Show("DCE PRO", "Cấu hình tham số và thiết lập bản vẽ HPDcePro.");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Reason: Handle configuration setup failure and report diagnostic message
                message = $"CommandDceSetup failed: {ex.Message}";
                return Result.Failed;
            }
        }
    }
}
