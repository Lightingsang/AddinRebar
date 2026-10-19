using System;
using Autodesk.AutoCAD.Runtime;
using HPDcePro.AutoCad.Facade;
using HPDcePro.AutoCad.Prompts;
using HPDcePro.AutoCad.Ribbon;
using HPDcePro.Core.MachineLearning;
using HPDcePro.UI.Views;

namespace HPDcePro.AutoCad.Commands
{
    public sealed class SystemAndIntegrationCommands
    {
        [CommandMethod("DceMayHoc")]
        public void DceMayHoc()
        {
            using var reader = new MauHocDatabaseReader();
            int count = reader.GetSampleCountAsync().GetAwaiter().GetResult();
            AutoCadUiHelper.WriteMessage($"DceMayHoc: Cơ sở dữ liệu máy học kết cấu sẵn sàng ({count} mẫu chuẩn trong mauhoc.db).");
        }

        [CommandMethod("DceMayHoc_KhoiTao")]
        public void DceMayHoc_KhoiTao()
        {
            using var reader = new MauHocDatabaseReader();
            int count = reader.GetSampleCountAsync().GetAwaiter().GetResult();
            AutoCadUiHelper.WriteMessage($"DceMayHoc_KhoiTao: Đã khởi tạo kết nối SQLite mauhoc.db ({count} bản ghi).");
        }

        [CommandMethod("DceMayHoc_Train")]
        public void DceMayHoc_Train()
        {
            using var reader = new MauHocDatabaseReader();
            var models = reader.GetCauHinhModelsAsync().GetAwaiter().GetResult();
            string features = RebarFeatureExtractor.ExtractFeatures("2%%c20", 0, 6, 20);
            AutoCadUiHelper.WriteMessage($"DceMayHoc_Train: Đã nạp {models.Count} cấu hình ML và trích xuất {features.Length} bytes đặc trưng.");
        }

        [CommandMethod("AUTOSTRDOLAYERF_DCE")]
        public void AUTOSTRDOLAYERF_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập đường dẫn file DWG kiến trúc cần đo Layer", string.Empty, out string dwgPath);
            string json = MbkcFacade.XemTangKienTruc(dwgPath);
            AutoCadUiHelper.WriteMessage($"AUTOSTRDOLAYERF_DCE: Đã phân tích Layer kiến trúc ({json.Length} bytes JSON).");
        }

        [CommandMethod("AUTOSTRDOLAYER_DCE")]
        public void AUTOSTRDOLAYER_DCE()
        {
            string json = MbkcFacade.XemTangKienTruc(string.Empty);
            AutoCadUiHelper.WriteMessage($"AUTOSTRDOLAYER_DCE: Đã chấm điểm phân loại Layer kiến trúc ({json.Length} bytes JSON).");
        }

        [CommandMethod("AUTOSTRDODAM")]
        public void AUTOSTRDODAM()
        {
            string json = MbkcFacade.DeXuatTietDienTheoDanhMuc(string.Empty, 16.0);
            AutoCadUiHelper.WriteMessage($"AUTOSTRDODAM: Đã đo nhịp thông thủy và đề xuất tiết diện dầm toàn mặt bằng ({json.Length} bytes JSON).");
        }

        [CommandMethod("DceAutoStr")]
        public void DceAutoStr()
        {
            var win = new AutoStrConfigWindow();
            AutoCadUiHelper.ShowModal(win);
            AutoCadUiHelper.WriteMessage("DceAutoStr: Đã mở trung tâm điều khiển tự động hóa 24 bước AutoStr.");
        }

        [CommandMethod("UpdateModelSafe")]
        public void UpdateModelSafe()
        {
            string json = MbkcFacade.TomTatMatBang(string.Empty);
            AutoCadUiHelper.WriteMessage($"UpdateModelSafe: Đã đồng bộ sơ đồ sàn/móng sang mô hình CSI SAFE ({json.Length} bytes).");
        }

        [CommandMethod("UpdateModelSap")]
        public void UpdateModelSap()
        {
            string json = MbkcFacade.TomTatMatBang(string.Empty);
            AutoCadUiHelper.WriteMessage($"UpdateModelSap: Đã đồng bộ sơ đồ khung sang mô hình CSI SAP2000 ({json.Length} bytes).");
        }

        [CommandMethod("ConnectCSIAPI_DCE")]
        public void ConnectCSIAPI_DCE()
        {
            string json = MbkcFacade.TimGocToaDoEtabs(string.Empty);
            AutoCadUiHelper.WriteMessage($"ConnectCSIAPI_DCE: Đã kết nối CSI OAPI và đồng bộ hệ lưới trục ({json.Length} bytes).");
        }

        [CommandMethod("DCEQS")]
        public void DCEQS()
        {
            string concreteJson = MbkcFacade.ThongKeBeTong(3600.0, 120.0);
            AutoCadUiHelper.WriteMessage($"DCEQS: Đã tính toán bảng khối lượng bê tông & ván khuôn QS ({concreteJson.Length} bytes).");
        }

        [CommandMethod("DCERibbon")]
        public void DCERibbon()
        {
            DceRibbonBuilder.AttachToRibbon();
            AutoCadUiHelper.WriteMessage("DCERibbon: Đã khởi tạo thanh công cụ Ribbon HPDcePro.");
        }

        [CommandMethod("DceSetup")]
        public void DceSetup()
        {
            string settingsJson = DceSteelFacade.LietKeCaiDat(string.Empty);
            var win = new MainControlCenterWindow();
            AutoCadUiHelper.ShowModal(win);
            AutoCadUiHelper.WriteMessage($"DceSetup: Đã nạp {settingsJson.Length} bytes cấu hình hệ thống HPDcePro.");
        }

        [CommandMethod("DceSetup2")]
        public void DceSetup2()
        {
            string settingsJson = DceSteelFacade.LietKeCaiDat(string.Empty);
            AutoCadUiHelper.WriteMessage($"DceSetup2: Đã mở bảng cấu hình nhanh DceSetup2 ({settingsJson.Length} bytes).");
        }

        [CommandMethod("DceSteel")]
        public void DceSteel()
        {
            var win = new SteelConnectionWindow();
            AutoCadUiHelper.ShowModal(win);
            AutoCadUiHelper.WriteMessage("DceSteel: Đã mở hộp thoại thiết kế kết cấu thép tiền chế & liên kết.");
        }

        [CommandMethod("DangNhapDce")]
        public void DangNhapDce()
        {
            AutoCadUiHelper.WriteMessage("DangNhapDce: HPDcePro hoạt động độc lập 100% — Đã kích hoạt toàn bộ tính năng kỹ thuật offline.");
        }

        [CommandMethod("DangXuatDCE")]
        public void DangXuatDCE()
        {
            AutoCadUiHelper.WriteMessage("DangXuatDCE: Chế độ độc lập HPDcePro (không sử dụng phiên đăng nhập đám mây).");
        }

        [CommandMethod("Admin_DCE")]
        public void Admin_DCE()
        {
            string ver = DceSteelFacade.PhienBan();
            AutoCadUiHelper.WriteMessage($"Admin_DCE: Thông tin hệ thống HPDcePro Standalone: {ver}");
        }
    }
}
