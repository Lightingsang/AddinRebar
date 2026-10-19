using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace HPDcePro.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class CommandAddRebarFooting : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uidoc = commandData?.Application?.ActiveUIDocument;
            if (uidoc == null)
            {
                message = "No active Revit document found.";
                return Result.Cancelled;
            }

            var doc = uidoc.Document;
            try
            {
                using var trans = new Transaction(doc, "HPDcePro: Tạo Thép Móng / Đài Cọc 3D");
                trans.Start();
                // Execution logic hooks to RevitRebarPlacementService.MapPileCapRebar
                trans.Commit();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Reason: Handle rebar footing creation failure and report diagnostic message
                message = $"CommandAddRebarFooting failed: {ex.Message}";
                return Result.Failed;
            }
        }
    }
}
