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
    /// Controller orchestrating staircase drafting (ve_thang / vethang) with re-entrant scissor crank bars.
    /// </summary>
    public static class StairDrawController
    {
        public static void DrawStairInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata] Pick insertion point for staircase elevation (ve_thang): ");
            if (ppr.Status != PromptStatus.OK) return;

            var flight = new StairFlightModel { FlightMark = "ST1" };
            var items = StairRebarCalculator.CalculateStairRebar(flight);
            var reinf = new StairReinforcementModel { FlightMark = "ST1", Flight = flight, ScheduleItems = items };
            StairTakeoffRegistry.RegisterStair(reinf);

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                DrawStairProfile(ms, tr, ppr.Value, flight);
                AttachStairXRecord(doc.Database, tr, reinf);

                tr.Commit();
                ed.WriteMessage($"\n[HPKata] ve_thang: Successfully drafted stair '{flight.FlightMark}' ({flight.RiserCount} risers, scissor crank L_ext={flight.ScissorCrankExtensionMm:F0}mm).\n");
            }
        }

        public static void DrawStairProfile(BlockTableRecord ms, Transaction tr, Point3d origin, StairFlightModel flight)
        {
            var pts = StairGeometryCalculator.GenerateSectionProfile(flight);
            var pline = new Polyline(pts.Count);

            for (int i = 0; i < pts.Count; i++)
            {
                pline.AddVertexAt(i, new Point2d(origin.X + pts[i].X, origin.Y + pts[i].Y), 0, 0, 0);
            }
            pline.Closed = true;

            ms.AppendEntity(pline);
            tr.AddNewlyCreatedDBObject(pline, true);
        }

        private static void AttachStairXRecord(Database db, Transaction tr, StairReinforcementModel model)
        {
            var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForWrite);
            string json = StairXRecordSerializer.SerializeToJson(model);
            using var xrec = new Xrecord();
            xrec.Data = new ResultBuffer(new TypedValue((int)DxfCode.Text, json));
            string key = $"KATA_STAIR_{model.FlightMark}";
            if (nod.Contains(key)) nod.Remove(key);
            nod.SetAt(key, xrec);
            tr.AddNewlyCreatedDBObject(xrec, true);
        }
    }
}
