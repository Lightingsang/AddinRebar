using System.IO;
using System.Linq;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.UI.ViewModels;
using Xunit;

namespace HPDcePro.Adapters.Tests
{
    public sealed class ViewModelsTests
    {
        [Fact]
        public void BeamDetailingViewModel_UpdatesCalculationsCorrectly()
        {
            var vm = new BeamDetailingViewModel
            {
                WidthMm = 220.0,
                HeightMm = 400.0,
                SelectedConcrete = ConcreteGrade.B25,
                SelectedRebar = RebarGrade.CB400V,
                TopContinuousCount = 2,
                TopContinuousDiameter = 18,
                BottomContinuousCount = 3,
                BottomContinuousDiameter = 18,
                DesignMomentKnM = 80.0,
                DesignShearKn = 60.0
            };

            Assert.True(vm.BottomAsMm2 > 700.0, "3d18 should be ~763mm2");
            Assert.True(vm.MomentCapacityKnM > 80.0, "Capacity should exceed 80 kNm");
            Assert.True(vm.IsFlexurePass);
            Assert.Equal("PASS", vm.OverallStatus);

            // Increase moment to overload
            vm.DesignMomentKnM = 300.0;
            Assert.False(vm.IsFlexurePass);
            Assert.Equal("FAIL", vm.OverallStatus);
        }

        [Fact]
        public void CuttingOptimizationViewModel_OptimizesPatterns()
        {
            var vm = new CuttingOptimizationViewModel();
            Assert.NotEmpty(vm.Demands);

            vm.Optimize();
            Assert.True(vm.TotalStockBarsUsed > 0);
            Assert.NotEmpty(vm.ResultPatterns);
            Assert.InRange(vm.ScrapPercentage, 0.0, 100.0);

            int prevCount = vm.Demands.Count;
            vm.AddDemand();
            Assert.Equal(prevCount + 1, vm.Demands.Count);

            vm.RemoveDemand(vm.Demands.Last());
            Assert.Equal(prevCount, vm.Demands.Count);
        }

        [Fact]
        public void AutoStrConfigViewModel_ManagesFloorsAndSteps()
        {
            var vm = new AutoStrConfigViewModel();
            Assert.NotEmpty(vm.Floors);
            Assert.Equal(24, vm.Steps.Count);

            vm.DeselectAllSteps();
            Assert.All(vm.Steps, s => Assert.False(s.IsEnabled));

            vm.SelectAllSteps();
            Assert.All(vm.Steps, s => Assert.True(s.IsEnabled));

            int prevFloors = vm.Floors.Count;
            vm.AddFloor();
            Assert.Equal(prevFloors + 1, vm.Floors.Count);

            string tempPath = Path.Combine(Path.GetTempPath(), $"test_project_{System.Guid.NewGuid():N}.autostr");
            try
            {
                vm.SaveToFile(tempPath);
                Assert.True(File.Exists(tempPath));

                var vmLoaded = new AutoStrConfigViewModel();
                vmLoaded.LoadFromFile(tempPath);
                Assert.Equal(vm.TenDuAn, vmLoaded.TenDuAn);
                Assert.Equal(vm.Floors.Count, vmLoaded.Floors.Count);
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        }
    }
}
