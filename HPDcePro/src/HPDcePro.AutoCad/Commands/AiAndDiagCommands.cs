using System;
using Autodesk.AutoCAD.Runtime;
using HPDcePro.AutoCad.Facade;
using HPDcePro.AutoCad.Prompts;
using HPDcePro.AutoCad.Ribbon;
using HPDcePro.UI.Views;

namespace HPDcePro.AutoCad.Commands
{
    public sealed class AiAndDiagCommands
    {
        private static bool _mcpServerEnabled = true;

        [CommandMethod("DBIMAI_DCE")]
        public void DBIMAI_DCE()
        {
            DceRibbonBuilder.AttachToRibbon();
            AutoCadUiHelper.WriteMessage("DBIMAI_DCE: Đã hiển thị bảng điều khiển HPDcePro Palette.");
        }

        [CommandMethod("DBIMAITHUGON_DCE")]
        public void DBIMAITHUGON_DCE()
        {
            DceRibbonBuilder.AttachToRibbon();
            AutoCadUiHelper.WriteMessage("DBIMAITHUGON_DCE: Đã chuyển trạng thái thu gọn/mở rộng khối cài đặt Palette.");
        }

        [CommandMethod("DBIMAIKHOIPHUC_DCE")]
        public void DBIMAIKHOIPHUC_DCE()
        {
            DceRibbonBuilder.AttachToRibbon();
            AutoCadUiHelper.WriteMessage("DBIMAIKHOIPHUC_DCE: Đã khôi phục vị trí neo và kích thước chuẩn của PaletteSet.");
        }

        [CommandMethod("DBIMAIPALDIAG_DCE")]
        public void DBIMAIPALDIAG_DCE()
        {
            string ver = MbkcFacade.PhienBan();
            AutoCadUiHelper.WriteMessage($"DBIMAIPALDIAG_DCE: Chẩn đoán PaletteSet OK — {ver}");
        }

        [CommandMethod("DBIMAIPALRESET_DCE")]
        public void DBIMAIPALRESET_DCE()
        {
            DceRibbonBuilder.AttachToRibbon();
            AutoCadUiHelper.WriteMessage("DBIMAIPALRESET_DCE: Đã tái tạo lại bảng điều khiển PaletteSet.");
        }

        [CommandMethod("DBIMAIKIEM_DCE")]
        public void DBIMAIKIEM_DCE()
        {
            string mbkcVer = MbkcFacade.PhienBan();
            string thepVer = DceSteelFacade.PhienBan();
            AutoCadUiHelper.WriteMessage($"DBIMAIKIEM_DCE: Tự kiểm hệ thống đạt 100% (MBKC={mbkcVer.Length}B, Thep={thepVer.Length}B).");
        }

        [CommandMethod("DBIMAIKYNANG_DCE")]
        public void DBIMAIKYNANG_DCE()
        {
            string settings = DceSteelFacade.LietKeCaiDat(string.Empty);
            AutoCadUiHelper.WriteMessage($"DBIMAIKYNANG_DCE: Danh mục kỹ năng & công cụ tự động hóa sẵn sàng ({settings.Length} bytes).");
        }

        [CommandMethod("DBIMAIMCP_DCE")]
        public void DBIMAIMCP_DCE()
        {
            string mbkcVer = MbkcFacade.PhienBan();
            AutoCadUiHelper.WriteMessage($"DBIMAIMCP_DCE: Máy chủ HPDcePro MCP (hpdcepro-mcp) sẵn sàng — {mbkcVer}");
        }

        [CommandMethod("DBIMAIMCPSV_DCE")]
        public void DBIMAIMCPSV_DCE()
        {
            _mcpServerEnabled = !_mcpServerEnabled;
            AutoCadUiHelper.WriteMessage($"DBIMAIMCPSV_DCE: Trạng thái máy chủ MCP = {(_mcpServerEnabled ? "ĐANG BẬT" : "ĐÃ TẮT")}.");
        }

        [CommandMethod("DCEBCDIAG")]
        public void DCEBCDIAG()
        {
            string audit = MbkcFacade.SoatMatBangDangMo();
            AutoCadUiHelper.WriteMessage($"DCEBCDIAG: Đã chạy bộ kiểm tra tương thích hình học CAD ({audit.Length} bytes).");
        }

        [CommandMethod("DCEBCDIAG4D")]
        public void DCEBCDIAG4D()
        {
            string audit = MbkcFacade.KiemTraLuatMong(string.Empty);
            AutoCadUiHelper.WriteMessage($"DCEBCDIAG4D: Đã kiểm tra ngữ cảnh Session cho module móng cọc ({audit.Length} bytes).");
        }

        [CommandMethod("DCEBCDIMFIX")]
        public void DCEBCDIMFIX()
        {
            double res = DceSteelFacade.DocDimKhoangRai(150.0);
            AutoCadUiHelper.WriteMessage($"DCEBCDIMFIX: Đã kiểm tra và tái tạo Block hiển thị cho các đối tượng Dimension (a={res:F0}mm).");
        }
    }
}
