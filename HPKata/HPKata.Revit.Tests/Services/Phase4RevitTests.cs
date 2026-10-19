using System;
using System.IO;
using ClosedXML.Excel;
using HPKata.Revit.Abstractions;
using HPKata.Revit.Runners;
using HPKata.Revit.Tests.Mocks;
using HPKata.Revit.Units;
using Xunit;

namespace HPKata.Revit.Tests.Services
{
    public sealed class Phase4RevitTests
    {
        [Fact]
        public void ExportFoundationSchedule_GeneratesValidWorkbookWithMongSheet()
        {
            var ctx = new MockRevitDocumentContext();
            ctx.AllRebars.Add(new RevitRebarDto
            {
                ElementId = 101,
                Partition = "Mong_M1",
                Mark = "1",
                Quantity = 12,
                DiameterMm = 16,
                SingleBarLengthMm = 1800
            });

            string tmp = Path.Combine(Path.GetTempPath(), $"test_mong_{Guid.NewGuid():N}.xlsx");
            try
            {
                string res = RevitFoundationSlabStairRunner.ExportFoundationSchedule(ctx, tmp);
                Assert.Equal(tmp, res);
                Assert.True(File.Exists(tmp));

                using var wb = new XLWorkbook(tmp);
                var ws = wb.Worksheet("Mong");
                Assert.NotNull(ws);
                Assert.Equal("STT", ws.Cell(2, 2).GetString());
                Assert.Equal("Mong_M1", ws.Cell(3, 3).GetString());
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }
        }

        [Fact]
        public void ExportSlabSchedule_GeneratesValidWorkbookWithSanSheet()
        {
            var ctx = new MockRevitDocumentContext();
            ctx.AllRebars.Add(new RevitRebarDto
            {
                ElementId = 201,
                Partition = "San_S1",
                Mark = "1",
                Quantity = 20,
                DiameterMm = 10,
                SingleBarLengthMm = 4500
            });

            string tmp = Path.Combine(Path.GetTempPath(), $"test_san_{Guid.NewGuid():N}.xlsx");
            try
            {
                string res = RevitFoundationSlabStairRunner.ExportSlabSchedule(ctx, tmp);
                Assert.Equal(tmp, res);
                Assert.True(File.Exists(tmp));

                using var wb = new XLWorkbook(tmp);
                var ws = wb.Worksheet("San");
                Assert.NotNull(ws);
                Assert.Equal("STT", ws.Cell(2, 2).GetString());
                Assert.Equal("San_S1", ws.Cell(3, 3).GetString());
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }
        }

        [Fact]
        public void ExportStairSchedule_GeneratesValidWorkbookWithThangSheet()
        {
            var ctx = new MockRevitDocumentContext();
            ctx.AllRebars.Add(new RevitRebarDto
            {
                ElementId = 301,
                Partition = "Thang_ST1",
                Mark = "1",
                Quantity = 11,
                DiameterMm = 12,
                SingleBarLengthMm = 3500
            });

            string tmp = Path.Combine(Path.GetTempPath(), $"test_thang_{Guid.NewGuid():N}.xlsx");
            try
            {
                string res = RevitFoundationSlabStairRunner.ExportStairSchedule(ctx, tmp);
                Assert.Equal(tmp, res);
                Assert.True(File.Exists(tmp));

                using var wb = new XLWorkbook(tmp);
                var ws = wb.Worksheet("Thang");
                Assert.NotNull(ws);
                Assert.Equal("STT", ws.Cell(2, 2).GetString());
                Assert.Equal("Thang_ST1", ws.Cell(3, 3).GetString());
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }
        }

        [Fact]
        public void FoundationGeometry_RevitUnitConversion_AccuratelyConvertsCoordinates()
        {
            double padWidthMm = 2200.0;
            double padHeightMm = 900.0;
            double coverSideMm = 50.0;

            double widthFt = RevitUnitConverter.MmToFeet(padWidthMm);
            double heightFt = RevitUnitConverter.MmToFeet(padHeightMm);
            double coverFt = RevitUnitConverter.MmToFeet(coverSideMm);

            double innerSpanFt = widthFt - 2.0 * coverFt;
            double backToMm = RevitUnitConverter.FeetToMm(innerSpanFt);

            Assert.True(RevitUnitConverter.AreCloseMm(2100.0, backToMm, 0.01));
            Assert.True(widthFt > heightFt);
        }

        [Fact]
        public void StairGeometry_RevitUnitConversion_SlopeConvertsAccurately()
        {
            double riserMm = 160.0;
            double treadMm = 280.0;

            double riserFt = RevitUnitConverter.MmToFeet(riserMm);
            double treadFt = RevitUnitConverter.MmToFeet(treadMm);

            double slopeAngleMm = Math.Atan2(riserMm, treadMm);
            double slopeAngleFt = Math.Atan2(riserFt, treadFt);

            Assert.Equal(slopeAngleMm, slopeAngleFt, 4);
        }
    }
}
