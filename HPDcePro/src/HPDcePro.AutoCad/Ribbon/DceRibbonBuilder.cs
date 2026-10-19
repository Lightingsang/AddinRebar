using System;
using Autodesk.Windows;

namespace HPDcePro.AutoCad.Ribbon
{
    /// <summary>
    /// Constructs the 6-panel "DBIM Apps" Ribbon tab matching legacy DCE Pro AutoCAD menu specification.
    /// </summary>
    public static class DceRibbonBuilder
    {
        public const string TabId = "HPDCE_TAB";
        public const string TabTitle = "DBIM Apps";

        public static RibbonTab? BuildRibbonTab()
        {
            var tab = new RibbonTab
            {
                Title = TabTitle,
                Id = TabId
            };

            tab.Panels.Add(CreateAccountPanel());
            tab.Panels.Add(CreateCadDesignPanel());
            tab.Panels.Add(CreateRebarDataPanel());
            tab.Panels.Add(CreateSchedulePanel());
            tab.Panels.Add(CreateShopDrawingPanel());
            tab.Panels.Add(CreateAutoStrPanel());

            return tab;
        }

        public static bool AttachToRibbon()
        {
            try
            {
                var ribbon = ComponentManager.Ribbon;
                if (ribbon == null) return false;

                foreach (var existing in ribbon.Tabs)
                {
                    if (existing.Id == TabId) return true;
                }

                var tab = BuildRibbonTab();
                if (tab != null)
                {
                    ribbon.Tabs.Add(tab);
                    return true;
                }
            }
            catch (Exception)
            {
                // Reason: Headless test runner or AutoCAD session without active Ribbon control.
                return false;
            }

            return false;
        }

        private static RibbonPanel CreateAccountPanel()
        {
            var panel = new RibbonPanel { Source = new RibbonPanelSource { Title = "DCE ACCOUNT" } };
            AddButton(panel.Source, "Đăng Nhập", "DangNhapDce");
            AddButton(panel.Source, "Đăng Xuất", "DangXuatDCE");
            AddButton(panel.Source, "Cài Đặt", "DceSetup");
            return panel;
        }

        private static RibbonPanel CreateCadDesignPanel()
        {
            var panel = new RibbonPanel { Source = new RibbonPanelSource { Title = "EXCEL - AUTOCAD" } };
            AddButton(panel.Source, "Vẽ Dầm", "VeDam_DCE");
            AddButton(panel.Source, "Vẽ Cột", "VeCotDcePro");
            AddButton(panel.Source, "Vẽ Vách", "VeVachDcePro");
            AddButton(panel.Source, "Móng Cọc", "VeMongCoc_DCE");
            AddButton(panel.Source, "Móng Đơn", "VeMongDon_DCE");
            AddButton(panel.Source, "Thép Sàn", "VeThepSan_DCE");
            return panel;
        }

        private static RibbonPanel CreateRebarDataPanel()
        {
            var panel = new RibbonPanel { Source = new RibbonPanelSource { Title = "REBAR DATA" } };
            AddButton(panel.Source, "Ẩn Số Hiệu", "AnSoHieuThep_dce");
            AddButton(panel.Source, "Dim Nhanh", "DimNhanh_dce");
            AddButton(panel.Source, "Dim Biến Thiên", "DimBienThien_Dce");
            AddButton(panel.Source, "Copy Số Hiệu", "CopySoHieuThep");
            return panel;
        }

        private static RibbonPanel CreateSchedulePanel()
        {
            var panel = new RibbonPanel { Source = new RibbonPanelSource { Title = "THỐNG KÊ THÉP" } };
            AddButton(panel.Source, "Tạo TKT", "TKT_DCE");
            AddButton(panel.Source, "Sửa SLCK", "TKTSSOLUONGCAUKIEN");
            AddButton(panel.Source, "Sửa ĐKT", "TKTSDUONGKINHTHEP");
            AddButton(panel.Source, "Quét TKT", "QuetThongKe_DCE");
            return panel;
        }

        private static RibbonPanel CreateShopDrawingPanel()
        {
            var panel = new RibbonPanel { Source = new RibbonPanelSource { Title = "SHOP DRAWING" } };
            AddButton(panel.Source, "Cắt Thép 1D", "ToiUuCatThep");
            AddButton(panel.Source, "Chia Thép", "ChiaThep_DCE");
            AddButton(panel.Source, "Cắt Thép", "CutThep_DCE");
            return panel;
        }

        private static RibbonPanel CreateAutoStrPanel()
        {
            var panel = new RibbonPanel { Source = new RibbonPanelSource { Title = "AUTOSTR & AI" } };
            AddButton(panel.Source, "AutoStr 24", "AutoStr");
            AddButton(panel.Source, "Trung Tâm", "GETFORM");
            AddButton(panel.Source, "Máy Học AI", "DceMayHoc");
            return panel;
        }

        private static void AddButton(RibbonPanelSource panelSource, string text, string command)
        {
            var btn = new RibbonButton
            {
                Text = text,
                CommandParameter = command + " ",
                ShowText = true,
                Size = RibbonItemSize.Standard
            };
            panelSource.Items.Add(btn);
        }
    }
}
