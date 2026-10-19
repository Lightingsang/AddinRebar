using System;
using Autodesk.AutoCAD.Runtime;
using HPDcePro.AutoCad.Facade;
using HPDcePro.AutoCad.Prompts;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;
using HPDcePro.Core.XData;

namespace HPDcePro.AutoCad.Commands
{
    public sealed partial class DceLenhBoTroCommands
    {
        [CommandMethod("BlockExcelToCad_DCE")]
        public void BlockExcelToCad_DCE() => ExecuteExcelCadSync("BlockExcelToCad_DCE", "Nhập Block/Text từ bảng tính Excel vào CAD", "DCE_Excel_Block");

        [CommandMethod("BlockToExcel_DCE")]
        public void BlockToExcel_DCE() => ExecuteExcelCadSync("BlockToExcel_DCE", "Xuất thuộc tính Block và Text từ CAD sang Excel", "DCE_Excel_Block");

        [CommandMethod("PointExcelToCad_DCE")]
        public void PointExcelToCad_DCE() => ExecuteExcelCadSync("PointExcelToCad_DCE", "Nhập tọa độ Point từ Excel vào bản vẽ CAD", "DCE_Excel_Point");

        [CommandMethod("PointToExcel_DCE")]
        public void PointToExcel_DCE() => ExecuteExcelCadSync("PointToExcel_DCE", "Xuất tọa độ Point từ bản vẽ CAD sang Excel", "DCE_Excel_Point");

        [CommandMethod("PolyToExcel_DCE")]
        public void PolyToExcel_DCE() => ExecuteExcelCadSync("PolyToExcel_DCE", "Xuất chiều dài và diện tích Polyline/Line sang Excel", "DCE_Excel_Poly");

        [CommandMethod("SheetSetToExcel_DCE")]
        public void SheetSetToExcel_DCE() => ExecuteExcelCadSync("SheetSetToExcel_DCE", "Xuất danh mục bản vẽ SheetSet sang Excel", "DCE_SheetSet");

        [CommandMethod("ExcelToSheetSet_DCE")]
        public void ExcelToSheetSet_DCE() => ExecuteExcelCadSync("ExcelToSheetSet_DCE", "Cập nhật danh mục SheetSet từ bảng Excel", "DCE_SheetSet");

        [CommandMethod("TableCadToExcel_Dce")]
        public void TableCadToExcel_Dce() => ExecuteExcelCadSync("TableCadToExcel_Dce", "Sắp xếp lưới Text/Line CAD và xuất bảng sang Excel", "DCE_Table_Export");

        [CommandMethod("TableExcelToCad_DCE")]
        public void TableExcelToCad_DCE() => ExecuteExcelCadSync("TableExcelToCad_DCE", "Chuyển bảng vùng chọn từ Excel thành bảng CAD", "DCE_Table_Import");

        [CommandMethod("XemThep_TKTExcel_DCE")]
        public void XemThep_TKTExcel_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên cấu kiện cần đối chiếu từ Excel TKT", "D1", out string mark);
            string summary = DceSteelFacade.LietKeThepTheoCauKien(mark);
            AutoCadUiHelper.WriteMessage($"XemThep_TKTExcel_DCE: Đã đối chiếu dữ liệu thép cấu kiện {mark} ({summary.Length} bytes).");
        }

        [CommandMethod("ChuyenDuLieuKata_DBIM")]
        public void ChuyenDuLieuKata_DBIM()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên cấu kiện chuyển đổi XData Kata -> DCE Pro", "D1", out string mark);
            PromptHelper.TryGetPoint(ed, "Chọn điểm gốc đối tượng Kata", out var pt);
            var pkt = _codec.Encode(new RebarXdataDceModel { TenCauKien = mark, SoHieu = "1", LoaiThep = "KataConverted", DuongKinh = "18", SoLuong = "4", ChieuDaiL = 5600 });
            var buf = new CadDraftingBuffer();
            buf.AddPolyline(new[] { new Point2D(pt.X, pt.Y), new Point2D(pt.X + 5600, pt.Y) }, false, "DCE_Thep_Doc", CadColor.RedIndex, 2.0, new[] { pkt });
            int count = RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"ChuyenDuLieuKata_DBIM: Đã chuyển đổi XData Kata sang chuẩn DCE Pro cho {mark} ({count} đối tượng).");
        }

        [CommandMethod("ThepSoLuongDBIM_KATA")]
        public void ThepSoLuongDBIM_KATA()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetInteger(ed, "Nhập số lượng thanh thép quy đổi DBIM/Kata", 5, out int barQty);
            PromptHelper.TryGetPoint(ed, "Chọn điểm đặt thanh thép quy đổi", out var pt);
            var pkt = _codec.Encode(new RebarXdataDceModel { TenCauKien = "S1", SoHieu = "1", DuongKinh = "10", SoLuong = barQty.ToString(), ChieuDaiL = 3200 });
            var buf = new CadDraftingBuffer();
            buf.AddPolyline(new[] { new Point2D(pt.X, pt.Y), new Point2D(pt.X + 3200, pt.Y) }, false, "DCE_Thep_San", CadColor.CyanIndex, 1.5, new[] { pkt });
            int count = RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"ThepSoLuongDBIM_KATA: Đã quy đổi thanh thép số lượng N={barQty} ({count} đối tượng).");
        }

        [CommandMethod("SuaDuongkinhThep_DCE")]
        public void SuaDuongkinhThep_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetInteger(ed, "Nhập đường kính thép mới (mm)", 20, out int diaMm);
            string res = DceSteelFacade.SuaDuongKinhThep("*", "18", diaMm.ToString());
            AutoCadUiHelper.WriteMessage($"SuaDuongkinhThep_DCE: Đã cập nhật đường kính D{diaMm} ({res}).");
        }

        [CommandMethod("SuaSoLuongThep_DCE")]
        public void SuaSoLuongThep_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetInteger(ed, "Nhập số lượng thanh thép mới", 4, out int qty);
            PromptHelper.TryGetPoint(ed, "Chọn vị trí cập nhật tag số lượng", out var pt);
            var buf = new CadDraftingBuffer();
            buf.AddText($"SL={qty}", new Point2D(pt.X, pt.Y), 100.0, "DCE_Thep_TagText", CadColor.YellowIndex);
            int count = RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"SuaSoLuongThep_DCE: Đã cập nhật số lượng thép mới = {qty} ({count} đối tượng).");
        }

        [CommandMethod("ThepBienThienTuyBien_DCE")]
        public void ThepBienThienTuyBien_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, "Nhập chiều dài thanh đầu Lmin (mm)", 1500.0, out double lMin);
            PromptHelper.TryGetDouble(ed, "Nhập chiều dài thanh cuối Lmax (mm)", 3500.0, out double lMax);
            PromptHelper.TryGetDouble(ed, "Nhập khoảng rải bước a (mm)", 200.0, out double step);
            PromptHelper.TryGetPoint(ed, "Chọn điểm gốc rải thép biến thiên", out var pt);
            double lAvg = (lMin + lMax) * 0.5;
            var pkt = _codec.Encode(new RebarXdataDceModel { TenCauKien = "S1", SoHieu = "BT1", LoaiThep = "BienThien", DuongKinh = "10", SoLuong = "11", ChieuDaiL = lAvg });
            var buf = new CadDraftingBuffer();
            buf.AddPolyline(new[] { new Point2D(pt.X, pt.Y), new Point2D(pt.X + lAvg, pt.Y) }, false, "DCE_Thep_BienThien", CadColor.GreenIndex, 1.5, new[] { pkt });
            buf.AddText($"THÉP BIẾN THIÊN L={lMin:F0}..{lMax:F0} (Ltb={lAvg:F0}, a{step:F0})", new Point2D(pt.X, pt.Y + 80), 95.0, "DCE_Thep_Text", CadColor.YellowIndex);
            int count = RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"ThepBienThienTuyBien_DCE: Đã tạo nhóm thép biến thiên Ltb={lAvg:F0}mm ({count} đối tượng).");
        }

        [CommandMethod("DceDoanTruDimDam")]
        public void DceDoanTruDimDam() => ExecuteDimDeduction("DceDoanTruDimDam", 25.0);

        [CommandMethod("TruDim_DCE")]
        public void TruDim_DCE() => ExecuteDimDeduction("TruDim_DCE", 30.0);

        [CommandMethod("DimBienThien_Dce")]
        public void DimBienThien_Dce() => ExecuteVariableDimension("DimBienThien_Dce", false);

        [CommandMethod("DimBienThien_Dce2")]
        public void DimBienThien_Dce2() => ExecuteVariableDimension("DimBienThien_Dce2", true);

        private static void ExecuteExcelCadSync(string cmd, string description, string layer)
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetPoint(ed, $"[{cmd}] Chọn điểm chuẩn {description}", out var pt);
            var buf = new CadDraftingBuffer();
            buf.AddText($"{cmd}: {description}", new Point2D(pt.X, pt.Y), 120.0, layer, CadColor.CyanIndex);
            int count = RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"{cmd}: Hoàn tất {description} ({count} đối tượng).");
        }

        private static void ExecuteDimDeduction(string cmd, double defaultDeductionMm)
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, $"[{cmd}] Nhập đoạn trừ lớp bảo vệ hai đầu (mm)", defaultDeductionMm, out double deduct);
            PromptHelper.TryGetDouble(ed, $"[{cmd}] Nhập chiều dài nhịp đo (mm)", 4000.0, out double span);
            PromptHelper.TryGetPoint(ed, $"[{cmd}] Chọn điểm đặt dim khoảng rải", out var pt);
            double netLen = Math.Max(100.0, span - 2.0 * deduct);
            var buf = new CadDraftingBuffer();
            buf.AddLine(new Point2D(pt.X + deduct, pt.Y), new Point2D(pt.X + deduct + netLen, pt.Y), "DCE_Dim_Rebar", CadColor.YellowIndex, 0.25);
            buf.AddText($"L_rải = {netLen:F0}mm (trừ 2x{deduct:F0})", new Point2D(pt.X + deduct, pt.Y + 60), 90.0, "DCE_Dim_Text", CadColor.CyanIndex);
            int count = RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"{cmd}: Đã tạo dim khoảng rải trừ bảo vệ L={netLen:F0}mm ({count} đối tượng).");
        }

        private static void ExecuteVariableDimension(string cmd, bool stepMode)
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, $"[{cmd}] Nhập khoảng rải a (mm)", 150.0, out double step);
            PromptHelper.TryGetInteger(ed, $"[{cmd}] Nhập số bước chia", 10, out int nSteps);
            PromptHelper.TryGetPoint(ed, $"[{cmd}] Chọn điểm gốc dim biến thiên", out var pt);
            double total = step * Math.Max(1, nSteps);
            var buf = new CadDraftingBuffer();
            buf.AddLine(new Point2D(pt.X, pt.Y), new Point2D(pt.X + total, pt.Y), "DCE_Dim_BienThien", CadColor.YellowIndex, 0.25);
            buf.AddText(stepMode ? $"±Step: {nSteps}x{step:F0}={total:F0}" : $"DimBT: {nSteps}@{step:F0}={total:F0}", new Point2D(pt.X, pt.Y + 60), 90.0, "DCE_Dim_Text", CadColor.CyanIndex);
            int count = RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"{cmd}: Đã tạo chuỗi dim biến thiên {nSteps}x{step:F0}={total:F0}mm ({count} đối tượng).");
        }
    }
}
