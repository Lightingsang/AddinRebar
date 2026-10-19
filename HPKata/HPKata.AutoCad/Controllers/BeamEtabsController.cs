using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPKata.AutoCad.Interop;
using HPKata.Core.Beams.Calculators;
using HPKata.Core.Serialization;
using HPKata.UI.ViewModels;
using HPKata.UI.Views;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller orchestrating ETABS live connection, loadbeam import, and slab load distribution
    /// (Etabs, loadbeam, GanTai, HoatTai).
    /// </summary>
    public static class BeamEtabsController
    {
        public static void ConnectEtabsAndImport()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var vm = new EtabsImportViewModel();
            var window = new EtabsImportWindow(vm);
            AcadApp.ShowModalWindow(window);

            if (vm.ResultLayouts.Count == 0) return;

            var ppr = ed.GetPoint($"\n[HPKata] Chọn điểm gốc vẽ {vm.ResultLayouts.Count} dầm nhập từ ETABS: ");
            if (ppr.Status != PromptStatus.OK) return;

            Point3d currentOrigin = ppr.Value;
            foreach (var layout in vm.ResultLayouts)
            {
                BeamTakeoffRegistry.Register(layout);
                BeamDrawController.DrawBeamLayoutAtPoint(doc, currentOrigin, layout, isShopMode: false);
                currentOrigin = new Point3d(currentOrigin.X, currentOrigin.Y - 2200.0, 0);
            }
            ed.WriteMessage($"\n[HPKata] Đã nhập và vẽ hoàn chỉnh {vm.ResultLayouts.Count} dầm từ ETABS.\n");
        }

        public static void LoadFromExcelKataDam()
        {
            BeamDrawController.DrawContinuousBeamInteractive(isShopMode: false, autoReadExcel: true);
        }

        public static void AssignDeadLoad()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var pDbl = ed.GetDouble(new PromptDoubleOptions("\n[HPKata] Enter distributed dead load q (kN/m): ") { DefaultValue = 12.0 });
            if (pDbl.Status != PromptStatus.OK) return;
            double load = pDbl.Value;

            var psr = ed.GetSelection();
            if (psr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\n[HPKata] GanTai: No beam elements selected.\n");
                return;
            }

            AttachLoadXData(doc, psr.Value.GetObjectIds(), "DEAD_LOAD", load);
            ed.WriteMessage($"\n[HPKata] GanTai: Assigned dead load {load:F1} kN/m to {psr.Value.Count} selected beam elements.\n");
        }

        public static void AssignLiveLoad()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var pDbl = ed.GetDouble(new PromptDoubleOptions("\n[HPKata] Enter distributed live load p (kN/m): ") { DefaultValue = 5.0 });
            if (pDbl.Status != PromptStatus.OK) return;
            double load = pDbl.Value;

            var psr = ed.GetSelection();
            if (psr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\n[HPKata] HoatTai: No beam elements selected.\n");
                return;
            }

            AttachLoadXData(doc, psr.Value.GetObjectIds(), "LIVE_LOAD", load);
            ed.WriteMessage($"\n[HPKata] HoatTai: Assigned live load {load:F1} kN/m to {psr.Value.Count} selected beam elements.\n");
        }

        private static void AttachLoadXData(Document doc, ObjectId[] ids, string loadType, double intensity)
        {
            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                XDataService.EnsureRegApp(doc.Database, KataXDataRecord.RegAppName);
                foreach (var id in ids)
                {
                    var ent = tr.GetObject(id, OpenMode.ForWrite) as Entity;
                    if (ent == null) continue;

                    var record = XDataService.ReadXData(ent);
                    record.ChuThich = $"{loadType}={intensity:F1} kN/m";
                    XDataService.WriteXData(ent, record);
                }
                tr.Commit();
            }
        }
    }
}
