using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPKata.Core.Stairs.Calculators;
using HPKata.Core.Stairs.Models;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller orchestrating universal generic section cuts (vemc) across structural elements.
    /// </summary>
    public static class SectionCutController
    {
        public static void DrawGenericSectionInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata] Pick insertion point for cross section (vemc): ");
            if (ppr.Status != PromptStatus.OK) return;

            var sec = GenericSectionCalculator.CreateRectangularSection(
                "MC1", 300.0, 600.0, 25.0, topBarCount: 3, topBarDia: 18, botBarCount: 3, botBarDia: 20, stirrupDia: 8);
            StairTakeoffRegistry.RegisterSection(sec);

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                DrawSectionEntities(ms, tr, ppr.Value, sec);

                tr.Commit();
                ed.WriteMessage($"\n[HPKata] vemc: Successfully drafted cross section '{sec.SectionTitle}' ({sec.WidthMm}x{sec.HeightMm}mm).\n");
            }
        }

        public static void DrawSectionEntities(BlockTableRecord ms, Transaction tr, Point3d origin, GenericSectionModel sec)
        {
            // Concrete outline
            var pline = new Polyline(4);
            pline.AddVertexAt(0, new Point2d(origin.X, origin.Y), 0, 0, 0);
            pline.AddVertexAt(1, new Point2d(origin.X + sec.WidthMm, origin.Y), 0, 0, 0);
            pline.AddVertexAt(2, new Point2d(origin.X + sec.WidthMm, origin.Y + sec.HeightMm), 0, 0, 0);
            pline.AddVertexAt(3, new Point2d(origin.X, origin.Y + sec.HeightMm), 0, 0, 0);
            pline.Closed = true;

            ms.AppendEntity(pline);
            tr.AddNewlyCreatedDBObject(pline, true);

            // Rebar circles
            foreach (var b in sec.LongitudinalBars)
            {
                var pt = new Point3d(origin.X + b.X, origin.Y + b.Y, 0);
                var circ = new Circle(pt, Vector3d.ZAxis, b.DiameterMm / 2.0);
                ms.AppendEntity(circ);
                tr.AddNewlyCreatedDBObject(circ, true);
            }
        }
    }
}
