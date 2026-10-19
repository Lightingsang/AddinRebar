using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPKata.AutoCad.Interop;
using HPKata.Core.Configuration;
using HPKata.Core.Serialization;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// Controller orchestrating interactive rebar drawing and entity-to-rebar conversion.
    /// </summary>
    public static class RebarDrawController
    {
        public static void DrawRebarInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var points = CollectPolylinePoints(ed);
            if (points.Count < 2) return;

            int dia = PromptDiameter(ed, defaultDia: 16);
            double bendRadius = dia <= 16 ? 2.0 * dia : 3.5 * dia;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                Polyline pl = RebarGeometryService.CreatePolyline(points, bendRadius);
                var record = new KataXDataRecord
                {
                    Dang = "01",
                    Phi = (short)dia,
                    STT = 1,
                    SoLuong = "1",
                    SoLuongCauKien = 1
                };
                record.ChieuDaiCacDoanMm = RebarGeometryService.MeasureSegmentLengths(pl).ToArray();

                XDataService.WriteXData(pl, record);
                ms.AppendEntity(pl);
                tr.AddNewlyCreatedDBObject(pl, true);
                tr.Commit();
                ed.WriteMessage($"\n[HPKata] Created rebar polyline Ø{dia}, length: {pl.Length:F0} mm.\n");
            }
        }

        public static void ConvertEntityToRebar()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            ObjectId? selId = CadSelectionService.PromptSelectEntity(
                ed,
                "Select Line or Polyline to convert to Kata rebar: ");
            if (!selId.HasValue) return;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                Entity ent = (Entity)tr.GetObject(selId.Value, OpenMode.ForWrite);
                List<Point2d> points = ExtractPointsFromEntity(ent);
                if (points.Count < 2) return;

                int dia = PromptDiameter(ed, defaultDia: 16);
                double bendRadius = dia <= 16 ? 2.0 * dia : 3.5 * dia;

                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                Polyline pl = RebarGeometryService.CreatePolyline(points, bendRadius);
                var record = new KataXDataRecord
                {
                    Dang = "01",
                    Phi = (short)dia,
                    STT = 1,
                    SoLuong = "1",
                    SoLuongCauKien = 1
                };
                record.ChieuDaiCacDoanMm = RebarGeometryService.MeasureSegmentLengths(pl).ToArray();

                XDataService.WriteXData(pl, record);
                ms.AppendEntity(pl);
                tr.AddNewlyCreatedDBObject(pl, true);
                ent.Erase();
                tr.Commit();
                ed.WriteMessage($"\n[HPKata] Converted entity to Kata rebar Ø{dia}.\n");
            }
        }

        private static List<Point2d> CollectPolylinePoints(Editor ed)
        {
            var points = new List<Point2d>();
            Point3d? pt1 = CadSelectionService.PromptPoint(ed, "Specify start point: ");
            if (!pt1.HasValue) return points;
            points.Add(new Point2d(pt1.Value.X, pt1.Value.Y));

            while (true)
            {
                Point3d? nextPt = CadSelectionService.PromptPoint(ed, "Specify next point (Enter to finish): ");
                if (!nextPt.HasValue) break;
                points.Add(new Point2d(nextPt.Value.X, nextPt.Value.Y));
            }
            return points;
        }

        private static int PromptDiameter(Editor ed, int defaultDia)
        {
            var opt = new PromptIntegerOptions($"\nEnter bar diameter Ø [6/8/10/12/14/16/18/20/22/25/28/32] <{defaultDia}>: ")
            {
                DefaultValue = defaultDia,
                UseDefaultValue = true
            };
            PromptIntegerResult res = ed.GetInteger(opt);
            return res.Status == PromptStatus.OK ? res.Value : defaultDia;
        }

        private static List<Point2d> ExtractPointsFromEntity(Entity ent)
        {
            var points = new List<Point2d>();
            if (ent is Line line)
            {
                points.Add(new Point2d(line.StartPoint.X, line.StartPoint.Y));
                points.Add(new Point2d(line.EndPoint.X, line.EndPoint.Y));
            }
            else if (ent is Polyline pl)
            {
                for (int i = 0; i < pl.NumberOfVertices; i++)
                {
                    points.Add(pl.GetPoint2dAt(i));
                }
            }
            return points;
        }
    }
}
