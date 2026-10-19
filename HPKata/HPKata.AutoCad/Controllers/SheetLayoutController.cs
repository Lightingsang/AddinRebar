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
    /// Controller managing drawing sheet borders, title blocks, and 2D shelf-packing block arrangements.
    /// </summary>
    public static class SheetLayoutController
    {
        public static void ArrangeDrawingBlocksInteractive()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata Layout] Chọn điểm đặt tờ bản vẽ đầu tiên: ");
            if (ppr.Status != PromptStatus.OK) return;

            var sheetDef = SheetSizeDefinition.Create(SheetFormat.A1, true);
            var blocks = new List<DrawingBlockItem>
            {
                new DrawingBlockItem("Mặt bằng định vị dầm sàn", 350, 250),
                new DrawingBlockItem("Chi tiết dầm D1", 400, 150),
                new DrawingBlockItem("Chi tiết dầm D2", 380, 140),
                new DrawingBlockItem("Bảng thống kê cốt thép", 250, 300)
            };

            var arrangedSheets = SheetArrangerCalculator.ArrangeBlocks(blocks, sheetDef);
            DrawArrangedSheets(doc, ppr.Value, arrangedSheets);

            ed.WriteMessage($"\n[HPKata Layout] Đã sắp xếp {blocks.Count} chi tiết lên {arrangedSheets.Count} bản vẽ khổ {sheetDef.Format}.\n");
        }

        public static void InsertTitleBlockInteractive()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata Layout] Chọn góc dưới bên phải khung tên: ");
            if (ppr.Status != PromptStatus.OK) return;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                DrawTitleBlockPolyline(tr, ms, ppr.Value, 160.0, 60.0);
                tr.Commit();
                ed.WriteMessage("\n[HPKata Layout] Đã chèn khung tên tiêu chuẩn (160x60mm).\n");
            }
        }

        private static void DrawArrangedSheets(Document doc, Point3d startPt, List<ArrangedSheetResult> sheets)
        {
            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                double spacing = 50.0;
                double offsetX = 0.0;

                foreach (var sheet in sheets)
                {
                    var sheetOrigin = new Point3d(startPt.X + offsetX, startPt.Y, 0);
                    DrawSheetBorder(tr, ms, sheetOrigin, sheet.SheetDefinition);

                    var tbOrigin = new Point3d(
                        sheetOrigin.X + sheet.SheetDefinition.WidthMm - sheet.SheetDefinition.MarginRightMm,
                        sheetOrigin.Y + sheet.SheetDefinition.MarginBottomMm,
                        0);
                    DrawTitleBlockPolyline(tr, ms, tbOrigin, sheet.TitleBlock.WidthMm, sheet.TitleBlock.HeightMm);

                    offsetX += sheet.SheetDefinition.WidthMm + spacing;
                }

                tr.Commit();
            }
        }

        private static void DrawSheetBorder(Transaction tr, BlockTableRecord btr, Point3d origin, SheetSizeDefinition def)
        {
            var pline = new Polyline(4);
            pline.AddVertexAt(0, new Point2d(origin.X, origin.Y), 0, 0, 0);
            pline.AddVertexAt(1, new Point2d(origin.X + def.WidthMm, origin.Y), 0, 0, 0);
            pline.AddVertexAt(2, new Point2d(origin.X + def.WidthMm, origin.Y + def.HeightMm), 0, 0, 0);
            pline.AddVertexAt(3, new Point2d(origin.X, origin.Y + def.HeightMm), 0, 0, 0);
            pline.Closed = true;

            btr.AppendEntity(pline);
            tr.AddNewlyCreatedDBObject(pline, true);
        }

        private static void DrawTitleBlockPolyline(Transaction tr, BlockTableRecord btr, Point3d cornerPt, double w, double h)
        {
            var pline = new Polyline(4);
            pline.AddVertexAt(0, new Point2d(cornerPt.X - w, cornerPt.Y), 0, 0, 0);
            pline.AddVertexAt(1, new Point2d(cornerPt.X, cornerPt.Y), 0, 0, 0);
            pline.AddVertexAt(2, new Point2d(cornerPt.X, cornerPt.Y + h), 0, 0, 0);
            pline.AddVertexAt(3, new Point2d(cornerPt.X - w, cornerPt.Y + h), 0, 0, 0);
            pline.Closed = true;

            btr.AppendEntity(pline);
            tr.AddNewlyCreatedDBObject(pline, true);
        }
    }
}
