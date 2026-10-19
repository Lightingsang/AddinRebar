using System;
using System.Collections.Generic;
using System.IO;
using HPKata.AutoCad.Abstractions;
using HPKata.AutoCad.Commands;
using HPKata.AutoCad.Tests.Mocks;
using HPKata.Core.Optimization;
using HPKata.Core.Serialization;
using Xunit;

namespace HPKata.AutoCad.Tests
{
    public class ScheduleCommandTests
    {
        private static MockCadDocumentContext CreateSampleContext()
        {
            var context = new MockCadDocumentContext();

            context.Entities.Add(new CadEntityDto
            {
                Handle = "1001",
                Layer = "kata_thep chu",
                EntityType = "Polyline",
                TotalLengthMm = 3500.0,
                XDataRecord = new KataXDataRecord
                {
                    Dang = "01",
                    STT = 1,
                    Phi = 20,
                    SoLuong = "4",
                    SoLuongCauKien = 2,
                    TenCauKien = "D1-TANG2",
                    KhoangCach = "@150",
                    ChuThich = "Coupler 1 dau",
                    ChieuDaiCacDoanMm = new[] { 3500.0 }
                }
            });

            context.Entities.Add(new CadEntityDto
            {
                Handle = "1002",
                Layer = "kata_thep dai",
                EntityType = "Polyline",
                TotalLengthMm = 1800.0,
                XDataRecord = new KataXDataRecord
                {
                    Dang = "22",
                    STT = 2,
                    Phi = 10,
                    SoLuong = "20",
                    SoLuongCauKien = 2,
                    TenCauKien = "D1-TANG2",
                    KhoangCach = "@150",
                    ChieuDaiCacDoanMm = new[] { 400.0, 500.0, 400.0, 500.0 }
                }
            });

            return context;
        }

        [Fact]
        public void ExecuteSchedule_WithRebarEntities_BuildsCadTableAndInserts()
        {
            var context = CreateSampleContext();

            CadTableDto table = ScheduleCommandRunner.ExecuteSchedule(context);

            Assert.NotNull(table);
            Assert.Equal("BẢNG THỐNG KÊ CỐT THÉP", table.Title);
            Assert.Equal(11, table.ColumnHeaders.Count);
            Assert.Equal(2, table.Rows.Count);
            Assert.Single(context.InsertedTables);
        }

        [Fact]
        public void ExecuteSummary_WithMultipleDiameters_BuildsDiameterSummaryAndBins()
        {
            var context = CreateSampleContext();

            CadTableDto table = SummaryCommandRunner.ExecuteSummary(context);

            Assert.NotNull(table);
            Assert.Equal("TỔNG HỢP CỐT THÉP", table.Title);
            Assert.Equal(4, table.ColumnHeaders.Count);
            Assert.True(table.Rows.Count >= 2);
            Assert.Single(context.InsertedTables);
        }

        [Fact]
        public void ExecuteCouplerSummary_WhenCouplerNotesPresent_ExtractsCouplers()
        {
            var context = CreateSampleContext();

            CadTableDto table = SummaryCommandRunner.ExecuteCouplerSummary(context);

            Assert.NotNull(table);
            Assert.StartsWith("BẢNG THỐNG KÊ COUPLER", table.Title);
            Assert.Single(table.Rows);
            Assert.Single(context.InsertedTables);
        }

        [Fact]
        public void ExecuteCouplerSummary_WhenNoCouplers_ReturnsEmptyNotice()
        {
            var context = new MockCadDocumentContext();
            context.Entities.Add(new CadEntityDto
            {
                Handle = "1003",
                Layer = "kata_thep chu",
                EntityType = "Polyline",
                TotalLengthMm = 2000.0,
                XDataRecord = new KataXDataRecord
                {
                    Dang = "01",
                    STT = 1,
                    Phi = 16,
                    SoLuong = "2",
                    ChieuDaiCacDoanMm = new[] { 2000.0 }
                }
            });

            CadTableDto table = SummaryCommandRunner.ExecuteCouplerSummary(context);

            Assert.NotNull(table);
            Assert.Contains("0 mối nối", table.Title);
            Assert.Empty(context.InsertedTables);
        }

        [Fact]
        public void ExecuteCuttingStock_WithRebarDemands_Solves1DCSPAndBuildsTable()
        {
            var context = CreateSampleContext();

            List<CuttingStockResult> results = CuttingStockCommandRunner.ExecuteCuttingStock(context, headless: true);

            Assert.NotEmpty(results);
            Assert.All(results, r => Assert.True(r.Patterns.Count > 0));
            Assert.Single(context.InsertedTables);
        }

        [Fact]
        public void ExecuteExcelExport_Headless_GeneratesXlsxFile()
        {
            var context = CreateSampleContext();
            string tempXlsx = Path.Combine(Path.GetTempPath(), $"HPKata_Test_{Guid.NewGuid():N}.xlsx");

            try
            {
                string exportedPath = ExportCommandRunner.ExecuteExcelExport(context, overridePath: tempXlsx);

                Assert.Equal(tempXlsx, exportedPath);
                Assert.True(File.Exists(exportedPath));
                var fileInfo = new FileInfo(exportedPath);
                Assert.True(fileInfo.Length > 0);
            }
            finally
            {
                if (File.Exists(tempXlsx))
                {
                    File.Delete(tempXlsx);
                }
            }
        }

        [Fact]
        public void ExecuteTagPrint_GeneratesQrCodePayloads()
        {
            var context = CreateSampleContext();

            var payloads = ExportCommandRunner.ExecuteTagPrint(context);

            Assert.Equal(2, payloads.Count);
            Assert.Equal(context.DocumentName, payloads[0].Project);
            Assert.Equal("1", payloads[0].Mark);
            Assert.Equal(20, payloads[0].DiameterMm);
        }
    }
}
