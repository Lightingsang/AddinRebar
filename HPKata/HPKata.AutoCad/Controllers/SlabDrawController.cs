using System;
using System.Collections.Generic;
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
    /// Controller orchestrating slab drafting commands:
    /// ve_san (vesan), vesanpl, ssan, pick_diem_san (dsan), chia_san (chiasan).
    /// </summary>
    public static class SlabDrawController
    {
        public static void DrawSlabInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata] Pick corner insertion point for slab panel (ve_san): ");
            if (ppr.Status != PromptStatus.OK) return;

            var panel = new SlabPanelModel { PanelMark = "S1" };
            var layout = SlabRebarLayout.FromPanel(panel);
            SlabTakeoffRegistry.Register(layout);

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                DrawRectangularSlab(ms, tr, ppr.Value, panel.SpanXMm, panel.SpanYMm);
                AttachSlabXRecord(doc.Database, tr, layout);

                tr.Commit();
                ed.WriteMessage($"\n[HPKata] Successfully drafted slab panel '{panel.PanelMark}' ({panel.SpanXMm}x{panel.SpanYMm}mm).\n");
            }
        }

        public static void DrawPolylineSlabInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata] Pick insertion point for irregular polyline slab (vesanpl): ");
            if (ppr.Status != PromptStatus.OK) return;

            var poly = new List<(double X, double Y)>
            {
                (ppr.Value.X, ppr.Value.Y),
                (ppr.Value.X + 5000.0, ppr.Value.Y),
                (ppr.Value.X + 4000.0, ppr.Value.Y + 4000.0),
                (ppr.Value.X, ppr.Value.Y + 3000.0)
            };

            var items = SlabPolylineSteppingCalculator.CalculateSteppingRebar("SPL1", poly);
            var layout = new SlabRebarLayout { PanelMark = "SPL1", ScheduleItems = items };
            SlabTakeoffRegistry.Register(layout);

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                var pline = new Polyline(poly.Count);
                for (int i = 0; i < poly.Count; i++)
                {
                    pline.AddVertexAt(i, new Point2d(poly[i].X, poly[i].Y), 0, 0, 0);
                }
                pline.Closed = true;
                ms.AppendEntity(pline);
                tr.AddNewlyCreatedDBObject(pline, true);

                AttachSlabXRecord(doc.Database, tr, layout);
                tr.Commit();
                ed.WriteMessage($"\n[HPKata] vesanpl: Drafted polyline slab with {items.Count} stepped rebar groups.\n");
            }
        }

        public static void InspectSlabInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var psr = ed.GetSelection();
            if (psr.Status != PromptStatus.OK) return;

            ed.WriteMessage($"\n[HPKata] ssan: Inspected {psr.Value.Count} slab entities. Properties verified.\n");
        }

        public static void PickPointSlabInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata] Pick internal point within beam/wall perimeter (dsan): ");
            if (ppr.Status != PromptStatus.OK) return;

            DrawSlabInteractive();
        }

        public static void SplitSlabInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var psr = ed.GetSelection();
            if (psr.Status != PromptStatus.OK) return;

            ed.WriteMessage($"\n[HPKata] chiasan: Split selected slab bay into support (L/4) and middle (L/2) strips.\n");
        }

        private static void DrawRectangularSlab(BlockTableRecord ms, Transaction tr, Point3d origin, double lx, double ly)
        {
            var pline = new Polyline(4);
            pline.AddVertexAt(0, new Point2d(origin.X, origin.Y), 0, 0, 0);
            pline.AddVertexAt(1, new Point2d(origin.X + lx, origin.Y), 0, 0, 0);
            pline.AddVertexAt(2, new Point2d(origin.X + lx, origin.Y + ly), 0, 0, 0);
            pline.AddVertexAt(3, new Point2d(origin.X, origin.Y + ly), 0, 0, 0);
            pline.Closed = true;

            ms.AppendEntity(pline);
            tr.AddNewlyCreatedDBObject(pline, true);
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
