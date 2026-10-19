using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPKata.Core.QuantitySurveying.Calculators;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller executing 2D formwork panel layout (chia_tam / chiatam)
    /// and architectural tile layout (op_gach / opgach) in ModelSpace.
    /// </summary>
    public static class PanelTileLayoutController
    {
        public static void OptimizePanelLayoutInteractive()
        {
            ExecuteGridOptimization("Formwork Panel (chia_tam)", 1220.0, 2440.0, 0.0);
        }

        public static void OptimizeTileLayoutInteractive()
        {
            ExecuteGridOptimization("Tile Layout (op_gach)", 600.0, 600.0, 2.0);
        }

        private static void ExecuteGridOptimization(string modeName, double unitW, double unitL, double gapMm)
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var ppr = ed.GetPoint($"\n[HPKata {modeName}] Chọn điểm gốc vùng bố trí: ");
            if (ppr.Status != PromptStatus.OK) return;

            double roomW = 4800.0;
            double roomL = 3600.0;
            var layout = PanelTileLayoutCalculator.OptimizeLayout(roomW, roomL, unitW, unitL, gapMm);

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                foreach (var piece in layout.Pieces)
                {
                    DrawPieceRectangle(ms, tr, ppr.Value, piece);
                }

                tr.Commit();
                ed.WriteMessage(
                    $"\n[HPKata {modeName}] Đã chia {layout.TotalPieces} tấm (Nguyên: {layout.FullCount}, Cắt: {layout.CutCount}, Hao hụt: {layout.WasteRatio:P1}).\n");
            }
        }

        private static void DrawPieceRectangle(BlockTableRecord ms, Transaction tr, Point3d origin, PanelTilePiece piece)
        {
            var pline = new Polyline(4);
            double x0 = origin.X + piece.X;
            double y0 = origin.Y + piece.Y;
            pline.AddVertexAt(0, new Point2d(x0, y0), 0, 0, 0);
            pline.AddVertexAt(1, new Point2d(x0 + piece.WidthMm, y0), 0, 0, 0);
            pline.AddVertexAt(2, new Point2d(x0 + piece.WidthMm, y0 + piece.HeightMm), 0, 0, 0);
            pline.AddVertexAt(3, new Point2d(x0, y0 + piece.HeightMm), 0, 0, 0);
            pline.Closed = true;
            pline.ColorIndex = piece.IsFullPiece ? 3 : 1;

            ms.AppendEntity(pline);
            tr.AddNewlyCreatedDBObject(pline, true);
        }
    }
}
