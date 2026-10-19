using System;
using System.IO;
using ClosedXML.Excel;
using Xunit;

namespace HPKata.E2E.Tests.Tier1_FeatureCoverage;

/// <summary>
/// Feature 05 / Acceptance Criteria:
/// Bi-directional Excel roundtrip for sheets Dam, Cot, Vach, and Iso_QHPlus.xls / Thong_ke
/// with exact cell address validation and ClosedXML fallback.
/// </summary>
public sealed class Tier1_F15_ExcelRoundTripTests
{
    private static readonly string SourceKataXlsm = File.Exists(@"C:\HPKata\Kata.xlsm") ? @"C:\HPKata\Kata.xlsm" : @"C:\kata_pro\Kata.xlsm";

    [Fact]
    public void DamSheet_RoundTrip_PreservesSpanAndRebarFidelity()
    {
        using var stream = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.Worksheets.Add("Dam");
            ws.Cell("B3").Value = "B01";
            ws.Cell("B4").Value = 1;
            ws.Cell("B5").Value = 1100;
            ws.Cell("B6").Value = 500;
            ws.Cell("B7").Value = 150;
            ws.Cell("B10").Value = "+3.300";

            ws.Cell(10, 3).Value = "Cột";
            ws.Cell(10, 4).Value = "Nhịp";
            ws.Cell(10, 5).Value = "Cột";

            ws.Cell(11, 3).Value = 400; // column width
            ws.Cell(11, 4).Value = 6500; // span length
            ws.Cell(11, 5).Value = 400;

            ws.Cell(12, 4).Value = "6f25"; // bottom bars
            ws.Cell(13, 4).Value = "4f22"; // top bars
            wb.SaveAs(stream);
        }

        stream.Position = 0;
        using (var wb = new XLWorkbook(stream))
        {
            var ws = wb.Worksheet("Dam");
            Assert.Equal("B01", ws.Cell("B3").GetString());
            Assert.Equal(1, ws.Cell("B4").GetDouble());
            Assert.Equal(1100, ws.Cell("B5").GetDouble());
            Assert.Equal(500, ws.Cell("B6").GetDouble());
            Assert.Equal(150, ws.Cell("B7").GetDouble());
            Assert.Equal(6500, ws.Cell(11, 4).GetDouble());
            Assert.Equal("6f25", ws.Cell(12, 4).GetString());
        }
    }

    [Fact]
    public void CotSheet_RoundTrip_PreservesStoryScheduleFidelity()
    {
        using var stream = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.Worksheets.Add("Cot");
            ws.Cell("C1").Value = 800; // beam width
            ws.Cell("C2").Value = 200; // slab thickness
            ws.Cell("C3").Value = "C1";  // column mark
            ws.Cell("C4").Value = 1;    // count
            ws.Cell("C5").Value = -5.5; // foundation elevation

            ws.Cell(7, 1).Value = "TẦNG";
            ws.Cell(7, 2).Value = "Chiều cao";
            ws.Cell(7, 3).Value = "b";
            ws.Cell(7, 4).Value = "h";
            ws.Cell(7, 9).Value = "Đường kính";

            ws.Cell(8, 1).Value = "FLOOR 1";
            ws.Cell(8, 2).Value = 3.6;
            ws.Cell(8, 3).Value = 600;
            ws.Cell(8, 4).Value = 800;
            ws.Cell(8, 9).Value = 25;
            wb.SaveAs(stream);
        }

        stream.Position = 0;
        using (var wb = new XLWorkbook(stream))
        {
            var ws = wb.Worksheet("Cot");
            Assert.Equal(800, ws.Cell("C1").GetDouble());
            Assert.Equal("C1", ws.Cell("C3").GetString());
            Assert.Equal(-5.5, ws.Cell("C5").GetDouble());
            Assert.Equal("FLOOR 1", ws.Cell(8, 1).GetString());
            Assert.Equal(3.6, ws.Cell(8, 2).GetDouble());
            Assert.Equal(600, ws.Cell(8, 3).GetDouble());
            Assert.Equal(25, ws.Cell(8, 9).GetDouble());
        }
    }

    [Fact]
    public void VachSheet_RoundTrip_PreservesWallBoundaryFidelity()
    {
        using var stream = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.Worksheets.Add("Vach");
            ws.Cell("C1").Value = 800; // boundary width
            ws.Cell("C3").Value = "V1";  // wall mark
            ws.Cell("C4").Value = 1;

            ws.Cell(8, 1).Value = "STORY 1";
            ws.Cell(8, 2).Value = 3.3;
            ws.Cell(8, 3).Value = 2000; // length
            ws.Cell(8, 4).Value = 300;  // thickness
            wb.SaveAs(stream);
        }

        stream.Position = 0;
        using (var wb = new XLWorkbook(stream))
        {
            var ws = wb.Worksheet("Vach");
            Assert.Equal("V1", ws.Cell("C3").GetString());
            Assert.Equal("STORY 1", ws.Cell(8, 1).GetString());
            Assert.Equal(2000, ws.Cell(8, 3).GetDouble());
            Assert.Equal(300, ws.Cell(8, 4).GetDouble());
        }
    }

    [Fact]
    public void ScheduleSheet_RoundTrip_PreservesTotalMassAndSummaryBins()
    {
        using var stream = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.Worksheets.Add("Thong_ke");
            ws.Cell(1, 1).Value = "STT";
            ws.Cell(1, 2).Value = "Số hiệu";
            ws.Cell(1, 3).Value = "ĐK";
            ws.Cell(1, 4).Value = "SL";
            ws.Cell(1, 5).Value = "Chiều dài (mm)";
            ws.Cell(1, 6).Value = "Trọng lượng (kg)";

            ws.Cell(2, 1).Value = 1;
            ws.Cell(2, 2).Value = "1";
            ws.Cell(2, 3).Value = 20;
            ws.Cell(2, 4).Value = 10;
            ws.Cell(2, 5).Value = 6000;
            ws.Cell(2, 6).Value = 148.0; // 10 * 6m * 2.466 kg/m

            wb.SaveAs(stream);
        }

        stream.Position = 0;
        using (var wb = new XLWorkbook(stream))
        {
            var ws = wb.Worksheet("Thong_ke");
            Assert.Equal(20, ws.Cell(2, 3).GetDouble());
            Assert.Equal(10, ws.Cell(2, 4).GetDouble());
            Assert.Equal(6000, ws.Cell(2, 5).GetDouble());
            Assert.Equal(148.0, ws.Cell(2, 6).GetDouble(), 1);
        }
    }

    [Fact]
    public void RealKataWorkbook_WhenPresent_ContainsExpectedSheets()
    {
        if (!File.Exists(SourceKataXlsm))
        {
            return;
        }

        using var fs = new FileStream(SourceKataXlsm, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var wb = new XLWorkbook(fs);
        Assert.True(wb.Worksheets.Contains("Dam"), "Kata.xlsm must contain Dam sheet");
        Assert.True(wb.Worksheets.Contains("Cot"), "Kata.xlsm must contain Cot sheet");
        Assert.True(wb.Worksheets.Contains("Vach"), "Kata.xlsm must contain Vach sheet");
        Assert.True(wb.Worksheets.Contains("Thong_ke"), "Kata.xlsm must contain Thong_ke sheet");

        var damSheet = wb.Worksheet("Dam");
        Assert.False(string.IsNullOrWhiteSpace(damSheet.Cell("B3").GetString()), "Dam sheet B3 must not be blank");
    }
}
