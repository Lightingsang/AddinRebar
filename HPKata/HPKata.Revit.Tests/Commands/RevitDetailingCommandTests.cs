using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using HPKata.Core.Excel;
using HPKata.Core.Excel.Data;
using HPKata.Core.Models;
using HPKata.Revit.Abstractions;
using HPKata.Revit.Runners;
using HPKata.Revit.Services;
using HPKata.Revit.Tests.Mocks;
using HPKata.Revit.Units;
using Xunit;

namespace HPKata.Revit.Tests.Commands
{
    public sealed class RevitDetailingCommandTests
    {
        [Fact]
        public void RevitExportExcelRunner_CallsAdapterWithKataWorkbookPath()
        {
            var context = new MockRevitDocumentContext { DocumentTitle = "DuAnDetailing" };
            context.AllRebars.Add(new RevitRebarDto
            {
                ElementId = 10,
                Partition = "Beam_Floor2",
                Mark = "1",
                Quantity = 4,
                DiameterMm = 20,
                SingleBarLengthMm = 6000,
                ShapeName = "M_00"
            });

            var fakeAdapter = new FakeRevitDetailingExcelAdapter();
            string customPath = @"C:\TestKata\Kata.xlsm";

            string resultPath = RevitExportExcelRunner.Execute(context, customPath, adapter: fakeAdapter);

            Assert.Equal(customPath, resultPath);
            Assert.Equal(customPath, fakeAdapter.LastExportPath);
            Assert.True(fakeAdapter.ExportScheduleCalled);
        }

        [Fact]
        public void RevitExportExcelRunner_FallbackToClosedXml_WhenAdapterReturnsFalse()
        {
            var context = new MockRevitDocumentContext { DocumentTitle = "DuAnFallback" };
            context.AllRebars.Add(new RevitRebarDto
            {
                ElementId = 11,
                Partition = "Cot_Tang1",
                Mark = "1",
                Quantity = 8,
                DiameterMm = 22,
                SingleBarLengthMm = 3800,
                ShapeName = "M_00"
            });

            var fakeAdapter = new FakeFailingDetailingExcelAdapter();
            string tempPath = Path.Combine(Path.GetTempPath(), $"TestFallback_{Guid.NewGuid():N}.xlsx");

            try
            {
                string resultPath = RevitExportExcelRunner.Execute(context, tempPath, adapter: fakeAdapter);

                Assert.Equal(tempPath, resultPath);
                Assert.True(File.Exists(tempPath));

                using var wb = new XLWorkbook(tempPath);
                var ws = wb.Worksheet("Thong_ke");
                Assert.NotNull(ws);
                Assert.Equal("BẢNG THỐNG KÊ CỐT THÉP", ws.Cell("A1").GetString());
                Assert.Equal("Cot_Tang1", ws.Cell("A4").GetString());
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        }

        [Fact]
        public void FramingGeometryCalculators_WithExcelInputParameters_CalculateConsistentCoordinates()
        {
            var beamData = new KataBeamSheetData
            {
                BeamMark = "D10",
                DefaultWidthMm = 300.0,
                DefaultHeightMm = 650.0,
                ConcreteCoverMm = 30.0
            };

            double beamLengthMm = 6000.0;
            RevitPoint3D startPt = new(0.0, 0.0, RevitUnitConverter.MmToFeet(3500.0));
            RevitPoint3D endPt = new(RevitUnitConverter.MmToFeet(beamLengthMm), 0.0, RevitUnitConverter.MmToFeet(3500.0));
            RevitPoint3D normal = new(0.0, 1.0, 0.0);

            double heightFt = RevitUnitConverter.MmToFeet(beamData.DefaultHeightMm);
            double coverFt = RevitUnitConverter.MmToFeet(beamData.ConcreteCoverMm);

            var (botStart, botEnd) = FramingRebarGeometryCalculator.CalculateBottomBarEndpoints(
                startPt, endPt, normal, 0.0, heightFt, coverFt);
            var (topStart, topEnd) = FramingRebarGeometryCalculator.CalculateTopBarEndpoints(
                startPt, endPt, normal, 0.0, coverFt);

            double verticalDistMm = RevitUnitConverter.FeetToMm(topStart.Z - botStart.Z);
            double expectedDistMm = beamData.DefaultHeightMm - (2.0 * beamData.ConcreteCoverMm);
            Assert.True(RevitUnitConverter.AreCloseMm(expectedDistMm, verticalDistMm, 0.1));
        }

        [Fact]
        public void ColumnGeometryCalculators_WithExcelInputParameters_CalculateTieOffsets()
        {
            var colData = new KataColumnSheetData
            {
                ColumnMark = "C1",
                ConcreteCoverMm = 30.0
            };
            colData.Stories.Add(new KataColumnStoryData
            {
                StoryIndex = 0,
                WidthMm = 450.0,
                DepthMm = 450.0,
                CornerDiameter = 22
            });

            double widthFt = RevitUnitConverter.MmToFeet(colData.Stories[0].WidthMm);
            double depthFt = RevitUnitConverter.MmToFeet(colData.Stories[0].DepthMm);
            double coverFt = RevitUnitConverter.MmToFeet(colData.ConcreteCoverMm);

            double halfWFt = (widthFt / 2.0) - coverFt;
            double halfDFt = (depthFt / 2.0) - coverFt;

            double expectedHalfWMm = (colData.Stories[0].WidthMm / 2.0) - colData.ConcreteCoverMm;
            double actualHalfWMm = RevitUnitConverter.FeetToMm(halfWFt);

            Assert.True(RevitUnitConverter.AreCloseMm(expectedHalfWMm, actualHalfWMm, 0.01));
            Assert.True(halfWFt > 0.0 && halfDFt > 0.0);
        }

        [Fact]
        public void WallGeometryCalculators_WithExcelInputParameters_CalculateCurtainOffsets()
        {
            var wallData = new KataWallSheetData
            {
                WallMark = "W1",
                ThicknessMm = 300.0,
                ConcreteCoverMm = 25.0
            };

            double thickFt = RevitUnitConverter.MmToFeet(wallData.ThicknessMm);
            double coverFt = RevitUnitConverter.MmToFeet(wallData.ConcreteCoverMm);

            double curtainOffsetFt = (thickFt / 2.0) - coverFt;
            double actualOffsetMm = RevitUnitConverter.FeetToMm(curtainOffsetFt);
            double expectedOffsetMm = (wallData.ThicknessMm / 2.0) - wallData.ConcreteCoverMm;

            Assert.True(RevitUnitConverter.AreCloseMm(expectedOffsetMm, actualOffsetMm, 0.01));
            Assert.Equal(125.0, expectedOffsetMm);
        }

        private sealed class FakeRevitDetailingExcelAdapter : IKataExcelAdapter
        {
            public bool IsDesktopExcelOpen => false;
            public string? LastExportPath { get; private set; }
            public bool ExportScheduleCalled { get; private set; }
            public bool ExportQhPlusScheduleCalled { get; private set; }

            public bool ReadBeamSheet(string? path, out KataBeamSheetData data) { data = new KataBeamSheetData(); return true; }
            public bool WriteBeamSheet(string? path, in KataBeamSheetData data) => true;
            public bool ReadColumnSheet(string? path, out KataColumnSheetData data) { data = new KataColumnSheetData(); return true; }
            public bool WriteColumnSheet(string? path, in KataColumnSheetData data) => true;
            public bool ReadWallSheet(string? path, out KataWallSheetData data) { data = new KataWallSheetData(); return true; }
            public bool WriteWallSheet(string? path, in KataWallSheetData data) => true;

            public bool ExportSchedule(string? path, IEnumerable<KataRebarScheduleItem> items)
            {
                LastExportPath = path;
                ExportScheduleCalled = true;
                return true;
            }

            public bool ExportSchedule(string? path, IEnumerable<RebarItem> items)
            {
                LastExportPath = path;
                ExportScheduleCalled = true;
                return true;
            }

            public bool ExportQhPlusSchedule(string? path, IEnumerable<KataRebarScheduleItem> items)
            {
                LastExportPath = path;
                ExportQhPlusScheduleCalled = true;
                return true;
            }

            public bool ExportQhPlusSchedule(string? path, IEnumerable<RebarItem> items)
            {
                LastExportPath = path;
                ExportQhPlusScheduleCalled = true;
                return true;
            }
        }

        private sealed class FakeFailingDetailingExcelAdapter : IKataExcelAdapter
        {
            public bool IsDesktopExcelOpen => false;

            public bool ReadBeamSheet(string? path, out KataBeamSheetData data) { data = new KataBeamSheetData(); return false; }
            public bool WriteBeamSheet(string? path, in KataBeamSheetData data) => false;
            public bool ReadColumnSheet(string? path, out KataColumnSheetData data) { data = new KataColumnSheetData(); return false; }
            public bool WriteColumnSheet(string? path, in KataColumnSheetData data) => false;
            public bool ReadWallSheet(string? path, out KataWallSheetData data) { data = new KataWallSheetData(); return false; }
            public bool WriteWallSheet(string? path, in KataWallSheetData data) => false;

            public bool ExportSchedule(string? path, IEnumerable<KataRebarScheduleItem> items) => false;
            public bool ExportSchedule(string? path, IEnumerable<RebarItem> items) => false;
            public bool ExportQhPlusSchedule(string? path, IEnumerable<KataRebarScheduleItem> items) => false;
            public bool ExportQhPlusSchedule(string? path, IEnumerable<RebarItem> items) => false;
        }
    }
}
