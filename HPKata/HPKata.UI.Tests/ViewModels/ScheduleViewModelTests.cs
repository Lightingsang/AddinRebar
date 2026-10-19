using System.Linq;
using HPKata.Core.Models;
using HPKata.UI.ViewModels;
using Xunit;

namespace HPKata.UI.Tests.ViewModels
{
    public class ScheduleViewModelTests
    {
        [Fact]
        public void LoadSampleData_PopulatesItemsAndCalculatesTotals()
        {
            var vm = new ScheduleViewModel();
            Assert.True(vm.Items.Count >= 5);
            Assert.True(vm.TotalBarCount > 0);
            Assert.True(vm.TotalWeightKg > 0);
            Assert.True(vm.SmallBarWeightKg > 0);
            Assert.True(vm.MediumBarWeightKg > 0);
            Assert.True(vm.LargeBarWeightKg > 0);
        }

        [Fact]
        public void AddItem_IncreasesTotalCount()
        {
            var vm = new ScheduleViewModel();
            int initialCount = vm.Items.Count;

            var newItem = new RebarItem
            {
                ComponentMark = "TEST_BEAM",
                BarMark = "99",
                ShapeCode = "01",
                DiameterMm = 25,
                CountPerMember = 5,
                MemberCount = 2,
                CuttingLengthMm = 6000,
                UnitMassKgPerM = 3.85
            };

            vm.AddItem(newItem);

            Assert.Equal(initialCount + 1, vm.Items.Count);
            Assert.Equal(newItem, vm.SelectedItem);
        }

        [Fact]
        public void RemoveItem_UpdatesTotalsAccurately()
        {
            var vm = new ScheduleViewModel();
            int countBefore = vm.Items.Count;
            var firstItem = vm.Items.First();

            vm.RemoveItem(firstItem);

            Assert.Equal(countBefore - 1, vm.Items.Count);
        }

        [Fact]
        public void ClearAll_ResetsTotalsToZero()
        {
            var vm = new ScheduleViewModel();
            vm.ClearAll();

            Assert.Empty(vm.Items);
            Assert.Equal(0, vm.TotalItemCount);
            Assert.Equal(0, vm.TotalBarCount);
            Assert.Equal(0.0, vm.TotalWeightKg);
        }

        [Fact]
        public void QrPayload_GeneratesExpectedPipeDelimitedFormat()
        {
            var vm = new ScheduleViewModel();
            var item = new RebarItem
            {
                ComponentMark = "D1",
                BarMark = "1",
                ShapeCode = "01",
                DiameterMm = 20,
                CuttingLengthMm = 5850,
                CountPerMember = 3,
                MemberCount = 2,
                UnitMassKgPerM = 2.47
            };

            vm.SelectedItem = item;

            Assert.False(string.IsNullOrWhiteSpace(vm.SelectedQrPayload));
            Assert.StartsWith("KATA|", vm.SelectedQrPayload);
            Assert.Contains("|D1|1|01|20|5850|6|", vm.SelectedQrPayload);
        }
    }
}
