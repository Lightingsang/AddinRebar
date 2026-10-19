using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPKata.Core.DrawingLayout.Calculators;
using HPKata.Core.DrawingLayout.Models;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller managing scanning of layout title blocks and generation of drawing index tables (danhmuc).
    /// </summary>
    public static class DrawingIndexController
    {
        public static void GenerateDrawingIndexTableInteractive()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata Index] Chọn điểm chèn bảng danh mục bản vẽ (danhmuc): ");
            if (ppr.Status != PromptStatus.OK) return;

            var items = ScanDrawingIndexItems(doc.Database);
            var tableDto = DrawingIndexAggregator.BuildTable(items);

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var curBtr = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite);

                Table tbl = BuildIndexCadTable(ppr.Value, tableDto);
                curBtr.AppendEntity(tbl);
                tr.AddNewlyCreatedDBObject(tbl, true);

                tr.Commit();
                ed.WriteMessage($"\n[HPKata Index] Đã tạo bảng danh mục với {tableDto.TotalSheets} bản vẽ. Cảnh báo: {tableDto.Warnings.Count}.\n");
            }
        }

        private static List<DrawingIndexItem> ScanDrawingIndexItems(Database db)
        {
            var items = new List<DrawingIndexItem>();
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var dno = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
                int order = 1;

                foreach (DBDictionaryEntry entry in dno)
                {
                    if (string.Equals(entry.Key, "Model", StringComparison.OrdinalIgnoreCase)) continue;
                    var layout = (Layout)tr.GetObject(entry.Value, OpenMode.ForRead);

                    items.Add(new DrawingIndexItem(
                        order++,
                        $"KC-{order - 1:D2}",
                        layout.LayoutName,
                        "1:100"));
                }
                tr.Commit();
            }

            if (items.Count == 0)
            {
                items.Add(new DrawingIndexItem(1, "KC-01", "Ghi chú chung và quy cách"));
                items.Add(new DrawingIndexItem(2, "KC-02", "Mặt bằng kết cấu móng"));
                items.Add(new DrawingIndexItem(3, "KC-03", "Chi tiết dầm sàn tầng 2"));
            }

            return items;
        }

        private static Table BuildIndexCadTable(Point3d insertPt, DrawingIndexTableDto dto)
        {
            var tbl = new Table();
            tbl.Position = insertPt;
            int totalRows = Math.Max(3, dto.Rows.Count + 2);
            tbl.SetSize(totalRows, dto.Headers.Length);

            tbl.Cells[0, 0].TextString = dto.Title;
            tbl.Rows[0].Height = 12.0;

            for (int c = 0; c < dto.Headers.Length; c++)
            {
                tbl.Cells[1, c].TextString = dto.Headers[c];
            }

            for (int r = 0; r < dto.Rows.Count; r++)
            {
                string[] rowData = dto.Rows[r];
                for (int c = 0; c < Math.Min(rowData.Length, dto.Headers.Length); c++)
                {
                    tbl.Cells[2 + r, c].TextString = rowData[c];
                }
            }

            tbl.GenerateLayout();
            return tbl;
        }
    }
}
