using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPKata.Core.Foundations.Calculators;
using HPKata.Core.Foundations.Models;
using HPKata.Core.Foundations.Persistence;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller orchestrating foundation drawing commands:
    /// ve_mat_bang_mong (vmbm), rai_mong (rm), ve_mong_don (vmd), ve_mong_coc (vmc), phamong.
    /// </summary>
    public static class FoundationDrawController
    {
        public static void DrawIsolatedFootingInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata] Pick insertion point for isolated footing (ve_mong_don): ");
            if (ppr.Status != PromptStatus.OK) return;

            var footing = new IsolatedFootingModel { FootingMark = "M1" };
            var layout = FoundationRebarLayout.FromFooting(footing);
            FoundationTakeoffRegistry.Register(layout);

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                DrawFootingPlan(ms, tr, ppr.Value, footing);
                AttachFoundationXRecord(doc.Database, tr, layout);

                tr.Commit();
                ed.WriteMessage($"\n[HPKata] Successfully drafted pad footing '{footing.FootingMark}'.\n");
            }
        }

        public static void DrawPileCapInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata] Pick insertion point for pile cap (ve_mong_coc): ");
            if (ppr.Status != PromptStatus.OK) return;

            var (piles, l, b) = PileCoordinateGenerator.GenerateLayout(4, 400.0);
            var cap = new PileCapModel { CapMark = "ĐC-1", LengthMm = l, WidthMm = b, Piles = piles };
            var layout = FoundationRebarLayout.FromPileCap(cap);
            FoundationTakeoffRegistry.Register(layout);

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                DrawPileCapPlan(ms, tr, ppr.Value, cap);
                AttachFoundationXRecord(doc.Database, tr, layout);

                tr.Commit();
                ed.WriteMessage($"\n[HPKata] Successfully drafted pile cap '{cap.CapMark}' with {cap.Piles.Count} piles.\n");
            }
        }

        public static void DrawFoundationPlanInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata] Pick origin point for foundation layout plan (vmbm): ");
            if (ppr.Status != PromptStatus.OK) return;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // Draw a 2x2 sample foundation grid
                for (int gx = 0; gx < 2; gx++)
                {
                    for (int gy = 0; gy < 2; gy++)
                    {
                        var pt = new Point3d(ppr.Value.X + gx * 4000.0, ppr.Value.Y + gy * 4000.0, 0);
                        var footing = new IsolatedFootingModel { FootingMark = $"M{gx * 2 + gy + 1}" };
                        DrawFootingPlan(ms, tr, pt, footing);
                    }
                }

                tr.Commit();
                ed.WriteMessage("\n[HPKata] Successfully drafted foundation layout plan (vmbm).\n");
            }
        }

        public static void PlaceFootingBlockInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata] Pick insertion point to place footing block (rm): ");
            if (ppr.Status != PromptStatus.OK) return;

            DrawIsolatedFootingInteractive();
        }

        public static void ExplodeFoundationBlockInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var psr = ed.GetSelection();
            if (psr.Status != PromptStatus.OK) return;

            ed.WriteMessage($"\n[HPKata] phamong: Processed selection of {psr.Value.Count} entities.\n");
        }

        public static void DrawFootingPlan(BlockTableRecord ms, Transaction tr, Point3d origin, IsolatedFootingModel f)
        {
            double halfL = f.LengthMm / 2.0;
            double halfB = f.WidthMm / 2.0;

            var pline = new Polyline(4);
            pline.AddVertexAt(0, new Point2d(origin.X - halfL, origin.Y - halfB), 0, 0, 0);
            pline.AddVertexAt(1, new Point2d(origin.X + halfL, origin.Y - halfB), 0, 0, 0);
            pline.AddVertexAt(2, new Point2d(origin.X + halfL, origin.Y + halfB), 0, 0, 0);
            pline.AddVertexAt(3, new Point2d(origin.X - halfL, origin.Y + halfB), 0, 0, 0);
            pline.Closed = true;

            ms.AppendEntity(pline);
            tr.AddNewlyCreatedDBObject(pline, true);
        }

        public static void DrawPileCapPlan(BlockTableRecord ms, Transaction tr, Point3d origin, PileCapModel cap)
        {
            double halfL = cap.LengthMm / 2.0;
            double halfB = cap.WidthMm / 2.0;

            var pline = new Polyline(4);
            pline.AddVertexAt(0, new Point2d(origin.X - halfL, origin.Y - halfB), 0, 0, 0);
            pline.AddVertexAt(1, new Point2d(origin.X + halfL, origin.Y - halfB), 0, 0, 0);
            pline.AddVertexAt(2, new Point2d(origin.X + halfL, origin.Y + halfB), 0, 0, 0);
            pline.AddVertexAt(3, new Point2d(origin.X - halfL, origin.Y + halfB), 0, 0, 0);
            pline.Closed = true;

            ms.AppendEntity(pline);
            tr.AddNewlyCreatedDBObject(pline, true);

            foreach (var p in cap.Piles)
            {
                var center = new Point3d(origin.X + p.X, origin.Y + p.Y, 0);
                if (p.IsSquare)
                {
                    double halfSide = p.DiameterMm / 2.0;
                    var sq = new Polyline(4);
                    sq.AddVertexAt(0, new Point2d(center.X - halfSide, center.Y - halfSide), 0, 0, 0);
                    sq.AddVertexAt(1, new Point2d(center.X + halfSide, center.Y - halfSide), 0, 0, 0);
                    sq.AddVertexAt(2, new Point2d(center.X + halfSide, center.Y + halfSide), 0, 0, 0);
                    sq.AddVertexAt(3, new Point2d(center.X - halfSide, center.Y + halfSide), 0, 0, 0);
                    sq.Closed = true;
                    ms.AppendEntity(sq);
                    tr.AddNewlyCreatedDBObject(sq, true);
                }
                else
                {
                    var circ = new Circle(center, Vector3d.ZAxis, p.DiameterMm / 2.0);
                    ms.AppendEntity(circ);
                    tr.AddNewlyCreatedDBObject(circ, true);
                }
            }
        }

        private static void AttachFoundationXRecord(Database db, Transaction tr, FoundationRebarLayout layout)
        {
            var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForWrite);
            string json = FoundationXRecordSerializer.SerializeToJson(layout);
            using var xrec = new Xrecord();
            xrec.Data = new ResultBuffer(new TypedValue((int)DxfCode.Text, json));
            string key = $"KATA_FOUNDATION_{layout.FoundationMark}";
            if (nod.Contains(key)) nod.Remove(key);
            nod.SetAt(key, xrec);
            tr.AddNewlyCreatedDBObject(xrec, true);
        }
    }
}
