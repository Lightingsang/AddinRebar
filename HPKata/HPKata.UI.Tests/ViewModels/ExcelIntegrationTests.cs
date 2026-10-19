using System;
using System.Collections.Generic;
using HPKata.Core.Excel;
using HPKata.Core.Excel.Data;
using HPKata.Core.Models;
using HPKata.UI.ViewModels;
using Xunit;

namespace HPKata.UI.Tests.ViewModels
{
    public class ExcelIntegrationTests
    {
        [Fact]
        public void BeamViewModel_ReadExcel_PopulatesProperties()
        {
            var fakeAdapter = new FakeKataExcelAdapter();
            fakeAdapter.BeamDataToReturn.BeamMark = "D-TEST";
            fakeAdapter.BeamDataToReturn.TopContinuousCount = 4;
            fakeAdapter.BeamDataToReturn.TopContinuousDiameter = 25;
            fakeAdapter.BeamDataToReturn.Spans.Add(new KataBeamSpanData
            {
                SpanName = "Nhịp A",
                ClearLengthMm = 6200.0,
                WidthMm = 350.0,
                HeightMm = 700.0
            });

            var vm = new BeamDetailingViewModel(fakeAdapter);
            vm.ReadExcelCommand.Execute(null);

            Assert.Equal("D-TEST", vm.BeamMark);
            Assert.Equal(4, vm.TopContinuousCount);
            Assert.Equal(25, vm.TopContinuousDiameter);
            Assert.Single(vm.Spans);
            Assert.Equal("Nhịp A", vm.Spans[0].SpanName);
            Assert.Contains("Đã đọc 1 nhịp dầm", vm.ExcelStatus);
        }

        [Fact]
        public void BeamViewModel_WriteExcel_PassesCorrectDataToAdapter()
        {
            var fakeAdapter = new FakeKataExcelAdapter();
            var vm = new BeamDetailingViewModel(fakeAdapter)
            {
                BeamMark = "D-WRITE",
                TopContinuousCount = 3,
                TopContinuousDiameter = 22
            };

            vm.WriteExcelCommand.Execute(null);

            Assert.NotNull(fakeAdapter.LastWrittenBeamData);
            Assert.Equal("D-WRITE", fakeAdapter.LastWrittenBeamData!.BeamMark);
            Assert.Equal(3, fakeAdapter.LastWrittenBeamData.TopContinuousCount);
            Assert.Equal(22, fakeAdapter.LastWrittenBeamData.TopContinuousDiameter);
            Assert.Contains("Đã lưu dầm", vm.ExcelStatus);
        }

        [Fact]
        public void BeamViewModel_ApplyCommand_AutoSyncsWhenEnabled()
        {
            var fakeAdapter = new FakeKataExcelAdapter();
            var vm = new BeamDetailingViewModel(fakeAdapter)
            {
                AutoSyncOnApply = true,
                BeamMark = "D-AUTOSYNC"
            };

            vm.ApplyCommand.Execute(null);

            Assert.Equal(1, fakeAdapter.WriteBeamCallCount);
            Assert.Equal("D-AUTOSYNC", fakeAdapter.LastWrittenBeamData?.BeamMark);
        }

        [Fact]
        public void ColumnViewModel_ReadAndWriteExcel_RoundTripsProperly()
        {
            var fakeAdapter = new FakeKataExcelAdapter();
            fakeAdapter.ColumnDataToReturn.ColumnMark = "C-TEST";
            fakeAdapter.ColumnDataToReturn.Stories.Add(new KataColumnStoryData
            {
                StoryName = "Tầng 1",
                WidthMm = 500.0,
                DepthMm = 500.0
            });

            var vm = new ColumnDetailingViewModel(fakeAdapter);
            vm.ReadExcelCommand.Execute(null);

            Assert.Equal("C-TEST", vm.ColumnMark);
            Assert.Single(vm.Stories);
            Assert.Equal(500.0, vm.Stories[0].WidthMm);

            vm.ColumnMark = "C-UPDATED";
            vm.WriteExcelCommand.Execute(null);

            Assert.Equal("C-UPDATED", fakeAdapter.LastWrittenColumnData?.ColumnMark);
        }

        [Fact]
        public void ColumnViewModel_ApplyCommand_AutoSyncsWhenEnabled()
        {
            var fakeAdapter = new FakeKataExcelAdapter();
            var vm = new ColumnDetailingViewModel(fakeAdapter)
            {
                AutoSyncOnApply = true,
                ColumnMark = "C-AUTOSYNC"
            };

            vm.ApplyCommand.Execute(null);

            Assert.Equal(1, fakeAdapter.WriteColumnCallCount);
            Assert.Equal("C-AUTOSYNC", fakeAdapter.LastWrittenColumnData?.ColumnMark);
        }

        [Fact]
        public void WallViewModel_ReadAndWriteExcel_RoundTripsProperly()
        {
            var fakeAdapter = new FakeKataExcelAdapter();
            fakeAdapter.WallDataToReturn.WallMark = "SW-TEST";
            fakeAdapter.WallDataToReturn.ThicknessMm = 350.0;
            fakeAdapter.WallDataToReturn.Openings.Add(new KataWallOpeningData
            {
                OpeningMark = "OP-1",
                WidthMm = 1000.0,
                HeightMm = 2200.0
            });

            var vm = new WallDetailingViewModel(fakeAdapter);
            vm.ReadExcelCommand.Execute(null);

            Assert.Equal("SW-TEST", vm.WallMark);
            Assert.Equal(350.0, vm.ThicknessMm);
            Assert.Single(vm.Openings);

            vm.WallMark = "SW-NEW";
            vm.WriteExcelCommand.Execute(null);

            Assert.Equal("SW-NEW", fakeAdapter.LastWrittenWallData?.WallMark);
        }

        [Fact]
        public void WallViewModel_ApplyCommand_AutoSyncsWhenEnabled()
        {
            var fakeAdapter = new FakeKataExcelAdapter();
            var vm = new WallDetailingViewModel(fakeAdapter)
            {
                AutoSyncOnApply = true,
                WallMark = "SW-AUTOSYNC"
            };

            vm.ApplyCommand.Execute(null);

            Assert.Equal(1, fakeAdapter.WriteWallCallCount);
            Assert.Equal("SW-AUTOSYNC", fakeAdapter.LastWrittenWallData?.WallMark);
        }

        private sealed class FakeKataExcelAdapter : IKataExcelAdapter
        {
            public bool IsDesktopExcelOpen => false;
            public KataBeamSheetData BeamDataToReturn { get; } = new();
            public KataColumnSheetData ColumnDataToReturn { get; } = new();
            public KataWallSheetData WallDataToReturn { get; } = new();

            public KataBeamSheetData? LastWrittenBeamData { get; private set; }
            public KataColumnSheetData? LastWrittenColumnData { get; private set; }
            public KataWallSheetData? LastWrittenWallData { get; private set; }

            public int WriteBeamCallCount { get; private set; }
            public int WriteColumnCallCount { get; private set; }
            public int WriteWallCallCount { get; private set; }

            public bool ReadBeamSheet(string? path, out KataBeamSheetData data)
            {
                data = BeamDataToReturn;
                return true;
            }

            public bool WriteBeamSheet(string? path, in KataBeamSheetData data)
            {
                LastWrittenBeamData = data;
                WriteBeamCallCount++;
                return true;
            }

            public bool ReadColumnSheet(string? path, out KataColumnSheetData data)
            {
                data = ColumnDataToReturn;
                return true;
            }

            public bool WriteColumnSheet(string? path, in KataColumnSheetData data)
            {
                LastWrittenColumnData = data;
                WriteColumnCallCount++;
                return true;
            }

            public bool ReadWallSheet(string? path, out KataWallSheetData data)
            {
                data = WallDataToReturn;
                return true;
            }

            public bool WriteWallSheet(string? path, in KataWallSheetData data)
            {
                LastWrittenWallData = data;
                WriteWallCallCount++;
                return true;
            }

            public bool ExportSchedule(string? path, IEnumerable<KataRebarScheduleItem> items) => true;
            public bool ExportSchedule(string? path, IEnumerable<RebarItem> items) => true;
            public bool ExportQhPlusSchedule(string? path, IEnumerable<KataRebarScheduleItem> items) => true;
            public bool ExportQhPlusSchedule(string? path, IEnumerable<RebarItem> items) => true;
        }
    }
}
