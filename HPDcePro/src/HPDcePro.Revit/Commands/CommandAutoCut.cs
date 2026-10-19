using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace HPDcePro.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class CommandAutoCut : IExternalCommand
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
                using var trans = new Transaction(doc, "HPDcePro: Auto Cut Giao Cấu Kiện");
                trans.Start();
                // Execution logic hooks to RevitJoinCutPlanner
                trans.Commit();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Reason: Handle auto cut failure and report diagnostic message
                message = $"CommandAutoCut failed: {ex.Message}";
                return Result.Failed;
            }
        }
    }
}
