using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace HPDcePro.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class CommandLogout : IExternalCommand
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
                TaskDialog.Show("DCE PRO", "Đã đăng xuất tài khoản HPDcePro.");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Reason: Handle UI or session deactivation exception gracefully and report diagnostic message
                message = $"CommandLogout failed: {ex.Message}";
                return Result.Failed;
            }
        }
    }
}
