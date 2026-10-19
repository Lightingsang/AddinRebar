using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPKata.Core.Columns.Models;
using HPKata.Core.Columns.Persistence;
using HPKata.Core.Models;
using HPKata.Core.Walls.Persistence;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller orchestrating column and wall schedule takeoff and table generation (tkcot).
    /// </summary>
    public static class ColumnTakeoffController
    {
        public static void TakeoffColumnsInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata] Pick insertion point for column/wall schedule table (tkcot): ");
            if (ppr.Status != PromptStatus.OK) return;

            var colLayouts = ResolveColumnLayouts(doc.Database);
            var wallModels = WallTakeoffRegistry.GetAll().ToList();

            var items = new List<RebarItem>();
            foreach (var col in colLayouts) items.AddRange(ColumnScheduleAggregator.AggregateSchedule(col));
            foreach (var w in wallModels) items.AddRange(WallScheduleAggregator.AggregateSchedule(w));

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                Table tbl = BuildScheduleTable(ppr.Value, items);
                ms.AppendEntity(tbl);
                tr.AddNewlyCreatedDBObject(tbl, true);

                tr.Commit();
                ed.WriteMessage($"\n[HPKata] tkcot: Inserted schedule table with {items.Count} items across {colLayouts.Count} columns and {wallModels.Count} walls.\n");
            }
        }

        private static List<ColumnRebarLayout> ResolveColumnLayouts(Database db)
        {
            var cached = ColumnTakeoffRegistry.GetAll().ToList();
            if (cached.Count > 0) return cached;

            var scanned = ScanColumnEntities(db);
            if (scanned.Count > 0)
            {
                foreach (var l in scanned) ColumnTakeoffRegistry.Register(l);
                return scanned;
            }

            var defaultCol = ColumnDrawController.CreateDefaultLayout();
            ColumnTakeoffRegistry.Register(defaultCol);
            return new List<ColumnRebarLayout> { defaultCol };
        }

        private static List<ColumnRebarLayout> ScanColumnEntities(Database db)
        {
            var results = new List<ColumnRebarLayout>();
            using var tr = db.TransactionManager.StartTransaction();
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
            int idx = 1;

            foreach (ObjectId id in ms)
            {
                var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (ent == null) continue;

                string layer = ent.Layer.ToUpperInvariant();
                if ((layer.Contains("COT") || layer.Contains("COLUMN")) && ent is Polyline pl && pl.Closed)
                {
                    double w = Math.Max(200.0, pl.Bounds.HasValue ? (pl.Bounds.Value.MaxPoint.X - pl.Bounds.Value.MinPoint.X) : 400.0);
                    double d = Math.Max(200.0, pl.Bounds.HasValue ? (pl.Bounds.Value.MaxPoint.Y - pl.Bounds.Value.MinPoint.Y) : 400.0);
                    var layout = ColumnDrawController.CreateDefaultLayout();
                    layout.ColumnMark = $"C{idx++}";
                    if (layout.Stories.Count > 0)
                    {
                        layout.Stories[0].WidthMm = Math.Round(w, 0);
                        layout.Stories[0].DepthMm = Math.Round(d, 0);
                    }
                    results.Add(layout);
                }
            }

            tr.Commit();
            return results;
        }

        private static Table BuildScheduleTable(Point3d insertionPt, List<RebarItem> items)
        {
            Table tbl = new Table();
            tbl.Position = insertionPt;
            int rowCount = Math.Max(3, items.Count + 2);
            tbl.SetSize(rowCount, 6);

            tbl.Cells[0, 0].TextString = "BẢNG THỐNG KÊ CẤU KIỆN CỘT & VÁCH";
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
    }
}
