using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using HPDcePro.Core.Serialization;
using HPDcePro.UI.ViewModels;
using HPDcePro.UI.Views;
using Xunit;

namespace HPDcePro.Adapters.Tests
{
    [Collection("WpfTests")]
    public sealed class Wave3Challenger2StressTests
    {
        [Fact]
        public void HPDceProLsp_ContainsExactAliasesForAllTenSteelCommandsAndGetform()
        {
            string lspPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HPDcePro.lsp");
            if (!File.Exists(lspPath))
            {
                lspPath = Path.GetFullPath(Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "src", "HPDcePro.AutoCad", "HPDcePro.lsp"));
            }

            Assert.True(File.Exists(lspPath), $"HPDcePro.lsp must exist at: {lspPath}");
            string lspContent = File.ReadAllText(lspPath);

            string[] expectedCommands =
            {
                "DrawElevationMiddleFrameBT",
                "DrawElevationMiddleFrame3",
                "DrawElevationMiddleFrame3BT",
                "DrawElevationMiddleFrame5",
                "DrawElevationMiddleFrame5a",
                "DrawElevationVDS1",
                "DrawPlanPulirnRoof",
                "DrawPlanPurlinMonitor",
                "DetailAnchorBolt",
                "SteelConnectionDialog",
                "GETFORM"
            };

            foreach (var cmd in expectedCommands)
            {
                string searchPattern = $"\"{cmd}\"";
                Assert.True(lspContent.Contains(searchPattern), $"HPDcePro.lsp must invoke command: {cmd}");
            }
        }

        [Fact]
        public void MainControlCenterViewModel_RapidRepeatedNavigationExecution_HandlesHighVolumeWithoutLeakOrError()
        {
            var callCounts = new Dictionary<Type, int>();
            var vm = new MainControlCenterViewModel(t =>
            {
                callCounts[t] = callCounts.GetValueOrDefault(t, 0) + 1;
            });

            const int iterations = 250;
            for (int i = 0; i < iterations; i++)
            {
                vm.OpenBeamDetailingCommand.Execute(null);
                vm.OpenColumnWallCommand.Execute(null);
                vm.OpenFoundationSlabCommand.Execute(null);
                vm.OpenCuttingOptimizationCommand.Execute(null);
                vm.OpenSteelConnectionCommand.Execute(null);
                vm.OpenAutoStrConfigCommand.Execute(null);
            }

            Assert.Equal(iterations, callCounts[typeof(BeamDetailingWindow)]);
            Assert.Equal(iterations, callCounts[typeof(ColumnWallDetailingWindow)]);
            Assert.Equal(iterations, callCounts[typeof(FoundationSlabWindow)]);
            Assert.Equal(iterations, callCounts[typeof(CuttingOptimizationWindow)]);
            Assert.Equal(iterations, callCounts[typeof(SteelConnectionWindow)]);
            Assert.Equal(iterations, callCounts[typeof(AutoStrConfigWindow)]);
        }

        [Fact]
        public void MainControlCenterViewModel_OpenModuleWithNullWindowAction_FallsBackSafelyWithoutCrashing()
        {
            var vm = new MainControlCenterViewModel(null);

            var ex = Record.Exception(() =>
            {
                vm.OpenModule(typeof(Window));
                vm.OpenModule(null!);
            });

            Assert.Null(ex);
        }

        [Fact]
        public void MainControlCenterViewModel_LoadFromFile_CorruptedAndEmptyFiles_ThrowsExpectedExceptions()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"DcePro_Stress_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                var vm = new MainControlCenterViewModel();
                string initialStatus = vm.StatusMessage;

                string emptyFile = Path.Combine(tempDir, "empty.autostr");
                File.WriteAllBytes(emptyFile, Array.Empty<byte>());
                Assert.Throws<ArgumentException>(() => vm.LoadFromFile(emptyFile));
                Assert.Equal(initialStatus, vm.StatusMessage);

                string whitespaceFile = Path.Combine(tempDir, "whitespace.autostr");
                File.WriteAllText(whitespaceFile, "   \r\n\t   ");
                Assert.Throws<ArgumentNullException>(() => vm.LoadFromFile(whitespaceFile));
                Assert.Equal(initialStatus, vm.StatusMessage);

                string brokenJsonFile = Path.Combine(tempDir, "broken.autostr");
                File.WriteAllText(brokenJsonFile, "{ \"TenDuAn\": \"Nhà máy thép\", \"Floors\": [ { \"StoryIndex\": 1, ");
                Assert.Throws<JsonException>(() => vm.LoadFromFile(brokenJsonFile));
                Assert.Equal(initialStatus, vm.StatusMessage);

                string nullJsonFile = Path.Combine(tempDir, "null.autostr");
                File.WriteAllText(nullJsonFile, "null");
                Assert.Throws<InvalidDataException>(() => vm.LoadFromFile(nullJsonFile));
                Assert.Equal(initialStatus, vm.StatusMessage);

                string emptyObjectFile = Path.Combine(tempDir, "empty_object.autostr");
                File.WriteAllText(emptyObjectFile, "{}");
                vm.LoadFromFile(emptyObjectFile);
                Assert.Contains("Đã nạp thành công dự án", vm.StatusMessage);

                string bomFile = Path.Combine(tempDir, "bom.autostr");
                var bomBytes = new byte[] { 0xEF, 0xBB, 0xBF };
                var jsonBytes = Encoding.UTF8.GetBytes("{ \"TenDuAn\": \"Dự án có BOM\" }");
                File.WriteAllBytes(bomFile, bomBytes.Concat(jsonBytes).ToArray());

                vm.LoadFromFile(bomFile);
                Assert.Equal("Dự án có BOM", vm.ProjectTitle);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public void MainControlCenterViewModel_ApplyConfig_ExtremeStoryCounts_CalculatesWithoutDivisionByZero()
        {
            var vm = new MainControlCenterViewModel();

            var config0 = new AutoStrConfig { TenDuAn = "Zero Stories" };
            config0.Floors.Clear();
            vm.ApplyConfig(config0);
            Assert.Equal("Zero Stories", vm.ProjectTitle);
            Assert.Equal(15, vm.TotalStories);

            var config1 = new AutoStrConfig { TenDuAn = "Single Story" };
            config1.Floors.Add(new AutoStrFloorInfo { StoryIndex = 1, TenTang = "T1", CaoDoTang = 0.0, ChieuCaoTang = 4000.0 });
            vm.ApplyConfig(config1);
            Assert.Equal(1, vm.TotalStories);
            Assert.Equal(4, vm.TotalColumns);
            Assert.Equal(8, vm.TotalBeams);
            Assert.Equal(4, vm.TotalFootings);

            var config500 = new AutoStrConfig { TenDuAn = "Sky Tower 500" };
            for (int i = 1; i <= 500; i++)
            {
                config500.Floors.Add(new AutoStrFloorInfo { StoryIndex = i, TenTang = $"T{i}", CaoDoTang = i * 3600.0, ChieuCaoTang = 3600.0 });
            }
            vm.ApplyConfig(config500);
            Assert.Equal(500, vm.TotalStories);
            Assert.Equal(2000, vm.TotalColumns);
            Assert.Equal(4000, vm.TotalBeams);
            Assert.Equal(4, vm.TotalFootings);
        }

        [Fact]
        public void MainControlCenterViewModel_PropertyChangedEvents_FireAccurately()
        {
            var vm = new MainControlCenterViewModel();
            var firedProps = new HashSet<string>();

            vm.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName != null)
                {
                    firedProps.Add(e.PropertyName);
                }
            };

            vm.ProjectTitle = "New Title";
            vm.ProjectCode = "NEW-CODE";
            vm.TotalStories = 20;
            vm.TotalColumns = 80;
            vm.TotalBeams = 160;
            vm.TotalFootings = 32;
            vm.TotalSteelFrames = 18;
            vm.RebarEfficiency = 97.2;
            vm.BeamModuleStatus = "Status 1";
            vm.ColumnModuleStatus = "Status 2";
            vm.FoundationModuleStatus = "Status 3";
            vm.CuttingOptStatus = "Status 4";
            vm.SteelModuleStatus = "Status 5";
            vm.AutoStrModuleStatus = "Status 6";
            vm.StatusMessage = "Status 7";

            Assert.Contains(nameof(vm.ProjectTitle), firedProps);
            Assert.Contains(nameof(vm.ProjectCode), firedProps);
            Assert.Contains(nameof(vm.TotalStories), firedProps);
            Assert.Contains(nameof(vm.TotalColumns), firedProps);
            Assert.Contains(nameof(vm.TotalBeams), firedProps);
            Assert.Contains(nameof(vm.TotalFootings), firedProps);
            Assert.Contains(nameof(vm.TotalSteelFrames), firedProps);
            Assert.Contains(nameof(vm.RebarEfficiency), firedProps);
            Assert.Contains(nameof(vm.StatusMessage), firedProps);
        }
    }
}
