using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace HPDcePro.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class CommandCreateCope : IExternalCommand
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
                using var trans = new Transaction(doc, "HPDcePro: Vát Mộng Dầm (Create Cope)");
                trans.Start();
                trans.Commit();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Reason: Handle coping failure and report diagnostic message
                message = $"CommandCreateCope failed: {ex.Message}";
                return Result.Failed;
            }
        }
    }
}
