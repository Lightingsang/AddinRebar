using System;
using System.Threading;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Steel;
using HPDcePro.UI.Common;
using HPDcePro.UI.ViewModels;
using HPDcePro.UI.Views;
using Xunit;

namespace HPDcePro.Adapters.Tests
{
    [Collection("WpfTests")]
    public sealed class SteelConnectionUiTests
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
        public void SteelConnectionViewModel_InitializesWithDefaultsAndCalculatesSuccessfully()
        {
            var vm = new SteelConnectionViewModel();

            Assert.Equal("PASS", vm.MomentConnectionStatus);
            Assert.True(vm.RequiredPlateThicknessMm > 0);
            Assert.True(vm.BoltInteractionRatio > 0);
            Assert.True(vm.WeldStressRatio > 0);

            Assert.Equal("PASS", vm.BasePlateStatus);
            Assert.True(vm.BearingStressMpa > 0);
            Assert.True(vm.RequiredBasePlateThicknessMm > 0);

            Assert.Equal("PASS", vm.GableFrameStatus);
            Assert.True(vm.ApexHeightMm > vm.FrameEaveHeightMm);
            Assert.True(vm.ActualPurlinSpacingMm > 0);
        }

        [Fact]
        public void SteelConnectionViewModel_MomentConnectionRecalculates_WhenParametersChange()
        {
            var vm = new SteelConnectionViewModel
            {
                MomentKnm = 260.0,
                ShearKn = 150.0,
                EndPlateThicknessMm = 30.0,
                BoltDiameterMm = 27.0,
                SelectedBoltGrade = BoltGrade.Grade10_9
            };

            vm.CalculateMomentConnection();

            Assert.True(vm.RequiredPlateThicknessMm > 15.0);
            Assert.True(vm.BoltInteractionRatio <= 1.0);
            Assert.Contains("tp,req=", vm.MomentConnectionSummary);
        }

        [Fact]
        public void SteelConnectionViewModel_BasePlateRecalculates_WhenParametersChange()
        {
            var vm = new SteelConnectionViewModel
            {
                PlateLengthMm = 700.0,
                PlateWidthMm = 500.0,
                PlateThicknessMm = 50.0,
                BaseAxialKn = 800.0,
                BaseMomentKnm = 120.0,
                SelectedPedestalGrade = ConcreteGrade.B30,
                AnchorBoltDiameterMm = 30.0
            };

            vm.CalculateBasePlate();

            Assert.True(vm.BearingStressMpa > 0);
            Assert.True(vm.AllowableBearingMpa > 0);
            Assert.True(vm.RequiredBasePlateThicknessMm > 10.0);
            Assert.True(vm.EmbedmentLengthMm >= 25.0 * 30.0);
            Assert.Equal("PASS", vm.BasePlateStatus);
        }

        [Fact]
        public void SteelConnectionViewModel_GableFrameRecalculates_WhenParametersChange()
        {
            var vm = new SteelConnectionViewModel
            {
                FrameSpanMm = 30000.0,
                FrameEaveHeightMm = 8000.0,
                FrameSlopePercent = 15.0,
                TargetPurlinSpacingMm = 1400.0
            };

            vm.CalculateGableFrame();

            // Rise = 15000 * 0.15 = 2250 -> Apex = 8000 + 2250 = 10250 mm
            Assert.Equal(10250.0, vm.ApexHeightMm, precision: 1);
            Assert.True(vm.ActualPurlinSpacingMm >= 1200.0 && vm.ActualPurlinSpacingMm <= 1500.0);
            Assert.True(vm.PurlinsPerRafter > 10);
            Assert.True(vm.KneeMomentKnm > 0);
        }

        [Fact]
        public void SteelConnectionViewModel_ExportCadDrafting_ProducesValidCenterlineBuffer()
        {
            var vm = new SteelConnectionViewModel();
            vm.ExportCadDrafting();

            Assert.NotNull(vm.LastGeneratedBuffer);
            Assert.True(vm.LastGeneratedBuffer.Count >= 5);
        }

        [Fact]
        public void SteelConnectionWindow_InstantiatesOnStaThread_AndBindsViewModel()
        {
            RunOnStaThread(() =>
            {
                ThemeManager.EnsureAssembliesLoaded();
                var window = new SteelConnectionWindow();
                Assert.NotNull(window);
                Assert.NotNull(window.ViewModel);
                Assert.Contains("Liên Kết Thép", window.Title);

                ThemeManager.Apply(window, dark: true);
                ThemeManager.Apply(window, dark: false);
            });
        }
    }
}
