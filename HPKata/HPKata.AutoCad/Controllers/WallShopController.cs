using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPKata.Core.Walls.Persistence;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller orchestrating shear wall shop drawing drafting (ve_vach_shop / vvs)
    /// with 11.7m curtailment, lap splices, and U-bar edge closures.
    /// </summary>
    public static class WallShopController
    {
        public static void DrawWallShopInteractive()
        {
            WallDrawController.DrawWallInteractive(isShopMode: true, autoReadExcel: true);
        }

        public static void DrawWallAllShopBatch()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var allWalls = WallTakeoffRegistry.GetAll();
            if (allWalls.Count == 0)
            {
                DrawWallShopInteractive();
                return;
            }

            var ppr = ed.GetPoint("\n[HPKata] Chọn điểm gốc vẽ hàng loạt Shop 11.7m các vách: ");
            if (ppr.Status != PromptStatus.OK) return;

            Point3d currentOrigin = ppr.Value;
            foreach (var wall in allWalls)
            {
                WallDrawController.DrawWallLayoutAtPoint(doc, currentOrigin, wall, isShopMode: true);
                currentOrigin = new Point3d(currentOrigin.X + 16000.0, currentOrigin.Y, 0);
            }
            ed.WriteMessage($"\n[HPKata] Đã vẽ hàng loạt Shop 11.7m cho {allWalls.Count} vách.\n");
        }
    }
}
