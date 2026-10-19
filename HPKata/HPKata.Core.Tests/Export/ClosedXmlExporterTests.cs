using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using HPKata.Core.Export;
using HPKata.Core.Models;
using HPKata.Core.Scheduling;
using Xunit;

namespace HPKata.Core.Tests.Export
{
    public class ClosedXmlExporterTests
    {
        [Fact]
        public void ExportScheduleToStream_ProducesValidClosedXmlWorkbook()
        {
            var report = CreateSampleReport();

            using var ms = new MemoryStream();
            ClosedXmlExporter.ExportScheduleToStream(report, ms);

            Assert.True(ms.Length > 0);

            // Verify ZIP magic header for OpenXML (.xlsx): 50 4B 03 04 ("PK\x03\x04")
            byte[] bytes = ms.ToArray();
            Assert.Equal((byte)'P', bytes[0]);
            Assert.Equal((byte)'K', bytes[1]);
            Assert.Equal(0x03, bytes[2]);
            Assert.Equal(0x04, bytes[3]);

            // Re-open with ClosedXML to inspect cells
            ms.Position = 0;
            using var wb = new XLWorkbook(ms);
            var ws = wb.Worksheet("Thong_ke");
            Assert.NotNull(ws);

            Assert.Equal("BẢNG THỐNG KÊ CỐT THÉP", ws.Cell("A1").GetString());
            Assert.Equal("TÊN CẤU KIỆN", ws.Cell("A3").GetString());
            Assert.Equal("DAM_D1", ws.Cell("A4").GetString());
            Assert.Equal("1", ws.Cell("B4").GetString());
            Assert.Equal("TỔNG HỢP CỐT THÉP", ws.Cell("U1").GetString());
            Assert.Equal("BẢNG THỐNG KÊ COUPLER (MỐI NỐI REN)", ws.Cell("U10").GetString());
        }

        private static RebarScheduleReport CreateSampleReport()
        {
            var items = new List<RebarItem>
            {
                new()
                {
                    ComponentMark = "DAM_D1",
                    BarMark = "1",
                    ShapeCode = "01",
                    DiameterMm = 20,
                    CountPerMember = 4,
                    MemberCount = 1,
                    CuttingLengthMm = 6000.0,
                    UnitMassKgPerM = 2.470,
                    ParametersMm = new[] { 6000.0 }
                },
                new()
                {
                    ComponentMark = "DAM_D1",
                    BarMark = "2",
                    ShapeCode = "coupler",
                    DiameterMm = 20,
                    CountPerMember = 4,
                    MemberCount = 1,
                    Notes = "Coupler sleeve",
                    CuttingLengthMm = 80.0
                }
            };
            return RebarScheduleAggregator.GenerateReport(items);
        }

        [Fact]
        public void ExportScheduleToFile_WritesFileAndCanBeReopened()
        {
            var items = new List<RebarItem>
            {
                new()
                {
                    ComponentMark = "COT_C1",
                    BarMark = "1",
                    ShapeCode = "02",
                    DiameterMm = 25,
                    CountPerMember = 8,
                    MemberCount = 2,
                    CuttingLengthMm = 4500.0,
                    UnitMassKgPerM = 3.850,
                    ParametersMm = new[] { 500.0, 3500.0 }
                }
            };

            var report = RebarScheduleAggregator.GenerateReport(items);
            string tempPath = Path.Combine(Path.GetTempPath(), $"HPKata_Test_Schedule_{Guid.NewGuid():N}.xlsx");

            try
            {
                ClosedXmlExporter.ExportScheduleToFile(report, tempPath);
                Assert.True(File.Exists(tempPath));

                using var wb = new XLWorkbook(tempPath);
                var ws = wb.Worksheet("Thong_ke");
                Assert.NotNull(ws);
                Assert.Equal("COT_C1", ws.Cell("A4").GetString());
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        [Fact]
        public void ExportMethods_NullArguments_ThrowExpectedExceptions()
        {
            var report = new RebarScheduleReport();

            Assert.Throws<ArgumentNullException>(() => ClosedXmlExporter.ExportScheduleToStream(null!, new MemoryStream()));
            Assert.Throws<ArgumentNullException>(() => ClosedXmlExporter.ExportScheduleToStream(report, null!));
            Assert.Throws<ArgumentNullException>(() => ClosedXmlExporter.ExportScheduleToFile(null!, "path.xlsx"));
            Assert.Throws<ArgumentException>(() => ClosedXmlExporter.ExportScheduleToFile(report, ""));
            Assert.Throws<ArgumentException>(() => ClosedXmlExporter.ExportScheduleToFile(report, "   "));
        }
    }
}
