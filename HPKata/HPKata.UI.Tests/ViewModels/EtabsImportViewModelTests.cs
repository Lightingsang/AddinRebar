using HPKata.UI.ViewModels;
using Xunit;

namespace HPKata.UI.Tests.ViewModels
{
    public class EtabsImportViewModelTests
    {
        [Fact]
        public void ParseTableText_EmptyInput_SetsStatusMessage()
        {
            var vm = new EtabsImportViewModel { TableInputText = "" };
            vm.ParseTableTextCommand.Execute(null);

            Assert.Empty(vm.Beams);
            Assert.Contains("Vui lòng dán nội dung", vm.StatusMessage);
        }

        [Fact]
        public void ParseTableText_ValidTable_PopulatesBeamsAndStories()
        {
            var vm = new EtabsImportViewModel
            {
                TableInputText = "Story\tBeam\tStation\tTopArea\tBotArea\n" +
                                 "Story 1\tB1\t0.0\t800.0\t400.0\n" +
                                 "Story 1\tB1\t5000.0\t800.0\t950.0\n"
            };

            vm.ParseTableTextCommand.Execute(null);

            Assert.Single(vm.Beams);
            Assert.Equal("B1", vm.Beams[0].RunName);
            Assert.Equal("Story 1", vm.Beams[0].StoryName);
            Assert.True(vm.Beams[0].IsCompliant);
            Assert.Contains("Đã nạp thành công 1 dầm", vm.StatusMessage);
        }

        [Fact]
        public void AutoSelectRebar_ReOptimizesSelectedBeams()
        {
            var vm = new EtabsImportViewModel
            {
                TableInputText = "Story\tBeam\tStation\tTopArea\tBotArea\n" +
                                 "Story 1\tB1\t0.0\t1200.0\t500.0\n"
            };

            vm.ParseTableTextCommand.Execute(null);
            Assert.Single(vm.Beams);

            vm.AutoSelectRebarCommand.Execute(null);

            Assert.NotEmpty(vm.Beams[0].TopRebarText);
            Assert.Contains("Đã tối ưu cốt thép", vm.StatusMessage);
        }

        [Fact]
        public void ApplyImportCommand_SelectedBeams_PopulatesResultLayouts()
        {
            var vm = new EtabsImportViewModel
            {
                TableInputText = "Story\tBeam\tStation\tTopArea\tBotArea\n" +
                                 "Story 1\tB1\t0.0\t800.0\t400.0\n"
            };

            vm.ParseTableTextCommand.Execute(null);

            bool closeRequested = false;
            vm.RequestClose += () => closeRequested = true;

            vm.ApplyImportCommand.Execute(null);

            Assert.True(closeRequested);
            Assert.Single(vm.ResultLayouts);
            Assert.Equal("B1", vm.ResultLayouts[0].BeamName);
        }
    }
}
