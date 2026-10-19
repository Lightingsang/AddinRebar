using HPKata.UI.ViewModels;
using Xunit;

namespace HPKata.UI.Tests.ViewModels
{
    public class BeamDetailingViewModelTests
    {
        [Fact]
        public void Initialization_HasDefaultSpanAndProperties()
        {
            var vm = new BeamDetailingViewModel();

            Assert.Equal("D1", vm.BeamMark);
            Assert.Equal("Tầng 2", vm.StoryName);
            Assert.Single(vm.Spans);
            Assert.Equal(2, vm.TopContinuousCount);
            Assert.Equal(20, vm.TopContinuousDiameter);
            Assert.Equal(2, vm.BotContinuousCount);
            Assert.Equal(20, vm.BotContinuousDiameter);
        }

        [Fact]
        public void AddAndRemoveSpan_UpdatesSpansCollection()
        {
            var vm = new BeamDetailingViewModel();
            Assert.Single(vm.Spans);

            vm.AddSpanCommand.Execute(null);
            Assert.Equal(2, vm.Spans.Count);

            vm.RemoveSpanCommand.Execute(null);
            Assert.Single(vm.Spans);
        }

        [Fact]
        public void ApplyCommand_GeneratesValidResultLayout()
        {
            var vm = new BeamDetailingViewModel
            {
                BeamMark = "D101",
                TopContinuousCount = 3,
                TopContinuousDiameter = 22,
                BotContinuousCount = 3,
                BotContinuousDiameter = 22
            };

            bool closeRequested = false;
            vm.RequestClose += () => closeRequested = true;

            vm.ApplyCommand.Execute(null);

            Assert.True(closeRequested);
            Assert.NotNull(vm.ResultLayout);
            Assert.Equal("D101", vm.ResultLayout!.BeamName);
            Assert.Single(vm.ResultLayout.TopContinuousBars);
            Assert.Equal(3, vm.ResultLayout.TopContinuousBars[0].BarCount);
            Assert.Equal(22, vm.ResultLayout.TopContinuousBars[0].DiameterMm);
        }

        [Fact]
        public void CancelCommand_ClearsResultLayoutAndRequestsClose()
        {
            var vm = new BeamDetailingViewModel();

            bool closeRequested = false;
            vm.RequestClose += () => closeRequested = true;

            vm.CancelCommand.Execute(null);

            Assert.True(closeRequested);
            Assert.Null(vm.ResultLayout);
        }
    }
}
