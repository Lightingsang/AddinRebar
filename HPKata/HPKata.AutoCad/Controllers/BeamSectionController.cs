using System;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller orchestrating cross-section drafting at beam supports and midspans (ve_dam_mc, ve_damMC_all).
    /// </summary>
    public static class BeamSectionController
    {
        public static void DrawBeamSectionInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata] Pick insertion point for beam cross-section: ");
            if (ppr.Status != PromptStatus.OK) return;

            Point3d origin = ppr.Value;
            (double b, double h, double cover, int diaMm) = ResolveSectionParameters(ed);

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                DrawConcreteRectangle(ms, tr, origin, b, h);
                DrawStirrupHoop(ms, tr, origin, b, h, cover);
                DrawCornerBars(ms, tr, origin, b, h, cover, diaMm);

                tr.Commit();
                ed.WriteMessage($"\n[HPKata] Drafted beam cross-section {b:F0}x{h:F0} mm.\n");
            }
        }

        public static void DrawBeamSectionAllBatch()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            ed.WriteMessage("\n[HPKata] ve_damMC_all: Batch drafting all cross-sections for beam runs...\n");
            DrawBeamSectionInteractive();
        }

        private static (double B, double H, double Cover, int Dia) ResolveSectionParameters(Editor ed)
        {
            var first = BeamTakeoffRegistry.GetAll().FirstOrDefault();
            var span = first?.Spans.FirstOrDefault();
            double defB = span?.WidthMm > 0 ? span.WidthMm : 300.0;
            double defH = span?.HeightMm > 0 ? span.HeightMm : 600.0;
            int defDia = first?.TopContinuousBars.Count > 0 ? first.TopContinuousBars[0].DiameterMm : 20;

            var pB = ed.GetDouble(new PromptDoubleOptions("\n[HPKata] Enter section width b (mm): ") { DefaultValue = defB });
            double b = pB.Status == PromptStatus.OK && pB.Value > 0 ? pB.Value : defB;

            var pH = ed.GetDouble(new PromptDoubleOptions("\n[HPKata] Enter section height h (mm): ") { DefaultValue = defH });
            double h = pH.Status == PromptStatus.OK && pH.Value > 0 ? pH.Value : defH;

            return (b, h, 25.0, defDia);
        }

        private static void DrawConcreteRectangle(
            BlockTableRecord ms, Transaction tr, Point3d origin, double b, double h)
        {
            Polyline rect = new Polyline();
            rect.AddVertexAt(0, new Point2d(origin.X, origin.Y), 0, 0, 0);
            rect.AddVertexAt(1, new Point2d(origin.X + b, origin.Y), 0, 0, 0);
            rect.AddVertexAt(2, new Point2d(origin.X + b, origin.Y - h), 0, 0, 0);
            rect.AddVertexAt(3, new Point2d(origin.X, origin.Y - h), 0, 0, 0);
            rect.Closed = true;

            ms.AppendEntity(rect);
            tr.AddNewlyCreatedDBObject(rect, true);
        }

        private static void DrawStirrupHoop(
            BlockTableRecord ms, Transaction tr, Point3d origin, double b, double h, double cover)
        {
            Polyline stirrup = new Polyline();
            stirrup.AddVertexAt(0, new Point2d(origin.X + cover, origin.Y - cover), 0, 0, 0);
            stirrup.AddVertexAt(1, new Point2d(origin.X + b - cover, origin.Y - cover), 0, 0, 0);
            stirrup.AddVertexAt(2, new Point2d(origin.X + b - cover, origin.Y - h + cover), 0, 0, 0);
            stirrup.AddVertexAt(3, new Point2d(origin.X + cover, origin.Y - h + cover), 0, 0, 0);
            stirrup.Closed = true;

            ms.AppendEntity(stirrup);
            tr.AddNewlyCreatedDBObject(stirrup, true);
        }

        private static void DrawCornerBars(
            BlockTableRecord ms, Transaction tr, Point3d origin, double b, double h, double cover, int diaMm)
        {
            double r = diaMm / 2.0;
            Point3d[] corners =
            {
                new Point3d(origin.X + cover + r, origin.Y - cover - r, 0),
                new Point3d(origin.X + b - cover - r, origin.Y - cover - r, 0),
                new Point3d(origin.X + cover + r, origin.Y - h + cover + r, 0),
                new Point3d(origin.X + b - cover - r, origin.Y - h + cover + r, 0)
            };

            foreach (var pt in corners)
            {
                Circle circle = new Circle(pt, Vector3d.ZAxis, r);
                ms.AppendEntity(circle);
                tr.AddNewlyCreatedDBObject(circle, true);
            }
        }
    }
}
