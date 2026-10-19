using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller orchestrating automated dimensioning of columns and grid lines (dimcot).
    /// </summary>
    public static class ColumnDimensionController
    {
        public static void DimColumnGridsInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata] Pick center of column to dimension (dimcot): ");
            if (ppr.Status != PromptStatus.OK) return;

            Point3d center = ppr.Value;
            double halfB = 200.0;
            double halfH = 200.0;
            double offset = 300.0;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // Horizontal dimension along width b
                Point3d p1 = new(center.X - halfB, center.Y - halfH, 0);
                Point3d p2 = new(center.X + halfB, center.Y - halfH, 0);
                Point3d dimLineH = new(center.X, center.Y - halfH - offset, 0);

                var dimH = new RotatedDimension(0.0, p1, p2, dimLineH, $"{halfB * 2:F0}", doc.Database.Dimstyle);
                ms.AppendEntity(dimH);
                tr.AddNewlyCreatedDBObject(dimH, true);

                // Vertical dimension along depth h
                Point3d p3 = new(center.X + halfB, center.Y - halfH, 0);
                Point3d p4 = new(center.X + halfB, center.Y + halfH, 0);
                Point3d dimLineV = new(center.X + halfB + offset, center.Y, 0);

                var dimV = new RotatedDimension(Math.PI / 2.0, p3, p4, dimLineV, $"{halfH * 2:F0}", doc.Database.Dimstyle);
                ms.AppendEntity(dimV);
                tr.AddNewlyCreatedDBObject(dimV, true);

                tr.Commit();
                ed.WriteMessage($"\n[HPKata] dimcot: Created horizontal ({halfB * 2:F0}mm) and vertical ({halfH * 2:F0}mm) dimensions.\n");
            }
        }
    }
}
