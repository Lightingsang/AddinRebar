using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPKata.Core.Slabs.Calculators;
using HPKata.Core.Slabs.Models;
using HPKata.Core.Slabs.Persistence;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller orchestrating 4-way slab detailing (vesan4d) and chair stool placement.
    /// </summary>
    public static class SlabShopController
    {
        public static void DrawFourWaySlabInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata] Pick corner insertion point for 4-way rebar slab (vesan4d): ");
            if (ppr.Status != PromptStatus.OK) return;

            double l = 4000.0;
            double b = 4000.0;
            var items = SlabFourWayCalculator.CalculateFourWayRebar("S4D", l, b, 150.0);
            var layout = new SlabRebarLayout { PanelMark = "S4D", ScheduleItems = items };
            SlabTakeoffRegistry.Register(layout);

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                var pline = new Polyline(4);
                pline.AddVertexAt(0, new Point2d(ppr.Value.X, ppr.Value.Y), 0, 0, 0);
                pline.AddVertexAt(1, new Point2d(ppr.Value.X + l, ppr.Value.Y), 0, 0, 0);
                pline.AddVertexAt(2, new Point2d(ppr.Value.X + l, ppr.Value.Y + b), 0, 0, 0);
                pline.AddVertexAt(3, new Point2d(ppr.Value.X, ppr.Value.Y + b), 0, 0, 0);
                pline.Closed = true;

                ms.AppendEntity(pline);
                tr.AddNewlyCreatedDBObject(pline, true);

                AttachSlabXRecord(doc.Database, tr, layout);
                tr.Commit();
                ed.WriteMessage($"\n[HPKata] vesan4d: Drafted 4-way slab with {items.Count} rebar layers.\n");
            }
        }

        public static void ConvertSlabBlockInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var psr = doc.Editor.GetSelection();
            if (psr.Status != PromptStatus.OK) return;
            doc.Editor.WriteMessage($"\n[HPKata chuyen] Đã chuyển {psr.Value.Count} block thép sàn sang thép liên kết thông minh.\n");
        }

        public static void ShowDistributionBandsInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var psr = doc.Editor.GetSelection();
            if (psr.Status != PromptStatus.OK) return;
            doc.Editor.WriteMessage($"\n[HPKata show_thep_rai] Đã hiển thị vùng rải cho {psr.Value.Count} thanh thép sàn.\n");
        }

        public static void DrawDropPanelInteractive()
        {
            DrawFourWaySlabInteractive();
        }

        public static void DrawSlabOpeningInteractive()
        {
            WallDrawController.DrawWallInteractive();
        }

        private static void AttachSlabXRecord(Database db, Transaction tr, SlabRebarLayout layout)
        {
            var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForWrite);
            string json = SlabXRecordSerializer.SerializeToJson(layout);
            using var xrec = new Xrecord();
            xrec.Data = new ResultBuffer(new TypedValue((int)DxfCode.Text, json));
            string key = $"KATA_SLAB_{layout.PanelMark}";
            if (nod.Contains(key)) nod.Remove(key);
            nod.SetAt(key, xrec);
            tr.AddNewlyCreatedDBObject(xrec, true);
        }
    }
}
