using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using HPKata.Core.CadUtilities.Calculators;
using HPKata.Core.CadUtilities.Models;
using HPKata.Core.DrawingLayout.Calculators;
using HPKata.Core.DrawingLayout.Models;
using HPKata.Core.QuantitySurveying.Calculators;
using HPKata.Core.QuantitySurveying.Models;
using HPKata.Core.QuantitySurveying.Persistence;
using HPKata.E2E.Tests.Common;
using HPKata.Revit.Runners;
using Xunit;

namespace HPKata.E2E.Tests.Tier4_RealWorldScenarios
{
    public sealed class Phase5E2ETests
    {
        [Fact]
        public void Scenario1_CompleteQsTakeoffAndExcelExportPipeline()
        {
            var elements = CreateSampleStructuralAssembly();
            var summary = QsScheduleAggregator.Aggregate(elements);

            Assert.Equal(5, summary.Items.Count);
            Assert.True(summary.TotalVolumeM3 > 0.0);
            Assert.True(summary.TotalFormworkM2 > 0.0);
            Assert.True(summary.ColumnVolumeM3 > 0.0);
            Assert.True(summary.BeamVolumeM3 > 0.0);
            Assert.True(summary.SlabVolumeM3 > 0.0);

            // 3. Export to Excel and verify sheets and formulas
            string tempXlsx = Path.Combine(Path.GetTempPath(), $"E2E_QS_{Guid.NewGuid():N}.xlsx");
            try
            {
                QsWorkbookGenerator.ExportToFile(summary, tempXlsx);
                Assert.True(File.Exists(tempXlsx));

                using var wb = new XLWorkbook(tempXlsx);
                var wsQs = wb.Worksheet("Kata_QS");
                Assert.NotNull(wsQs);
                Assert.Equal("M1", wsQs.Cell("B3").GetString());

                var wsSum = wb.Worksheet("Thong_ke_QS");
                Assert.NotNull(wsSum);
                Assert.Contains("BẢNG TỔNG HỢP", wsSum.Cell("A1").GetString());
            }
            finally
            {
                if (File.Exists(tempXlsx)) File.Delete(tempXlsx);
            }
        }

        [Fact]
        public void Scenario2_CompleteDrawingLayoutAndShelfArrangerPipeline()
        {
            var sheetDef = SheetSizeDefinition.Create(SheetFormat.A1, true);
            var titleBlock = new TitleBlockModel { WidthMm = 160, HeightMm = 60 };

            var blocks = new List<DrawingBlockItem>();
            for (int i = 0; i < 8; i++)
            {
                blocks.Add(new DrawingBlockItem($"Detail {i + 1}", 300, 160));
            }

            var arrangedSheets = SheetArrangerCalculator.ArrangeBlocks(blocks, sheetDef, titleBlock);

            Assert.True(arrangedSheets.Count >= 2);
            foreach (var s in arrangedSheets)
            {
                Assert.True(s.UtilizationRatio > 0.0);
                Assert.True(s.UtilizationRatio <= 100.0);
            }
        }

        [Fact]
        public void Scenario3_DrawingIndexRegisterPipeline()
        {
            var items = new List<DrawingIndexItem>
            {
                new DrawingIndexItem(1, "KC-05", "Chi tiết cầu thang"),
                new DrawingIndexItem(2, "KC-01", "Ghi chú chung"),
                new DrawingIndexItem(3, "KC-02", "Mặt bằng kết cấu móng"),
                new DrawingIndexItem(4, "KC-03", "Chi tiết dầm tầng 2")
            };

            var table = DrawingIndexAggregator.BuildTable(items);

            Assert.Equal(4, table.TotalSheets);
            Assert.Equal("KC-01", table.Rows[0][1]);
            Assert.Equal("KC-02", table.Rows[1][1]);
            Assert.Equal("KC-03", table.Rows[2][1]);
            Assert.Equal("KC-05", table.Rows[3][1]);
            Assert.Contains(table.Warnings, w => w.Contains("Gián đoạn"));
        }

        [Fact]
        public void Scenario4_CadUtilitiesGeometricCalculators()
        {
            var p1 = new Point2D(0, 0);
            var p2 = new Point2D(2000, 40); // 40 / 2000 = 2%

            var slope = SlopeCalculator.Calculate(p1, p2);
            Assert.Equal(2.0, slope.GradientPercent);
            Assert.Equal("i = 2%", slope.FormattedText);

            string elevPositive = ElevationFormatter.Format(3.6);
            string elevZero = ElevationFormatter.Format(0.0);
            string elevNegative = ElevationFormatter.Format(-1.5);
            Assert.Equal("+3.600", elevPositive);
            Assert.Equal("±0.000", elevZero);
            Assert.Equal("-1.500", elevNegative);

            var (newExt1, newExt2) = VectorProjectionHelper.TrimWitnessLines(p1, p2, new Point2D(1000, 200), 50.0);
            Assert.True(newExt1.Y > 0.0);
            Assert.True(newExt2.Y > 0.0);
        }

        [Fact]
        public void Scenario5_RevitHeadlessRunnerIntegration()
        {
            var ctx = new E2ERevitContext();
            string tmp = Path.Combine(Path.GetTempPath(), $"Revit_E2E_QS_{Guid.NewGuid():N}.xlsx");

            try
            {
                string result = RevitQsLayoutRunner.ExportQsTakeoff(ctx, tmp);
                Assert.Equal(tmp, result);
                Assert.True(File.Exists(tmp));

                var blocks = new List<DrawingBlockItem>
                {
                    new DrawingBlockItem("Beam 1", 300, 150),
                    new DrawingBlockItem("Beam 2", 300, 150)
                };

                var sheets = RevitQsLayoutRunner.ArrangeSheets(ctx, blocks, SheetFormat.A1);
                Assert.Single(sheets);
                Assert.Equal(2, sheets[0].PlacedBlocks.Count);
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }
        }

        private static QsElementModel[] CreateSampleStructuralAssembly()
        {
            var footing = new QsElementModel
            {
                Id = "F1", Mark = "M1", MemberType = QsMemberType.Footing,
                LengthMm = 2000, WidthMm = 2000, HeightMm = 800, MemberCount = 2
            };
            var col = new QsElementModel
            {
                Id = "C1", Mark = "C1", MemberType = QsMemberType.Column,
                WidthMm = 400, HeightMm = 400, LengthMm = 3600, MemberCount = 2,
                EmbedmentDepthMm = 300, IntersectingElementIds = new List<string> { "F1" }
            };
            var primBeam = new QsElementModel
            {
                Id = "B1", Mark = "D1", MemberType = QsMemberType.Beam, IsPrimary = true,
                WidthMm = 300, HeightMm = 600, LengthMm = 6000, MemberCount = 2,
                IntersectingElementIds = new List<string> { "C1" }
            };
            var secBeam = new QsElementModel
            {
                Id = "B2", Mark = "D2", MemberType = QsMemberType.Beam, IsPrimary = false,
                WidthMm = 200, HeightMm = 400, LengthMm = 4000, MemberCount = 2,
                IntersectingElementIds = new List<string> { "B1" }
            };
            var slab = new QsElementModel
            {
                Id = "S1", Mark = "S1", MemberType = QsMemberType.Slab,
                LengthMm = 6000, WidthMm = 4000, ThicknessMm = 120, MemberCount = 1,
                IntersectingElementIds = new List<string> { "C1", "B1", "B2" }
            };
            return new[] { footing, col, primBeam, secBeam, slab };
        }
    }
}
