using HPKata.UI.ViewModels;
using Xunit;

namespace HPKata.UI.Tests.ViewModels
{
    public class FoundationDetailingViewModelTests
    {
        [Fact]
        public void Default_IsPadFooting_CalculatesVolumeAndWeight()
        {
            var vm = new FoundationDetailingViewModel();

            Assert.Equal("F1", vm.FoundationMark);
            Assert.False(vm.IsPileCap);
            Assert.True(vm.EstimatedVolumeM3 > 0);
            Assert.True(vm.EstimatedWeightKg > 0);
        }

        [Fact]
        public void SwitchToPileCap_RecalculatesAccurately()
        {
            var vm = new FoundationDetailingViewModel
            {
                IsPileCap = true,
                PileCount = 4,
                PileDiameterMm = 400.0,
                LengthMm = 2000.0,
                WidthMm = 2000.0,
                TotalHeightMm = 1000.0
            };

            vm.Recalculate();

            Assert.InRange(vm.EstimatedVolumeM3, 3.8, 4.0);
            Assert.True(vm.EstimatedWeightKg > 0);
        }

        [Fact]
        public void ApplyCommand_Footing_SetsResultLayoutAndCloses()
        {
            var vm = new FoundationDetailingViewModel
            {
                FoundationMark = "M1",
                IsPileCap = false
            };

            bool closed = false;
            vm.RequestClose += () => closed = true;

            vm.ApplyCommand.Execute(null);

            Assert.True(closed);
            Assert.NotNull(vm.ResultLayout);
            Assert.Equal("M1", vm.ResultLayout!.FoundationMark);
            Assert.NotNull(vm.ResultLayout.Footing);
            Assert.Null(vm.ResultLayout.PileCap);
        }

        [Fact]
        public void ApplyCommand_PileCap_SetsResultLayoutWithCap()
        {
            var vm = new FoundationDetailingViewModel
            {
                FoundationMark = "MC1",
                IsPileCap = true,
                PileCount = 5
            };

            bool closed = false;
            vm.RequestClose += () => closed = true;

            vm.ApplyCommand.Execute(null);

            Assert.True(closed);
            Assert.NotNull(vm.ResultLayout);
            Assert.Equal("MC1", vm.ResultLayout!.FoundationMark);
            Assert.NotNull(vm.ResultLayout.PileCap);
            Assert.Null(vm.ResultLayout.Footing);
            Assert.Equal(5, vm.ResultLayout.PileCap.Piles.Count);
        }
    }
}
