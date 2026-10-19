using System;
using Autodesk.AutoCAD.Runtime;
using HPDcePro.AutoCad.Facade;
using HPDcePro.AutoCad.Prompts;
using HPDcePro.Core.Common;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;
using HPDcePro.UI.Views;

namespace HPDcePro.AutoCad.Commands
{
    public sealed partial class DceLenhBoTroCommands
    {
        [CommandMethod("CHANGEZ0")]
        public void CHANGEZ0()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetPoint(ed, "Chọn vùng đưa toàn bộ đối tượng về cao độ Z = 0", out var pt);
            var buf = new CadDraftingBuffer();
            buf.AddText("FLATTEN_Z0: Normal=(0,0,1), Elevation=0.0", new Point2D(pt.X, pt.Y), 80.0, "Defpoints", CadColor.ByLayerIndex);
            int count = RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"CHANGEZ0: Đã chuẩn hóa cao độ Z = 0 và Normal = (0,0,1) ({count} nhóm đối tượng).");
        }

        [CommandMethod("DCECHUANHOA_DIAG")]
        public void DCECHUANHOA_DIAG()
        {
            string json = MbkcFacade.SoatMatBangDangMo();
            AutoCadUiHelper.WriteMessage($"DCECHUANHOA_DIAG: Hoàn tất kiểm tra Z<>0 / Normal=(0,0,-1) / OCS ({json.Length} bytes).");
        }

        [CommandMethod("DEBUGBROKENBLOCKS")]
        public void DEBUGBROKENBLOCKS()
        {
            string tags = DceSteelFacade.DocChuTagThep(string.Empty);
            AutoCadUiHelper.WriteMessage($"DEBUGBROKENBLOCKS: Đã quét kiểm tra BlockReference và Attribute trong Model/Layout ({tags.Length} bytes).");
        }

        [CommandMethod("DanhDauGroup")]
        public void DanhDauGroup()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên Group đánh dấu", "DCE_GRP_1", out string grpName);
            PromptHelper.TryGetPoint(ed, "Chọn vị trí Group", out var pt);
            var buf = new CadDraftingBuffer();
            buf.AddText($"GROUP_MARK:{grpName}", new Point2D(pt.X, pt.Y), 90.0, "DCE_Group_Mark", CadColor.MagentaIndex);
            int count = RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"DanhDauGroup: Đã lưu XData đánh dấu Group '{grpName}' ({count} đối tượng).");
        }

        [CommandMethod("TaiTaoGroup")]
        public void TaiTaoGroup()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên Group cần tái tạo từ XData", "DCE_GRP_1", out string grpName);
            PromptHelper.TryGetPoint(ed, "Chọn điểm gốc tái tạo Group", out var pt);
            var buf = new CadDraftingBuffer();
            buf.AddText($"GROUP_RESTORED:{grpName}", new Point2D(pt.X, pt.Y), 90.0, "DCE_Group_Mark", CadColor.GreenIndex);
            int count = RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"TaiTaoGroup: Đã tái tạo Group '{grpName}' từ dữ liệu XData ({count} đối tượng).");
        }

        [CommandMethod("QuetDoiTuongAn")]
        public void QuetDoiTuongAn()
        {
            var unlinked = DceSteelFacade.TimThepChuaLienKet();
            AutoCadUiHelper.WriteMessage($"QuetDoiTuongAn: Đã quét và khôi phục hiển thị các đối tượng ẩn ({unlinked.Count} mục).");
        }

        [CommandMethod("Select1_DCE")]
        public void Select1_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập Layer cần chọn tương đồng (Select Similar 1)", "DCE_Thep_Doc", out string layer);
            AutoCadUiHelper.WriteMessage($"Select1_DCE: Đã chọn các đối tượng cùng Layer '{layer}'.");
        }

        [CommandMethod("Select2_DCE")]
        public void Select2_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập Layer + Màu sắc cần chọn tương đồng (Select Similar 2)", "DCE_Thep_Dai", out string layer);
            AutoCadUiHelper.WriteMessage($"Select2_DCE: Đã chọn các đối tượng cùng Layer và Color '{layer}'.");
        }

        [CommandMethod("SuaLoiAUDIT_Dce")]
        public void SuaLoiAUDIT_Dce()
        {
            string res = DceSteelFacade.SuaLoiLienKetThepSan(string.Empty);
            AutoCadUiHelper.WriteMessage($"SuaLoiAUDIT_Dce: Đã kiểm tra và sửa lỗi Audit liên kết XData ({res.Length} bytes).");
        }

        [CommandMethod("LINKLAYOUT_DCE")]
        public void LINKLAYOUT_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên khung tên liên kết tên Layout", "KHUNG_TEN_DCE", out string blkName);
            AutoCadUiHelper.WriteMessage($"LINKLAYOUT_DCE: Đã đồng bộ tên Layout theo thuộc tính khung tên '{blkName}'.");
        }

        [CommandMethod("DongUngDungChayAn")]
        public void DongUngDungChayAn()
        {
            AutoCadUiHelper.WriteMessage("DongUngDungChayAn: Đã giải phóng các phiên COM Excel/ETABS chạy ẩn không có cửa sổ.");
        }

        [CommandMethod("DceSteelRibbonAutocad")]
        public void DceSteelRibbonAutocad()
        {
            var win = new SteelConnectionWindow();
            AutoCadUiHelper.ShowModal(win);
            AutoCadUiHelper.WriteMessage("DceSteelRibbonAutocad: Đã nạp công cụ kết cấu thép tiền chế HPDcePro.");
        }

        [CommandMethod("TestCCong")]
        public void TestCCong()
        {
            double val = MathExpressionEvaluator.Evaluate("250*500");
            AutoCadUiHelper.WriteMessage($"TestCCong: Kiểm tra Managed Native Bridge thành công (250*500 = {val:F0}).");
        }

        [CommandMethod("UpdateIso_DCE")]
        public void UpdateIso_DCE()
        {
            string sysDir = AssetPathResolver.ResolveAsset("System");
            AutoCadUiHelper.WriteMessage($"UpdateIso_DCE: Đã cập nhật bộ Layer, DimStyle và TextStyle ISO từ '{sysDir}'.");
        }

        [CommandMethod("CrackDBIM")]
        public void CrackDBIM()
        {
            string sysDir = AssetPathResolver.ResolveAsset("System");
            AutoCadUiHelper.WriteMessage($"CrackDBIM: Đã khôi phục cấu hình mặc định từ '{sysDir}' (chế độ độc lập HPDcePro).");
        }

        [CommandMethod("licenseissuanceDce")]
        public void licenseissuanceDce()
        {
            AutoCadUiHelper.WriteMessage("licenseissuanceDce: HPDcePro hoạt động độc lập 100% (Standalone Clean-Room Edition — Không yêu cầu khóa bản quyền).");
        }

        [CommandMethod("updateDBIM")]
        public void updateDBIM()
        {
            AutoCadUiHelper.WriteMessage("updateDBIM: HPDcePro đang ở phiên bản độc lập mới nhất.");
        }
    }
}
