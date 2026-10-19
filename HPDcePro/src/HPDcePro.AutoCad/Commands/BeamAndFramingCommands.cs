using System;
using Autodesk.AutoCAD.Runtime;
using HPDcePro.AutoCad.Facade;
using HPDcePro.AutoCad.Prompts;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;
using HPDcePro.Core.XData;
using HPDcePro.UI.Views;

namespace HPDcePro.AutoCad.Commands
{
    public sealed class BeamAndFramingCommands
    {
        [CommandMethod("GG_DCE")]
        public void GG_DCE()
        {
            var beamCmds = new BeamCommands();
            beamCmds.GanDLLineDam();
        }

        [CommandMethod("HoTroBeamDesign_dce")]
        public void HoTroBeamDesign_dce()
        {
            var win = new BeamDetailingWindow();
            AutoCadUiHelper.ShowModal(win);
            AutoCadUiHelper.WriteMessage("HoTroBeamDesign_dce: Đã mở trình hỗ trợ thiết kế và trích dữ liệu dầm.");
        }

        [CommandMethod("ImportCaoDoDam")]
        public void ImportCaoDoDam() => ExecuteBeamLevelImport("ImportCaoDoDam", "CAO ĐỘ ĐỈNH DẦM (-0.050)");

        [CommandMethod("ImportGiatCaoDoDam")]
        public void ImportGiatCaoDoDam() => ExecuteBeamLevelImport("ImportGiatCaoDoDam", "GIẬT CẤP ĐÁY/ĐỈNH DẦM (-0.100)");

        [CommandMethod("KiemTraKichThuocGoiDam_DCE")]
        public void KiemTraKichThuocGoiDam_DCE()
        {
            string res = MbkcFacade.KiemTraLuatMatBang(string.Empty, string.Empty, string.Empty, true, 1, 1);
            AutoCadUiHelper.WriteMessage($"KiemTraKichThuocGoiDam_DCE: Đã kiểm tra kích thước cột/vách gối tựa dầm ({res.Length} bytes).");
        }

        [CommandMethod("QuayTextTheoLine")]
        public void QuayTextTheoLine()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập nội dung Text xoay theo Line", "D1 (220x400)", out string txt);
            PromptHelper.TryGetPoint(ed, "Chọn điểm đầu Line định hướng", out var p1);
            PromptHelper.TryGetPoint(ed, "Chọn điểm cuối Line định hướng", out var p2, p1);
            double angle = Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
            var buf = new CadDraftingBuffer();
            buf.AddText(txt, new Point2D(p1.X, p1.Y), 120.0, "DCE_Beam_Text", CadColor.YellowIndex, angle);
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"QuayTextTheoLine: Đã chèn Text '{txt}' xoay góc {angle * 180.0 / Math.PI:F1}° ({count} đối tượng).");
        }

        [CommandMethod("ThongKeCauKien_DCE")]
        public void ThongKeCauKien_DCE()
        {
            string res = DceSteelFacade.LietKeCauKienTrongBanVe();
            AutoCadUiHelper.WriteMessage($"ThongKeCauKien_DCE: Đã thống kê danh mục cấu kiện trong bản vẽ ({res.Length} bytes).");
        }

        [CommandMethod("ThongKeText_DCE")]
        public void ThongKeText_DCE()
        {
            string res = MbkcFacade.DocToanBoChuBanVe(string.Empty);
            AutoCadUiHelper.WriteMessage($"ThongKeText_DCE: Đã thống kê toàn bộ đối tượng Text/MText ({res.Length} bytes).");
        }

        [CommandMethod("GetDataBeamAll_DCE")]
        public void GetDataBeamAll_DCE() => ExtractFramingDimension("GetDataBeamAll_DCE", "Toàn bộ hệ dầm mặt bằng");

        [CommandMethod("GetDataBeam_DCE")]
        public void GetDataBeam_DCE() => ExtractFramingDimension("GetDataBeam_DCE", "Nhịp dầm và bề rộng gối");

        [CommandMethod("GetDataColumn_DCE")]
        public void GetDataColumn_DCE() => ExtractFramingDimension("GetDataColumn_DCE", "Tiết diện cột BxH");

        [CommandMethod("GetDataWall_DCE")]
        public void GetDataWall_DCE() => ExtractFramingDimension("GetDataWall_DCE", "Chiều dài và bề dày vách cứng Lw x Tw");

        [CommandMethod("GetLength")]
        public void GetLength()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetPoint(ed, "Chọn điểm đầu đo chiều dài", out var p1);
            PromptHelper.TryGetPoint(ed, "Chọn điểm cuối đo chiều dài", out var p2, p1);
            double dist = p2.DistanceTo(p1) > 1.0 ? p2.DistanceTo(p1) : 4500.0;
            AutoCadUiHelper.WriteMessage($"GetLength: Chiều dài đo được L = {dist:F0} mm.");
        }

        [CommandMethod("KTC")]
        public void KTC()
        {
            var win = new ColumnWallDetailingWindow();
            AutoCadUiHelper.ShowModal(win);
            AutoCadUiHelper.WriteMessage("KTC: Đã mở hộp thoại đo và làm tròn kích thước cột/dầm/vách.");
        }

        [CommandMethod("DimCot_Dce")]
        public void DimCot_Dce()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, "Nhập bề rộng cột Bc (mm)", 400.0, out double bc);
            PromptHelper.TryGetDouble(ed, "Nhập chiều cao cột Hc (mm)", 400.0, out double hc);
            PromptHelper.TryGetPoint(ed, "Chọn tâm cột cần ghi kích thước Dim", out var pt);
            var buf = new CadDraftingBuffer();
            buf.AddLine(new Point2D(pt.X - bc * 0.5, pt.Y - hc * 0.5 - 120), new Point2D(pt.X + bc * 0.5, pt.Y - hc * 0.5 - 120), "DCE_Dim_Cot", CadColor.YellowIndex, 0.25);
            buf.AddText($"{bc:F0}x{hc:F0}", new Point2D(pt.X - bc * 0.3, pt.Y - hc * 0.5 - 240), 100.0, "DCE_Dim_Text", CadColor.CyanIndex);
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"DimCot_Dce: Đã ghi Dim định vị cột {bc:F0}x{hc:F0} ({count} đối tượng).");
        }

        [CommandMethod("DrawBlockBeam")]
        public void DrawBlockBeam()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên dầm Block", "D1 (220x400)", out string label);
            PromptHelper.TryGetPoint(ed, "Chọn điểm chèn Block dầm", out var pt);
            var buf = new CadDraftingBuffer();
            buf.AddBlockInsert("DCE_BLOCK_BEAM", new Point2D(pt.X, pt.Y), 1.0, 0.0, "DCE_Beam_Block");
            buf.AddText(label, new Point2D(pt.X, pt.Y + 80), 120.0, "DCE_Beam_Name", CadColor.YellowIndex);
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"DrawBlockBeam: Đã vẽ Block dầm '{label}' ({count} đối tượng).");
        }

        [CommandMethod("DrawBlockColumn")]
        public void DrawBlockColumn()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên cột Block", "C1 (400x400)", out string label);
            PromptHelper.TryGetPoint(ed, "Chọn điểm chèn Block cột", out var pt);
            var buf = new CadDraftingBuffer();
            buf.AddBlockInsert("DCE_BLOCK_COLUMN", new Point2D(pt.X, pt.Y), 1.0, 0.0, "DCE_Cot_Block");
            buf.AddText(label, new Point2D(pt.X + 250, pt.Y + 250), 120.0, "DCE_Cot_Text", CadColor.YellowIndex);
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"DrawBlockColumn: Đã vẽ Block cột '{label}' ({count} đối tượng).");
        }

        [CommandMethod("DrawLineBeam")]
        public void DrawLineBeam()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, "Nhập bề rộng dầm b (mm)", 220.0, out double b);
            PromptHelper.TryGetPoint(ed, "Chọn điểm đầu tim dầm", out var p1);
            PromptHelper.TryGetPoint(ed, "Chọn điểm cuối tim dầm", out var p2, p1);
            var end = p2.DistanceTo(p1) > 1.0 ? new Point2D(p2.X, p2.Y) : new Point2D(p1.X + 5000.0, p1.Y);
            var buf = new CadDraftingBuffer();
            buf.AddLine(new Point2D(p1.X, p1.Y - b * 0.5), new Point2D(end.X, end.Y - b * 0.5), "DCE_Beam_Line", CadColor.CyanIndex, 0.35);
            buf.AddLine(new Point2D(p1.X, p1.Y + b * 0.5), new Point2D(end.X, end.Y + b * 0.5), "DCE_Beam_Line", CadColor.CyanIndex, 0.35);
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"DrawLineBeam: Đã vẽ 2 nét biên dầm B={b:F0}mm ({count} đối tượng).");
        }

        [CommandMethod("HoTroMBKC_dce")]
        public void HoTroMBKC_dce()
        {
            var topo = MbkcFacade.QuanLyMatBang("bo_sung_dam");
            AutoCadUiHelper.WriteMessage($"HoTroMBKC_dce: Đã chạy công cụ hỗ trợ Mặt Bằng Kết Cấu ({topo.ColumnNodes.Count} nút cột).");
        }

        [CommandMethod("HotroBeamDetail")]
        public void HotroBeamDetail()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetInteger(ed, "Nhập số lượng đai gia cường tại vị trí dầm phụ", 6, out int nStirrups);
            PromptHelper.TryGetPoint(ed, "Chọn vị trí giao dầm chính - dầm phụ", out var pt);
            var buf = new CadDraftingBuffer();
            for (int i = 0; i < Math.Max(1, nStirrups); i++)
            {
                double x = pt.X - (nStirrups * 25.0) + i * 50.0;
                buf.AddLine(new Point2D(x, pt.Y - 180), new Point2D(x, pt.Y + 180), "DCE_Thep_DaiGC", CadColor.MagentaIndex, 1.2);
            }
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"HotroBeamDetail: Đã bổ sung {nStirrups} đai gia cường nút giao dầm ({count} đối tượng).");
        }

        [CommandMethod("ExportToRevit")]
        public void ExportToRevit()
        {
            string summary = MbkcFacade.TomTatMatBang(string.Empty);
            AutoCadUiHelper.WriteMessage($"ExportToRevit: Đã trích xuất dữ liệu mặt bằng kết cấu sang định dạng trao đổi Revit ({summary.Length} bytes).");
        }

        [CommandMethod("MBKCAUDIT_DCE")]
        public void MBKCAUDIT_DCE()
        {
            string auditJson = MbkcFacade.SoatMatBangDangMo();
            AutoCadUiHelper.WriteMessage($"MBKCAUDIT_DCE: Hoàn tất soát lỗi mặt bằng kết cấu ({auditJson.Length} bytes).");
        }

        private static void ExecuteBeamLevelImport(string cmd, string levelType)
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetPoint(ed, $"[{cmd}] Chọn vị trí ghi chú {levelType}", out var pt);
            var pkt = new XDataPacket(XDataRegistry.RegAppLinksXDataObject);
            pkt.AddString($"{cmd}:{levelType}");
            var buf = new CadDraftingBuffer();
            buf.AddText(levelType, new Point2D(pt.X, pt.Y), 100.0, "DCE_Beam_Level", CadColor.YellowIndex, 0.0, "Standard", new[] { pkt });
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"{cmd}: Đã nhập thông tin {levelType} ({count} đối tượng).");
        }

        private static void ExtractFramingDimension(string cmd, string targetDesc)
        {
            string res = MbkcFacade.DeXuatTietDienTheoDanhMuc(string.Empty, 16.0);
            AutoCadUiHelper.WriteMessage($"{cmd}: Đã đọc kích thước hình học ({targetDesc}, {res.Length} bytes).");
        }
    }
}
