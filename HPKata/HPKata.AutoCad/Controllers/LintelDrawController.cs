using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPKata.Core.Stairs.Calculators;
using HPKata.Core.Stairs.Models;
using HPKata.Core.Stairs.Persistence;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller orchestrating lintel drafting (velanhto) over wall openings.
    /// </summary>
    public static class LintelDrawController
    {
        public static void DrawLintelInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata] Pick insertion point for lintel beam (velanhto): ");
            if (ppr.Status != PromptStatus.OK) return;

            var lintel = new LintelModel { LintelMark = "LT1" };
            var items = LintelRebarCalculator.CalculateLintelRebar(lintel);
            StairTakeoffRegistry.RegisterLintel(lintel);

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                DrawLintelElevation(ms, tr, ppr.Value, lintel);
                AttachLintelXRecord(doc.Database, tr, lintel);

                tr.Commit();
                ed.WriteMessage($"\n[HPKata] velanhto: Successfully drafted lintel '{lintel.LintelMark}' (Span {lintel.ClearSpanMm}mm, bearing {lintel.BearingLengthMm}mm).\n");
            }
        }

        public static void DrawLintelElevation(BlockTableRecord ms, Transaction tr, Point3d origin, LintelModel lintel)
        {
            double totalL = lintel.TotalLengthMm;
            double h = lintel.HeightMm;

            var pline = new Polyline(4);
            pline.AddVertexAt(0, new Point2d(origin.X, origin.Y), 0, 0, 0);
            pline.AddVertexAt(1, new Point2d(origin.X + totalL, origin.Y), 0, 0, 0);
            pline.AddVertexAt(2, new Point2d(origin.X + totalL, origin.Y + h), 0, 0, 0);
            pline.AddVertexAt(3, new Point2d(origin.X, origin.Y + h), 0, 0, 0);
            pline.Closed = true;

            ms.AppendEntity(pline);
            tr.AddNewlyCreatedDBObject(pline, true);
        }

        private static void AttachLintelXRecord(Database db, Transaction tr, LintelModel lintel)
        {
            var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForWrite);
            string json = LintelXRecordSerializer.SerializeToJson(lintel);
            using var xrec = new Xrecord();
            xrec.Data = new ResultBuffer(new TypedValue((int)DxfCode.Text, json));
            string key = $"KATA_LINTEL_{lintel.LintelMark}";
            if (nod.Contains(key)) nod.Remove(key);
            nod.SetAt(key, xrec);
            tr.AddNewlyCreatedDBObject(xrec, true);
        }
    }
}
