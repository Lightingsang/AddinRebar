using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using HPKata.Core.QuantitySurveying.Models;
using HPKata.Core.QuantitySurveying.Persistence;
using Xunit;

namespace HPKata.Core.Tests.QuantitySurveying
{
    public class QsWorkbookGeneratorTests
    {
        [Fact]
        public void ExportToStream_GeneratesKataQsAndSummarySheets()
        {
            var summary = new QsTakeoffSummary
            {
                TotalVolumeM3 = 100.5,
                TotalFormworkM2 = 450.2,
                ColumnVolumeM3 = 20.0,
                ColumnFormworkM2 = 120.0,
                BeamVolumeM3 = 30.5,
                BeamFormworkM2 = 150.2,
                SlabVolumeM3 = 50.0,
                SlabFormworkM2 = 180.0
            };

            summary.Items.Add(new QsTakeoffItem
            {
                DrawingName = "Tầng 1",
                ItemMark = "C1",
                MemberType = QsMemberType.Column,
                Count = 4,
                AreaM2 = 0.16,
                LengthM = 3.6,
                HeightM = 3.6,
                TotalAreaM2 = 0.64,
                TotalLengthM = 14.4,
                TotalVolumeM3 = 2.304,
                FormworkAreaM2 = 23.04
            });

            using var ms = new MemoryStream();
            QsWorkbookGenerator.ExportToStream(summary, ms);
            ms.Position = 0;

            using var wb = new XLWorkbook(ms);
            Assert.NotNull(wb.Worksheet("Kata_QS"));
            Assert.NotNull(wb.Worksheet("Thong_ke_QS"));

            var wsQs = wb.Worksheet("Kata_QS");
            Assert.Equal("C1", wsQs.Cell("B3").GetString());
            Assert.Equal(4, wsQs.Cell("C3").GetDouble());

            var wsSum = wb.Worksheet("Thong_ke_QS");
            Assert.Equal("BẢNG TỔNG HỢP KHỐI LƯỢNG BÊ TÔNG & CỐP PHA", wsSum.Cell("A1").GetString());
        }

        [Fact]
        public void ExportToFile_CreatesRealFileOnDisk()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"Test_QS_{Guid.NewGuid():N}.xlsx");
            var summary = new QsTakeoffSummary();

            try
            {
                QsWorkbookGenerator.ExportToFile(summary, tempFile);
                Assert.True(File.Exists(tempFile));
                var fileInfo = new FileInfo(tempFile);
                Assert.True(fileInfo.Length > 0);
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
        public void ExportToFile_NullSummary_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => QsWorkbookGenerator.ExportToFile(null!, "dummy.xlsx"));
        }
    }
}
