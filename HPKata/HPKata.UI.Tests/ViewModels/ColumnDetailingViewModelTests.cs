using HPKata.UI.ViewModels;
using Xunit;

namespace HPKata.UI.Tests.ViewModels
{
    public class ColumnDetailingViewModelTests
    {
        [Fact]
        public void Initialization_HasDefaultStoryAndProperties()
        {
            var vm = new ColumnDetailingViewModel();

            Assert.Equal("C1", vm.ColumnMark);
            Assert.Equal("B25", vm.ConcreteGrade);
            Assert.Single(vm.Stories);
            Assert.Equal(20, vm.Stories[0].CornerDiameter);
            Assert.Equal(400.0, vm.Stories[0].WidthMm);
            Assert.Equal(400.0, vm.Stories[0].DepthMm);
        }

        [Fact]
        public void AddAndRemoveStory_UpdatesStoriesCollection()
        {
            var vm = new ColumnDetailingViewModel();
            Assert.Single(vm.Stories);

            vm.AddStoryCommand.Execute(null);
            Assert.Equal(2, vm.Stories.Count);
            Assert.Equal("Tầng 2", vm.Stories[1].StoryName);

            vm.RemoveStoryCommand.Execute(null);
            Assert.Single(vm.Stories);
        }

        [Fact]
        public void AutoSizeBars_UpdatesReinforcementSizing()
        {
            var vm = new ColumnDetailingViewModel();
            vm.Stories[0].WidthMm = 600.0;
            vm.Stories[0].DepthMm = 600.0;

            vm.AutoSizeBarsCommand.Execute(null);

            Assert.True(vm.Stories[0].CornerDiameter >= 18);
            Assert.True(vm.Stories[0].SideCountB >= 0);
        }

        [Fact]
        public void ApplyCommand_GeneratesValidResultLayout()
        {
            var vm = new ColumnDetailingViewModel
            {
                ColumnMark = "C101"
            };

            bool closeRequested = false;
            vm.RequestClose += () => closeRequested = true;

            vm.ApplyCommand.Execute(null);

            Assert.True(closeRequested);
            Assert.NotNull(vm.ResultLayout);
            Assert.Equal("C101", vm.ResultLayout!.ColumnMark);
            Assert.Single(vm.ResultLayout.Stories);
            Assert.True(vm.ResultLayout.LongitudinalBars.Count > 0);
        }
    }
}
