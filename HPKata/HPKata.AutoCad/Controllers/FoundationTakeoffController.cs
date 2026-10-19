using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPKata.Core.Foundations.Models;
using HPKata.Core.Foundations.Persistence;
using HPKata.Core.Models;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller orchestrating foundation schedule takeoff and real Table entity generation in ModelSpace.
    /// </summary>
    public static class FoundationTakeoffController
    {
        public static void TakeoffFoundationsInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata] Pick insertion point for foundation schedule table (tkmong): ");
            if (ppr.Status != PromptStatus.OK) return;

            var layouts = ResolveLayouts(doc.Database);
            var items = new List<RebarItem>();
            foreach (var l in layouts) items.AddRange(FoundationScheduleAggregator.AggregateLayout(l));

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                Table tbl = BuildScheduleTable(ppr.Value, items);
                ms.AppendEntity(tbl);
                tr.AddNewlyCreatedDBObject(tbl, true);

                tr.Commit();
                ed.WriteMessage($"\n[HPKata] tkmong: Inserted schedule table with {items.Count} items across {layouts.Count} foundations.\n");
            }
        }

        private static List<FoundationRebarLayout> ResolveLayouts(Database db)
        {
            var cached = FoundationTakeoffRegistry.GetAll().ToList();
            if (cached.Count > 0) return cached;

            var def = FoundationRebarLayout.FromFooting(new IsolatedFootingModel { FootingMark = "M1" });
            FoundationTakeoffRegistry.Register(def);
            return new List<FoundationRebarLayout> { def };
        }

        public static Table BuildScheduleTable(Point3d insertionPoint, List<RebarItem> items)
        {
            int rowCount = Math.Max(3, items.Count + 2);
            var tbl = new Table();
            tbl.SetSize(rowCount, 8);
            tbl.Position = insertionPoint;

            tbl.Cells[0, 0].TextString = "BẢNG THỐNG KÊ CỐT THÉP MÓNG (HPKATA)";
            string[] headers = { "STT", "Cấu Kiện", "Ký Hiệu", "Đk (mm)", "Số Lượng", "Dài (mm)", "Trọng Lượng (kg)", "Ghi Chú" };
            for (int c = 0; c < headers.Length; c++)
            {
                tbl.Cells[1, c].TextString = headers[c];
            }

            for (int r = 0; r < items.Count; r++)
            {
                var it = items[r];
                int row = r + 2;
                tbl.Cells[row, 0].TextString = (r + 1).ToString();
                tbl.Cells[row, 1].TextString = it.ComponentMark;
                tbl.Cells[row, 2].TextString = it.BarMark;
                tbl.Cells[row, 3].TextString = it.DiameterMm.ToString();
                tbl.Cells[row, 4].TextString = it.TotalCount.ToString();
                tbl.Cells[row, 5].TextString = it.CuttingLengthMm.ToString("F0");
                tbl.Cells[row, 6].TextString = it.TotalWeightKg.ToString("F1");
                tbl.Cells[row, 7].TextString = it.Notes;
            }

            tbl.GenerateLayout();
            return tbl;
        }
    }
}
