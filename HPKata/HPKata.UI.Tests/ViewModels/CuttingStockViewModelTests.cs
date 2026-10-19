using System.Collections.Generic;
using HPKata.Core.Models;
using HPKata.Core.Optimization;
using HPKata.UI.ViewModels;
using Xunit;

namespace HPKata.UI.Tests.ViewModels
{
    public class CuttingStockViewModelTests
    {
        [Fact]
        public void SampleDemands_PopulateAvailableDiameters()
        {
            var vm = new CuttingStockViewModel();
            Assert.Contains(20, vm.AvailableDiameters);
            Assert.Contains(25, vm.AvailableDiameters);
            Assert.True(vm.Demands.Count > 0);
        }

        [Fact]
        public void Optimization_ProducesValidPatterns_AndReportsWasteMetrics()
        {
            var vm = new CuttingStockViewModel
            {
                SelectedDiameter = 20,
                SelectedStrategy = SolverStrategy.Auto,
                StandardStockLengthMm = 11700.0,
                MinRemnantLengthMm = 500.0,
                SawKerfMm = 5.0
            };

            vm.Optimize();

            Assert.True(vm.HasResults);
            Assert.True(vm.NewStandardBarsCount > 0);
            Assert.True(vm.CuttingPatterns.Count > 0);
            Assert.True(vm.GrossWastePercent >= 0.0);
            Assert.True(vm.NetWastePercent <= vm.GrossWastePercent + 0.001);
        }

        [Fact]
        public void LoadFromRebarItems_ExtractsCorrectDemands()
        {
            var vm = new CuttingStockViewModel();
            var rebarItems = new List<RebarItem>
            {
                new() { ComponentMark = "B1", BarMark = "1", DiameterMm = 16, CuttingLengthMm = 4500, CountPerMember = 4, MemberCount = 1 },
                new() { ComponentMark = "B1", BarMark = "2", DiameterMm = 16, CuttingLengthMm = 2800, CountPerMember = 6, MemberCount = 1 },
                new() { ComponentMark = "B2", BarMark = "1", DiameterMm = 22, CuttingLengthMm = 5200, CountPerMember = 3, MemberCount = 2 }
            };

            vm.LoadFromRebarItems(rebarItems);

            Assert.Equal(3, vm.Demands.Count);
            Assert.Contains(16, vm.AvailableDiameters);
            Assert.Contains(22, vm.AvailableDiameters);
        }

        [Fact]
        public void PatternCat_WithSawKerf_DoesNotExceedStockLength()
        {
            var vm = new CuttingStockViewModel
            {
                SelectedDiameter = 20,
                SelectedStrategy = SolverStrategy.PatternCat,
                StandardStockLengthMm = 11700.0,
                SawKerfMm = 5.0
            };

            vm.Optimize();

            Assert.True(vm.HasResults);
            Assert.All(vm.CuttingPatterns, p =>
            {
                Assert.True(p.UsedLengthMm <= p.StockLengthMm + 1e-4,
                    $"Pattern exceeds stock: Used={p.UsedLengthMm}mm > Stock={p.StockLengthMm}mm");
            });
        }
    }
}
