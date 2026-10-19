using System.IO;
using ClosedXML.Excel;
using HPKata.Core.Export;
using HPKata.Core.Models;
using HPKata.Core.Scheduling;
using Xunit;

namespace HPKata.E2E.Tests.Tier1_FeatureCoverage;

/// <summary>
/// Feature 09: Headless ClosedXML schedule workbook generation, sheet "Thong_ke", and formulas.
/// </summary>
public sealed class Tier1_F09_ClosedXmlExportTests
{
    [Fact]
    public void ExportScheduleToStream_ProducesValidWorkbookWithThongKeSheet()
    {
        // Arrange
        var items = new List<RebarItem>
        {
            new()
            {
                ComponentMark = "D1",
                BarMark = "1",
                ShapeCode = "01",
                DiameterMm = 20,
                CountPerMember = 2,
                MemberCount = 1,
                ParametersMm = [5000.0],
                CuttingLengthMm = 5000.0
            }
        };
        var report = RebarScheduleAggregator.GenerateReport(items);
        using var stream = new MemoryStream();

        // Act
        ClosedXmlExporter.ExportScheduleToStream(report, stream);

        // Assert
        stream.Seek(0, SeekOrigin.Begin);
        using var wb = new XLWorkbook(stream);
        Assert.True(wb.Worksheets.Contains("Thong_ke"));
        var ws = wb.Worksheet("Thong_ke");
        Assert.Equal("BẢNG THỐNG KÊ CỐT THÉP", ws.Cell("A1").GetString());
    }

    [Fact]
    public void ExportScheduleToFile_GeneratesFileOnDiskWithCorrectRows()
    {
        // Arrange
        string tempPath = Path.Combine(Path.GetTempPath(), $"HPKata_Test_{System.Guid.NewGuid():N}.xlsx");
        try
        {
            var items = new List<RebarItem>
            {
                new() { ComponentMark = "B1", BarMark = "1", ShapeCode = "01", DiameterMm = 20, CountPerMember = 3, MemberCount = 2, ParametersMm = [6000.0], CuttingLengthMm = 6000.0 },
                new() { ComponentMark = "B1", BarMark = "2", ShapeCode = "22", DiameterMm = 8, CountPerMember = 30, MemberCount = 2, ParametersMm = [400.0, 200.0, 80.0], CuttingLengthMm = 1360.0 }
            };
            var report = RebarScheduleAggregator.GenerateReport(items);

            // Act
            ClosedXmlExporter.ExportScheduleToFile(report, tempPath);

            // Assert
            Assert.True(File.Exists(tempPath));
            using var wb = new XLWorkbook(tempPath);
            var ws = wb.Worksheet("Thong_ke");
            Assert.Equal("B1", ws.Cell(4, 1).GetString()); // First rebar row at row 4
            Assert.Equal("1", ws.Cell(4, 2).GetString());
            Assert.Equal("20", ws.Cell(4, 11).GetString());
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public void ExportSchedule_IncludesDiameterSummaryAndTcvnBins()
    {
        // Arrange
        var items = new List<RebarItem>
        {
            new() { ComponentMark = "D1", BarMark = "1", ShapeCode = "01", DiameterMm = 16, CountPerMember = 2, MemberCount = 1, ParametersMm = [4000.0], CuttingLengthMm = 4000.0 },
            new() { ComponentMark = "D1", BarMark = "2", ShapeCode = "01", DiameterMm = 25, CountPerMember = 2, MemberCount = 1, ParametersMm = [6000.0], CuttingLengthMm = 6000.0 }
        };
        var report = RebarScheduleAggregator.GenerateReport(items);
        using var stream = new MemoryStream();

        // Act
        ClosedXmlExporter.ExportScheduleToStream(report, stream);

        // Assert
        stream.Seek(0, SeekOrigin.Begin);
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheet("Thong_ke");

        // Verify summary exists in cells
        bool hasSummaryHeader = false;
        var range = ws.RangeUsed();
        if (range != null)
        {
            foreach (var cell in range.Cells())
            {
                if (cell.GetString().Contains("TỔNG HỢP CỐT THÉP"))
                {
                    hasSummaryHeader = true;
                    break;
                }
            }
        }
        Assert.True(hasSummaryHeader, "Workbook should contain TỔNG HỢP CỐT THÉP");
    }

    [Fact]
    public void ExportSchedule_WithCouplers_PopulatesCouplerSection()
    {
        // Arrange
        var items = new List<RebarItem>
        {
            new() { ComponentMark = "Col1", BarMark = "1", ShapeCode = "01", DiameterMm = 32, CountPerMember = 8, MemberCount = 1, ParametersMm = [4500.0], CuttingLengthMm = 4500.0, Notes = "Nối coupler D32" }
        };
        var report = RebarScheduleAggregator.GenerateReport(items);
        using var stream = new MemoryStream();

        // Act
        ClosedXmlExporter.ExportScheduleToStream(report, stream);

        // Assert
        stream.Seek(0, SeekOrigin.Begin);
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheet("Thong_ke");

        bool hasCouplerHeader = false;
        var couplerRange = ws.RangeUsed();
        if (couplerRange != null)
        {
            foreach (var cell in couplerRange.Cells())
            {
                if (cell.GetString().Contains("COUPLER"))
                {
                    hasCouplerHeader = true;
                    break;
                }
            }
        }
        Assert.True(hasCouplerHeader, "Workbook should contain COUPLER section");
    }

    [Fact]
    public void ExportSchedule_WithNullOrInvalidInputs_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<System.ArgumentNullException>(() => ClosedXmlExporter.ExportScheduleToFile(null!, "test.xlsx"));
        Assert.Throws<System.ArgumentException>(() => ClosedXmlExporter.ExportScheduleToFile(new RebarScheduleReport(), ""));
    }
}
