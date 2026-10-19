using HPKata.UI.ViewModels;
using Xunit;

namespace HPKata.UI.Tests.ViewModels
{
    public class StairLintelDetailingViewModelTests
    {
        [Fact]
        public void Default_IsStair_ChecksErgonomicsAndCalculatesWeight()
        {
            var vm = new StairLintelDetailingViewModel();

            Assert.True(vm.IsStair);
            Assert.Equal("ST1", vm.StairMark);
            Assert.True(vm.IsErgonomicOk);
            Assert.Equal(600.0, vm.BlondelValue);
            Assert.Equal(500.0, vm.ScissorExtensionMm);
            Assert.True(vm.EstimatedVolumeM3 > 0);
            Assert.True(vm.EstimatedWeightKg > 0);
        }

        [Fact]
        public void SwitchToLintel_RecalculatesAccurately()
        {
            var vm = new StairLintelDetailingViewModel
            {
                IsStair = false,
                LintelMark = "LT1",
                ClearSpanMm = 1400.0,
                BearingLengthMm = 250.0,
                LintelWidthMm = 220.0,
                LintelHeightMm = 200.0
            };

            vm.Recalculate();

            // Total length = 1400 + 500 = 1900 mm. Volume = 1.9 * 0.22 * 0.20 = 0.084 m3
            Assert.Equal(0.084, vm.EstimatedVolumeM3);
            Assert.True(vm.EstimatedWeightKg > 0);
        }

        [Fact]
        public void ApplyCommand_Stair_SetsResultFlightAndCloses()
        {
            var vm = new StairLintelDetailingViewModel
            {
                IsStair = true,
                StairMark = "ST-TEST"
            };

            bool closed = false;
            vm.RequestClose += () => closed = true;

            vm.ApplyCommand.Execute(null);

            Assert.True(closed);
            Assert.NotNull(vm.ResultFlight);
            Assert.Null(vm.ResultLintel);
            Assert.Equal("ST-TEST", vm.ResultFlight!.FlightMark);
        }

        [Fact]
        public void ApplyCommand_Lintel_SetsResultLintelAndCloses()
        {
            var vm = new StairLintelDetailingViewModel
            {
                IsStair = false,
                LintelMark = "LT-TEST"
            };

            bool closed = false;
            vm.RequestClose += () => closed = true;

            vm.ApplyCommand.Execute(null);

            Assert.True(closed);
            Assert.NotNull(vm.ResultLintel);
            Assert.Null(vm.ResultFlight);
            Assert.Equal("LT-TEST", vm.ResultLintel!.LintelMark);
        }
    }
}
