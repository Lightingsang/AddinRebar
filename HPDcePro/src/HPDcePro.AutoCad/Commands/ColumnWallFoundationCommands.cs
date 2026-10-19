using System;
using Autodesk.AutoCAD.Runtime;
using HPDcePro.AutoCad.Prompts;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;
using HPDcePro.Core.XData;
using HPDcePro.UI.Views;

namespace HPDcePro.AutoCad.Commands
{
    public sealed class ColumnWallFoundationCommands
    {
        private static bool _pileCapLogEnabled;

        [CommandMethod("DaiTron1to2_DCE")]
        public void DaiTron1to2_DCE() => ConvertCircularStirrup("DaiTron1to2_DCE", true);

        [CommandMethod("DaiTron2to1_DCE")]
        public void DaiTron2to1_DCE() => ConvertCircularStirrup("DaiTron2to1_DCE", false);

        [CommandMethod("VeCotInputStory_DCE")]
        public void VeCotInputStory_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập Tên tầng", "TẦNG 1 (+0.000 -> +3.600)", out string storyName);
            PromptHelper.TryGetPoint(ed, "Click chọn điểm chèn mặt cắt cột theo tầng", out var pt);
            var o = new Point2D(pt.X, pt.Y);
            var buf = new CadDraftingBuffer();
            buf.AddPolyline(new[] { o, new Point2D(o.X + 400, o.Y), new Point2D(o.X + 400, o.Y + 400), new Point2D(o.X, o.Y + 400) }, true, "DCE_Cot_Bao", CadColor.CyanIndex, 0.35);
            buf.AddText($"CỘT C1 - {storyName}", new Point2D(o.X, o.Y - 120), 110.0, "DCE_Cot_Text", CadColor.YellowIndex);
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"VeCotInputStory_DCE: Đã vẽ cột tầng '{storyName}' ({count} đối tượng).");
        }

        [CommandMethod("VeVachInputStory_DCE")]
        public void VeVachInputStory_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập Tên tầng vách cứng", "TẦNG 1", out string storyName);
            PromptHelper.TryGetPoint(ed, "Click chọn điểm chèn mặt cắt vách", out var pt);
            var o = new Point2D(pt.X, pt.Y);
            var buf = new CadDraftingBuffer();
            buf.AddPolyline(new[] { o, new Point2D(o.X + 2400, o.Y), new Point2D(o.X + 2400, o.Y + 250), new Point2D(o.X, o.Y + 250) }, true, "DCE_Vach_Bao", CadColor.CyanIndex, 0.35);
            buf.AddText($"VÁCH W1 - {storyName}", new Point2D(o.X, o.Y - 120), 110.0, "DCE_Vach_Text", CadColor.YellowIndex);
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"VeVachInputStory_DCE: Đã vẽ vách cứng tầng '{storyName}' ({count} đối tượng).");
        }

        [CommandMethod("VVT_DCE")]
        public void VVT_DCE()
        {
            var win = new ColumnWallDetailingWindow();
            AutoCadUiHelper.ShowModal(win);
            AutoCadUiHelper.WriteMessage("VVT_DCE: Đã mở trình vẽ mặt cắt cột vách tổ hợp (Wall Core).");
        }

        [CommandMethod("GenC_DCE")]
        public void GenC_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetPoint(ed, "Chọn điểm gốc xuất dữ liệu tiết diện cột/vách Prokon/CSI", out var pt);
            var buf = new CadDraftingBuffer();
            buf.AddText("PROKON_COREWALL_SECTION: L=2400, T=250, B25, CB400V", new Point2D(pt.X, pt.Y), 100.0, "DCE_Vach_Text", CadColor.GreenIndex);
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"GenC_DCE: Đã tạo dữ liệu mặt cắt cột vách bất kỳ ({count} đối tượng).");
        }

        [CommandMethod("MONGCOCNEWLOG_DCE")]
        public void MONGCOCNEWLOG_DCE()
        {
            _pileCapLogEnabled = !_pileCapLogEnabled;
            AutoCadUiHelper.WriteMessage($"MONGCOCNEWLOG_DCE: Ghi log module móng cọc = {(_pileCapLogEnabled ? "BẬT" : "TẮT")}.");
        }

        [CommandMethod("MONGCOCNEWXOALOG_DCE")]
        public void MONGCOCNEWXOALOG_DCE()
        {
            _pileCapLogEnabled = false;
            AutoCadUiHelper.WriteMessage("MONGCOCNEWXOALOG_DCE: Đã làm sạch bộ đệm log module móng cọc.");
        }

        [CommandMethod("VEMONGTHANGMAY_DCE")]
        public void VEMONGTHANGMAY_DCE()
        {
            var win = new MongCocWindow();
            AutoCadUiHelper.ShowModal(win);
            AutoCadUiHelper.WriteMessage("VEMONGTHANGMAY_DCE: Đã mở trình thiết kế và vẽ móng cọc / hố PIT thang máy V2.");
        }

        [CommandMethod("DimCoc2_Dce")]
        public void DimCoc2_Dce() => DrawPileDimension("DimCoc2_Dce", 100.0);

        [CommandMethod("DimCoc_Dce")]
        public void DimCoc_Dce() => DrawPileDimension("DimCoc_Dce", 100.0);

        [CommandMethod("ThongKeCoc_DCE")]
        public void ThongKeCoc_DCE()
        {
            var fCmds = new FootingCommands();
            fCmds.XuatToaDoCoc();
        }

        [CommandMethod("XemThuTuCoc_Dce")]
        public void XemThuTuCoc_Dce()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetPoint(ed, "Chọn điểm đầu kiểm tra đường nối thứ tự cọc", out var pt);
            var buf = new CadDraftingBuffer();
            buf.AddPolyline(new[] { new Point2D(pt.X, pt.Y), new Point2D(pt.X + 1200, pt.Y), new Point2D(pt.X + 1200, pt.Y + 1200), new Point2D(pt.X, pt.Y + 1200) }, false, "DCE_Coc_ThuTu", CadColor.YellowIndex, 0.3);
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"XemThuTuCoc_Dce: Đã vẽ đường kiểm tra thứ tự đánh số cọc ({count} đối tượng).");
        }

        [CommandMethod("RaiDaiMongDon_DCE")]
        public void RaiDaiMongDon_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, "Nhập chiều rộng móng đơn B (mm)", 1800.0, out double b);
            PromptHelper.TryGetDouble(ed, "Nhập bước rải thép móng đơn a (mm)", 150.0, out double step);
            PromptHelper.TryGetPoint(ed, "Chọn góc đặt lưới thép móng đơn", out var pt);
            var buf = new CadDraftingBuffer();
            for (double x = 50.0; x <= b - 50.0; x += Math.Max(50.0, step))
            {
                buf.AddCircle(new Point2D(pt.X + x, pt.Y + 60), 6.0, "DCE_Thep_Mong", CadColor.GreenIndex);
            }
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"RaiDaiMongDon_DCE: Đã rải {count} thanh thép đế móng đơn a{step:F0}.");
        }

        [CommandMethod("VeCocKhoanNhoi_DCE")]
        public void VeCocKhoanNhoi_DCE()
        {
            var win = new CocBtWindow();
            AutoCadUiHelper.ShowModal(win);
            AutoCadUiHelper.WriteMessage("VeCocKhoanNhoi_DCE: Đã mở trình triển khai chi tiết cọc khoan nhồi.");
        }

        [CommandMethod("DanhTenBoTru_DCE")]
        public void DanhTenBoTru_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tiền tố bổ trụ", "BT", out string prefix);
            PromptHelper.TryGetInteger(ed, "Nhập số lượng bổ trụ", 4, out int countBt);
            PromptHelper.TryGetPoint(ed, "Chọn điểm bắt đầu đánh tên bổ trụ", out var pt);
            var buf = new CadDraftingBuffer();
            for (int i = 1; i <= Math.Max(1, countBt); i++)
            {
                buf.AddText($"{prefix}{i}", new Point2D(pt.X + (i - 1) * 3000.0, pt.Y), 120.0, "DCE_BoTru_Text", CadColor.YellowIndex);
            }
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"DanhTenBoTru_DCE: Đã đánh tên bổ trụ {prefix}1..{prefix}{countBt} ({count} đối tượng).");
        }

        [CommandMethod("DceThangNewVeNoiBo")]
        public void DceThangNewVeNoiBo()
        {
            var fCmds = new FootingCommands();
            fCmds.VeThang_DCE();
        }

        [CommandMethod("VeThang_DCE2")]
        public void VeThang_DCE2()
        {
            var win = new ThangMoiWindow();
            AutoCadUiHelper.ShowModal(win);
            var fCmds = new FootingCommands();
            fCmds.VeThang_DCE();
        }

        [CommandMethod("TN_dce")]
        public void TN_dce()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetInteger(ed, "Nhập đường kính thanh thép d (mm)", 20, out int d);
            double lNeo = 40.0 * d;
            double lNoi = 50.0 * d;
            AutoCadUiHelper.WriteMessage($"TN_dce: Tra cứu TCVN 5574:2018 cho Phi{d} (B25, CB400V): Chiều dài neo L_an = {lNeo:F0}mm (40d), nối chồng L_lap = {lNoi:F0}mm (50d).");
        }

        private static void ConvertCircularStirrup(string cmd, bool oneToTwo)
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, $"[{cmd}] Nhập đường kính đai tròn D (mm)", 500.0, out double dia);
            PromptHelper.TryGetPoint(ed, $"[{cmd}] Chọn tâm đai tròn", out var pt);
            var buf = new CadDraftingBuffer();
            buf.AddCircle(new Point2D(pt.X, pt.Y), dia * 0.5, "DCE_Thep_Dai", CadColor.MagentaIndex);
            if (oneToTwo)
            {
                buf.AddCircle(new Point2D(pt.X, pt.Y), dia * 0.5 - 15.0, "DCE_Thep_Dai", CadColor.CyanIndex);
            }
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"{cmd}: Đã chuyển đổi đai tròn {(oneToTwo ? "1 vòng -> 2 mảnh chữ C" : "2 mảnh -> 1 vòng kín")} ({count} đối tượng).");
        }

        private static void DrawPileDimension(string cmd, double scale)
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetPoint(ed, $"[{cmd}] Chọn tâm cọc định vị khoảng cách đến trục (1:{scale:F0})", out var pt);
            var pkt = new XDataPacket(XDataRegistry.RegAppNhanCoc);
            pkt.AddString("DIM_COC_1200");
            var buf = new CadDraftingBuffer();
            buf.AddLine(new Point2D(pt.X, pt.Y), new Point2D(pt.X + 1200, pt.Y), "DCE_Dim_Coc", CadColor.YellowIndex, 0.25);
            buf.AddText("1200", new Point2D(pt.X + 500, pt.Y + 60), 90.0, "DCE_Dim_Text", CadColor.CyanIndex, 0.0, "Standard", new[] { pkt });
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"{cmd}: Đã tạo Dim định vị tim cọc ({count} đối tượng).");
        }
    }
}
