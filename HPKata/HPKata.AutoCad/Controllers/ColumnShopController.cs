using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPKata.Core.Columns.Calculators;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller orchestrating column shop drawing drafting (ve_cot_shop / vcs)
    /// with 11.7m stock bar division and staggered lap splices.
    /// </summary>
    public static class ColumnShopController
    {
        public static void DrawColumnShopInteractive()
        {
            ColumnDrawController.DrawColumnInteractive(isShopMode: true, autoReadExcel: true);
        }

        public static void DrawColumnAllShopBatch()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var allCols = ColumnTakeoffRegistry.GetAll();
            if (allCols.Count == 0)
            {
                DrawColumnShopInteractive();
                return;
            }

            var ppr = ed.GetPoint("\n[HPKata] Chọn điểm gốc vẽ hàng loạt Shop 11.7m các cột: ");
            if (ppr.Status != PromptStatus.OK) return;

            Point3d currentOrigin = ppr.Value;
            foreach (var col in allCols)
            {
                ColumnDrawController.DrawColumnLayoutAtPoint(doc, currentOrigin, col, isShopMode: true);
                currentOrigin = new Point3d(currentOrigin.X + 10000.0, currentOrigin.Y, 0);
            }
            ed.WriteMessage($"\n[HPKata] Đã vẽ hàng loạt Shop 11.7m cho {allCols.Count} cột.\n");
        }
    }
}
