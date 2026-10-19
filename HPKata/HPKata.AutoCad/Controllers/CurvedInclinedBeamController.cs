using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller orchestrating inclined beam (ve_dam_xien) and horizontally curved beam (ve_dam_cong) drafting.
    /// </summary>
    public static class CurvedInclinedBeamController
    {
        public static void DrawInclinedBeam()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata] Pick start point for inclined beam (ve_dam_xien): ");
            if (ppr.Status != PromptStatus.OK) return;

            Point3d startPt = ppr.Value;
            double horizRun = 4000.0;
            double vertRise = 1500.0;
            double depth = 400.0;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                Polyline pl = new Polyline();
                pl.AddVertexAt(0, new Point2d(startPt.X, startPt.Y), 0, 0, 0);
                pl.AddVertexAt(1, new Point2d(startPt.X + horizRun, startPt.Y + vertRise), 0, 0, 0);
                pl.AddVertexAt(2, new Point2d(startPt.X + horizRun, startPt.Y + vertRise - depth), 0, 0, 0);
                pl.AddVertexAt(3, new Point2d(startPt.X, startPt.Y - depth), 0, 0, 0);
                pl.Closed = true;

                ms.AppendEntity(pl);
                tr.AddNewlyCreatedDBObject(pl, true);
                tr.Commit();

                double slopeLength = Math.Sqrt(horizRun * horizRun + vertRise * vertRise);
                double angleDeg = Math.Atan2(vertRise, horizRun) * 180.0 / Math.PI;
                ed.WriteMessage($"\n[HPKata] Drafted inclined beam: Run={horizRun:F0}, Rise={vertRise:F0}, Length={slopeLength:F0} mm, Angle={angleDeg:F1}°.\n");
            }
        }

        public static void DrawCurvedBeam()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata] Pick center point for curved beam arc (ve_dam_cong): ");
            if (ppr.Status != PromptStatus.OK) return;

            Point3d center = ppr.Value;
            double radius = 5000.0;
            double sweepDeg = 60.0;
            double width = 300.0;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // Draw outer and inner arc boundary
                Arc outerArc = new Arc(center, radius + (width / 2.0), 0.0, sweepDeg * Math.PI / 180.0);
                Arc innerArc = new Arc(center, radius - (width / 2.0), 0.0, sweepDeg * Math.PI / 180.0);

                ms.AppendEntity(outerArc);
                tr.AddNewlyCreatedDBObject(outerArc, true);

                ms.AppendEntity(innerArc);
                tr.AddNewlyCreatedDBObject(innerArc, true);

                tr.Commit();
                double arcLength = Math.PI * radius * sweepDeg / 180.0;
                ed.WriteMessage($"\n[HPKata] Drafted curved beam: R={radius:F0} mm, Sweep={sweepDeg:F1}°, Developed Length={arcLength:F0} mm.\n");
            }
        }
    }
}
