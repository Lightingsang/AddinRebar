using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPKata.Core.Columns.Models;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller orchestrating column block marker placement at grid intersections (raicot).
    /// </summary>
    public static class ColumnPlacementController
    {
        public static void PlaceColumnBlocksInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata] Pick insertion point for column location (raicot): ");
            if (ppr.Status != PromptStatus.OK) return;

            Point3d pt = ppr.Value;
            double b = 400.0;
            double h = 400.0;
            string mark = "C1";

            var layout = ColumnDrawController.CreateDefaultLayout();
            layout.ColumnMark = mark;
            ColumnTakeoffRegistry.Register(layout);

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                var poly = new Polyline(4);
                poly.AddVertexAt(0, new Point2d(pt.X - b / 2.0, pt.Y - h / 2.0), 0, 0, 0);
                poly.AddVertexAt(1, new Point2d(pt.X + b / 2.0, pt.Y - h / 2.0), 0, 0, 0);
                poly.AddVertexAt(2, new Point2d(pt.X + b / 2.0, pt.Y + h / 2.0), 0, 0, 0);
                poly.AddVertexAt(3, new Point2d(pt.X - b / 2.0, pt.Y + h / 2.0), 0, 0, 0);
                poly.Closed = true;

                ms.AppendEntity(poly);
                tr.AddNewlyCreatedDBObject(poly, true);

                var txt = new DBText
                {
                    Position = new Point3d(pt.X + b / 2.0 + 50.0, pt.Y, 0),
                    Height = 150.0,
                    TextString = $"{mark} {b}x{h}"
                };
                ms.AppendEntity(txt);
                tr.AddNewlyCreatedDBObject(txt, true);

                tr.Commit();
                ed.WriteMessage($"\n[HPKata] raicot: Placed column marker '{mark}' ({b}x{h}) at ({pt.X:F0}, {pt.Y:F0}).\n");
            }
        }
    }
}
