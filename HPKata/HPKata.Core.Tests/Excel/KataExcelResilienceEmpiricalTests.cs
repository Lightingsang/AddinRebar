using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using HPKata.Core.Excel;
using HPKata.Core.Excel.Data;
using HPKata.Core.Models;
using Xunit;

namespace HPKata.Core.Tests.Excel
{
    public sealed class KataExcelResilienceEmpiricalTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly KataExcelAdapter _adapter;

        public KataExcelResilienceEmpiricalTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"HPKata_Resilience_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
            _adapter = new KataExcelAdapter();
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, recursive: true);
                }
            }
            catch (Exception)
            {
                // Suppress cleanup exceptions during test teardown
            }
        }

        [Theory]
        [InlineData("zero_byte.xlsx", 0)]
        [InlineData("corrupt_binary.xlsm", 1)]
        [InlineData("plain_text.xlsx", 2)]
        [InlineData("non_excel_zip.xlsm", 3)]
        public void ReadBeamSheet_InvalidFiles_ReturnsFalseGracefully(string fileName, int kind)
        {
            string path = CreateInvalidFile(fileName, kind);
            bool success = _adapter.ReadBeamSheet(path, out var data);

            Assert.False(success);
            Assert.NotNull(data);
        }

        [Fact]
        public void ReadBeamSheet_NonExistentFile_ReturnsFalseGracefully()
        {
            string path = Path.Combine(_tempDir, $"NonExistent_{Guid.NewGuid():N}.xlsm");
            bool success = _adapter.ReadBeamSheet(path, out var data);

            Assert.False(success);
            Assert.NotNull(data);
        }

        [Theory]
        [InlineData("zero_byte.xlsx", 0)]
        [InlineData("corrupt_binary.xlsm", 1)]
        [InlineData("plain_text.xlsx", 2)]
        public void ReadColumnSheet_InvalidFiles_ReturnsFalseGracefully(string fileName, int kind)
        {
            string path = CreateInvalidFile(fileName, kind);
            bool success = _adapter.ReadColumnSheet(path, out var data);

            Assert.False(success);
            Assert.NotNull(data);
        }

        [Fact]
        public void ReadColumnSheet_NonExistentFile_ReturnsFalseGracefully()
        {
            string path = Path.Combine(_tempDir, $"NonExistent_{Guid.NewGuid():N}.xlsm");
            bool success = _adapter.ReadColumnSheet(path, out var data);

            Assert.False(success);
            Assert.NotNull(data);
        }

        [Theory]
        [InlineData("zero_byte.xlsx", 0)]
        [InlineData("corrupt_binary.xlsm", 1)]
        [InlineData("plain_text.xlsx", 2)]
        public void ReadWallSheet_InvalidFiles_ReturnsFalseGracefully(string fileName, int kind)
        {
            string path = CreateInvalidFile(fileName, kind);
            bool success = _adapter.ReadWallSheet(path, out var data);

            Assert.False(success);
            Assert.NotNull(data);
        }

        [Fact]
        public void ReadWallSheet_NonExistentFile_ReturnsFalseGracefully()
        {
            string path = Path.Combine(_tempDir, $"NonExistent_{Guid.NewGuid():N}.xlsm");
            bool success = _adapter.ReadWallSheet(path, out var data);

            Assert.False(success);
            Assert.NotNull(data);
        }

        [Theory]
        [InlineData("zero_byte.xlsx", 0)]
        [InlineData("corrupt_binary.xlsm", 1)]
        [InlineData("plain_text.xlsx", 2)]
        public void WriteBeamSheet_CorruptedOrZeroByteFiles_ReturnsFalseGracefully(string fileName, int kind)
        {
            string path = CreateInvalidFile(fileName, kind);
            var data = new KataBeamSheetData { BeamMark = "B_TEST" };
            bool success = _adapter.WriteBeamSheet(path, in data);

            Assert.False(success);
        }

        [Fact]
        public void WriteBeamSheet_InvalidPath_ReturnsFalseGracefully()
        {
            string invalidPath = @"Z:\NonExistentDrive_999\Invalid\path.xlsm";
            var data = new KataBeamSheetData { BeamMark = "B_TEST" };
            bool success = _adapter.WriteBeamSheet(invalidPath, in data);

            Assert.False(success);
        }

        [Theory]
        [InlineData("zero_byte.xlsx", 0)]
        [InlineData("corrupt_binary.xlsm", 1)]
        public void WriteColumnSheet_CorruptedOrZeroByteFiles_ReturnsFalseGracefully(string fileName, int kind)
        {
            string path = CreateInvalidFile(fileName, kind);
            var data = new KataColumnSheetData { ColumnMark = "C_TEST" };
            bool success = _adapter.WriteColumnSheet(path, in data);

            Assert.False(success);
        }

        [Fact]
        public void WriteColumnSheet_InvalidPath_ReturnsFalseGracefully()
        {
            string invalidPath = @"Z:\NonExistentDrive_999\Invalid\path.xlsm";
            var data = new KataColumnSheetData { ColumnMark = "C_TEST" };
            bool success = _adapter.WriteColumnSheet(invalidPath, in data);

            Assert.False(success);
        }

        [Theory]
        [InlineData("zero_byte.xlsx", 0)]
        [InlineData("corrupt_binary.xlsm", 1)]
        public void WriteWallSheet_CorruptedOrZeroByteFiles_ReturnsFalseGracefully(string fileName, int kind)
        {
            string path = CreateInvalidFile(fileName, kind);
            var data = new KataWallSheetData { WallMark = "W_TEST" };
            bool success = _adapter.WriteWallSheet(path, in data);

            Assert.False(success);
        }

        [Fact]
        public void WriteWallSheet_InvalidPath_ReturnsFalseGracefully()
        {
            string invalidPath = @"Z:\NonExistentDrive_999\Invalid\path.xlsm";
            var data = new KataWallSheetData { WallMark = "W_TEST" };
            bool success = _adapter.WriteWallSheet(invalidPath, in data);

            Assert.False(success);
        }

        [Theory]
        [InlineData("zero_byte.xlsx", 0)]
        [InlineData("corrupt_binary.xlsm", 1)]
        public void ExportSchedule_CorruptedOrZeroByteFiles_ReturnsFalseGracefully(string fileName, int kind)
        {
            string path = CreateInvalidFile(fileName, kind);
            var items = new List<KataRebarScheduleItem>
            {
                new() { ComponentMark = "C1", BarMark = "1", DiameterMm = 16 }
            };
            bool success = _adapter.ExportSchedule(path, items);

            Assert.False(success);
        }

        [Fact]
        public void ExportSchedule_InvalidPath_ReturnsFalseGracefully()
        {
            string invalidPath = @"Z:\NonExistentDrive_999\Invalid\path.xlsm";
            var items = new List<KataRebarScheduleItem>
            {
                new() { ComponentMark = "C1", BarMark = "1", DiameterMm = 16 }
            };
            bool success = _adapter.ExportSchedule(invalidPath, items);

            Assert.False(success);
        }

        [Fact]
        public void ExportQhPlusSchedule_ReadOnlyFile_ReturnsFalseGracefully()
        {
            string path = Path.Combine(_tempDir, "readonly_target.xlsx");
            File.WriteAllText(path, "locked");
            File.SetAttributes(path, FileAttributes.ReadOnly);

            try
            {
                var items = new List<KataRebarScheduleItem>
                {
                    new() { ComponentMark = "C1", BarMark = "1", DiameterMm = 16 }
                };
                bool success = _adapter.ExportQhPlusSchedule(path, items);

                Assert.False(success);
            }
            finally
            {
                File.SetAttributes(path, FileAttributes.Normal);
            }
        }

        [Fact]
        public void ExportQhPlusSchedule_ZeroByteFile_OverwritesWithValidSpreadsheet()
        {
            string path = CreateInvalidFile("zero_byte_export.xlsx", 0);
            var items = new List<KataRebarScheduleItem>
            {
                new() { ComponentMark = "C1", BarMark = "1", DiameterMm = 16, CuttingLengthMm = 3000 }
            };
            bool success = _adapter.ExportQhPlusSchedule(path, items);

            Assert.True(success);
            Assert.True(new FileInfo(path).Length > 0);
        }

        [Fact]
        public void ExportQhPlusSchedule_InvalidPath_ReturnsFalseGracefully()
        {
            string invalidPath = @"Z:\NonExistentDrive_999\Invalid\path.xlsx";
            var items = new List<KataRebarScheduleItem>
            {
                new() { ComponentMark = "C1", BarMark = "1", DiameterMm = 16 }
            };
            bool success = _adapter.ExportQhPlusSchedule(invalidPath, items);

            Assert.False(success);
        }

        private string CreateInvalidFile(string fileName, int kind)
        {
            string path = Path.Combine(_tempDir, fileName);
            switch (kind)
            {
                case 0:
                    File.WriteAllBytes(path, Array.Empty<byte>());
                    break;
                case 1:
                    File.WriteAllBytes(path, new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66 });
                    break;
                case 2:
                    File.WriteAllText(path, "This is not an Excel OpenXML archive or BIFF8 spreadsheet.");
                    break;
                case 3:
                    CreateInvalidZipFile(path);
                    break;
            }
            return path;
        }

        private static void CreateInvalidZipFile(string path)
        {
            using var fileStream = new FileStream(path, FileMode.Create);
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create);
            var entry = archive.CreateEntry("readme.txt");
            using var writer = new StreamWriter(entry.Open());
            writer.WriteLine("This zip does not contain xl/workbook.xml");
        }
    }
}
