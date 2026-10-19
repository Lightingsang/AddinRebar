using HPKata.UI.ViewModels;
using Xunit;

namespace HPKata.UI.Tests.ViewModels
{
    public class SlabDetailingViewModelTests
    {
        [Fact]
        public void Default_HasTwoWayPanelAndChairStoolCalculated()
        {
            var vm = new SlabDetailingViewModel();

            Assert.Equal("S1", vm.PanelMark);
            Assert.Equal(4000.0, vm.SpanXMm);
            Assert.Equal(5000.0, vm.SpanYMm);
            Assert.Equal(120.0, vm.ThicknessMm);

            // Stool height = 120 - 15 - 15 - 20 - 20 = 50 mm (multiple of 5)
            Assert.Equal(50.0, vm.CalculatedStoolHeightMm);
            // 4m * 5m = 20 m2 * 1.2 = 24 chairs
            Assert.Equal(24, vm.TotalChairs);
            Assert.True(vm.EstimatedVolumeM3 > 0);
            Assert.True(vm.EstimatedWeightKg > 0);
        }

        [Fact]
        public void Recalculate_WithThickerSlab_UpdatesStoolHeight()
        {
            var vm = new SlabDetailingViewModel
            {
                ThicknessMm = 150.0,
                CoverMm = 20.0,
                TopSupportDia = 12,
                BottomMeshXDia = 12,
                SpanXMm = 6000.0,
                SpanYMm = 6000.0
            };

            vm.Recalculate();

            // Stool height = 150 - 20 - 20 - 24 - 24 = 62 mm -> rounded to 60 mm
            Assert.Equal(60.0, vm.CalculatedStoolHeightMm);
            // 36 m2 * 1.2 = 43.2 -> ceil = 44 chairs
            Assert.Equal(44, vm.TotalChairs);
        }

        [Fact]
        public void ApplyCommand_SetsResultLayoutAndCloses()
        {
            var vm = new SlabDetailingViewModel { PanelMark = "S101" };

            bool closed = false;
            vm.RequestClose += () => closed = true;

            vm.ApplyCommand.Execute(null);

            Assert.True(closed);
            Assert.NotNull(vm.ResultLayout);
            Assert.Equal("S101", vm.ResultLayout!.PanelMark);
            Assert.NotNull(vm.ResultLayout.Panel);
        }
    }
}
