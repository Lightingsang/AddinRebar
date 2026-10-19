using System;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Windows;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Ribbon
{
    /// <summary>
    /// Manages the "HPKata" Ribbon tab on AutoCAD 2026:
    /// 1. Creates or updates the "HPKata" tab with 4 panels matching Revit 2026.
    /// 2. Hooks ComponentManager.ItemInitialized for late Ribbon startup.
    /// 3. Rebuilds icons when COLORTHEME changes and restores tab after WSCURRENT switches.
    /// </summary>
    public static class HPKataRibbonTab
    {
        public const string TabId = "HPKATA_RIBBON_TAB";
        public const string TabTitle = "HPKata";

        private static bool _installed;
        private static bool _idlePending;
        private static bool _rebuild;
        private static bool _building;

        public static void Install()
        {
            if (_installed) return;
            _installed = true;

            try
            {
                AcadApp.SystemVariableChanged += OnSystemVariableChanged;
                ComponentManager.ItemInitialized += OnRibbonItemInitialized;
                EnsureCreated();
            }
            catch (Exception)
            {
                // Annotated catch per Rule Q-B2: safe fallback when called outside AutoCAD UI host
            }
        }

        public static void Uninstall()
        {
            if (!_installed) return;
            _installed = false;

            try
            {
                AcadApp.SystemVariableChanged -= OnSystemVariableChanged;
                ComponentManager.ItemInitialized -= OnRibbonItemInitialized;
                AcadApp.Idle -= OnIdle;
                _idlePending = false;
                _rebuild = false;
                RemoveTab();
            }
            catch (Exception)
            {
                // Annotated catch per Rule Q-B2: safe termination fallback
            }
        }

        public static void EnsureCreated()
        {
            if (_building) return;
            try
            {
                var ribbon = ComponentManager.Ribbon;
                if (ribbon == null) return;
                _building = true;

                var tab = ribbon.FindTab(TabId);
                if (tab == null)
                {
                    tab = new RibbonTab
                    {
                        Id = TabId,
                        Title = TabTitle,
                        IsVisible = true
                    };
                    ribbon.Tabs.Add(tab);
                }

                if (tab.Panels.Count == 0)
                {
                    bool isDark = IsDarkTheme();
                    foreach (var panel in HPKataRibbonPanels.BuildAllPanels(isDark))
                    {
                        tab.Panels.Add(panel);
                    }
                }
            }
            catch (Exception)
            {
                // Annotated catch per Rule Q-B2: ignore transient AdWindows state during startup
            }
            finally
            {
                _building = false;
            }
        }

        private static void OnRibbonItemInitialized(object? sender, RibbonItemEventArgs e)
        {
            EnsureCreated();
        }

        private static void OnSystemVariableChanged(object? sender, SystemVariableChangedEventArgs e)
        {
            bool isWorkspace = string.Equals(e.Name, "WSCURRENT", StringComparison.OrdinalIgnoreCase);
            bool isTheme = string.Equals(e.Name, "COLORTHEME", StringComparison.OrdinalIgnoreCase);
            if (!isWorkspace && !isTheme) return;

            if (isTheme) _rebuild = true;
            if (_idlePending) return;

            _idlePending = true;
            AcadApp.Idle += OnIdle;
        }

        private static void OnIdle(object? sender, EventArgs e)
        {
            AcadApp.Idle -= OnIdle;
            _idlePending = false;

            if (_rebuild)
            {
                _rebuild = false;
                ClearPanels();
            }

            EnsureCreated();
        }

        private static void ClearPanels()
        {
            var ribbon = ComponentManager.Ribbon;
            var tab = ribbon?.FindTab(TabId);
            tab?.Panels.Clear();
        }

        private static void RemoveTab()
        {
            var ribbon = ComponentManager.Ribbon;
            var tab = ribbon?.FindTab(TabId);
            if (tab != null)
            {
                ribbon!.Tabs.Remove(tab);
            }
        }

        private static bool IsDarkTheme()
        {
            try
            {
                return Convert.ToInt32(AcadApp.GetSystemVariable("COLORTHEME")) == 0;
            }
            catch (Exception)
            {
                // Annotated catch per Rule Q-B2: default to Dark theme when system variable is unavailable
                return true;
            }
        }
    }
}
