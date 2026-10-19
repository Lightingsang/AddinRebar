using HPKata.UI.ViewModels;
using Xunit;

namespace HPKata.UI.Tests.ViewModels
{
    public class WallDetailingViewModelTests
    {
        [Fact]
        public void Initialization_HasDefaultProperties()
        {
            var vm = new WallDetailingViewModel();

            Assert.Equal("V1", vm.WallMark);
            Assert.Equal(300.0, vm.ThicknessMm);
            Assert.Equal(3000.0, vm.LengthMm);
            Assert.True(vm.HasBoundaryElements);
            Assert.True(vm.IsDoubleCurtain);
        }

        [Fact]
        public void AddAndRemoveOpening_UpdatesOpeningsCollection()
        {
            var vm = new WallDetailingViewModel();
            Assert.Empty(vm.Openings);

            vm.AddOpeningCommand.Execute(null);
            Assert.Single(vm.Openings);
            Assert.Equal("OP1", vm.Openings[0].OpeningMark);

            vm.RemoveOpeningCommand.Execute(null);
            Assert.Empty(vm.Openings);
        }

        [Fact]
        public void AutoCalculateBoundary_CalculatesLengthAndTieSpacing()
        {
            var vm = new WallDetailingViewModel
            {
                LengthMm = 4000.0,
                ThicknessMm = 300.0,
                BoundaryBarDiameter = 20
            };

            vm.AutoCalculateBoundaryCommand.Execute(null);

            // bbz = max(0.15*4000=600, 1.5*300=450, 400) = 600
            Assert.Equal(600.0, vm.BoundaryLengthMm);
            Assert.Equal(100.0, vm.BoundaryTieSpacing);
        }

        [Fact]
        public void ApplyCommand_GeneratesValidResultModel()
        {
            var vm = new WallDetailingViewModel
            {
                WallMark = "SW-10",
                ThicknessMm = 350.0,
                LengthMm = 3500.0,
                StoryCount = 2
            };

            bool closeRequested = false;
            vm.RequestClose += () => closeRequested = true;

            vm.ApplyCommand.Execute(null);

            Assert.True(closeRequested);
            Assert.NotNull(vm.ResultModel);
            Assert.Equal("SW-10", vm.ResultModel!.WallMark);
            Assert.Equal(2, vm.ResultModel.Stories.Count);
            Assert.Equal(2, vm.ResultModel.BoundaryElements.Count);
            Assert.True(vm.ResultModel.WebReinforcement.IsDoubleCurtain);
        }
    }
}
