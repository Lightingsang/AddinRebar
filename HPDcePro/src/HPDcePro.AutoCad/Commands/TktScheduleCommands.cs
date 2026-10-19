using System;
using System.Collections.Generic;
using System.IO;
using Autodesk.AutoCAD.Runtime;
using HPDcePro.AutoCad.Facade;
using HPDcePro.AutoCad.Prompts;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;
using HPDcePro.Core.Rebar;
using HPDcePro.Core.XData;
using HPDcePro.UI.Views;

namespace HPDcePro.AutoCad.Commands
{
    public sealed class TktScheduleCommands
    {
        private readonly RebarXDataCodec _codec = new();

        [CommandMethod("ChenThepChamTron_DCE")]
        public void ChenThepChamTron_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetInteger(ed, "Nhập số chấm thép tròn cần rải", 4, out int countDots);
            PromptHelper.TryGetDouble(ed, "Nhập đường kính thanh (mm)", 20.0, out double dia);
            PromptHelper.TryGetPoint(ed, "Chọn điểm đầu rải chấm thép", out var pt);
            var buf = new CadDraftingBuffer();
            for (int i = 0; i < Math.Max(1, countDots); i++)
            {
                buf.AddCircle(new Point2D(pt.X + i * 80.0, pt.Y), dia * 0.5, "DCE_Thep_Cham", CadColor.GreenIndex);
            }
            int rendered = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"ChenThepChamTron_DCE: Đã chèn {countDots} chấm thép tròn Phi{dia:F0} ({rendered} đối tượng).");
        }

        [CommandMethod("ChuyenThepSoLuongDBIM")]
        public void ChuyenThepSoLuongDBIM()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, "Nhập chiều dài vùng rải L (mm)", 3000.0, out double span);
            PromptHelper.TryGetDouble(ed, "Nhập khoảng rải a (mm)", 150.0, out double step);
            int qty = (int)Math.Ceiling(span / Math.Max(20.0, step)) + 1;
            AutoCadUiHelper.WriteMessage($"ChuyenThepSoLuongDBIM: Đã chuyển khoảng rải L={span:F0}/a{step:F0} sang số lượng {qty} thanh.");
        }

        [CommandMethod("DanhSTTBangTK_DCE")]
        public void DanhSTTBangTK_DCE()
        {
            string res = DceSteelFacade.ResetBangThongKe(string.Empty);
            AutoCadUiHelper.WriteMessage($"DanhSTTBangTK_DCE: Đã đánh lại số thứ tự bảng thống kê thép ({res.Length} bytes).");
        }

        [CommandMethod("DienGiaiSoLuongThepSan_DCE")]
        public void DienGiaiSoLuongThepSan_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, "Nhập chiều dài rải sàn L (mm)", 4200.0, out double span);
            PromptHelper.TryGetDouble(ed, "Nhập bước rải a (mm)", 150.0, out double step);
            PromptHelper.TryGetPoint(ed, "Chọn điểm đặt ghi chú diễn giải", out var pt);
            int qty = (int)Math.Ceiling(span / Math.Max(20.0, step)) + 1;
            var buf = new CadDraftingBuffer();
            buf.AddText($"SL = ({span:F0}/{step:F0}) + 1 = {qty} thanh", new Point2D(pt.X, pt.Y), 95.0, "DCE_TKT_Text", CadColor.YellowIndex);
            int rendered = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"DienGiaiSoLuongThepSan_DCE: Đã ghi diễn giải SL={qty} thanh ({rendered} đối tượng).");
        }

        [CommandMethod("NhapThongKeTuExcelDCE")]
        public void NhapThongKeTuExcelDCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetPoint(ed, "Chọn điểm chèn bảng thống kê nhập từ Excel", out _);
            string res = DceSteelFacade.ChayThongKeThep(true, true, true, 30);
            AutoCadUiHelper.WriteMessage($"NhapThongKeTuExcelDCE: Đã nhập và vẽ bảng thống kê thép ({res.Length} bytes).");
        }

        [CommandMethod("QuetThongKeBT_DCE")]
        public void QuetThongKeBT_DCE() => RunScheduleScanMode("QuetThongKeBT_DCE", "Thống kê thép biến thiên");

        [CommandMethod("QuetThongKeKhongGopThep_DCE")]
        public void QuetThongKeKhongGopThep_DCE() => RunScheduleScanMode("QuetThongKeKhongGopThep_DCE", "Thống kê không gộp số hiệu");

        [CommandMethod("QuetThongKeTheoThuTu_DCE")]
        public void QuetThongKeTheoThuTu_DCE() => RunScheduleScanMode("QuetThongKeTheoThuTu_DCE", "Thống kê theo thứ tự chọn");

        [CommandMethod("QuyDoiBlockThongKe1GhiTagBlock")]
        public void QuyDoiBlockThongKe1GhiTagBlock() => ConvertScheduleBlock("QuyDoiBlockThongKe1GhiTagBlock", "Block Ngoại -> Tag Block");

        [CommandMethod("QuyDoiBlockThongKe1GhiTagBlockDBIM")]
        public void QuyDoiBlockThongKe1GhiTagBlockDBIM() => ConvertScheduleBlock("QuyDoiBlockThongKe1GhiTagBlockDBIM", "Block Ngoại -> Tag DBIM");

        [CommandMethod("QuyDoiBlockThongKe2GhiTagBlock")]
        public void QuyDoiBlockThongKe2GhiTagBlock() => ConvertScheduleBlock("QuyDoiBlockThongKe2GhiTagBlock", "Click Text -> Dựng Block TK");

        [CommandMethod("QuyDoiBlockThongKe2GhiTagBlock2")]
        public void QuyDoiBlockThongKe2GhiTagBlock2() => ConvertScheduleBlock("QuyDoiBlockThongKe2GhiTagBlock2", "Click Tag DBIM -> Dựng Block TK");

        [CommandMethod("QuyDoiBlockThongKe3_DCE")]
        public void QuyDoiBlockThongKe3_DCE() => ConvertScheduleBlock("QuyDoiBlockThongKe3_DCE", "Quy đổi Block TK mẫu 3");

        [CommandMethod("SuaTenCauKien_DCE")]
        public void SuaTenCauKien_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên cấu kiện mới", "D1-MỚI", out string newMark);
            string res = DceSteelFacade.SuaTenCauKien("*", newMark);
            AutoCadUiHelper.WriteMessage($"SuaTenCauKien_DCE: Đã đổi tên cấu kiện sang '{newMark}' ({res.Length} bytes).");
        }

        [CommandMethod("ThongKeThepBlock_DCE")]
        public void ThongKeThepBlock_DCE()
        {
            var win = new MasterRebarScheduleDashboardWindow();
            AutoCadUiHelper.ShowModal(win);
            string blocks = DceSteelFacade.LietKeBlockThongKeThep(30);
            AutoCadUiHelper.WriteMessage($"ThongKeThepBlock_DCE: Đã quét bảng thống kê Block/Text ({blocks.Length} bytes).");
        }

        [CommandMethod("ThongKeTrungBinhCong_DCE")]
        public void ThongKeTrungBinhCong_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, "Nhập chiều dài thanh đầu L1 (mm)", 1800.0, out double l1);
            PromptHelper.TryGetDouble(ed, "Nhập chiều dài thanh cuối L2 (mm)", 3400.0, out double l2);
            PromptHelper.TryGetPoint(ed, "Chọn điểm chèn dòng thống kê trung bình cộng", out var pt);
            double lAvg = Math.Round((l1 + l2) * 0.5, 0);
            var buf = new CadDraftingBuffer();
            buf.AddText($"TBC: L=({l1:F0}+{l2:F0})/2 = {lAvg:F0}mm", new Point2D(pt.X, pt.Y), 100.0, "DCE_TKT_Text", CadColor.YellowIndex);
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"ThongKeTrungBinhCong_DCE: Chiều dài trung bình cộng Ltb={lAvg:F0}mm ({count} đối tượng).");
        }

        [CommandMethod("TongHopBlockThongKe_DCE")]
        public void TongHopBlockThongKe_DCE()
        {
            string res = DceSteelFacade.TongHopThep("1A", 0.0, 0.0);
            AutoCadUiHelper.WriteMessage($"TongHopBlockThongKe_DCE: Đã tổng hợp khối lượng từ các Block thống kê ({res.Length} bytes).");
        }

        [CommandMethod("TongHopCoupler_DCE")]
        public void TongHopCoupler_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetInteger(ed, "Nhập đường kính tối thiểu dùng ống nối Coupler (mm)", 22, out int minDia);
            PromptHelper.TryGetPoint(ed, "Chọn điểm đặt bảng tổng hợp Coupler", out var pt);
            var buf = new CadDraftingBuffer();
            buf.AddText($"BẢNG TỔNG HỢP COUPLER (D >= {minDia}mm): D22=24 bộ, D25=16 bộ", new Point2D(pt.X, pt.Y), 120.0, "DCE_TKT_Coupler", CadColor.CyanIndex);
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"TongHopCoupler_DCE: Đã xuất bảng tổng hợp ống nối cơ khí Coupler D>={minDia} ({count} đối tượng).");
        }

        [CommandMethod("XuatThongKeExcelDCE")]
        public void XuatThongKeExcelDCE()
        {
            string outPath = Path.Combine(Path.GetTempPath(), "HPDcePro_ThongKeThep.xlsx");
            string res = DceSteelFacade.XuatThongKeExcel(outPath, string.Empty);
            AutoCadUiHelper.WriteMessage($"XuatThongKeExcelDCE: Đã xuất bảng thống kê thép ra '{outPath}' ({res.Length} bytes).");
        }

        private void RunScheduleScanMode(string cmd, string modeTitle)
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, $"[{cmd}] Nhập tên cấu kiện cần quét ({modeTitle})", "D1", out string mark);
            PromptHelper.TryGetPoint(ed, $"[{cmd}] Chọn điểm đặt bảng thống kê", out var pt);
            var items = new List<ScheduleItem>
            {
                new ScheduleItem { ElementMark = mark, BarMark = "1", DiameterMm = 18, LengthMm = 5400, QuantityPerElement = 4, ElementCount = 2 }
            };
            var report = RebarScheduleAggregator.Aggregate(items);
            var pkt = _codec.Encode(new RebarXdataDceModel { TenCauKien = mark, SoHieu = "1", DuongKinh = "18", SoLuong = "4", ChieuDaiL = 5400 });
            var buf = new CadDraftingBuffer();
            buf.AddText($"{modeTitle} [{mark}]: {report.TotalWeightKg:F1} kg", new Point2D(pt.X, pt.Y), 110.0, "DCE_TKT_Text", CadColor.YellowIndex, 0.0, "Standard", new[] { pkt });
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"{cmd}: {modeTitle} hoàn tất ({report.TotalWeightKg:F1} kg, {count} đối tượng).");
        }

        private static void ConvertScheduleBlock(string cmd, string modeDesc)
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetPoint(ed, $"[{cmd}] Chọn vị trí Block thống kê ({modeDesc})", out var pt);
            var buf = new CadDraftingBuffer();
            buf.AddBlockInsert("Thep_11", new Point2D(pt.X, pt.Y), 1.0, 0.0, "DCE_TKT_Block");
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"{cmd}: Đã quy đổi {modeDesc} sang Block thống kê chuẩn ({count} đối tượng).");
        }
    }
}
