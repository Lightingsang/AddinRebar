using System;
using System.Collections.Generic;
using System.Linq;
using HPKata.Core.DrawingLayout.Calculators;
using HPKata.Core.DrawingLayout.Models;
using Xunit;

namespace HPKata.Core.Tests.DrawingLayout
{
    public class DrawingLayoutStressChallengeTests
    {
        [Fact]
        public void ISO128_ExhaustiveAlphabet_OmitsForbiddenCharacters()
        {
            var forbidden = new[] { 'I', 'O', 'Q', 'Z' };
            for (int i = 0; i < 500; i++)
            {
                string label = GridAxisGenerator.GetIsoLabel(i);
                Assert.False(string.IsNullOrEmpty(label), $"Label at index {i} was empty.");
                foreach (char c in forbidden)
                {
                    Assert.DoesNotContain(c, label);
                }
            }
        }

        [Fact]
        public void ISO128_MultiCycleSequence_FollowsStandardPattern()
        {
            Assert.Equal(string.Empty, GridAxisGenerator.GetIsoLabel(-1));
            Assert.Equal("A", GridAxisGenerator.GetIsoLabel(0));
            Assert.Equal("H", GridAxisGenerator.GetIsoLabel(7));
            Assert.Equal("J", GridAxisGenerator.GetIsoLabel(8)); // I skipped
            Assert.Equal("Y", GridAxisGenerator.GetIsoLabel(21)); // Z skipped
            Assert.Equal("A2", GridAxisGenerator.GetIsoLabel(22)); // Cycle 2
            Assert.Equal("Y2", GridAxisGenerator.GetIsoLabel(43));
            Assert.Equal("A3", GridAxisGenerator.GetIsoLabel(44)); // Cycle 3
        }

        [Fact]
        public void RadialGrids_FullCircleTrig_MatchesMathematicalOracle()
        {
            double[] angles = { 0, Math.PI / 6, Math.PI / 4, Math.PI / 2, Math.PI, 3 * Math.PI / 2, 7 * Math.PI / 4 };
            double originX = -1000.0;
            double originY = 2500.0;
            double rStart = 800.0;
            double rEnd = 12000.0;

            for (int i = 0; i < angles.Length; i++)
            {
                double theta = angles[i];
                var ray = GridAxisGenerator.GenerateRadialRay(originX, originY, rStart, rEnd, theta, $"R{i + 1}");

                double expectedX1 = originX + rStart * Math.Cos(theta);
                double expectedY1 = originY + rStart * Math.Sin(theta);
                double expectedX2 = originX + rEnd * Math.Cos(theta);
                double expectedY2 = originY + rEnd * Math.Sin(theta);

                Assert.Equal(expectedX1, ray.StartX, 3);
                Assert.Equal(expectedY1, ray.StartY, 3);
                Assert.Equal(expectedX2, ray.EndX, 3);
                Assert.Equal(expectedY2, ray.EndY, 3);
            }
        }

        [Fact]
        public void OrthogonalGrids_LargeSpansAndScales_DimensionChainsAndBubbles()
        {
            var xSpans = new List<double> { 3500, 7200, 4800, 6000, 9000 };
            var ySpans = Enumerable.Repeat(4000.0, 25).ToList(); // 25 spans -> 26 axes (cycles into A2..)

            var system = GridAxisGenerator.GenerateOrthogonal(xSpans, ySpans, scale: 200.0);

            Assert.Equal(200.0, system.Scale);
            Assert.Equal(3000.0, system.ExtensionLengthMm); // 15 * 200
            Assert.Equal(2000.0, system.DimensionOffsetMm); // 10 * 200
            Assert.Equal(900.0, system.BubbleRadiusMm);     // 4.5 * 200
            Assert.Equal(700.0, system.TextHeightMm);       // 3.5 * 200

            Assert.Equal(6, system.YAxes.Count);
            Assert.Equal(26, system.XAxes.Count);
            Assert.Equal("A", system.XAxes[0].Label);
            Assert.Equal("A2", system.XAxes[22].Label);
            Assert.Equal("D2", system.XAxes[25].Label);
        }

        [Fact]
        public void TitleBlockCollisionAvoidance_50RandomizedBlocks_NeverOverlapsBottomRight()
        {
            var sheetDef = SheetSizeDefinition.Create(SheetFormat.A1, true); // 841 x 594
            var tb = new TitleBlockModel { WidthMm = 180, HeightMm = 80 };
            double tbMinX = sheetDef.WidthMm - sheetDef.MarginRightMm - tb.WidthMm;
            double tbMaxX = sheetDef.WidthMm - sheetDef.MarginRightMm;
            double tbMinY = sheetDef.MarginBottomMm;
            double tbMaxY = sheetDef.MarginBottomMm + tb.HeightMm;

            var blocks = new List<DrawingBlockItem>();
            var rnd = new Random(42);
            for (int i = 0; i < 50; i++)
            {
                double w = rnd.Next(80, 250);
                double h = rnd.Next(60, 180);
                blocks.Add(new DrawingBlockItem($"Block_{i}", w, h));
            }

            var results = SheetArrangerCalculator.ArrangeBlocks(blocks, sheetDef, tb);

            Assert.NotEmpty(results);
            foreach (var sheet in results)
            {
                foreach (var b in sheet.PlacedBlocks)
                {
                    bool xOverlap = b.X < tbMaxX && (b.X + b.WidthMm) > tbMinX;
                    bool yOverlap = b.Y < tbMaxY && (b.Y + b.HeightMm) > tbMinY;
                    Assert.False(xOverlap && yOverlap, $"Block {b.Title} at ({b.X}, {b.Y}) collided with TitleBlock!");
                }
            }
        }

        [Fact]
        public void OversizedBlocks_LargerThanPrintableArea_HandledWithoutInfiniteLoop()
        {
            var sheetDef = SheetSizeDefinition.Create(SheetFormat.A2, true); // 594 x 420, Margins: Top 10, Bottom 10
            double maxUsableY = sheetDef.HeightMm - sheetDef.MarginTopMm; // 410 mm

            var tooTallBlock = new DrawingBlockItem("TooTall", 100, 800);
            var normalBlock = new DrawingBlockItem("Normal", 200, 150);
            var results = SheetArrangerCalculator.ArrangeBlocks(new[] { tooTallBlock, normalBlock }, sheetDef);

            Assert.NotEmpty(results);
            var placedBlockNames = results.SelectMany(s => s.PlacedBlocks).Select(b => b.Title).ToList();

            Assert.Contains("Normal", placedBlockNames);
            Assert.DoesNotContain("TooTall", placedBlockNames);

            foreach (var sheet in results)
            {
                Assert.NotEmpty(sheet.PlacedBlocks);
                foreach (var b in sheet.PlacedBlocks)
                {
                    Assert.True(b.Y + b.HeightMm <= maxUsableY,
                        $"Block {b.Title} (Y={b.Y}, Height={b.HeightMm}) exceeds maximum sheet boundary (MaxY={maxUsableY}).");
                    Assert.True(b.X + b.WidthMm <= sheetDef.WidthMm - sheetDef.MarginRightMm);
                }
            }
        }

        [Fact]
        public void Defect_SheetArrangerCalculator_UnplaceableBlockSpawnsEmptySheet()
        {
            var sheetDef = SheetSizeDefinition.Create(SheetFormat.A2, true); // 594 x 420
            var tooWideBlock = new DrawingBlockItem("TooWideBlock_1000mm", 1000, 100);
            var results = SheetArrangerCalculator.ArrangeBlocks(new[] { tooWideBlock }, sheetDef);

            // An unplaceable block must not spawn empty sheets.
            Assert.True(results.All(s => s.PlacedBlocks.Count > 0),
                "SheetArrangerCalculator returned an empty sheet (PlacedBlocks.Count == 0) when block could not fit.");
            Assert.Empty(results);
        }

        [Fact]
        public void MultiSheetPagination_HighDensityBlocks_PaginatesCleanly()
        {
            var sheetDef = SheetSizeDefinition.Create(SheetFormat.A3, true); // 420 x 297
            var blocks = new List<DrawingBlockItem>();
            for (int i = 0; i < 80; i++)
            {
                blocks.Add(new DrawingBlockItem($"Detail_{i + 1:D2}", 140, 100));
            }

            var results = SheetArrangerCalculator.ArrangeBlocks(blocks, sheetDef);

            Assert.True(results.Count > 1, "Should paginate into multiple sheets.");
            for (int sIdx = 0; sIdx < results.Count; sIdx++)
            {
                var sheet = results[sIdx];
                Assert.Equal(sIdx, sheet.SheetIndex);
                Assert.True(sheet.UtilizationRatio > 0.0 && sheet.UtilizationRatio <= 100.0);
                foreach (var b in sheet.PlacedBlocks)
                {
                    Assert.Equal(sIdx, b.SheetIndex);
                }
            }
        }

        [Fact]
        public void NaturalSorting_MixedTokensAndNumbers_StrictNaturalOrder()
        {
            var items = new List<DrawingIndexItem>
            {
                new DrawingIndexItem(1, "KC-10", "Dầm sàn tầng 3"),
                new DrawingIndexItem(2, "KC-01", "Ghi chú chung"),
                new DrawingIndexItem(3, "KC-02", "Mặt bằng móng"),
                new DrawingIndexItem(4, "KC-09", "Dầm sàn tầng 2"),
                new DrawingIndexItem(5, "KC-11", "Dầm sàn mái"),
                new DrawingIndexItem(6, "KC-100", "Bảng thống kê thép tổng thể"),
                new DrawingIndexItem(7, "KC-01A", "Ghi chú bổ sung"),
                new DrawingIndexItem(8, "KC-01B", "Ghi chú quy chuẩn"),
                new DrawingIndexItem(9, "KC-2", "Mặt bằng cột"),
            };

            var table = DrawingIndexAggregator.BuildTable(items);

            var orderedNumbers = table.Rows.Select(r => r[1]).ToList();

            int idx02 = orderedNumbers.IndexOf("KC-02");
            int idx09 = orderedNumbers.IndexOf("KC-09");
            int idx10 = orderedNumbers.IndexOf("KC-10");
            int idx11 = orderedNumbers.IndexOf("KC-11");
            int idx100 = orderedNumbers.IndexOf("KC-100");

            Assert.True(idx02 < idx09, "KC-02 must precede KC-09");
            Assert.True(idx09 < idx10, "KC-09 must precede KC-10");
            Assert.True(idx10 < idx11, "KC-10 must precede KC-11");
            Assert.True(idx11 < idx100, "KC-11 must precede KC-100");
        }

        [Fact]
        public void DuplicateDetection_CaseInsensitiveDuplicates_ReportsWarnings()
        {
            var items = new List<DrawingIndexItem>
            {
                new DrawingIndexItem(1, "KC-05", "Chi tiết vách thang máy"),
                new DrawingIndexItem(2, "kc-05", "Bản vẽ trùng viết thường"),
                new DrawingIndexItem(3, "KC-06", "Chi tiết thang bộ")
            };

            var table = DrawingIndexAggregator.BuildTable(items);

            Assert.Contains(table.Warnings, w => w.Contains("Trùng số hiệu bản vẽ") && w.Contains("kc-05", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void SequenceGapDetection_DiscontinuousAndContinuous_ReportsAccurately()
        {
            var itemsWithGap = new List<DrawingIndexItem>
            {
                new DrawingIndexItem(1, "KC-01", "Sheet 1"),
                new DrawingIndexItem(2, "KC-04", "Sheet 4"), // Gap: 2, 3
                new DrawingIndexItem(3, "KC-08", "Sheet 8")  // Gap: 5, 6, 7
            };

            var tableGap = DrawingIndexAggregator.BuildTable(itemsWithGap);
            Assert.Contains(tableGap.Warnings, w => w.Contains("Gián đoạn số hiệu bản vẽ giữa 1 và 4"));
            Assert.Contains(tableGap.Warnings, w => w.Contains("Gián đoạn số hiệu bản vẽ giữa 4 và 8"));

            var continuousItems = new List<DrawingIndexItem>
            {
                new DrawingIndexItem(1, "KC-01", "Sheet 1"),
                new DrawingIndexItem(2, "KC-02", "Sheet 2"),
                new DrawingIndexItem(3, "KC-03", "Sheet 3"),
                new DrawingIndexItem(4, "KC-04", "Sheet 4")
            };

            var tableContinuous = DrawingIndexAggregator.BuildTable(continuousItems);
            Assert.DoesNotContain(tableContinuous.Warnings, w => w.Contains("Gián đoạn"));
        }
    }
}
