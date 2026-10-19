using System.Linq;
using HPKata.Core.Shapes;
using HPKata.UI.ViewModels;
using Xunit;

namespace HPKata.UI.Tests.ViewModels
{
    public class DetailingViewModelTests
    {
        [Fact]
        public void CatalogLoaded_Contains85Shapes()
        {
            var vm = new DetailingViewModel();
            Assert.Equal(85, vm.Shapes.Count);
        }

        [Fact]
        public void StraightBar_CuttingLength_EqualsL1()
        {
            var vm = new DetailingViewModel();
            var straightShape = vm.Shapes.First(s => s.ShapeCode == "01");
            vm.SelectedShape = straightShape;
            vm.L1 = 3500;

            Assert.Equal(3500, vm.CuttingLengthMm);
        }

        [Fact]
        public void StirrupBar_CuttingLength_CalculatesCorrectly()
        {
            var vm = new DetailingViewModel();
            var stirrupShape = vm.Shapes.First(s => s.ShapeCode == "22");
            vm.SelectedShape = stirrupShape;
            vm.L1 = 200;
            vm.L2 = 400;
            vm.L3 = 75; // 135 hook length

            // dang22: 2 * (L1 + L2) + 2 * L3 = 2 * (200 + 400) + 150 = 1350 mm
            Assert.Equal(1350, vm.CuttingLengthMm);
        }

        [Fact]
        public void Visibilities_UpdateBasedOnParameterCount()
        {
            var vm = new DetailingViewModel();

            // Dang01 (straight bar) has parameter count 1
            vm.SelectedShape = vm.Shapes.First(s => s.ShapeCode == "01");
            Assert.True(vm.IsL1Visible);
            Assert.False(vm.IsL2Visible);
            Assert.False(vm.IsL3Visible);

            // Dang02 (symmetric U bar) has parameter count 2
            vm.SelectedShape = vm.Shapes.First(s => s.ShapeCode == "02");
            Assert.True(vm.IsL1Visible);
            Assert.True(vm.IsL2Visible);
            Assert.False(vm.IsL3Visible);

            // Dang06 (cranked bar) has angle parameter
            vm.SelectedShape = vm.Shapes.First(s => s.ShapeCode == "06");
            Assert.True(vm.IsAngleVisible);
        }

        [Fact]
        public void DiameterChange_UpdatesMassAndTotalWeight()
        {
            var vm = new DetailingViewModel
            {
                Diameter = 20,
                L1 = 1000,
                CountPerMember = 2,
                MemberCount = 1
            };
            vm.SelectedShape = vm.Shapes.First(s => s.ShapeCode == "01");

            // Phi 20: 2.47 kg/m. 2 bars of 1000mm = 2m * 2.47 kg/m = 4.94 kg
            Assert.Equal(2.47, vm.UnitMassKgPerM);
            Assert.Equal(2, vm.TotalCount);
            Assert.Equal(2.0, vm.TotalLengthM);
            Assert.Equal(4.94, vm.TotalWeightKg);
        }

        [Fact]
        public void ToRebarItem_OutputsValidRebarItem()
        {
            var vm = new DetailingViewModel
            {
                ComponentMark = "D2",
                BarMark = "5",
                Diameter = 18,
                L1 = 4500,
                CountPerMember = 4,
                MemberCount = 2
            };
            vm.SelectedShape = vm.Shapes.First(s => s.ShapeCode == "01");

            var item = vm.ToRebarItem();

            Assert.Equal("D2", item.ComponentMark);
            Assert.Equal("5", item.BarMark);
            Assert.Equal("01", item.ShapeCode);
            Assert.Equal(18, item.DiameterMm);
            Assert.Equal(8, item.TotalCount);
            Assert.Equal(4500, item.CuttingLengthMm);
        }
    }
}
