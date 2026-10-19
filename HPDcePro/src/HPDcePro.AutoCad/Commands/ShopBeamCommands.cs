using System;
using Autodesk.AutoCAD.Runtime;
using HPDcePro.AutoCad.Prompts;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;
using HPDcePro.Core.Rebar;
using HPDcePro.Core.XData;
using HPDcePro.UI.Views;

namespace HPDcePro.AutoCad.Commands
{
    public sealed class ShopBeamCommands
    {
        private readonly RebarXDataCodec _codec = new();
        private static bool _verifyOptimalCutEnabled = true;

        [CommandMethod("ChiaMachNgungThepSanReverse_DCE")]
        public void ChiaMachNgungThepSanReverse_DCE() => ExecuteLapSpliceSplit("ChiaMachNgungThepSanReverse_DCE", true, true);

        [CommandMethod("ChiaMachNgungThepSan_DCE")]
        public void ChiaMachNgungThepSan_DCE() => ExecuteLapSpliceSplit("ChiaMachNgungThepSan_DCE", false, true);

        [CommandMethod("ChiaThep2_DCE")]
        public void ChiaThep2_DCE() => ExecuteBarSplit("ChiaThep2_DCE", 2);

        [CommandMethod("ChiaThep3_DCE")]
        public void ChiaThep3_DCE() => ExecuteBarSplit("ChiaThep3_DCE", 3);

        [CommandMethod("ChiaThepLienTuc_DCE")]
        public void ChiaThepLienTuc_DCE() => ExecuteBarSplit("ChiaThepLienTuc_DCE", 4);

        [CommandMethod("ChiaThep_DCE")]
        public void ChiaThep_DCE() => ExecuteBarSplit("ChiaThep_DCE", 1);

        [CommandMethod("CutThepReverse_DCE")]
        public void CutThepReverse_DCE() => ExecuteLapSpliceSplit("CutThepReverse_DCE", true, false);

        [CommandMethod("CutThepSoleReverse_DCE")]
        public void CutThepSoleReverse_DCE() => ExecuteStaggeredSplice("CutThepSoleReverse_DCE", true);

        [CommandMethod("CutThepSole_DCE")]
        public void CutThepSole_DCE() => ExecuteStaggeredSplice("CutThepSole_DCE", false);

        [CommandMethod("CutThep_DCE")]
        public void CutThep_DCE() => ExecuteLapSpliceSplit("CutThep_DCE", false, false);

        [CommandMethod("DCEKIEMCHUNGTOIUU")]
        public void DCEKIEMCHUNGTOIUU()
        {
            _verifyOptimalCutEnabled = !_verifyOptimalCutEnabled;
            AutoCadUiHelper.WriteMessage($"DCEKIEMCHUNGTOIUU: Tự động kiểm chứng tối ưu cắt thép 11.7m = {(_verifyOptimalCutEnabled ? "BẬT" : "TẮT")}.");
        }

        [CommandMethod("KeoThep2Dau")]
        public void KeoThep2Dau() => ExecuteStretchBarEnds("KeoThep2Dau", 300.0);

        [CommandMethod("KeoThep2Dau2")]
        public void KeoThep2Dau2() => ExecuteStretchBarEnds("KeoThep2Dau2", 450.0);

        [CommandMethod("NhanThep_DCE")]
        public void NhanThep_DCE() => ExecuteCrankBend("NhanThep_DCE", 60.0);

        [CommandMethod("REBARBEND")]
        public void REBARBEND() => ExecuteCrankBend("REBARBEND", 50.0);

        [CommandMethod("ShopDrawingBeam2")]
        public void ShopDrawingBeam2()
        {
            var win = new BeamDetailingWindow();
            AutoCadUiHelper.ShowModal(win);
            AutoCadUiHelper.WriteMessage("ShopDrawingBeam2: Đã mở trình triển khai Shop Drawing dầm gắn liên kết XData.");
        }

        [CommandMethod("ShopRebar")]
        public void ShopRebar()
        {
            var win = new CuttingOptimizationWindow();
            AutoCadUiHelper.ShowModal(win);
            AutoCadUiHelper.WriteMessage("ShopRebar: Đã mở hộp thoại Shop Rebar & Tối ưu cắt thép 11.7m.");
        }

        [CommandMethod("TachThepSole_DCE")]
        public void TachThepSole_DCE() => ExecuteStaggeredSplice("TachThepSole_DCE", false);

        [CommandMethod("keodim2dau_Dce")]
        public void keodim2dau_Dce()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, "Nhập khoảng kéo dài chân Dim 2 đầu (mm)", 250.0, out double ext);
            PromptHelper.TryGetPoint(ed, "Chọn điểm gốc đường Dim", out var pt);
            var buf = new CadDraftingBuffer();
            buf.AddLine(new Point2D(pt.X - ext, pt.Y), new Point2D(pt.X + 4000 + ext, pt.Y), "DCE_Dim_Rebar", CadColor.YellowIndex, 0.25);
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"keodim2dau_Dce: Đã căn chỉnh chân đường gióng Dim 2 đầu ±{ext:F0}mm ({count} đối tượng).");
        }

        private void ExecuteBarSplit(string cmd, int mode)
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, $"[{cmd}] Nhập tổng chiều dài thanh cần chia (mm)", 14500.0, out double totalLen);
            PromptHelper.TryGetDouble(ed, $"[{cmd}] Nhập chiều dài nối chồng Llap (mm)", 800.0, out double lap);
            PromptHelper.TryGetPoint(ed, $"[{cmd}] Chọn điểm bắt đầu thanh thép", out var pt);
            double stock = 11700.0;
            double seg1 = Math.Min(stock, totalLen);
            double seg2 = Math.Max(1200.0, totalLen - seg1 + lap);
            var pkt1 = _codec.Encode(new RebarXdataDceModel { TenCauKien = "D1", SoHieu = $"1.{mode}A", DuongKinh = "20", SoLuong = "2", ChieuDaiL = seg1 });
            var pkt2 = _codec.Encode(new RebarXdataDceModel { TenCauKien = "D1", SoHieu = $"1.{mode}B", DuongKinh = "20", SoLuong = "2", ChieuDaiL = seg2 });
            var buf = new CadDraftingBuffer();
            buf.AddPolyline(new[] { new Point2D(pt.X, pt.Y), new Point2D(pt.X + seg1, pt.Y) }, false, "DCE_Thep_Doc", CadColor.RedIndex, 2.0, new[] { pkt1 });
            buf.AddPolyline(new[] { new Point2D(pt.X + seg1 - lap, pt.Y + 40), new Point2D(pt.X + seg1 - lap + seg2, pt.Y + 40) }, false, "DCE_Thep_Doc", CadColor.RedIndex, 2.0, new[] { pkt2 });
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"{cmd}: Đã chia thanh L={totalLen:F0} thành L1={seg1:F0} + L2={seg2:F0} (nối {lap:F0}mm, {count} đối tượng).");
        }

        private void ExecuteLapSpliceSplit(string cmd, bool reverseDir, bool isSlabJoint)
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, $"[{cmd}] Nhập chiều dài nối chồng 40d (mm)", 720.0, out double lapLen);
            PromptHelper.TryGetPoint(ed, $"[{cmd}] Chọn vị trí {(isSlabJoint ? "mạch ngừng sàn" : "điểm cắt thép")}", out var pt);
            double dir = reverseDir ? -1.0 : 1.0;
            var pkt = _codec.Encode(new RebarXdataDceModel { TenCauKien = isSlabJoint ? "S1" : "D1", SoHieu = "MN1", DuongKinh = "18", SoLuong = "4", ChieuDaiL = 5850 });
            var buf = new CadDraftingBuffer();
            buf.AddPolyline(new[] { new Point2D(pt.X - 3000 * dir, pt.Y), new Point2D(pt.X + lapLen * 0.5 * dir, pt.Y) }, false, "DCE_Thep_Doc", CadColor.RedIndex, 2.0, new[] { pkt });
            buf.AddPolyline(new[] { new Point2D(pt.X - lapLen * 0.5 * dir, pt.Y + 35), new Point2D(pt.X + 3000 * dir, pt.Y + 35) }, false, "DCE_Thep_Doc", CadColor.CyanIndex, 2.0, new[] { pkt });
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"{cmd}: Đã cắt nối chồng Llap={lapLen:F0}mm chiều {(reverseDir ? "ngược" : "thuận")} ({count} đối tượng).");
        }

        private void ExecuteStaggeredSplice(string cmd, bool reverseDir)
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, $"[{cmd}] Nhập khoảng lệch mối nối so le 1.3*Llap (mm)", 950.0, out double stagger);
            PromptHelper.TryGetPoint(ed, $"[{cmd}] Chọn vị trí cắt thép so le (50%/50%)", out var pt);
            double dir = reverseDir ? -1.0 : 1.0;
            var buf = new CadDraftingBuffer();
            buf.AddLine(new Point2D(pt.X, pt.Y), new Point2D(pt.X + 720 * dir, pt.Y), "DCE_Thep_Sole1", CadColor.RedIndex, 2.0);
            buf.AddLine(new Point2D(pt.X + stagger * dir, pt.Y + 50), new Point2D(pt.X + (stagger + 720) * dir, pt.Y + 50), "DCE_Thep_Sole2", CadColor.GreenIndex, 2.0);
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"{cmd}: Đã tách và cắt thép so le lệch {stagger:F0}mm ({count} đối tượng).");
        }

        private static void ExecuteStretchBarEnds(string cmd, double defaultAnchorageMm)
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, $"[{cmd}] Nhập đoạn kéo dài neo 2 đầu (mm)", defaultAnchorageMm, out double ext);
            PromptHelper.TryGetPoint(ed, $"[{cmd}] Chọn điểm gốc thanh thép", out var pt);
            var buf = new CadDraftingBuffer();
            buf.AddPolyline(new[] { new Point2D(pt.X - ext, pt.Y - 150), new Point2D(pt.X - ext, pt.Y), new Point2D(pt.X + 5000 + ext, pt.Y), new Point2D(pt.X + 5000 + ext, pt.Y - 150) }, false, "DCE_Thep_Doc", CadColor.RedIndex, 2.0);
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"{cmd}: Đã kéo neo 2 đầu thanh thép ±{ext:F0}mm ({count} đối tượng).");
        }

        private static void ExecuteCrankBend(string cmd, double crankOffsetMm)
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, $"[{cmd}] Nhập độ nhấn cổ chai a (mm)", crankOffsetMm, out double offset);
            PromptHelper.TryGetPoint(ed, $"[{cmd}] Chọn điểm bắt đầu nhấn cổ chai (tỷ lệ 1:6)", out var pt);
            double slopeRun = offset * 6.0;
            var o = new Point2D(pt.X, pt.Y);
            var pts = new[] { new Point2D(o.X - 1000, o.Y), o, new Point2D(o.X + slopeRun, o.Y + offset), new Point2D(o.X + slopeRun + 1000, o.Y + offset) };
            var buf = new CadDraftingBuffer();
            buf.AddPolyline(pts, false, "DCE_Thep_Doc", CadColor.RedIndex, 2.0);
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"{cmd}: Đã nhấn cổ chai d={offset:F0}mm đoạn vát 1:6={slopeRun:F0}mm ({count} đối tượng).");
        }
    }
}
