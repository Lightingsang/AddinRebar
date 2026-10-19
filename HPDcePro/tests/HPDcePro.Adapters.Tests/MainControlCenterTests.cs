using System;
using System.IO;
using System.Threading;
using HPDcePro.Core.Serialization;
using HPDcePro.UI.Common;
using HPDcePro.UI.ViewModels;
using HPDcePro.UI.Views;
using Xunit;

namespace HPDcePro.Adapters.Tests
{
    [Collection("WpfTests")]
    public sealed class MainControlCenterTests
    {
        private static void RunOnStaThread(Action action)
        {
            Exception? caught = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    // Reason: Capture STA thread exception to propagate back to test runner
                    caught = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (caught != null)
            {
                throw new InvalidOperationException($"STA thread failed: {caught.Message}", caught);
            }
        }

        [Fact]
        public void DefaultProperties_InitializeWithExpectedMetricsAndStatuses()
        {
            var vm = new MainControlCenterViewModel();

            Assert.Equal("Dự án HPDcePro Master", vm.ProjectTitle);
            Assert.Equal("DCE-2026-PRO", vm.ProjectCode);
            Assert.Equal(15, vm.TotalStories);
            Assert.Equal(48, vm.TotalColumns);
            Assert.Equal(120, vm.TotalBeams);
            Assert.Equal(24, vm.TotalFootings);
            Assert.Equal(12, vm.TotalSteelFrames);
            Assert.Equal(96.8, vm.RebarEfficiency);

            Assert.Contains("TCVN 5574:2018", vm.BeamModuleStatus);
            Assert.Contains("Biaxial P-M-M", vm.ColumnModuleStatus);
            Assert.Contains("TCVN 10304:2025", vm.FoundationModuleStatus);
            Assert.Contains("11.7m", vm.CuttingOptStatus);
            Assert.Contains("TCVN 5575:2024", vm.SteelModuleStatus);
            Assert.Contains("24 Bước", vm.AutoStrModuleStatus);
        }

        [Fact]
        public void PropertyChanges_UpdateValuesCorrectly()
        {
            var vm = new MainControlCenterViewModel();

            vm.ProjectTitle = "Cao ốc Landmark";
            vm.ProjectCode = "LM-81";
            vm.TotalStories = 81;
            vm.RebarEfficiency = 98.5;

            Assert.Equal("Cao ốc Landmark", vm.ProjectTitle);
            Assert.Equal("LM-81", vm.ProjectCode);
            Assert.Equal(81, vm.TotalStories);
            Assert.Equal(98.5, vm.RebarEfficiency);
        }

        [Fact]
        public void RefreshMetrics_UpdatesStatusMessage()
        {
            var vm = new MainControlCenterViewModel();
            vm.Refresh();
            Assert.Contains("Đã làm mới dữ liệu dự án lúc", vm.StatusMessage);
        }

        [Fact]
        public void NavigationCommands_InvokeOpenWindowActionWithCorrectType()
        {
            Type? openedType = null;
            var vm = new MainControlCenterViewModel(t => openedType = t);

            vm.OpenBeamDetailingCommand.Execute(null);
            Assert.Equal(typeof(BeamDetailingWindow), openedType);

            vm.OpenColumnWallCommand.Execute(null);
            Assert.Equal(typeof(ColumnWallDetailingWindow), openedType);

            vm.OpenFoundationSlabCommand.Execute(null);
            Assert.Equal(typeof(FoundationSlabWindow), openedType);

            vm.OpenCuttingOptimizationCommand.Execute(null);
            Assert.Equal(typeof(CuttingOptimizationWindow), openedType);

            vm.OpenSteelConnectionCommand.Execute(null);
            Assert.Equal(typeof(SteelConnectionWindow), openedType);

            vm.OpenAutoStrConfigCommand.Execute(null);
            Assert.Equal(typeof(AutoStrConfigWindow), openedType);
        }

        [Fact]
        public void ApplyConfig_UpdatesKpiMetricsAndProjectMetadata()
        {
            var vm = new MainControlCenterViewModel();
            var config = new AutoStrConfig
            {
                TenDuAn = "Nhà máy Thép Tân Á",
                TenCongTrinh = "NM-02"
            };
            config.Floors.Add(new AutoStrFloorInfo { StoryIndex = 1, TenTang = "TẦNG 1", CaoDoTang = 0.0, ChieuCaoTang = 4000.0 });
            config.Floors.Add(new AutoStrFloorInfo { StoryIndex = 2, TenTang = "TẦNG 2", CaoDoTang = 4000.0, ChieuCaoTang = 3600.0 });
            config.Floors.Add(new AutoStrFloorInfo { StoryIndex = 3, TenTang = "TẦNG 3", CaoDoTang = 7600.0, ChieuCaoTang = 3600.0 });

            vm.ApplyConfig(config);

            Assert.Equal("Nhà máy Thép Tân Á", vm.ProjectTitle);
            Assert.Equal("NM-02", vm.ProjectCode);
            Assert.Equal(3, vm.TotalStories);
            Assert.Equal(12, vm.TotalColumns);
            Assert.Equal(24, vm.TotalBeams);
            Assert.Equal(4, vm.TotalFootings);
        }

        [Fact]
        public void SaveToFileAndLoadFromFile_RoundTripsConfiguration()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"AutoStr_{Guid.NewGuid():N}.autostr");
            try
            {
                var vm1 = new MainControlCenterViewModel
                {
                    ProjectTitle = "Trường Đại học Quốc gia",
                    ProjectCode = "VNU-2026"
                };
                vm1.SaveToFile(tempFile);
                Assert.True(File.Exists(tempFile));

                var vm2 = new MainControlCenterViewModel();
                vm2.LoadFromFile(tempFile);

                Assert.Equal("Trường Đại học Quốc gia", vm2.ProjectTitle);
                Assert.Equal("VNU-2026", vm2.ProjectCode);
                Assert.Contains("Đã nạp thành công dự án", vm2.StatusMessage);
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
        public void LoadFromFile_MissingFile_ThrowsFileNotFoundException()
        {
            var vm = new MainControlCenterViewModel();
            Assert.Throws<FileNotFoundException>(() => vm.LoadFromFile(@"C:\fake_dir\non_existent_file.autostr"));
        }

        [Fact]
        public void SaveToFile_EmptyPath_ThrowsArgumentNullException()
        {
            var vm = new MainControlCenterViewModel();
            Assert.Throws<ArgumentNullException>(() => vm.SaveToFile(string.Empty));
        }

        [Fact]
        public void MainControlCenterWindow_InstantiatesOnStaThread()
        {
            RunOnStaThread(() =>
            {
                ThemeManager.EnsureAssembliesLoaded();
                var vm = new MainControlCenterViewModel { ProjectTitle = "Chung cư Ecopark" };
                var window = new MainControlCenterWindow(vm);

                Assert.NotNull(window);
                Assert.Same(vm, window.ViewModel);
                Assert.Same(vm, window.DataContext);

                ThemeManager.Apply(window, dark: true);
                ThemeManager.Apply(window, dark: false);
            });
        }

        [Fact]
        public void DesktopLauncher_HeadlessSmoke_InitializesViewModelAndReturnsZero()
        {
            int exitCode = HPDcePro.Desktop.Program.Main(new[] { "--headless-smoke" });
            Assert.Equal(0, exitCode);

            var vm = HPDcePro.Desktop.Program.CreateInitialViewModel();
            Assert.NotNull(vm);
            Assert.False(string.IsNullOrWhiteSpace(vm.ProjectTitle));
        }
    }
}
