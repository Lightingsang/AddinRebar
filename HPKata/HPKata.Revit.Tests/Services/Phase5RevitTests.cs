using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using HPKata.Core.DrawingLayout.Models;
using HPKata.Core.QuantitySurveying.Models;
using HPKata.Revit.Runners;
using HPKata.Revit.Tests.Mocks;
using HPKata.Revit.Units;
using Xunit;

namespace HPKata.Revit.Tests.Services
{
    public sealed class Phase5RevitTests
    {
        [Fact]
        public void RevitUnitConverter_VolumeAndAreaConversions()
        {
            double cuFt = 35.3146667;
            double m3 = RevitUnitConverter.CubicFeetToCubicMeters(cuFt);
            Assert.Equal(1.0, Math.Round(m3, 3));

            double sqFt = 10.7639104;
            double m2 = RevitUnitConverter.SquareFeetToSquareMeters(sqFt);
            Assert.Equal(1.0, Math.Round(m2, 3));
        }

        [Fact]
        public void RevitQsLayoutRunner_ExportQsTakeoff_GeneratesValidWorkbook()
        {
            var ctx = new MockRevitDocumentContext();
            string tmp = Path.Combine(Path.GetTempPath(), $"test_qs_{Guid.NewGuid():N}.xlsx");

            try
            {
                var elements = new List<QsElementModel>
                {
                    new QsElementModel
                    {
                        Id = "C1",
                        Mark = "C1",
                        MemberType = QsMemberType.Column,
                        LengthMm = 3600,
                        WidthMm = 400,
                        HeightMm = 400
                    }
                };

                string res = RevitQsLayoutRunner.ExportQsTakeoff(ctx, tmp, elements);
                Assert.Equal(tmp, res);
                Assert.True(File.Exists(tmp));

                using var wb = new XLWorkbook(tmp);
                var ws = wb.Worksheet("Kata_QS");
                Assert.NotNull(ws);
                Assert.Equal("C1", ws.Cell("B3").GetString());
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }
        }

        [Fact]
        public void RevitQsLayoutRunner_ArrangeSheets_PacksBlocksOnSheets()
        {
            var ctx = new MockRevitDocumentContext();
            var blocks = new List<DrawingBlockItem>
            {
                new DrawingBlockItem("Chi tiết dầm 1", 300, 150),
                new DrawingBlockItem("Chi tiết dầm 2", 300, 150)
            };

            var sheets = RevitQsLayoutRunner.ArrangeSheets(ctx, blocks, SheetFormat.A1);
            Assert.Single(sheets);
            Assert.Equal(2, sheets[0].PlacedBlocks.Count);
        }

        [Fact]
        public void RevitQsLayoutRunner_NullContext_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => RevitQsLayoutRunner.ExportQsTakeoff(null!, "dummy.xlsx"));
            Assert.Throws<ArgumentNullException>(() => RevitQsLayoutRunner.ArrangeSheets(null!, Array.Empty<DrawingBlockItem>()));
        }

        [Fact]
        public void RevitQsLayoutRunner_EmptyElements_FallsBackToDefaultColumnElement()
        {
            var ctx = new MockRevitDocumentContext();
            string tmp = Path.Combine(Path.GetTempPath(), $"test_qs_fallback_{Guid.NewGuid():N}.xlsx");

            try
            {
                string res = RevitQsLayoutRunner.ExportQsTakeoff(ctx, tmp, null);
                Assert.Equal(tmp, res);
                Assert.True(File.Exists(tmp));
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }
        }

        [Fact]
        public void RevitQsLayoutRunner_MultipleSheets_PaginatesCorrectly()
        {
            var ctx = new MockRevitDocumentContext();
            var blocks = new List<DrawingBlockItem>();
            for (int i = 0; i < 8; i++)
            {
                blocks.Add(new DrawingBlockItem($"Detail {i}", 250, 200));
            }

            var sheets = RevitQsLayoutRunner.ArrangeSheets(ctx, blocks, SheetFormat.A3);
            Assert.True(sheets.Count > 1);
        }
    }
}
