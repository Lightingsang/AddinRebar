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
    public sealed class FoundationSlabUiTests
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
        public void FoundationSlabWindow_InstantiatesOnStaThread()
        {
            RunOnStaThread(() =>
            {
                ThemeManager.EnsureAssembliesLoaded();
                var window = new FoundationSlabWindow();
                Assert.NotNull(window);
                Assert.NotNull(window.ViewModel);

                ThemeManager.Apply(window, dark: true);
                ThemeManager.Apply(window, dark: false);
            });
        }

        [Fact]
        public void FoundationSlabViewModel_CalculatesPileCap_Correctly()
        {
            var vm = new FoundationSlabViewModel
            {
                CapWidthBMm = 1800.0,
                CapLengthLMm = 1800.0,
                CapThicknessHMm = 900.0,
                CapAxialNKn = 1400.0,
                CapMomentMxKnm = 40.0,
                CapMomentMyKnm = 30.0,
                PileCapacityRcKn = 600.0
            };

            vm.CalculatePileCap();

            Assert.True(vm.MaxPileReactionKn > 300.0);
            Assert.True(vm.PileCapacityRatio <= 1.0);
            Assert.True(vm.CapPunchingRatio <= 1.0);
            Assert.True(vm.CapRequiredAsXMm2 > 500.0);
            Assert.Equal("PASS", vm.CapStatusText);
        }

        [Fact]
        public void FoundationSlabViewModel_CalculatesPadFooting_Correctly()
        {
            var vm = new FoundationSlabViewModel
            {
                PadWidthBMm = 2000.0,
                PadLengthLMm = 2400.0,
                PadThicknessHMm = 600.0,
                PadAxialNKn = 800.0,
                PadMomentMxKnm = 40.0,
                PadMomentMyKnm = 30.0,
                AllowableSoilRtcKpa = 250.0
            };

            vm.CalculatePadFooting();

            Assert.True(vm.PadBasePressureMaxKpa > 0.0);
            Assert.True(vm.PadPunchingRatio <= 1.0);
            Assert.False(vm.PadHasUplift);
            Assert.Equal("PASS", vm.PadStatusText);
        }

        [Fact]
        public void FoundationSlabViewModel_CalculatesSlab_Correctly()
        {
            var vm = new FoundationSlabViewModel
            {
                SlabSpanLxMm = 4000.0,
                SlabSpanLyMm = 5000.0,
                SlabThicknessHMm = 120.0,
                SlabDeadGKnM2 = 3.5,
                SlabLivePKnM2 = 2.4
            };

            vm.CalculateSlab();

            Assert.True(vm.SlabAspectRatio <= 2.0);
            Assert.True(vm.SlabSpanMomentMxKnm > 0.0);
            Assert.True(vm.SlabRequiredAsBotX > 100.0);
            Assert.Equal("PASS", vm.SlabStatusText);
        }

        [Fact]
        public void FoundationSlabViewModel_CalculatesStairAndLintel_Correctly()
        {
            var vm = new FoundationSlabViewModel
            {
                StairSpanLMm = 3000.0,
                StairRiseHMm = 1760.0,
                StairStepsCount = 11,
                LintelSpanL0Mm = 1200.0,
                LintelBearingMm = 250.0
            };

            vm.CalculateStaircase();
            vm.CalculateLintel();

            Assert.Equal(620.0, vm.StairBlondelValue, 1);
            Assert.Equal("PASS", vm.StairStatusText);
            Assert.True(vm.LintelMomentKnm > 0.0);
            Assert.Equal("PASS", vm.LintelStatusText);
        }

        [Fact]
        public void FootingAndSlabCommands_ExecuteWithoutException()
        {
            var footingCmds = new FootingCommands();
            var slabCmds = new SlabCommands();

            var ex = Record.Exception(() =>
            {
                footingCmds.VeBlockCoc();
                footingCmds.DanhTenCoc();
                footingCmds.XuatToaDoCoc();
                footingCmds.StripMong();
                footingCmds.TextDaiMong();
                footingCmds.VeMongCoc_DCE();
                footingCmds.VeMongDon_DCE();
                footingCmds.TMCoc();
                footingCmds.RaiDaiCoc();
                footingCmds.VeBlockLanhTo();
                footingCmds.VeMatCatLanhTo();
                footingCmds.VeThang_DCE();

                slabCmds.VeThepSanTopX();
                slabCmds.VeThepSanTopY();
                slabCmds.SoHieuThepSan();
                slabCmds.VeThepSanBotX();
                slabCmds.VeThepSanBotY();
                slabCmds.ThongKeThepSan();
                slabCmds.ChiaKhoangCach();
            });

            Assert.Null(ex);
        }
    }
}
