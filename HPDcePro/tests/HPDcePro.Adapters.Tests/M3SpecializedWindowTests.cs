using System;
using System.Threading;
using HPDcePro.AutoCad.Ribbon;
using HPDcePro.UI.Common;
using HPDcePro.UI.Controls;
using HPDcePro.UI.Views;
using Xunit;

namespace HPDcePro.Adapters.Tests
{
    [Collection("WpfTests")]
    public sealed class M3SpecializedWindowTests
    {
        private static void RunOnStaThread(Action action)
        {
            Exception? caught = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    // Reason: Capture STA thread exception to propagate back to test runner
                    caught = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (caught != null)
            {
                throw new InvalidOperationException($"STA thread failed: {caught.Message}", caught);
            }
        }

        [Fact]
        public void DamCongWindow_InstantiatesOnStaThread()
        {
            RunOnStaThread(() =>
            {
                ThemeManager.EnsureAssembliesLoaded();
                var window = new DamCongWindow();
                Assert.NotNull(window);
                Assert.NotNull(window.ViewModel);

                ThemeManager.Apply(window, dark: true);
                ThemeManager.Apply(window, dark: false);
            });
        }

        [Fact]
        public void MongCocWindows_InstantiateOnStaThread()
        {
            RunOnStaThread(() =>
            {
                ThemeManager.EnsureAssembliesLoaded();

                var winMain = new MongCocWindow();
                Assert.NotNull(winMain);
                Assert.NotNull(winMain.ViewModel);

                var winLopThem = new MongCocLopThepThemWindow();
                Assert.NotNull(winLopThem);
                Assert.NotNull(winLopThem.ViewModel);

                var winChonThep = new MongCocChonThepTuDongWindow();
                Assert.NotNull(winChonThep);
                Assert.NotNull(winChonThep.ViewModel);

                ThemeManager.Apply(winMain, dark: true);
            });
        }

        [Fact]
        public void ThangMoiAndCocBtWindows_InstantiateOnStaThread()
        {
            RunOnStaThread(() =>
            {
                ThemeManager.EnsureAssembliesLoaded();

                var winThang = new ThangMoiWindow();
                Assert.NotNull(winThang);
                Assert.NotNull(winThang.ViewModel);

                var winCoc = new CocBtWindow();
                Assert.NotNull(winCoc);
                Assert.NotNull(winCoc.ViewModel);

                ThemeManager.Apply(winThang, dark: true);
                ThemeManager.Apply(winCoc, dark: true);
            });
        }

        [Fact]
        public void DashboardAndExecutionWindows_InstantiateOnStaThread()
        {
            RunOnStaThread(() =>
            {
                ThemeManager.EnsureAssembliesLoaded();

                var winDash = new MasterRebarScheduleDashboardWindow();
                Assert.NotNull(winDash);
                Assert.NotNull(winDash.ViewModel);

                var winExec = new AutoStrExecutionWindow();
                Assert.NotNull(winExec);
                Assert.NotNull(winExec.ViewModel);

                var winCutAdv = new CuttingOptimizationAdvancedWindow();
                Assert.NotNull(winCutAdv);
                Assert.NotNull(winCutAdv.ViewModel);

                ThemeManager.Apply(winDash, dark: true);
                ThemeManager.Apply(winExec, dark: true);
                ThemeManager.Apply(winCutAdv, dark: true);
            });
        }

        [Fact]
        public void PaletteControls_InstantiateOnStaThread()
        {
            RunOnStaThread(() =>
            {
                ThemeManager.EnsureAssembliesLoaded();

                var dcePalette = new DcePaletteControl();
                Assert.NotNull(dcePalette);

                var wallPalette = new WallThicknessPaletteControl();
                Assert.NotNull(wallPalette);
            });
        }

        [Fact]
        public void DceRibbonBuilder_BuildsTabsAndPanels()
        {
            var tab = DceRibbonBuilder.BuildRibbonTab();
            Assert.NotNull(tab);
            Assert.Equal("HPDCE_TAB", tab.Id);
            Assert.Equal("DBIM Apps", tab.Title);
            Assert.Equal(6, tab.Panels.Count);

            // Safe call in headless environment
            bool attached = DceRibbonBuilder.AttachToRibbon();
            // Should not throw, returns false in headless without AutoCAD ribbon
            Assert.False(attached);
        }
    }
}
