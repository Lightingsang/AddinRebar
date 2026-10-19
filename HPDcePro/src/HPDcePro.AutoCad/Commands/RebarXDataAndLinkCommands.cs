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
    public sealed class RebarXDataAndLinkCommands
    {
        private readonly RebarXDataCodec _codec = new();

        [CommandMethod("CopyKhoangRaiThep")]
        public void CopyKhoangRaiThep()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập handle thanh nguồn", "1A", out string src);
            PromptHelper.TryGetString(ed, "Nhập handle các thanh đích", "1B", out string dst);
            string res = DceSteelFacade.CopyKhoangRaiThep(src, dst);
            AutoCadUiHelper.WriteMessage($"CopyKhoangRaiThep: Đã sao chép khoảng rải thép ({res.Length} bytes).");
        }

        [CommandMethod("CopySoHieuThep")]
        public void CopySoHieuThep()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập handle thanh nguồn", "1A", out string src);
            PromptHelper.TryGetString(ed, "Nhập handle các thanh đích", "1B", out string dst);
            string res = DceSteelFacade.CopySoHieuThep(src, dst);
            AutoCadUiHelper.WriteMessage($"CopySoHieuThep: Đã sao chép số hiệu thép ({res.Length} bytes).");
        }

        [CommandMethod("DocXdata")]
        public void DocXdata()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập handle đối tượng cần đọc XData", "1A", out string handle);
            string res = DceSteelFacade.DocXDataThoTatCaRegapp(handle);
            AutoCadUiHelper.WriteMessage($"DocXdata: Kết quả đọc toàn bộ RegApp trên '{handle}': {res}");
        }

        [CommandMethod("RegenColor_DCE")]
        public void RegenColor_DCE()
        {
            string res = DceSteelFacade.TraMauThepVeByLayer();
            AutoCadUiHelper.WriteMessage($"RegenColor_DCE: Đã đưa màu các thanh thép về ByLayer ({res.Length} bytes).");
        }

        [CommandMethod("rrr")]
        public void rrr() => RegenColor_DCE();

        [CommandMethod("ReplaceDuLieuAn")]
        public void ReplaceDuLieuAn()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập chuỗi XData cần tìm", "D1", out string findStr);
            PromptHelper.TryGetString(ed, "Nhập chuỗi XData thay thế", "D2", out string replStr);
            string res = DceSteelFacade.TimThayTheXData(findStr, replStr, string.Empty);
            AutoCadUiHelper.WriteMessage($"ReplaceDuLieuAn: Đã tìm '{findStr}' và thay bằng '{replStr}' ({res.Length} bytes).");
        }

        [CommandMethod("ResetSoHieuThep_DCE")]
        public void ResetSoHieuThep_DCE()
        {
            string res = DceSteelFacade.ResetBangThongKe(string.Empty);
            AutoCadUiHelper.WriteMessage($"ResetSoHieuThep_DCE: Đã đánh lại số hiệu thép tuần tự ({res.Length} bytes).");
        }

        [CommandMethod("TagThep_DCE")]
        public void TagThep_DCE()
        {
            string res = DceSteelFacade.TagThepTuDong(string.Empty);
            AutoCadUiHelper.WriteMessage($"TagThep_DCE: Đã gắn và tạo Tag thép tự động ({res.Length} bytes).");
        }

        [CommandMethod("UpdateSoHieuExcel")]
        public void UpdateSoHieuExcel()
        {
            string res = DceSteelFacade.ResetBangThongKe(string.Empty);
            AutoCadUiHelper.WriteMessage($"UpdateSoHieuExcel: Đã đồng bộ số hiệu thép từ bảng Excel vào XData ({res.Length} bytes).");
        }

        [CommandMethod("XemLienKetTag")]
        public void XemLienKetTag()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập handle Tag thép cần kiểm tra liên kết", "1A", out string tagHandle);
            string res = DceSteelFacade.DocLienKetCuaTag(tagHandle);
            AutoCadUiHelper.WriteMessage($"XemLienKetTag: Liên kết của Tag '{tagHandle}': {res}");
        }

        [CommandMethod("XoaLinks_DCE")]
        public void XoaLinks_DCE()
        {
            string res = DceSteelFacade.XoaLienKetThep("*", "*");
            AutoCadUiHelper.WriteMessage($"XoaLinks_DCE: Đã xóa liên kết Tag/Dim khỏi XData thép ({res.Length} bytes).");
        }

        [CommandMethod("CreateField_DCE")]
        public void CreateField_DCE() => ExecuteFieldLink("CreateField_DCE", "Chiều dài L -> Attribute Tag");

        [CommandMethod("TaoField_DCE")]
        public void TaoField_DCE() => ExecuteFieldLink("TaoField_DCE", "Tạo biểu thức Field tự động cập nhật");

        [CommandMethod("FieldBlockAndPoly_DCE")]
        public void FieldBlockAndPoly_DCE() => ExecuteFieldLink("FieldBlockAndPoly_DCE", "Liên kết Field giữa Polyline thép và Block Tag");

        [CommandMethod("DimNhanh_dce")]
        public void DimNhanh_dce()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetPoint(ed, "Chọn điểm đầu đường gióng Dim nhanh", out var p1);
            PromptHelper.TryGetPoint(ed, "Chọn điểm cuối đường gióng Dim nhanh", out var p2, p1);
            double dist = p2.DistanceTo(p1) > 1.0 ? p2.DistanceTo(p1) : 3600.0;
            var buf = new CadDraftingBuffer();
            buf.AddLine(new Point2D(p1.X, p1.Y), new Point2D(p1.X + dist, p1.Y), "DCE_Dim", CadColor.YellowIndex, 0.25);
            buf.AddText($"{dist:F0}", new Point2D(p1.X + dist * 0.5, p1.Y + 60), 90.0, "DCE_Dim_Text", CadColor.CyanIndex);
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"DimNhanh_dce: Đã tạo Dim nhanh L={dist:F0}mm ({count} đối tượng).");
        }

        [CommandMethod("DimRebar_Dce2")]
        public void DimRebar_Dce2()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, "Nhập chiều dài vùng rải thép (mm)", 4000.0, out double span);
            PromptHelper.TryGetDouble(ed, "Nhập khoảng rải a (mm)", 150.0, out double step);
            PromptHelper.TryGetPoint(ed, "Chọn điểm đặt Dim Rebar 2", out var pt);
            int nBars = (int)Math.Ceiling(span / Math.Max(20.0, step)) + 1;
            var buf = new CadDraftingBuffer();
            buf.AddLine(new Point2D(pt.X, pt.Y), new Point2D(pt.X + span, pt.Y), "DCE_Dim_Rebar", CadColor.YellowIndex, 0.25);
            buf.AddText($"{nBars}Phi10a{step:F0}", new Point2D(pt.X + span * 0.5, pt.Y + 70), 95.0, "DCE_Thep_TagText", CadColor.WhiteIndex);
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"DimRebar_Dce2: Đã tạo Dim rải thép {nBars} thanh a{step:F0} ({count} đối tượng).");
        }

        [CommandMethod("ResetTag")]
        public void ResetTag()
        {
            string res = DceSteelFacade.SuaLoiLienKetThepSan(string.Empty);
            AutoCadUiHelper.WriteMessage($"ResetTag: Đã cập nhật lại chiều dài và số lượng trên Tag thép ({res.Length} bytes).");
        }

        [CommandMethod("SuaKhoangRaiThep_DCE")]
        public void SuaKhoangRaiThep_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetInteger(ed, "Nhập khoảng rải thép mới a (mm)", 150, out int stepMm);
            string res = DceSteelFacade.SuaKhoangRaiThep("*", "150", stepMm.ToString());
            AutoCadUiHelper.WriteMessage($"SuaKhoangRaiThep_DCE: Đã sửa khoảng rải sang a{stepMm} ({res.Length} bytes).");
        }

        [CommandMethod("SuaNhieuThep_DCE")]
        public void SuaNhieuThep_DCE()
        {
            var win = new MasterRebarScheduleDashboardWindow();
            AutoCadUiHelper.ShowModal(win);
            AutoCadUiHelper.WriteMessage("SuaNhieuThep_DCE: Đã mở trình chỉnh sửa hàng loạt thông số cốt thép.");
        }

        [CommandMethod("TKTSDuongKinhThep2")]
        public void TKTSDuongKinhThep2()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetInteger(ed, "Nhập đường kính thép mới D (mm)", 20, out int diaMm);
            string res = DceSteelFacade.SuaDuongKinhThep("*", "18", diaMm.ToString());
            AutoCadUiHelper.WriteMessage($"TKTSDuongKinhThep2: Đã cập nhật đường kính sang D{diaMm} ({res.Length} bytes).");
        }

        [CommandMethod("TKTSSoLuongCauKien2")]
        public void TKTSSoLuongCauKien2()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetInteger(ed, "Nhập số lượng cấu kiện mới", 4, out int countCk);
            string res = DceSteelFacade.SuaSoLuongCauKien("*", countCk.ToString());
            AutoCadUiHelper.WriteMessage($"TKTSSoLuongCauKien2: Đã cập nhật số lượng cấu kiện = {countCk} ({res.Length} bytes).");
        }

        [CommandMethod("CapNhapSoLuongBlockTag")]
        public void CapNhapSoLuongBlockTag()
        {
            string res = DceSteelFacade.CapNhapSoLuongDai("*");
            AutoCadUiHelper.WriteMessage($"CapNhapSoLuongBlockTag: Đã tính lại số lượng đai/thép rải trên Block Tag ({res.Length} bytes).");
        }

        private void ExecuteFieldLink(string cmd, string desc)
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetPoint(ed, $"[{cmd}] Chọn vị trí đặt Field ({desc})", out var pt);
            var pkt = _codec.Encode(new RebarXdataDceModel { TenCauKien = "D1", SoHieu = "F1", DuongKinh = "18", SoLuong = "2", ChieuDaiL = 4500 });
            var buf = new CadDraftingBuffer();
            buf.AddText($"%<\\AcObjProp Object(%<\\_ObjId 0>%).Length \\f \"%lu2%pr0\">%", new Point2D(pt.X, pt.Y), 95.0, "DCE_Field_Tag", CadColor.YellowIndex, 0.0, "Standard", new[] { pkt });
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"{cmd}: Đã tạo liên kết {desc} ({count} đối tượng).");
        }
    }
}
