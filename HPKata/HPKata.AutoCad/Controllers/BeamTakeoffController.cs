using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPKata.Core.Beams.Models;
using HPKata.Core.Beams.Persistence;
using HPKata.Core.Models;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller orchestrating framing plan recognition scanning and component schedule takeoff
    /// (tkdam, quet_dam, quet_dam_all).
    /// </summary>
    public static class BeamTakeoffController
    {
        public static void GenerateBeamScheduleTable()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata] Pick insertion point for beam component takeoff table (tkdam): ");
            if (ppr.Status != PromptStatus.OK) return;

            var layouts = ResolveLayoutsForTakeoff(doc.Database);
            var items = layouts.SelectMany(l => BeamScheduleAggregator.AggregateSchedule(l)).ToList();

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                Table tbl = BuildScheduleTable(ppr.Value, items);
                ms.AppendEntity(tbl);
                tr.AddNewlyCreatedDBObject(tbl, true);

                tr.Commit();
                ed.WriteMessage($"\n[HPKata] Inserted beam schedule table with {items.Count} items across {layouts.Count} beam runs.\n");
            }
        }

        public static void ScanFramingPlanRegion()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var psr = ed.GetSelection();
            if (psr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\n[HPKata] quet_dam: No framing elements selected.\n");
                return;
            }

            var recognized = ScanEntityIds(doc.Database, psr.Value.GetObjectIds());
            foreach (var layout in recognized) BeamTakeoffRegistry.Register(layout);

            ed.WriteMessage($"\n[HPKata] quet_dam: Scanned {psr.Value.Count} entities. Recognized and cached {recognized.Count} continuous beam runs.\n");
        }

        public static void ScanFramingPlanAll()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            int totalEntities = 0;
            var framingIds = new List<ObjectId>();

            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                foreach (ObjectId id in ms)
                {
                    totalEntities++;
                    if (IsFramingEntity(id, tr)) framingIds.Add(id);
                }
                tr.Commit();
            }

            var recognized = ScanEntityIds(doc.Database, framingIds);
            foreach (var layout in recognized) BeamTakeoffRegistry.Register(layout);

            ed.WriteMessage($"\n[HPKata] quet_dam_all: Scanned {totalEntities} model space entities. Cached {recognized.Count} continuous beam runs for batch detailing.\n");
        }

        public static void ExportBeamScheduleToExcel(string filePath)
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            var db = doc?.Database;
            var layouts = db != null ? ResolveLayoutsForTakeoff(db) : BeamTakeoffRegistry.GetAll().ToList();
            if (layouts.Count == 0)
            {
                layouts = new List<BeamRebarLayout> { CreateDefaultLayout("D1") };
            }
            BeamPlanScheduleWorkbookGenerator.GenerateWorkbookFile(layouts, filePath);
        }

        private static List<BeamRebarLayout> ResolveLayoutsForTakeoff(Database db)
        {
            var dwgLayouts = BeamPersistenceController.LoadAllBeamsFromDwg(db);
            if (dwgLayouts.Count > 0) return dwgLayouts;

            var cached = BeamTakeoffRegistry.GetAll().ToList();
            if (cached.Count > 0) return cached;

            return new List<BeamRebarLayout> { CreateDefaultLayout("D1") };
        }

        private static BeamRebarLayout CreateDefaultLayout(string name)
        {
            var layout = new BeamRebarLayout { BeamName = name, TotalLengthMm = 6000.0 };
            layout.Spans.Add(new BeamSpanModel { SpanIndex = 0, SpanName = name, ClearLengthMm = 5600.0, WidthMm = 300.0, HeightMm = 600.0 });
            layout.TopContinuousBars.Add(new RebarRunModel { DiameterMm = 20, BarCount = 2, LengthMm = 6000.0 });
            layout.BottomContinuousBars.Add(new RebarRunModel { DiameterMm = 20, BarCount = 2, LengthMm = 6000.0 });
            return layout;
        }

        private static Table BuildScheduleTable(Point3d insertionPt, List<RebarItem> items)
        {
            Table tbl = new Table();
            tbl.Position = insertionPt;
            int rowCount = Math.Max(3, items.Count + 2);
            tbl.SetSize(rowCount, 6);

            tbl.Cells[0, 0].TextString = "BẢNG THỐNG KÊ CẤU KIỆN DẦM";
            string[] headers = { "CẤU KIỆN", "STT", "HÌNH DẠNG", "Ø (mm)", "SỐ LƯỢNG", "TRỌNG LƯỢNG (kg)" };
            for (int c = 0; c < headers.Length; c++) tbl.Cells[1, c].TextString = headers[c];

            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                int r = i + 2;
                tbl.Cells[r, 0].TextString = it.ComponentMark;
                tbl.Cells[r, 1].TextString = it.BarMark;
                tbl.Cells[r, 2].TextString = it.ShapeCode;
                tbl.Cells[r, 3].TextString = it.DiameterMm.ToString();
                tbl.Cells[r, 4].TextString = it.TotalCount.ToString();
                tbl.Cells[r, 5].TextString = it.TotalWeightKg.ToString("F1");
            }

            tbl.GenerateLayout();
            return tbl;
        }

        private static List<BeamRebarLayout> ScanEntityIds(Database db, IEnumerable<ObjectId> ids)
        {
            var results = new List<BeamRebarLayout>();
            using var tr = db.TransactionManager.StartTransaction();
            int index = 1;

            foreach (var id in ids)
            {
                var ent = tr.GetObject(id, OpenMode.ForRead) as Curve;
                if (ent == null) continue;

                double lengthMm = ent.GetDistanceAtParameter(ent.EndParam);
                if (lengthMm < 500.0) continue;

                string beamName = $"D{index++}";
                var layout = new BeamRebarLayout { BeamName = beamName, TotalLengthMm = Math.Round(lengthMm, 0) };
                layout.Spans.Add(new BeamSpanModel { SpanIndex = 0, SpanName = beamName, ClearLengthMm = Math.Round(lengthMm - 400.0, 0), WidthMm = 300.0, HeightMm = 600.0 });
                layout.TopContinuousBars.Add(new RebarRunModel { DiameterMm = 20, BarCount = 2, LengthMm = layout.TotalLengthMm });
                layout.BottomContinuousBars.Add(new RebarRunModel { DiameterMm = 20, BarCount = 2, LengthMm = layout.TotalLengthMm });
                results.Add(layout);
            }
            tr.Commit();
            return results;
        }

        private static bool IsFramingEntity(ObjectId id, Transaction tr)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
            if (ent == null) return false;
            if (ent is not (Line or Polyline or Polyline2d or Polyline3d)) return false;

            string layer = ent.Layer.ToUpperInvariant();
            return layer.Contains("DAM") || layer.Contains("BEAM") || layer.Contains("FRAMING");
        }
    }
}
