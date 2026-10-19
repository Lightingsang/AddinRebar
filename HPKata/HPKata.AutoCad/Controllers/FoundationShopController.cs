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
    /// Controller orchestrating stepped pile cap detailing (ve_mong_coc_giat_cap).
    /// </summary>
    public static class FoundationShopController
    {
        public static void DrawSteppedPileCapInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata] Pick insertion point for stepped pile cap (ve_mong_coc_giat_cap): ");
            if (ppr.Status != PromptStatus.OK) return;

            var (piles, l, b) = PileCoordinateGenerator.GenerateLayout(5, 400.0);
            var cap = new PileCapModel
            {
                CapMark = "ĐC-GC1",
                LengthMm = l,
                WidthMm = b,
                TotalHeightMm = 1100.0,
                IsStepped = true,
                Tier2LengthMm = l - 600.0,
                Tier2WidthMm = b - 600.0,
                Tier2HeightMm = 450.0,
                Piles = piles
            };

            var layout = FoundationRebarLayout.FromPileCap(cap);
            FoundationTakeoffRegistry.Register(layout);

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                FoundationDrawController.DrawPileCapPlan(ms, tr, ppr.Value, cap);
                DrawSteppedTierOutline(ms, tr, ppr.Value, cap);

                tr.Commit();
                ed.WriteMessage($"\n[HPKata] Successfully drafted stepped pile cap '{cap.CapMark}'.\n");
            }
        }

        private static void DrawSteppedTierOutline(BlockTableRecord ms, Transaction tr, Point3d origin, PileCapModel cap)
        {
            double halfL2 = cap.Tier2LengthMm / 2.0;
            double halfB2 = cap.Tier2WidthMm / 2.0;

            var pline = new Polyline(4);
            pline.AddVertexAt(0, new Point2d(origin.X - halfL2, origin.Y - halfB2), 0, 0, 0);
            pline.AddVertexAt(1, new Point2d(origin.X + halfL2, origin.Y - halfB2), 0, 0, 0);
            pline.AddVertexAt(2, new Point2d(origin.X + halfL2, origin.Y + halfB2), 0, 0, 0);
            pline.AddVertexAt(3, new Point2d(origin.X - halfL2, origin.Y + halfB2), 0, 0, 0);
            pline.Closed = true;

            ms.AppendEntity(pline);
            tr.AddNewlyCreatedDBObject(pline, true);
        }
    }
}
