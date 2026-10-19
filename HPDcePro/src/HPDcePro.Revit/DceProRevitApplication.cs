using System;
using Autodesk.Revit.UI;
using HPDcePro.Revit.Commands;
using HPDcePro.Revit.Common;
using HPDcePro.UI.Common;

namespace HPDcePro.Revit
{
    public sealed class DceProRevitApplication : IExternalApplication
    {
        public const string TabName = "DCE PRO";

        public Result OnStartup(UIControlledApplication application)
        {
            if (application == null) return Result.Cancelled;

            try
            {
                ThemeManager.EnsureAssembliesLoaded();
                CreateRibbon(application);
                return Result.Succeeded;
            }
            catch (Exception)
            {
                // Reason: Prevent Revit startup crash if ribbon initialization fails in headless or restricted mode
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private static void CreateRibbon(UIControlledApplication app)
        {
            try
            {
                app.CreateRibbonTab(TabName);
            }
            catch (Exception)
            {
                // Reason: Ribbon tab may already exist if previously created
            }

            RegisterModelingPanel(app);
            RegisterJoinCutPanel(app);
            RegisterReinforcementPanel(app);
            RegisterSettingsPanel(app);
        }

        private static void RegisterModelingPanel(UIControlledApplication app)
        {
            RibbonPanel panel = app.CreateRibbonPanel(TabName, "Modeling");
            panel.AddItem(RevitRibbonHelper.CreatePushButton("BtnAddGrid", "Lưới Trục", typeof(CommandAddGrid), "IconRevit/Gird.png", "Tạo hệ lưới trục kết cấu"));
            panel.AddItem(RevitRibbonHelper.CreatePushButton("BtnAddColumn", "Cột BTCT", typeof(CommandAddColumn), "IconRevit/Column.png", "Tạo cột bê tông cốt thép"));
            panel.AddItem(RevitRibbonHelper.CreatePushButton("BtnAddBeam", "Dầm BTCT", typeof(CommandAddBeam), "IconRevit/Beam.png", "Tạo dầm bê tông cốt thép"));
            panel.AddItem(RevitRibbonHelper.CreatePushButton("BtnAddSlab", "Sàn BTCT", typeof(CommandAddSlab), "IconRevit/Floor.png", "Tạo bản sàn bê tông cốt thép"));
            panel.AddItem(RevitRibbonHelper.CreatePushButton("BtnAddWall", "Vách BTCT", typeof(CommandAddWall), "IconRevit/Wall.png", "Tạo vách/tường bê tông cốt thép"));
            panel.AddItem(RevitRibbonHelper.CreatePushButton("BtnDatTenDam", "Đặt Tên Dầm", typeof(CommandDatTenDam), "IconRevit/Name.png", "Tự động đánh tên dầm theo trục"));
            panel.AddItem(RevitRibbonHelper.CreatePushButton("BtnAnalytical", "Mô Hình\nGiải Tích", typeof(CommandCreateAnalytical), "IconCad/Calculator.png", "Tạo mô hình phần tử giải tích"));
        }

        private static void RegisterJoinCutPanel(UIControlledApplication app)
        {
            RibbonPanel panel = app.CreateRibbonPanel(TabName, "Join/Cut");
            panel.AddItem(RevitRibbonHelper.CreatePushButton("BtnAutoJoin", "Auto Join", typeof(CommandAutoJoin), "IconRevit/Join.png", "Tự động Join hình học theo thứ tự ưu tiên"));
            panel.AddItem(RevitRibbonHelper.CreatePushButton("BtnAutoCut", "Auto Cut", typeof(CommandAutoCut), "IconCad/Cut.png", "Tự động cắt cấu kiện giao nhau"));
            panel.AddItem(RevitRibbonHelper.CreatePushButton("BtnBatchUnjoin", "Tách Join", typeof(CommandBatchUnjoin), "IconRevit/DeleteLink.png", "Tách Join hàng loạt cấu kiện được chọn"));
            panel.AddItem(RevitRibbonHelper.CreatePushButton("BtnJoinByOrder", "Thứ Tự Join", typeof(CommandJoinByOrder), "IconCad/Repair1.png", "Sắp xếp thứ tự Cột > Vách > Dầm > Sàn"));
            panel.AddItem(RevitRibbonHelper.CreatePushButton("BtnDisallowJoin", "Khóa Đầu Dầm", typeof(CommandBeamDisallowJoin), "IconCad/Repair2.png", "Khóa liên kết đầu dầm Disallow Join"));
            panel.AddItem(RevitRibbonHelper.CreatePushButton("BtnCreateCope", "Vát Mộng Dầm", typeof(CommandCreateCope), "IconCad/Repair3.png", "Tạo vát mộng Coping liên kết dầm-dầm"));
            panel.AddItem(RevitRibbonHelper.CreatePushButton("BtnBeamBreaks", "Tách Dầm Nhịp", typeof(DceBeamBreaks), "IconCad/Cut.png", "Tách dầm liên tục thành các phân đoạn nhịp"));
        }

        private static void RegisterReinforcementPanel(UIControlledApplication app)
        {
            RibbonPanel panel = app.CreateRibbonPanel(TabName, "Reinforcement");
            panel.AddItem(RevitRibbonHelper.CreatePushButton("BtnRebarBeam", "Thép Dầm 3D", typeof(CommandAddRebarBeam), "IconRevit/Rebar.png", "Bố trí cốt thép 3D dầm liên tục"));
            panel.AddItem(RevitRibbonHelper.CreatePushButton("BtnRebarCol", "Thép Cột 3D", typeof(CommandAddRebarColumn), "IconCad/Rebarlayout.png", "Bố trí cốt thép 3D cột kết cấu"));
            panel.AddItem(RevitRibbonHelper.CreatePushButton("BtnRebarWall", "Thép Vách 3D", typeof(CommandAddRebarWall), "IconCad/Spread.png", "Bố trí cốt thép 3D vách cứng"));
            panel.AddItem(RevitRibbonHelper.CreatePushButton("BtnRebarSlab", "Thép Sàn 3D", typeof(CommandAddRebarSlab), "IconCad/Drawing.png", "Bố trí lưới thép sàn 3D"));
            panel.AddItem(RevitRibbonHelper.CreatePushButton("BtnRebarFooting", "Thép Đài Móng", typeof(CommandAddRebarFooting), "IconCad/HouSe.png", "Bố trí cốt thép đài móng / móng cọc"));
            panel.AddItem(RevitRibbonHelper.CreatePushButton("BtnTakeoff", "Thống Kê BOQ", typeof(CommandXuatKhoiLuong), "IconRevit/ThongKe.png", "Bóc tách khối lượng bê tông, ván khuôn, cốt thép"));
        }

        private static void RegisterSettingsPanel(UIControlledApplication app)
        {
            RibbonPanel panel = app.CreateRibbonPanel(TabName, "Settings");
            panel.AddItem(RevitRibbonHelper.CreatePushButton("BtnHub", "DCE Pro Hub", typeof(DceProControlCenterCommand), "IconCad/AutoStr_32.png", "Mở Trung tâm điều hành HPDcePro"));
            panel.AddItem(RevitRibbonHelper.CreatePushButton("BtnExportExcel", "Xuất Excel", typeof(CommandExportBeamToExcel), "IconRevit/ExportToExcel.png", "Xuất dữ liệu tính toán dầm sang Excel"));
            panel.AddItem(RevitRibbonHelper.CreatePushButton("BtnExportCad", "Xuất CAD", typeof(CommandExportToAutocad), "IconRevit/Autocad.png", "Xuất bản vẽ mặt bằng sang AutoCAD"));
            panel.AddItem(RevitRibbonHelper.CreatePushButton("BtnEtabsToRvt", "ETABS → RVT", typeof(CommandEtabsToRevit), "IconRevit/Etabs.png", "Nhập mô hình từ ETABS sang Revit"));
            panel.AddItem(RevitRibbonHelper.CreatePushButton("BtnRvtToEtabs", "RVT → ETABS", typeof(CommandExportToEtabs), "IconCad/ETABS.png", "Xuất mô hình từ Revit sang ETABS"));
            panel.AddItem(RevitRibbonHelper.CreatePushButton("BtnSetup", "Cài Đặt", typeof(CommandDceSetup), "IconCad/Setup.png", "Cài đặt tham số và môi trường DCE PRO"));
            panel.AddItem(RevitRibbonHelper.CreatePushButton("BtnLogin", "Đăng Nhập", typeof(CommandLogin), "IconCad/Login.png", "Đăng nhập tài khoản DCE PRO"));
            panel.AddItem(RevitRibbonHelper.CreatePushButton("BtnLogout", "Đăng Xuất", typeof(CommandLogout), "IconCad/Logout.png", "Đăng xuất tài khoản"));
        }
    }
}
