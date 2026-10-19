using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using HPDcePro.Core.Rebar;
using HPDcePro.Excel;
using Xunit;

namespace HPDcePro.Core.Tests.Excel
{
    public sealed class ExcelAdapterTests
    {
        [Fact]
        public void ClosedXmlScheduleExporter_ExportsValidWorkbook()
        {
            var items = new List<ScheduleItem>
            {
                new() { ElementMark = "D1", BarMark = "1", DiameterMm = 18, LengthMm = 5800, QuantityPerElement = 2, ElementCount = 2 },
                new() { ElementMark = "D1", BarMark = "2", DiameterMm = 18, LengthMm = 4200, QuantityPerElement = 3, ElementCount = 2 },
                new() { ElementMark = "D1", BarMark = "3", DiameterMm = 8, LengthMm = 1140, QuantityPerElement = 24, ElementCount = 2 }
            };

            using var wb = ClosedXmlScheduleExporter.CreateWorkbook(items, "BẢNG THỐNG KÊ CỐT THÉP DẦM");
            Assert.NotNull(wb);
            Assert.True(wb.Worksheets.Count > 0);

            var ws = wb.Worksheet("ThongKeThep");
            Assert.NotNull(ws);
            Assert.Equal("BẢNG THỐNG KÊ CỐT THÉP DẦM", ws.Cell(1, 1).GetString());

            using var ms = new MemoryStream();
            ClosedXmlScheduleExporter.ExportToStream(items, ms);
            Assert.True(ms.Length > 2000, "Workbook bytes should exceed 2KB.");
        }

        [Fact]
        public void ClosedXmlReportExporter_ExportsCalculationSheet()
        {
            var data = new BeamDesignReportData
            {
                BeamMark = "D2",
                StoryName = "TẦNG 2",
                WidthMm = 250,
                HeightMm = 500,
                SpanMm = 6000,
                ConcreteGrade = "B30",
                SteelGrade = "CB400-V",
                DesignMomentKnm = 120,
                DesignShearKn = 90,
                AsRequiredMm2 = 850,
                AsProvidedMm2 = 1018,
                MomentCapacityKnm = 145,
                ShearCapacityKn = 115,
                IsFlexureCompliant = true,
                IsShearCompliant = true
            };

            using var wb = ClosedXmlReportExporter.CreateWorkbook(data);
            Assert.NotNull(wb);
            var ws = wb.Worksheet("ThuyetMinhDam");
            Assert.NotNull(ws);
            Assert.Equal("THUYẾT MINH TÍNH TOÁN KẾT CẤU DẦM BTCT", ws.Cell(1, 1).GetString());

            using var ms = new MemoryStream();
            ClosedXmlReportExporter.ExportToStream(data, ms);
            Assert.True(ms.Length > 2000);
        }

        [Fact]
        public void CalculationUdfBridge_ComputesAccurateEngineeringValues()
        {
            double asReq = CalculationUdfBridge.TinhThepDam_M(220, 400, 80.0, "B25", "CB400-V", 40.0);
            Assert.InRange(asReq, 500.0, 900.0);

            double spacing = CalculationUdfBridge.TinhThepDam_V(220, 400, 60.0, "B25", "CB400-V", 8.0, 2);
            Assert.InRange(spacing, 50.0, 300.0);

            double pileCap = CalculationUdfBridge.SucChiuTaiCoc_SPT(0.4, 20.0, 15.0, 25.0, true);
            Assert.True(pileCap > 200.0, "Pile capacity should be substantial.");

            double weight = CalculationUdfBridge.TrongLuongThep(18, 11.7);
            Assert.InRange(weight, 23.0, 24.0);

            double area = CalculationUdfBridge.DienTichThep(3, 18);
            Assert.InRange(area, 760.0, 770.0);

            double mu = CalculationUdfBridge.HamLuongThep(area, 220, 400, 40.0);
            Assert.InRange(mu, 0.8, 1.2);

            int checkPass = CalculationUdfBridge.KiemTraUon(220, 400, 80.0, 800.0, "B25", "CB400-V", 40.0);
            Assert.Equal(1, checkPass);

            int checkFail = CalculationUdfBridge.KiemTraUon(220, 400, 80.0, 100.0, "B25", "CB400-V", 40.0);
            Assert.Equal(0, checkFail);
        }
    }
}
