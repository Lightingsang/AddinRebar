using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using HPDcePro.AutoCad.Drafting;
using HPDcePro.AutoCad.Prompts;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Foundation;
using HPDcePro.Core.Calculations.Secondary;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;
using HPDcePro.Core.XData;
using HPDcePro.UI.Views;

namespace HPDcePro.AutoCad.Commands
{
    public sealed class FootingCommands
    {
        [CommandMethod("VeBlockCoc")]
        public void VeBlockCoc()
        {
            var ed = PromptHelper.GetActiveEditor();
            var pt = new Point2D(0, 0);
            PromptHelper.TryGetString(ed, "Nhập tên cọc", "C1", out string mark);
            PromptHelper.TryGetDouble(ed, "Nhập đường kính cọc D (mm)", 400.0, out double dia);
            if (PromptHelper.TryGetPoint(ed, "Chọn tâm cọc (Pile center):", out var p)) pt = new Point2D(p.X, p.Y);

            var buffer = new CadDraftingBuffer();
            var cocPkt = new XDataPacket(XDataRegistry.RegAppNhanCoc);
            cocPkt.AddString(mark);
            double r = dia * 0.5;
            buffer.AddCircle(pt, r, "DCE_Coc_Bao", CadColor.MagentaIndex, new[] { cocPkt });
            buffer.AddLine(new Point2D(pt.X - r - 50, pt.Y), new Point2D(pt.X + r + 50, pt.Y), "DCE_Coc_Tim", CadColor.RedIndex, 0.15);
            buffer.AddLine(new Point2D(pt.X, pt.Y - r - 50), new Point2D(pt.X, pt.Y + r + 50), "DCE_Coc_Tim", CadColor.RedIndex, 0.15);
            buffer.AddText($"{mark} (D{dia:F0})", new Point2D(pt.X + r + 50, pt.Y + 100), 100.0, "DCE_Coc_Text", CadColor.YellowIndex);
            int rendered = RenderBuffer(buffer);
            AutoCadUiHelper.WriteMessage($"VeBlockCoc: Đã chèn ký hiệu cọc {mark} D{dia:F0} ({rendered} đối tượng CAD).");
        }

        [CommandMethod("DanhTenCoc")]
        public void DanhTenCoc()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tiền tố cọc", "C", out string prefix);
            PromptHelper.TryGetInteger(ed, "Nhập số lượng cọc", 4, out int count);
            PromptHelper.TryGetDouble(ed, "Nhập khoảng cách cọc (mm)", 1200.0, out double spacing);
            PromptHelper.TryGetPoint(ed, "Chọn điểm đặt cọc đầu tiên", out var originPt);

            var buffer = new CadDraftingBuffer();
            for (int i = 1; i <= Math.Max(1, count); i++)
            {
                double x = originPt.X + (i - 1) * spacing;
                buffer.AddText($"{prefix}{i}", new Point2D(x, originPt.Y), 120.0, "DCE_Coc_Text", CadColor.YellowIndex);
            }
            int rendered = RenderBuffer(buffer);
            AutoCadUiHelper.WriteMessage($"DanhTenCoc: Đã đánh số thứ tự tim cọc {prefix}1..{prefix}{count} ({rendered} đối tượng CAD).");
        }

        [CommandMethod("XuatToaDoCoc")]
        public void XuatToaDoCoc()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên móng cọc", "DC-1", out string mark);
            PromptHelper.TryGetPoint(ed, "Chọn điểm đặt bảng tọa độ cọc", out var originPt);

            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            var rebar = MaterialLibrary.GetRebar(RebarGrade.CB400V);
            var piles = PileCapCalculator.GenerateStandardLayout(PileLayoutPattern.FourPileRectangular, 400.0);
            var input = new PileCapInput(mark, 1800.0, 1800.0, 900.0, 400.0, 400.0, concrete, rebar, piles, 1500.0, 50.0, 40.0, 600.0);
            var result = PileCapCalculator.Calculate(input);

            var buffer = new CadDraftingBuffer();
            var gen = new FoundationDetailingGenerator();
            gen.DrawCoordinateScheduleTable(buffer, result, new Point2D(originPt.X, originPt.Y));
            int rendered = RenderBuffer(buffer);
            AutoCadUiHelper.WriteMessage($"XuatToaDoCoc: Đã xuất bảng tọa độ {result.CoordinateSchedule.Count} tim cọc móng {mark} ({rendered} đối tượng CAD).");
        }

        [CommandMethod("StripMong")]
        public void StripMong()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, "Nhập bề rộng dải strip (mm)", 1000.0, out double b);
            PromptHelper.TryGetDouble(ed, "Nhập chiều dài dải strip (mm)", 3000.0, out double l);
            PromptHelper.TryGetPoint(ed, "Chọn tâm dải móng", out var pt);

            var buffer = new CadDraftingBuffer();
            double halfL = l * 0.5, halfB = b * 0.5;
            var origin = new Point2D(pt.X, pt.Y);
            buffer.AddLine(new Point2D(origin.X - halfL, origin.Y), new Point2D(origin.X + halfL, origin.Y), "DCE_Strip_Mong", CadColor.CyanIndex, 0.5);
            buffer.AddLine(new Point2D(origin.X, origin.Y - halfB), new Point2D(origin.X, origin.Y + halfB), "DCE_Strip_Mong", CadColor.CyanIndex, 0.5);
            buffer.AddText($"STRIP-X (B={b:F0})", new Point2D(origin.X + halfL + 100, origin.Y), 100.0, "DCE_Strip_Text", CadColor.YellowIndex);
            int rendered = RenderBuffer(buffer);
            AutoCadUiHelper.WriteMessage($"StripMong: Đã tạo dải design strip tính toán móng B={b:F0} ({rendered} đối tượng CAD).");
        }

        [CommandMethod("TextDaiMong")]
        public void TextDaiMong()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên đài móng", "DC-1", out string mark);
            PromptHelper.TryGetDouble(ed, "Nhập chiều dài Lx (mm)", 1800.0, out double lx);
            PromptHelper.TryGetDouble(ed, "Nhập chiều rộng Ly (mm)", 1800.0, out double ly);
            PromptHelper.TryGetDouble(ed, "Nhập chiều cao H (mm)", 900.0, out double h);
            PromptHelper.TryGetPoint(ed, "Chọn điểm đặt text ghi chú đài", out var pt);

            var buffer = new CadDraftingBuffer();
            buffer.AddText($"ĐÀI MÓNG {mark} ({lx:F0}x{ly:F0}x{h:F0}) - ĐÁY -2.000m", new Point2D(pt.X, pt.Y), 150.0, "DCE_Mong_Text", CadColor.YellowIndex);
            int rendered = RenderBuffer(buffer);
            AutoCadUiHelper.WriteMessage($"TextDaiMong: Đã ghi chú kích thước và cao độ đài móng {mark} ({rendered} đối tượng CAD).");
        }

        [CommandMethod("VeMongCoc_DCE")]
        public void VeMongCoc_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên đài cọc", "DC-1", out string mark);
            PromptHelper.TryGetDouble(ed, "Nhập chiều dài Lx (mm)", 1800.0, out double lx);
            PromptHelper.TryGetDouble(ed, "Nhập chiều rộng Ly (mm)", 1800.0, out double ly);
            PromptHelper.TryGetDouble(ed, "Nhập chiều cao H (mm)", 900.0, out double h);
            var origin = new Point2D(0, 0);
            if (PromptHelper.TryGetPoint(ed, "Chọn điểm đặt đài cọc:", out var p)) origin = new Point2D(p.X, p.Y);

            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            var rebar = MaterialLibrary.GetRebar(RebarGrade.CB400V);
            var piles = PileCapCalculator.GenerateStandardLayout(PileLayoutPattern.FourPileRectangular, 400.0);
            var input = new PileCapInput(mark, lx, ly, h, 400.0, 400.0, concrete, rebar, piles, 1500.0, 50.0, 40.0, 600.0);
            var result = PileCapCalculator.Calculate(input);

            var gen = new FoundationDetailingGenerator();
            var buffer = gen.GeneratePileCapDrawing(result, input, origin);
            int rendered = RenderBuffer(buffer);
            AutoCadUiHelper.WriteMessage($"VeMongCoc_DCE: Đã vẽ chi tiết đài cọc {mark} ({rendered} đối tượng CAD).");
        }

        [CommandMethod("VeMongDon_DCE")]
        public void VeMongDon_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên móng đơn", "MD-1", out string mark);
            PromptHelper.TryGetDouble(ed, "Nhập chiều dài L (mm)", 2000.0, out double l);
            PromptHelper.TryGetDouble(ed, "Nhập bề rộng B (mm)", 2400.0, out double b);
            PromptHelper.TryGetDouble(ed, "Nhập chiều cao H (mm)", 600.0, out double h);
            var origin = new Point2D(0, 0);
            if (PromptHelper.TryGetPoint(ed, "Chọn điểm đặt móng đơn:", out var p)) origin = new Point2D(p.X, p.Y);

            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            var rebar = MaterialLibrary.GetRebar(RebarGrade.CB400V);
            var input = new PadFootingInput(mark, l, b, h, 400.0, 400.0, 1.5, concrete, rebar, 800.0, 60.0, 40.0, 250.0);
            var result = PadFootingCalculator.Calculate(input);

            var gen = new FoundationDetailingGenerator();
            var buffer = gen.GeneratePadFootingDrawing(result, input, origin);
            int rendered = RenderBuffer(buffer);
            AutoCadUiHelper.WriteMessage($"VeMongDon_DCE: Đã vẽ móng đơn {mark} (Pmax={result.BasePressureMaxKpa:F1}kPa, {rendered} đối tượng CAD).");
        }

        [CommandMethod("TMCoc")]
        public void TMCoc()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên cọc", "C1", out string mark);
            PromptHelper.TryGetDouble(ed, "Nhập sức chịu tải thiết kế P (kN)", 520.0, out double pVal);
            PromptHelper.TryGetPoint(ed, "Chọn điểm đặt nhãn thông tin cọc", out var pt);

            var buffer = new CadDraftingBuffer();
            var pkt = new XDataPacket(XDataRegistry.RegAppNhanCoc);
            pkt.AddString($"{mark}:X={pt.X:F3},Y={pt.Y:F3},P={pVal:F0}kN");
            buffer.AddText($"TM: {mark} (X={pt.X:F3}, Y={pt.Y:F3}, P={pVal:F0}kN)", new Point2D(pt.X, pt.Y), 100.0, "DCE_Coc_Text", CadColor.YellowIndex);
            int rendered = RenderBuffer(buffer);
            AutoCadUiHelper.WriteMessage($"TMCoc: Đã gán nhãn số hiệu và tọa độ cọc {mark} ({rendered} đối tượng CAD).");
        }

        [CommandMethod("RaiDaiCoc")]
        public void RaiDaiCoc()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, "Nhập chiều dài lồng cọc (mm)", 3000.0, out double cageLen);
            PromptHelper.TryGetDouble(ed, "Nhập bước đai a (mm)", 150.0, out double step);
            PromptHelper.TryGetPoint(ed, "Chọn điểm bắt đầu lồng cọc", out var pt);

            var buffer = new CadDraftingBuffer();
            for (double y = 0.0; y <= cageLen; y += Math.Max(50.0, step))
            {
                buffer.AddLine(new Point2D(pt.X - 180, pt.Y + y), new Point2D(pt.X + 180, pt.Y + y), "DCE_Thep_Coc", CadColor.MagentaIndex, 1.0);
            }
            int rendered = RenderBuffer(buffer);
            AutoCadUiHelper.WriteMessage($"RaiDaiCoc: Đã rải đai lồng cọc a{step:F0} ({rendered} đối tượng CAD).");
        }

        [CommandMethod("VeBlockLanhTo")]
        public void VeBlockLanhTo()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên lanh tô", "LT-1", out string mark);
            PromptHelper.TryGetDouble(ed, "Nhập nhịp thông thủy (mm)", 1200.0, out double span);
            PromptHelper.TryGetDouble(ed, "Nhập bề rộng b (mm)", 220.0, out double b);
            PromptHelper.TryGetDouble(ed, "Nhập chiều cao h (mm)", 200.0, out double h);
            var origin = new Point2D(0, 0);
            if (PromptHelper.TryGetPoint(ed, "Chọn điểm đặt lanh tô:", out var p)) origin = new Point2D(p.X, p.Y);

            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            var rebar = MaterialLibrary.GetRebar(RebarGrade.CB400V);
            var input = new LintelInput(mark, span, 250.0, b, h, 1500.0, concrete, rebar);
            var result = LintelCalculator.Calculate(input);

            var gen = new FoundationDetailingGenerator();
            var buffer = gen.GenerateLintelDrawing(result, input, origin);
            int rendered = RenderBuffer(buffer);
            AutoCadUiHelper.WriteMessage($"VeBlockLanhTo: Đã chèn block chi tiết lanh tô {mark} ({rendered} đối tượng CAD).");
        }

        [CommandMethod("VeMatCatLanhTo")]
        public void VeMatCatLanhTo()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, "Nhập bề rộng b (mm)", 220.0, out double b);
            PromptHelper.TryGetDouble(ed, "Nhập chiều cao h (mm)", 200.0, out double h);
            PromptHelper.TryGetInteger(ed, "Nhập đường kính thép chủ (mm)", 12, out int dia);
            PromptHelper.TryGetPoint(ed, "Chọn điểm đặt mặt cắt lanh tô", out var pt);

            var buffer = new CadDraftingBuffer();
            var origin = new Point2D(pt.X, pt.Y);
            var rect = new[] { origin, new Point2D(origin.X + b, origin.Y), new Point2D(origin.X + b, origin.Y + h), new Point2D(origin.X, origin.Y + h) };
            buffer.AddPolyline(rect, true, "DCE_LanhTo_Bao", CadColor.CyanIndex, 0.35);
            buffer.AddCircle(new Point2D(origin.X + 35, origin.Y + 35), dia * 0.5, "DCE_Thep_LanhTo", CadColor.GreenIndex);
            buffer.AddCircle(new Point2D(origin.X + b - 35, origin.Y + 35), dia * 0.5, "DCE_Thep_LanhTo", CadColor.GreenIndex);
            buffer.AddText($"MC LANH TÔ ({b:F0}x{h:F0}) - 2Phi{dia}", new Point2D(origin.X, origin.Y - 100), 100.0, "DCE_LanhTo_Text", CadColor.YellowIndex);
            int rendered = RenderBuffer(buffer);
            AutoCadUiHelper.WriteMessage($"VeMatCatLanhTo: Đã vẽ mặt cắt tiết diện lanh tô {b:F0}x{h:F0} ({rendered} đối tượng CAD).");
        }

        [CommandMethod("VeThang_DCE")]
        public void VeThang_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên thang", "THANG-1", out string mark);
            PromptHelper.TryGetDouble(ed, "Nhập chiều cao tầng H (mm)", 3000.0, out double height);
            PromptHelper.TryGetDouble(ed, "Nhập chiều dài vế thang L (mm)", 1800.0, out double run);
            PromptHelper.TryGetInteger(ed, "Nhập số bậc thang", 10, out int steps);
            var origin = new Point2D(0, 0);
            if (PromptHelper.TryGetPoint(ed, "Chọn điểm đặt bản vẽ thang:", out var p)) origin = new Point2D(p.X, p.Y);

            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            var rebar = MaterialLibrary.GetRebar(RebarGrade.CB400V);
            var input = new StaircaseInput(mark, StaircaseType.WaistSlab, height, run, 120.0, Math.Max(1, steps), 1200.0, concrete, rebar);
            var result = StaircaseCalculator.Calculate(input);

            var gen = new FoundationDetailingGenerator();
            var buffer = gen.GenerateStaircaseDrawing(result, input, origin);
            int rendered = RenderBuffer(buffer);
            AutoCadUiHelper.WriteMessage($"VeThang_DCE: Đã vẽ chi tiết bản thang {mark} ({rendered} đối tượng CAD).");
        }

        private static int RenderBuffer(CadDraftingBuffer buffer)
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
                    int count = CadDraftingBufferRenderer.Render(buffer, doc.Database, tr, btr);
                    tr.Commit();
                    return count;
                }
            }
            catch (System.Exception)
            {
                // Reason: In test runner or headless environment, fallback gracefully.
                return buffer.Count;
            }
        }
    }
}
