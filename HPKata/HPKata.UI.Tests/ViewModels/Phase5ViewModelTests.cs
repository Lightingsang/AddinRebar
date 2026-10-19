using System;
using System.IO;
using HPKata.Core.DrawingLayout.Models;
using HPKata.Core.QuantitySurveying.Models;
using HPKata.UI.ViewModels;
using Xunit;

namespace HPKata.UI.Tests.ViewModels
{
    public class Phase5ViewModelTests
    {
        [Fact]
        public void QsTakeoffViewModel_InitializesWithSampleDataAndCalculates()
        {
            var vm = new QsTakeoffViewModel();

            Assert.True(vm.Items.Count > 0);
            Assert.True(vm.TotalVolumeM3 > 0.0);
            Assert.True(vm.TotalFormworkM2 > 0.0);
            Assert.Contains("Đã tính toán", vm.StatusMessage);
        }

        [Fact]
        public void QsTakeoffViewModel_SetElements_UpdatesCalculations()
        {
            var vm = new QsTakeoffViewModel();
            var col = new QsElementModel
            {
                Id = "C100",
                Mark = "C100",
                MemberType = QsMemberType.Column,
                WidthMm = 500,
                HeightMm = 500,
                LengthMm = 4000,
                MemberCount = 1
            };

            vm.SetElements(new[] { col });

            Assert.Single(vm.Items);
            // 0.5 * 0.5 * 4.0 = 1.0 m3
            Assert.Equal(1.0, Math.Round(vm.TotalVolumeM3, 4));
        }

        [Fact]
        public void QsTakeoffViewModel_ExportExcelCommand_CreatesValidFile()
        {
            var vm = new QsTakeoffViewModel();
            string tempFile = Path.Combine(Path.GetTempPath(), $"Vm_QS_{Guid.NewGuid():N}.xlsx");

            try
            {
                vm.ExportExcel(tempFile);
                Assert.True(File.Exists(tempFile));
                Assert.Contains("Đã xuất", vm.StatusMessage);
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
        public void DrawingUtilityViewModel_GenerateGridCommand_PopulatesAxes()
        {
            var vm = new DrawingUtilityViewModel
            {
                XSpansText = "3000, 4000, 5000",
                YSpansText = "4000, 5000",
                Scale = 100
            };

            vm.GenerateGridCommand.Execute(null);

            // 4 vertical axes + 3 horizontal axes = 7 axes
            Assert.Equal(7, vm.GeneratedAxes.Count);
            Assert.Contains("Đã sinh", vm.GridStatusMessage);
        }

        [Fact]
        public void DrawingUtilityViewModel_ArrangeSheetsCommand_PacksBlocksOnSheets()
        {
            var vm = new DrawingUtilityViewModel
            {
                SelectedFormat = SheetFormat.A1,
                IsLandscape = true
            };

            vm.ArrangeSheetsCommand.Execute(null);

            Assert.True(vm.ArrangedSheets.Count > 0);
            Assert.Contains("Đã sắp xếp", vm.SheetStatusMessage);
        }

        [Fact]
        public void DrawingUtilityViewModel_GenerateIndexCommand_BuildsRegisterTable()
        {
            var vm = new DrawingUtilityViewModel();

            vm.GenerateIndexCommand.Execute(null);

            Assert.NotNull(vm.GeneratedTable);
            Assert.True(vm.GeneratedTable.TotalSheets > 0);
            Assert.Contains("Đã tổng hợp", vm.IndexStatusMessage);
        }
    }
}
