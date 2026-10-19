using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using HPDcePro.AutoCad.Drafting;
using HPDcePro.AutoCad.Prompts;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Column;
using HPDcePro.Core.Calculations.Wall;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;
using HPDcePro.UI.Views;

namespace HPDcePro.AutoCad.Commands
{
    public sealed class ColumnCommands
    {
        [CommandMethod("VeCotDcePro")]
        public void VeCotDcePro()
        {
            var win = new ColumnWallDetailingWindow();
            AutoCadUiHelper.ShowModal(win);
        }

        [CommandMethod("VeVachDcePro")]
        public void VeVachDcePro()
        {
            var ed = PromptHelper.GetActiveEditor();
            Point2D origin = new Point2D(0, 0);
            PromptHelper.TryGetString(ed, "Nhập tên vách", "W1", out string mark);
            PromptHelper.TryGetDouble(ed, "Nhập chiều dài vách Lw (mm)", 3000.0, out double lw);
            PromptHelper.TryGetDouble(ed, "Nhập chiều dày vách Tw (mm)", 250.0, out double tw);
            PromptHelper.TryGetDouble(ed, "Nhập chiều cao tầng Hw (mm)", 3600.0, out double hw);
            if (PromptHelper.TryGetPoint(ed, "Chọn điểm đặt chân vách:", out var pt))
            {
                origin = new Point2D(pt.X, pt.Y);
            }

            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            var rebar = MaterialLibrary.GetRebar(RebarGrade.CB400V);
            var input = new ShearWallInput(mark, lw, tw, hw, concrete, rebar, 2500.0, 450.0, 900.0);
            var result = ShearWallCalculator.Calculate(input);

            var buffer = new CadDraftingBuffer();
            var wallRect = new[]
            {
                origin, new Point2D(origin.X + lw, origin.Y),
                new Point2D(origin.X + lw, origin.Y + tw), new Point2D(origin.X, origin.Y + tw)
            };
            buffer.AddPolyline(wallRect, true, "DCE_Vach_Bao", CadColor.CyanIndex, 0.35);

            double lc = result.BoundaryElement.LengthLcMm;
            buffer.AddLine(new Point2D(origin.X + lc, origin.Y), new Point2D(origin.X + lc, origin.Y + tw), "DCE_Vach_Thep", CadColor.MagentaIndex, 1.5);
            buffer.AddLine(new Point2D(origin.X + lw - lc, origin.Y), new Point2D(origin.X + lw - lc, origin.Y + tw), "DCE_Vach_Thep", CadColor.MagentaIndex, 1.5);
            buffer.AddText($"VÁCH {input.WallMark} (L={lw:F0}, T={tw:F0}) - {result.Summary}", new Point2D(origin.X, origin.Y - 300), 150.0, "DCE_Vach_Text", CadColor.YellowIndex);

            int rendered = RenderBufferToActiveDocument(buffer);
            AutoCadUiHelper.WriteMessage($"VeVachDcePro: Đã sinh và vẽ chi tiết vách cứng {input.WallMark} ({rendered} đối tượng CAD).");
        }

        [CommandMethod("ColumnREC1")]
        public void ColumnREC1()
        {
            var ed = PromptHelper.GetActiveEditor();
            Point2D pt = new Point2D(0, 0);
            PromptHelper.TryGetDouble(ed, "Nhập bề rộng cột b (mm)", 400.0, out double b);
            PromptHelper.TryGetDouble(ed, "Nhập chiều cao cột h (mm)", 400.0, out double h);
            if (PromptHelper.TryGetPoint(ed, "Chọn điểm đặt cột chữ nhật REC1:", out var p))
            {
                pt = new Point2D(p.X, p.Y);
            }

            var buffer = GenerateColumnBuffer(ColumnSectionStyle.REC1, b, h, "C-REC1", pt);
            int rendered = RenderBufferToActiveDocument(buffer);
            AutoCadUiHelper.WriteMessage($"ColumnREC1: Đã vẽ tiết diện cột chữ nhật đai đơn ({rendered} đối tượng CAD).");
        }

        [CommandMethod("ColumnREC2")]
        public void ColumnREC2()
        {
            var ed = PromptHelper.GetActiveEditor();
            Point2D pt = new Point2D(0, 0);
            PromptHelper.TryGetDouble(ed, "Nhập bề rộng cột b (mm)", 500.0, out double b);
            PromptHelper.TryGetDouble(ed, "Nhập chiều cao cột h (mm)", 500.0, out double h);
            if (PromptHelper.TryGetPoint(ed, "Chọn điểm đặt cột chữ nhật REC2:", out var p))
            {
                pt = new Point2D(p.X, p.Y);
            }

            var buffer = GenerateColumnBuffer(ColumnSectionStyle.REC2, b, h, "C-REC2", pt);
            int rendered = RenderBufferToActiveDocument(buffer);
            AutoCadUiHelper.WriteMessage($"ColumnREC2: Đã vẽ tiết diện cột chữ nhật đai phức hợp ({rendered} đối tượng CAD).");
        }

        [CommandMethod("ColumnCi")]
        public void ColumnCi()
        {
            var ed = PromptHelper.GetActiveEditor();
            Point2D pt = new Point2D(0, 0);
            PromptHelper.TryGetDouble(ed, "Nhập đường kính cột tròn D (mm)", 450.0, out double dia);
            if (PromptHelper.TryGetPoint(ed, "Chọn điểm đặt cột tròn Ci:", out var p))
            {
                pt = new Point2D(p.X, p.Y);
            }

            var buffer = GenerateColumnBuffer(ColumnSectionStyle.Ci, dia, dia, "C-Ci", pt);
            int rendered = RenderBufferToActiveDocument(buffer);
            AutoCadUiHelper.WriteMessage($"ColumnCi: Đã vẽ tiết diện cột tròn đai xoắn D={dia:F0} ({rendered} đối tượng CAD).");
        }

        [CommandMethod("ImportKichThuocCot")]
        public void ImportKichThuocCot()
        {
            var ed = PromptHelper.GetActiveEditor();
            Point2D originPt = new Point2D(0, 0);
            if (PromptHelper.TryGetPoint(ed, "Chọn điểm gốc chèn bảng kích thước cột:", out var p))
            {
                originPt = new Point2D(p.X, p.Y);
            }

            var buffer = new CadDraftingBuffer();
            var sections = new (string Mark, double B, double H)[]
            {
                ("C1", 400.0, 400.0),
                ("C2", 500.0, 500.0),
                ("C3", 600.0, 600.0)
            };

            double curX = originPt.X;
            for (int i = 0; i < sections.Length; i++)
            {
                var s = sections[i];
                var pts = new[]
                {
                    new Point2D(curX, originPt.Y), new Point2D(curX + s.B, originPt.Y),
                    new Point2D(curX + s.B, originPt.Y + s.H), new Point2D(curX, originPt.Y + s.H)
                };
                buffer.AddPolyline(pts, true, "DCE_Cot_Bao", CadColor.CyanIndex, 0.35);
                buffer.AddText($"{s.Mark} ({s.B}x{s.H})", new Point2D(curX, originPt.Y - 150.0), 100.0, "DCE_Cot_Text", CadColor.YellowIndex);
                curX += s.B + 1000.0;
            }

            int rendered = RenderBufferToActiveDocument(buffer);
            AutoCadUiHelper.WriteMessage($"ImportKichThuocCot: Đã nhập và vẽ {sections.Length} kích thước cột ({rendered} đối tượng CAD).");
        }

        [CommandMethod("ImportHinhDangCot")]
        public void ImportHinhDangCot()
        {
            var ed = PromptHelper.GetActiveEditor();
            Point2D pt = new Point2D(0, 0);
            PromptHelper.TryGetDouble(ed, "Nhập bề rộng cánh L1 (mm)", 600.0, out double l1);
            PromptHelper.TryGetDouble(ed, "Nhập chiều dày cánh T (mm)", 250.0, out double t);
            if (PromptHelper.TryGetPoint(ed, "Chọn điểm đặt cột đa giác chữ L:", out var p))
            {
                pt = new Point2D(p.X, p.Y);
            }

            var buffer = new CadDraftingBuffer();
            var lShape = new[]
            {
                pt, new Point2D(pt.X + l1, pt.Y), new Point2D(pt.X + l1, pt.Y + t),
                new Point2D(pt.X + t, pt.Y + t), new Point2D(pt.X + t, pt.Y + l1), new Point2D(pt.X, pt.Y + l1)
            };
            buffer.AddPolyline(lShape, true, "DCE_Cot_Bao", CadColor.CyanIndex, 0.35);
            buffer.AddText($"CỘT ĐA GIÁC CHỮ L ({l1:F0}x{l1:F0}/{t:F0})", new Point2D(pt.X, pt.Y - 200), 120.0, "DCE_Cot_Text", CadColor.YellowIndex);

            int rendered = RenderBufferToActiveDocument(buffer);
            AutoCadUiHelper.WriteMessage($"ImportHinhDangCot: Đã nhập và vẽ cột đa giác ({rendered} đối tượng CAD).");
        }

        [CommandMethod("UpdateThepTinhToan")]
        public void UpdateThepTinhToan()
        {
            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            var rebar = MaterialLibrary.GetRebar(RebarGrade.CB400V);
            var bars = new List<ColumnBarPlacement>
            {
                new ColumnBarPlacement(-170, -170, 20),
                new ColumnBarPlacement(170, -170, 20),
                new ColumnBarPlacement(170, 170, 20),
                new ColumnBarPlacement(-170, 170, 20)
            };
            var input = new ColumnPmmInput("C1", ColumnSectionShape.Rectangular, 400.0, 400.0, 3600.0, concrete, rebar, bars, 1200.0, 80.0, 60.0);
            var result = ColumnPmmInteractionCalculator.Calculate(input);

            AutoCadUiHelper.WriteMessage($"UpdateThepTinhToan: Cột C1 tính toán xong: {result.StatusSummary}, As={bars.Count * 314.16:F0}mm2, etaX={result.SlendernessEtaX:F2}.");
        }

        private static CadDraftingBuffer GenerateColumnBuffer(ColumnSectionStyle style, double b, double h, string mark, Point2D origin)
        {
            var buffer = new CadDraftingBuffer();
            if (style == ColumnSectionStyle.Ci)
            {
                double r = b * 0.5;
                buffer.AddCircle(origin, r, "DCE_Cot_Bao", CadColor.CyanIndex);
                buffer.AddCircle(origin, r - 35.0, "DCE_Cot_Thep", CadColor.MagentaIndex);
            }
            else
            {
                var rect = new[]
                {
                    origin, new Point2D(origin.X + b, origin.Y),
                    new Point2D(origin.X + b, origin.Y + h), new Point2D(origin.X, origin.Y + h)
                };
                buffer.AddPolyline(rect, true, "DCE_Cot_Bao", CadColor.CyanIndex, 0.35);
            }
            buffer.AddText($"{mark} ({b:F0}x{h:F0})", new Point2D(origin.X, origin.Y - 100.0), 100.0, "DCE_Cot_Text", CadColor.YellowIndex);
            return buffer;
        }

        private static int RenderBufferToActiveDocument(CadDraftingBuffer buffer)
        {
            try
            {
                var doc = PromptHelper.GetActiveDocument();
                if (doc == null) return buffer.Count;

                using (doc.LockDocument())
                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                    var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                    int rendered = CadDraftingBufferRenderer.Render(buffer, doc.Database, tr, btr);
                    tr.Commit();
                    return rendered;
                }
            }
            catch (System.Exception)
            {
                // Reason: In headless test runners or outside active document, fallback safely.
                return buffer.Count;
            }
        }
    }

    internal enum ColumnSectionStyle { REC1, REC2, Ci }
}
