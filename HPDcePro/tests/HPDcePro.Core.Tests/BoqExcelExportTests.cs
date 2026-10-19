using System;
using System.IO;
using ClosedXML.Excel;
using HPDcePro.Core.Calculations.Takeoff;
using HPDcePro.Excel;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public class BoqExcelExportTests
    {
        private static QuantityTakeoffResult CreateSampleBoq()
        {
            var elements = new[]
            {
                new ElementTakeoffItem { StoryName = "TẦNG 1", ElementType = TakeoffElementType.Beam, Mark = "D1", Count = 5, WidthMm = 220, HeightMm = 400, LengthMm = 5000, ConcreteVolumeM3 = 2.2, FormworkAreaM2 = 19.5 },
                new ElementTakeoffItem { StoryName = "TẦNG 1", ElementType = TakeoffElementType.Column, Mark = "C1", Count = 8, WidthMm = 300, HeightMm = 300, LengthMm = 3600, ConcreteVolumeM3 = 2.592, FormworkAreaM2 = 34.56 }
            };
            var rebars = new[]
            {
                new RebarTakeoffItem { StoryName = "TẦNG 1", Mark = "d10", DiameterMm = 10, TotalLengthM = 200, WeightKg = 123.3, Category = RebarDiameterCategory.SmallLe10 },
                new RebarTakeoffItem { StoryName = "TẦNG 1", Mark = "d18", DiameterMm = 18, TotalLengthM = 150, WeightKg = 299.7, Category = RebarDiameterCategory.MediumLe18 },
                new RebarTakeoffItem { StoryName = "TẦNG 1", Mark = "d20", DiameterMm = 20, TotalLengthM = 100, WeightKg = 246.6, Category = RebarDiameterCategory.LargeGt18 }
            };
            return QuantityTakeoffCalculator.CalculateFromElements(elements, rebars);
        }

        [Fact]
        public void Export_CreatesAll5RequiredSheets()
        {
            var boq = CreateSampleBoq();
            using var wb = ClosedXmlBoqExporter.CreateWorkbook(boq, "TestProject");

            Assert.Equal(5, wb.Worksheets.Count);
            Assert.NotNull(wb.Worksheet("TongHop"));
            Assert.NotNull(wb.Worksheet("BeTong"));
            Assert.NotNull(wb.Worksheet("VanKhuon"));
            Assert.NotNull(wb.Worksheet("CotThep"));
            Assert.NotNull(wb.Worksheet("ChiTietTheoTang"));
        }

        [Fact]
        public void Export_ContainsExcelFormulas()
        {
            var boq = CreateSampleBoq();
            using var wb = ClosedXmlBoqExporter.CreateWorkbook(boq);

            var wsTongHop = wb.Worksheet("TongHop");
            Assert.True(wsTongHop.Cell("D10").HasFormula);
            Assert.Contains("SUM", wsTongHop.Cell("D10").FormulaA1);

            var wsChiTiet = wb.Worksheet("ChiTietTheoTang");
            Assert.True(wsChiTiet.Cell(4, 8).HasFormula);
            Assert.Contains("SUM", wsChiTiet.Cell(4, 8).FormulaA1);
        }

        [Fact]
        public void Export_PreservesValuesAndFormatting()
        {
            var boq = CreateSampleBoq();
            using var wb = ClosedXmlBoqExporter.CreateWorkbook(boq);

            var wsBeTong = wb.Worksheet("BeTong");
            Assert.Equal("CHI TIẾT KHỐI LƯỢNG BÊ TÔNG THEO CẤU KIỆN", wsBeTong.Cell(1, 1).GetString());
            Assert.Equal(2.2, wsBeTong.Cell(4, 7).GetDouble());
            Assert.True(wsBeTong.Cell(3, 1).Style.Font.Bold);

            var wsCotThep = wb.Worksheet("CotThep");
            Assert.Equal(123.3, wsCotThep.Cell(4, 8).GetDouble());
        }

        [Fact]
        public void Export_WritesToStream_Successfully()
        {
            var boq = CreateSampleBoq();
            using var ms = new MemoryStream();

            ClosedXmlBoqExporter.ExportToStream(boq, ms, "StreamProject");

            Assert.True(ms.Length > 0);
            ms.Position = 0;
            using var wb = new XLWorkbook(ms);
            Assert.Equal(5, wb.Worksheets.Count);
        }

        [Fact]
        public void Export_WritesToFile_AndRoundTrips()
        {
            var boq = CreateSampleBoq();
            string tempFile = Path.Combine(Path.GetTempPath(), $"BOQ_{Guid.NewGuid():N}.xlsx");

            try
            {
                ClosedXmlBoqExporter.ExportToFile(boq, tempFile, "RoundtripProject");
                Assert.True(File.Exists(tempFile));

                using var wb = new XLWorkbook(tempFile);
                Assert.Equal(5, wb.Worksheets.Count);
                Assert.Equal("TỔNG HỢP KHỐI LƯỢNG DỰ TOÁN KẾT CẤU (BOQ)", wb.Worksheet("TongHop").Cell(1, 1).GetString());
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
        public void Export_HandlesEmptyTakeoff_WithoutException()
        {
            var emptyBoq = new QuantityTakeoffResult();
            using var wb = ClosedXmlBoqExporter.CreateWorkbook(emptyBoq);

            Assert.Equal(5, wb.Worksheets.Count);
            Assert.NotNull(wb.Worksheet("TongHop"));
        }

        [Fact]
        public void Export_ThrowsArgumentNull_OnNullArguments()
        {
            Assert.Throws<ArgumentNullException>(() => ClosedXmlBoqExporter.CreateWorkbook(null!));
            Assert.Throws<ArgumentNullException>(() => ClosedXmlBoqExporter.ExportToFile(null!, "path"));
            Assert.Throws<ArgumentNullException>(() => ClosedXmlBoqExporter.ExportToFile(new QuantityTakeoffResult(), null!));
            Assert.Throws<ArgumentNullException>(() => ClosedXmlBoqExporter.ExportToStream(null!, new MemoryStream()));
            Assert.Throws<ArgumentNullException>(() => ClosedXmlBoqExporter.ExportToStream(new QuantityTakeoffResult(), null!));
        }
    }
}
