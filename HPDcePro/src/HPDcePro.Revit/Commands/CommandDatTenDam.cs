using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace HPDcePro.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class CommandDatTenDam : IExternalCommand
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
                using var trans = new Transaction(doc, "HPDcePro: Đặt Tên Dầm Tự Động");
                trans.Start();
                // Execution logic hooks to RevitNamingGenerator
                trans.Commit();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Reason: Handle beam naming failure and report diagnostic message
                message = $"CommandDatTenDam failed: {ex.Message}";
                return Result.Failed;
            }
        }
    }
}
