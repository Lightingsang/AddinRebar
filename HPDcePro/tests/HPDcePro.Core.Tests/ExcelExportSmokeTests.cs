using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using HPDcePro.Core.Rebar;
using HPDcePro.Excel;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public class ExcelExportSmokeTests
    {
        [Fact]
        public void ClosedXmlScheduleExporter_InMemoryStream_VerifiesHeaderAndRowCounts()
        {
            var items = new List<ScheduleItem>
            {
                new() { ElementMark = "D1", BarMark = "1", DiameterMm = 20, LengthMm = 5800.0, QuantityPerElement = 2, ElementCount = 2 },
                new() { ElementMark = "D1", BarMark = "2", DiameterMm = 8, LengthMm = 1140.0, QuantityPerElement = 20, ElementCount = 2 },
                new() { ElementMark = "D2", BarMark = "1", DiameterMm = 25, LengthMm = 6500.0, QuantityPerElement = 3, ElementCount = 1 }
            };

            using var ms = new MemoryStream();
            ClosedXmlScheduleExporter.ExportToStream(items, ms, "BẢNG THỐNG KÊ TEST");

            ms.Position = 0;
            using var wb = new XLWorkbook(ms);

            var ws = wb.Worksheet("ThongKeThep");
            Assert.NotNull(ws);
            Assert.Equal("BẢNG THỐNG KÊ TEST", ws.Cell(1, 1).GetString());

            string[] expectedHeaders =
            {
                "STT", "Cấu kiện", "Số hiệu", "ĐK (mm)", "Dài L (mm)",
                "SL/CK", "Số CK", "Tổng SL", "Tổng dài (m)", "KLR (kg/m)", "Tổng KL (kg)"
            };

            for (int col = 0; col < expectedHeaders.Length; col++)
            {
                Assert.Equal(expectedHeaders[col], ws.Cell(3, col + 1).GetString());
            }

            // 3 data rows
            Assert.Equal("D1", ws.Cell(4, 2).GetString());
            Assert.Equal("D1", ws.Cell(5, 2).GetString());
            Assert.Equal("D2", ws.Cell(6, 2).GetString());
        }

        [Fact]
        public void ClosedXmlScheduleExporter_InMemoryStream_VerifiesSummaryAndWeightIntegrity()
        {
            var items = new List<ScheduleItem>
            {
                new() { ElementMark = "D1", BarMark = "1", DiameterMm = 20, LengthMm = 6000.0, QuantityPerElement = 2, ElementCount = 1 },
                new() { ElementMark = "D1", BarMark = "2", DiameterMm = 10, LengthMm = 2000.0, QuantityPerElement = 5, ElementCount = 1 }
            };

            var report = RebarScheduleAggregator.Aggregate(items);

            using var ms = new MemoryStream();
            ClosedXmlScheduleExporter.ExportToStream(items, ms);

            ms.Position = 0;
            using var wb = new XLWorkbook(ms);
            var ws = wb.Worksheet("ThongKeThep");

            // Row 4 and 5 are data rows, Row 6 is TỔNG CỘNG
            Assert.Equal("TỔNG CỘNG", ws.Cell(6, 1).GetString());
            double totalWeightCell = ws.Cell(6, 11).GetDouble();
            Assert.Equal(Math.Round(report.TotalWeightKg, 2), totalWeightCell, 2);

            // Row 8 is "TỔNG HỢP THEO ĐƯỜNG KÍNH"
            Assert.Equal("TỔNG HỢP THEO ĐƯỜNG KÍNH", ws.Cell(8, 1).GetString());
        }

        [Fact]
        public void ClosedXmlReportExporter_BeamReport_InMemoryStream_VerifiesSectionsAndCompliantStatus()
        {
            var data = new BeamDesignReportData
            {
                BeamMark = "D10",
                StoryName = "TẦNG 3",
                WidthMm = 250.0,
                HeightMm = 500.0,
                SpanMm = 6000.0,
                ConcreteGrade = "B30",
                SteelGrade = "CB400-V",
                DesignMomentKnm = 120.0,
                DesignShearKn = 90.0,
                IsFlexureCompliant = true,
                IsShearCompliant = true
            };

            using var ms = new MemoryStream();
            ClosedXmlReportExporter.ExportToStream(data, ms);

            ms.Position = 0;
            using var wb = new XLWorkbook(ms);
            var ws = wb.Worksheet("ThuyetMinhDam");
            Assert.NotNull(ws);

            Assert.Contains("THUYẾT MINH TÍNH TOÁN", ws.Cell(1, 1).GetString());
            Assert.Contains("1. THÔNG SỐ HÌNH HỌC VÀ VẬT LIỆU", ws.Cell(4, 1).GetString());
            Assert.Contains("2. NỘI LỰC TÍNH TOÁN (ULS)", ws.Cell(10, 1).GetString());
            Assert.Contains("3. KIỂM TRA KHẢ NĂNG CHỊU UỐN", ws.Cell(14, 1).GetString());
            Assert.Contains("4. KIỂM TRA KHẢ NĂNG CHỊU CẮT", ws.Cell(21, 1).GetString());
            Assert.Contains("5. KẾT LUẬN CHUNG", ws.Cell(25, 1).GetString());

            Assert.Contains("THỎA MÃN", ws.Cell(26, 2).GetString());
        }

        [Fact]
        public void ClosedXmlReportExporter_FailingBeamReport_InMemoryStream_VerifiesNonCompliantStatus()
        {
            var data = new BeamDesignReportData
            {
                BeamMark = "D_FAIL",
                IsFlexureCompliant = false,
                IsShearCompliant = true
            };

            using var ms = new MemoryStream();
            ClosedXmlReportExporter.ExportToStream(data, ms);

            ms.Position = 0;
            using var wb = new XLWorkbook(ms);
            var ws = wb.Worksheet("ThuyetMinhDam");

            Assert.Contains("KHÔNG THỎA MÃN", ws.Cell(26, 2).GetString());
        }

        [Fact]
        public void ClosedXmlScheduleExporter_EmptySchedule_GeneratesValidWorkbookWithoutCrash()
        {
            using var ms = new MemoryStream();
            ClosedXmlScheduleExporter.ExportToStream(new List<ScheduleItem>(), ms);

            ms.Position = 0;
            using var wb = new XLWorkbook(ms);
            var ws = wb.Worksheet("ThongKeThep");

            Assert.NotNull(ws);
            Assert.Equal("TỔNG CỘNG", ws.Cell(4, 1).GetString());
            Assert.Equal(0.0, ws.Cell(4, 11).GetDouble());
        }

        [Fact]
        public void ClosedXmlExporters_NullInputs_ThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ClosedXmlScheduleExporter.ExportToFile(new List<ScheduleItem>(), null!));
            Assert.Throws<ArgumentNullException>(() => ClosedXmlScheduleExporter.ExportToFile(new List<ScheduleItem>(), string.Empty));
            Assert.Throws<ArgumentNullException>(() => ClosedXmlScheduleExporter.ExportToStream(new List<ScheduleItem>(), null!));
            Assert.Throws<ArgumentNullException>(() => ClosedXmlReportExporter.ExportToStream(new BeamDesignReportData(), null!));
            Assert.Throws<ArgumentNullException>(() => ClosedXmlReportExporter.CreateWorkbook(null!));
        }
    }
}
