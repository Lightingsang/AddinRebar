using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPKata.Core.Beams.Calculators;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller orchestrating shop drawing generation with 11.7m commercial rebar optimization
    /// and compression lap splicing (ve_dam_shop, ve_dam_all_shop).
    /// </summary>
    public static class BeamShopController
    {
        public static void DrawBeamShopInteractive()
        {
            BeamDrawController.DrawContinuousBeamInteractive(isShopMode: true);
        }

        public static void DrawBeamAllShopBatch()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var allBeams = BeamTakeoffRegistry.GetAll();
            if (allBeams.Count == 0)
            {
                DrawBeamShopInteractive();
                return;
            }

            var ppr = ed.GetPoint("\n[HPKata] Chọn điểm gốc vẽ hàng loạt Shop 11.7m các dầm: ");
            if (ppr.Status != PromptStatus.OK) return;

            Point3d currentOrigin = ppr.Value;
            foreach (var beam in allBeams)
            {
                BeamDrawController.DrawBeamLayoutAtPoint(doc, currentOrigin, beam, isShopMode: true);
                currentOrigin = new Point3d(currentOrigin.X, currentOrigin.Y - 2600.0, 0);
            }
            ed.WriteMessage($"\n[HPKata] Đã vẽ hàng loạt Shop 11.7m cho {allBeams.Count} dầm.\n");
        }
    }
}
