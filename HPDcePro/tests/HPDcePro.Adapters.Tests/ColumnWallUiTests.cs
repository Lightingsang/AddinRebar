using System;
using System.Threading;
using HPDcePro.AutoCad.Commands;
using HPDcePro.UI.Common;
using HPDcePro.UI.ViewModels;
using HPDcePro.UI.Views;
using Xunit;

namespace HPDcePro.Adapters.Tests
{
    [Collection("WpfTests")]
    public sealed class ColumnWallUiTests
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
                    // Reason: Capture thread exception to rethrow on main test thread for STA assertion.
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
        public void ColumnWallDetailingWindow_InstantiatesOnStaThread()
        {
            RunOnStaThread(() =>
            {
                ThemeManager.EnsureAssembliesLoaded();
                var window = new ColumnWallDetailingWindow();
                Assert.NotNull(window);
                Assert.NotNull(window.ViewModel);

                ThemeManager.Apply(window, dark: true);
                ThemeManager.Apply(window, dark: false);
            });
        }

        [Fact]
        public void ColumnWallDetailingViewModel_CalculatesPmmCorrectly()
        {
            var vm = new ColumnWallDetailingViewModel
            {
                WidthBMm = 400.0,
                HeightHMm = 400.0,
                LengthL0Mm = 3600.0,
                DesignAxialNKn = 1000.0,
                DesignMomentMxKnm = 60.0,
                DesignMomentMyKnm = 40.0
            };

            vm.CalculatePmm();

            Assert.True(vm.AxialCapacityN0 > 2500.0);
            Assert.True(vm.MomentCapacityMx0 > 60.0);
            Assert.True(vm.UtilizationRatio < 1.0);
            Assert.True(vm.IsSafe);
            Assert.Equal("PASS", vm.StatusText);
        }

        [Fact]
        public void ColumnWallDetailingViewModel_CalculatesShearWallCorrectly()
        {
            var vm = new ColumnWallDetailingViewModel
            {
                WallLengthLwMm = 3000.0,
                WallThicknessTwMm = 250.0,
                WallHeightHwMm = 3600.0,
                WallDesignAxialNKn = 2000.0,
                WallDesignShearVuKn = 350.0,
                WallDesignMomentMuKnm = 600.0
            };

            vm.CalculateWall();

            Assert.True(vm.BoundaryLengthLc >= 450.0);
            Assert.True(vm.BoundaryRebarRatio >= 0.50);
            Assert.True(vm.WebShearCapacity > 350.0);
            Assert.True(vm.ShearUtilizationRatio < 1.0);
            Assert.True(vm.IsWallSafe);
            Assert.Equal("PASS", vm.WallStatusText);
        }

        [Fact]
        public void ColumnWallDetailingViewModel_HandlesOverloadState()
        {
            var vm = new ColumnWallDetailingViewModel
            {
                // Extreme column overload
                DesignAxialNKn = 6000.0,
                DesignMomentMxKnm = 400.0,
                DesignMomentMyKnm = 300.0,
                // Extreme wall overload
                WallDesignShearVuKn = 5000.0
            };

            vm.CalculatePmm();
            vm.CalculateWall();

            Assert.False(vm.IsSafe);
            Assert.Equal("FAIL", vm.StatusText);
            Assert.False(vm.IsWallSafe);
            Assert.Equal("FAIL", vm.WallStatusText);
        }

        [Fact]
        public void ColumnCommands_ExecutesDomainWorkflowsWithoutException()
        {
            var commands = new ColumnCommands();

            // All commands should execute cleanly with internal fallbacks when running headlessly
            var ex = Record.Exception(() =>
            {
                commands.ColumnREC1();
                commands.ColumnREC2();
                commands.ColumnCi();
                commands.VeVachDcePro();
                commands.ImportKichThuocCot();
                commands.ImportHinhDangCot();
                commands.UpdateThepTinhToan();
            });

            Assert.Null(ex);
        }
    }
}
