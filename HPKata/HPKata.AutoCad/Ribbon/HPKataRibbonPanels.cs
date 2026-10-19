using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using Autodesk.Windows;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Ribbon
{
    /// <summary>
    /// Builds the 4 synchronized panels on the HPKata AutoCAD 2026 Ribbon tab:
    /// 1. Cốt Thép (Rebar)
    /// 2. Dầm, Cột &amp; Vách
    /// 3. Móng, Sàn &amp; Thang
    /// 4. QS &amp; Bản Vẽ
    /// </summary>
    public static class HPKataRibbonPanels
    {
        public const string CorePanelId = "HPKATA_PANEL_CORE";
        public const string CorePanelTitle = "Cốt Thép (Rebar)";

        public const string FramingColPanelId = "HPKATA_PANEL_FRAMING_COL";
        public const string FramingColPanelTitle = "Dầm, Cột & Vách";

        public const string FoundSlabPanelId = "HPKATA_PANEL_FOUND_SLAB";
        public const string FoundSlabPanelTitle = "Móng, Sàn & Thang";

        public const string QsLayoutPanelId = "HPKATA_PANEL_QS_LAYOUT";
        public const string QsLayoutPanelTitle = "QS & Bản Vẽ";

        public static IReadOnlyList<RibbonPanel> BuildAllPanels(bool isDark)
        {
            return new[]
            {
                BuildCorePanel(isDark),
                BuildFramingColumnPanel(isDark),
                BuildFoundationSlabPanel(isDark),
                BuildQsLayoutPanel(isDark)
            };
        }

        private static RibbonPanel BuildCorePanel(bool isDark)
        {
            var src = new RibbonPanelSource { Id = CorePanelId, Title = CorePanelTitle };
            src.Items.Add(CreateLargeButton("HPKATA_SETUP", "Thiết Lập\nKata", "setupkata1",
                "Cấu hình tham số và tiêu chuẩn uốn móc cốt thép KataPro (setupkata)",
                AutocadRibbonIconFactory.CreateSetupIcon(isDark)));
            src.Items.Add(CreateLargeButton("HPKATA_VT", "Vẽ & Sửa\nThép", "ve_thep",
                "Vẽ, điều chỉnh và liên kết thanh cốt thép thông minh (vt / sua)",
                AutocadRibbonIconFactory.CreateDetailingIcon(isDark)));
            src.Items.Add(CreateLargeButton("HPKATA_TKT", "Thống Kê\nCốt Thép", "thong_ke_thep",
                "Lập bảng thống kê thép và tổng hợp theo đường kính (tkt / ththep)",
                AutocadRibbonIconFactory.CreateScheduleIcon(isDark)));
            src.Items.Add(CreateLargeButton("HPKATA_TOHOP", "Tổ Hợp\nCắt Thép", "to_hop_thep",
                "Tối ưu hóa tổ hợp cắt thanh thương mại 11.7m (tohopthep)",
                AutocadRibbonIconFactory.CreateCuttingStockIcon(isDark)));
            src.Items.Add(CreateLargeButton("HPKATA_EXCEL", "Xuất\nExcel", "toexcel",
                "Xuất bảng thống kê cốt thép và coupler ra Excel (toexcel)",
                AutocadRibbonIconFactory.CreateExportExcelIcon(isDark)));
            return new RibbonPanel { Source = src };
        }

        private static RibbonPanel BuildFramingColumnPanel(bool isDark)
        {
            var src = new RibbonPanelSource { Id = FramingColPanelId, Title = FramingColPanelTitle };
            src.Items.Add(CreateLargeButton("HPKATA_VD", "Thép\nDầm", "ve_dam",
                "Triển khai cốt thép dầm liên tục, mặt cắt và dầm giao (vd / vds / vdmc)",
                AutocadRibbonIconFactory.CreateBeamIcon(isDark)));
            src.Items.Add(CreateLargeButton("HPKATA_ETABS_BEAM", "ETABS\nDầm", "Etabs",
                "Nhập diện tích cốt thép từ ETABS và bố trí tự động (Etabs / loadbeam)",
                AutocadRibbonIconFactory.CreateEtabsIcon(isDark)));
            src.Items.Add(CreateLargeButton("HPKATA_VC", "Thép\nCột", "ve_cot",
                "Triển khai cốt thép cột đa tầng, nhấn cổ chai 1:6 (vc / vcs)",
                AutocadRibbonIconFactory.CreateColumnWallIcon(isDark)));
            src.Items.Add(CreateLargeButton("HPKATA_VV", "Thép\nVách", "ve_vach",
                "Triển khai cốt thép vách cứng và lõi thang máy (vv / vvs / vlv)",
                AutocadRibbonIconFactory.CreateColumnWallIcon(isDark)));
            src.Items.Add(CreateLargeButton("HPKATA_LOADCOL", "ETABS\nCột/Vách", "loadcolumn",
                "Nhập kết quả thiết kế cột và vách từ ETABS (loadcolumn / loadwall)",
                AutocadRibbonIconFactory.CreateEtabsIcon(isDark)));
            return new RibbonPanel { Source = src };
        }

        private static RibbonPanel BuildFoundationSlabPanel(bool isDark)
        {
            var src = new RibbonPanelSource { Id = FoundSlabPanelId, Title = FoundSlabPanelTitle };
            src.Items.Add(CreateLargeButton("HPKATA_VMD", "Móng\nĐơn", "ve_mong_don",
                "Triển khai cốt thép móng đơn và mặt bằng móng (vmd / vmbm)",
                AutocadRibbonIconFactory.CreateFoundationIcon(isDark)));
            src.Items.Add(CreateLargeButton("HPKATA_VMC", "Đài\nCọc", "ve_mong_coc",
                "Triển khai cốt thép đài cọc và đài cọc giật cấp (vmc / vmcg)",
                AutocadRibbonIconFactory.CreateFoundationIcon(isDark)));
            src.Items.Add(CreateLargeButton("HPKATA_VESAN", "Thép\nSàn", "ve_san",
                "Triển khai cốt thép sàn 2 phương, sàn polyline và chân chó (vesan / vesanpl / vesan4d)",
                AutocadRibbonIconFactory.CreateSlabIcon(isDark)));
            src.Items.Add(CreateLargeButton("HPKATA_VETHANG", "Thép\nThang", "ve_thang",
                "Triển khai cốt thép bản thang, chiếu nghỉ và lanh tô (vethang / velanhto)",
                AutocadRibbonIconFactory.CreateStairIcon(isDark)));
            return new RibbonPanel { Source = src };
        }

        private static RibbonPanel BuildQsLayoutPanel(bool isDark)
        {
            var src = new RibbonPanelSource { Id = QsLayoutPanelId, Title = QsLayoutPanelTitle };
            src.Items.Add(CreateLargeButton("HPKATA_QS", "Bóc Tách\nQS", "QS",
                "Bóc tách khối lượng bê tông và cốp pha có khấu trừ giao cắt (QS / tkqs / xuatQS)",
                AutocadRibbonIconFactory.CreateQsIcon(isDark)));
            src.Items.Add(CreateLargeButton("HPKATA_VG", "Lưới\nTrục", "ve_grid",
                "Vẽ hệ lưới trục kết cấu tự động kèm kích thước (vg)",
                AutocadRibbonIconFactory.CreateLayoutIcon(isDark)));
            src.Items.Add(CreateLargeButton("HPKATA_SBV", "Dàn Trang\nBản Vẽ", "sap_ban_ve",
                "Tự động xếp bản vẽ vào khung tên A1/A3 theo Shelf-Packing 2D (sbv / ibv)",
                AutocadRibbonIconFactory.CreateLayoutIcon(isDark)));
            src.Items.Add(CreateLargeButton("HPKATA_CHIATAM", "Chia Tấm\n& Ốp Gạch", "chia_tam",
                "Tối ưu cắt tấm cốp pha và bố trí gạch lát nền 2D (chiatam / opgach)",
                AutocadRibbonIconFactory.CreateSlabIcon(isDark)));
            return new RibbonPanel { Source = src };
        }

        private static RibbonButton CreateLargeButton(
            string id,
            string text,
            string commandName,
            string tooltipContent,
            ImageSource icon)
        {
            return new RibbonButton
            {
                Id = id,
                Text = text,
                ShowText = true,
                ShowImage = true,
                Size = RibbonItemSize.Large,
                Orientation = Orientation.Vertical,
                Image = icon,
                LargeImage = icon,
                CommandHandler = new RibbonCommandHandler(id, () => RunAutoCadCommand(commandName)),
                ToolTip = new RibbonToolTip
                {
                    Title = text.Replace("\n", " "),
                    Content = tooltipContent,
                    Command = commandName,
                    IsHelpEnabled = false
                }
            };
        }

        internal static void RunAutoCadCommand(string commandName)
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            doc.SendStringToExecute("_." + commandName + " ", true, false, false);
        }
    }
}
