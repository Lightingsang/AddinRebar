using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace HPDcePro.Revit
{
    /// <summary>
    /// Revit 2026 external command to export structural framing elements to HPDcePro domain models.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class ExportFramingCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData?.Application;
            var uidoc = uiapp?.ActiveUIDocument;
            if (uidoc == null)
            {
                message = "No active Revit document found.";
                return Result.Cancelled;
            }

            var doc = uidoc.Document;

            try
            {
                var framings = RevitDataExchangeService.CollectAllFramingElements(doc);
                var columns = RevitDataExchangeService.CollectAllColumnElements(doc);

                string info = $"HPDcePro Data Exchange:\n" +
                              $"- Dầm kết cấu (Framing): {framings.Count} cấu kiện\n" +
                              $"- Cột kết cấu (Columns): {columns.Count} cấu kiện\n\n" +
                              $"Dữ liệu sẵn sàng cho HPDcePro Detailing & Calculation.";

                TaskDialog.Show("HPDcePro Revit Integration", info);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Reason: Return diagnostic error message to Revit host when unexpected extraction failure occurs
                message = $"Lỗi khi trích xuất dữ liệu khung kết cấu: {ex.Message}";
                return Result.Failed;
            }
        }
    }
}
