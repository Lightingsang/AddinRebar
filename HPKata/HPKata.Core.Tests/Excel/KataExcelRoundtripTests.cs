using System;
using System.Collections.Generic;
using System.IO;
using HPKata.Core.Configuration;
using HPKata.Core.Excel;
using HPKata.Core.Excel.Data;
using Xunit;

namespace HPKata.Core.Tests.Excel
{
    public sealed class KataExcelRoundtripTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly IDisposable _envScope;

        public KataExcelRoundtripTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"HPKata_ExcelTest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);

            string assetDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Distribution", "Assets");
            if (!Directory.Exists(assetDir))
            {
                assetDir = @"C:\HPKata";
            }

            string kataSrc = Path.Combine(assetDir, "Kata.xlsm");
            if (File.Exists(kataSrc))
            {
                File.Copy(kataSrc, Path.Combine(_tempDir, "Kata.xlsm"), true);
            }

            string qhSrc = Path.Combine(assetDir, "Iso_QHPlus.xls");
            if (File.Exists(qhSrc))
            {
                File.Copy(qhSrc, Path.Combine(_tempDir, "Iso_QHPlus.xls"), true);
            }

            _envScope = KataInstallEnvironment.UseRootDirectory(_tempDir);
        }

        public void Dispose()
        {
            _envScope.Dispose();
            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, recursive: true);
                }
            }
            catch
            {
                // Suppress file in-use on test teardown
            }
        }

        [Fact]
        public void ReadBeamSheet_MasterWorkbook_ReturnsValidData()
        {
            var adapter = new KataExcelAdapter();
            bool success = adapter.ReadBeamSheet(null, out var data);

            Assert.True(success);
            Assert.False(string.IsNullOrEmpty(data.BeamMark));
            Assert.True(data.DefaultWidthMm > 0.0);
            Assert.True(data.DefaultHeightMm > 0.0);
        }

        [Fact]
        public void Roundtrip_BeamSheet_PreservesModifiedValues()
        {
            var adapter = new KataExcelAdapter();
            Assert.True(adapter.ReadBeamSheet(null, out var data));

            data.BeamMark = "D_ROUNDTRIP";
            data.DefaultWidthMm = 350.0;
            data.DefaultHeightMm = 750.0;
            data.TopContinuousCount = 4;
            data.TopContinuousDiameter = 22;
            data.TopContinuousRebar = "4f22";

            Assert.True(adapter.WriteBeamSheet(null, in data));

            Assert.True(adapter.ReadBeamSheet(null, out var reloaded));
            Assert.Equal("D_ROUNDTRIP", reloaded.BeamMark);
            Assert.Equal(350.0, reloaded.DefaultWidthMm);
            Assert.Equal(750.0, reloaded.DefaultHeightMm);
            Assert.Equal("4f22", reloaded.TopContinuousRebar);
        }

        [Fact]
        public void ReadColumnSheet_MasterWorkbook_ReturnsValidData()
        {
            var adapter = new KataExcelAdapter();
            bool success = adapter.ReadColumnSheet(null, out var data);

            Assert.True(success);
            Assert.False(string.IsNullOrEmpty(data.ColumnMark));
            Assert.True(data.Stories.Count > 0);
            Assert.True(data.Stories[0].WidthMm > 0.0);
        }

        [Fact]
        public void Roundtrip_ColumnSheet_PreservesModifiedValues()
        {
            var adapter = new KataExcelAdapter();
            Assert.True(adapter.ReadColumnSheet(null, out var data));

            data.ColumnMark = "C_ROUNDTRIP";
            data.Stories[0].WidthMm = 550.0;
            data.Stories[0].DepthMm = 650.0;
            data.Stories[0].RebarDiameterSpec = "25";
            data.Stories[0].OuterTieSpec = "d10a120/240";

            Assert.True(adapter.WriteColumnSheet(null, in data));

            Assert.True(adapter.ReadColumnSheet(null, out var reloaded));
            Assert.Equal("C_ROUNDTRIP", reloaded.ColumnMark);
            Assert.Equal(550.0, reloaded.Stories[0].WidthMm);
            Assert.Equal(650.0, reloaded.Stories[0].DepthMm);
            Assert.Equal("25", reloaded.Stories[0].RebarDiameterSpec);
        }

        [Fact]
        public void ReadWallSheet_MasterWorkbook_ReturnsValidData()
        {
            var adapter = new KataExcelAdapter();
            bool success = adapter.ReadWallSheet(null, out var data);

            Assert.True(success);
            Assert.False(string.IsNullOrEmpty(data.WallMark));
            Assert.True(data.Stories.Count > 0);
            Assert.True(data.Stories[0].LengthLwMm > 0.0);
        }

        [Fact]
        public void Roundtrip_WallSheet_PreservesModifiedValues()
        {
            var adapter = new KataExcelAdapter();
            Assert.True(adapter.ReadWallSheet(null, out var data));

            data.WallMark = "SW_ROUNDTRIP";
            data.Stories[0].LengthLwMm = 4500.0;
            data.Stories[0].ThicknessTwMm = 400.0;
            data.Stories[0].BoundaryBarsCount = 12;

            Assert.True(adapter.WriteWallSheet(null, in data));

            Assert.True(adapter.ReadWallSheet(null, out var reloaded));
            Assert.Equal("SW_ROUNDTRIP", reloaded.WallMark);
            Assert.Equal(4500.0, reloaded.Stories[0].LengthLwMm);
            Assert.Equal(400.0, reloaded.Stories[0].ThicknessTwMm);
            Assert.Equal(12, reloaded.Stories[0].BoundaryBarsCount);
        }

        [Fact]
        public void ExportSchedule_WritesThongKeAndSummaries()
        {
            var adapter = new KataExcelAdapter();
            var items = new List<KataRebarScheduleItem>
            {
                new()
                {
                    ComponentMark = "D1",
                    BarMark = "1",
                    ShapeCode = "01",
                    DiameterMm = 10,
                    CuttingLengthMm = 2000.0,
                    MemberCount = 1,
                    CountPerMember = 4,
                    UnitMassKgPerM = 0.617
                },
                new()
                {
                    ComponentMark = "D1",
                    BarMark = "2",
                    ShapeCode = "02",
                    DiameterMm = 16,
                    CuttingLengthMm = 5000.0,
                    MemberCount = 1,
                    CountPerMember = 4,
                    UnitMassKgPerM = 1.578
                },
                new()
                {
                    ComponentMark = "D1",
                    BarMark = "3",
                    ShapeCode = "02",
                    DiameterMm = 25,
                    CuttingLengthMm = 6000.0,
                    MemberCount = 1,
                    CountPerMember = 4,
                    UnitMassKgPerM = 3.853
                }
            };

            bool success = adapter.ExportSchedule(null, items);
            Assert.True(success);
        }

        [Fact]
        public void ExportQhPlusSchedule_OutputsFile()
        {
            var adapter = new KataExcelAdapter();
            var items = new List<KataRebarScheduleItem>
            {
                new()
                {
                    ComponentMark = "C1",
                    BarMark = "1",
                    ShapeCode = "00",
                    DiameterMm = 20,
                    CuttingLengthMm = 4000.0,
                    MemberCount = 2,
                    CountPerMember = 4,
                    UnitMassKgPerM = 2.466
                }
            };

            string outPath = Path.Combine(_tempDir, "Iso_QHPlus.xlsx");
            bool success = adapter.ExportQhPlusSchedule(outPath, items);
            Assert.True(success);
            Assert.True(File.Exists(outPath));
        }

        [Fact]
        public void ReadBeamSheet_CorruptedFile_ReturnsFalseGracefullyWithoutThrowing()
        {
            string corruptPath = Path.Combine(_tempDir, "CorruptFile.xlsm");
            File.WriteAllBytes(corruptPath, new byte[] { 0x00, 0x01, 0x02, 0x03 });

            var adapter = new KataExcelAdapter();
            bool success = adapter.ReadBeamSheet(corruptPath, out var data);

            Assert.False(success);
            Assert.NotNull(data);
        }
    }
}
