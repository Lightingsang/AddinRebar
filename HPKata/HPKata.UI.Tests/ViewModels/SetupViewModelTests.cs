using System.IO;
using HPKata.Core.Configuration;
using HPKata.UI.ViewModels;
using Xunit;

namespace HPKata.UI.Tests.ViewModels
{
    public class SetupViewModelTests
    {
        [Fact]
        public void DefaultProperties_MatchKataConfigDefaults()
        {
            var vm = new SetupViewModel();
            Assert.Equal("kata_dim", vm.LayerDim);
            Assert.Equal("kata_thep chu", vm.LayerThepChu);
            Assert.Equal("kata_thep dai", vm.LayerThepDai);
            Assert.Equal("a", vm.SpacingPrefix);
            Assert.Equal("Ø", vm.SmallBarPrefix);
            Assert.Equal(30.0, vm.ConcreteCoverMm);
            Assert.Equal(16, vm.BaseBarDiameter);
            Assert.Equal(11700, vm.StandardBarLength);
            Assert.Equal(100, vm.MinRemnantLength);
            Assert.Equal(0.04, vm.DetailDrawingScale);
            Assert.Equal("Tiếng Việt", vm.LanguageUiSched);
        }

        [Fact]
        public void ModifyingProperties_UpdatesToConfigResult()
        {
            var vm = new SetupViewModel
            {
                ConcreteCoverMm = 35.0,
                BaseBarDiameter = 20,
                StandardBarLength = 12000,
                LayerThepChu = "CUSTOM_THEP_CHU",
                SpacingPrefix = "s"
            };

            var config = vm.ToConfig();

            Assert.Equal(35.0, config.ConcreteCoverMm);
            Assert.Equal(20, config.BaseBarDiameter);
            Assert.Equal(12000, config.StandardBarLength);
            Assert.Equal("CUSTOM_THEP_CHU", config.LayerThepChu);
            Assert.Equal("s", config.SpacingPrefix);
        }

        [Fact]
        public void ResetToDefaults_RestoresInitialState()
        {
            var vm = new SetupViewModel
            {
                ConcreteCoverMm = 50.0,
                BaseBarDiameter = 25
            };

            vm.ResetToDefaults();

            Assert.Equal(30.0, vm.ConcreteCoverMm);
            Assert.Equal(16, vm.BaseBarDiameter);
            Assert.True(vm.IsDirty);
        }

        [Fact]
        public void SaveAndLoadFromFile_RoundTripsAccurately()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"setup_test_{System.Guid.NewGuid():N}.txt");
            try
            {
                var vm1 = new SetupViewModel
                {
                    ConcreteCoverMm = 40.0,
                    BaseBarDiameter = 22,
                    LayerDim = "test_dim_layer"
                };
                vm1.SaveToFile(tempFile);

                var vm2 = new SetupViewModel();
                vm2.LoadFromFile(tempFile);

                Assert.Equal(40.0, vm2.ConcreteCoverMm);
                Assert.Equal(22, vm2.BaseBarDiameter);
                Assert.Equal("test_dim_layer", vm2.LayerDim);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public void PropertyMutation_SetsIsDirtyTrue()
        {
            var vm = new SetupViewModel();
            Assert.False(vm.IsDirty);

            vm.ConcreteCoverMm = 45.0;
            Assert.True(vm.IsDirty);

            string tempFile = Path.Combine(Path.GetTempPath(), $"setup_dirty_{System.Guid.NewGuid():N}.txt");
            try
            {
                vm.SaveToFile(tempFile);
                Assert.False(vm.IsDirty);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }

            vm.BaseBarDiameter = 25;
            Assert.True(vm.IsDirty);

            vm.ApplyAndClose();
            Assert.False(vm.IsDirty);
        }

        [Fact]
        public void IsDirty_NotTriggeredByInitializationOrStatusMessage()
        {
            var config = KataConfigDefaults.CreateDefault();
            config.ConcreteCoverMm = 40.0;
            var vm = new SetupViewModel(config);
            Assert.False(vm.IsDirty);

            vm.StatusMessage = "Testing status";
            Assert.False(vm.IsDirty);
        }
    }
}
