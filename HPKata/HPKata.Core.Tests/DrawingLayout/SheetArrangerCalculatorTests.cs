using System;
using System.Collections.Generic;
using HPKata.Core.DrawingLayout.Calculators;
using HPKata.Core.DrawingLayout.Models;
using Xunit;

namespace HPKata.Core.Tests.DrawingLayout
{
    public class SheetArrangerCalculatorTests
    {
        [Fact]
        public void ArrangeBlocks_SingleSheetCapacity_PlacesAllBlocksOnSheet0()
        {
            var sheetDef = SheetSizeDefinition.Create(SheetFormat.A1, true); // 841 x 594
            var blocks = new List<DrawingBlockItem>
            {
                new DrawingBlockItem("Block 1", 200, 150),
                new DrawingBlockItem("Block 2", 200, 150),
                new DrawingBlockItem("Block 3", 200, 150)
            };

            var results = SheetArrangerCalculator.ArrangeBlocks(blocks, sheetDef);

            Assert.Single(results);
            Assert.Equal(3, results[0].PlacedBlocks.Count);
            Assert.True(results[0].UtilizationRatio > 0.0);
        }

        [Fact]
        public void ArrangeBlocks_OverflowBlocks_PaginatesToMultipleSheets()
        {
            var sheetDef = SheetSizeDefinition.Create(SheetFormat.A3, true); // 420 x 297
            var blocks = new List<DrawingBlockItem>();
            for (int i = 0; i < 10; i++)
            {
                blocks.Add(new DrawingBlockItem($"Large Block {i}", 200, 180));
            }

            var results = SheetArrangerCalculator.ArrangeBlocks(blocks, sheetDef);

            Assert.True(results.Count > 1);
            int totalPlaced = 0;
            foreach (var s in results)
            {
                totalPlaced += s.PlacedBlocks.Count;
            }
            Assert.Equal(10, totalPlaced);
        }

        [Fact]
        public void ArrangeBlocks_TitleBlockCollisionAvoidance_DoesNotOverlapBottomRight()
        {
            var sheetDef = SheetSizeDefinition.Create(SheetFormat.A1, true); // 841 x 594
            var tb = new TitleBlockModel { WidthMm = 160, HeightMm = 60 };

            var blocks = new List<DrawingBlockItem>
            {
                new DrawingBlockItem("Detail 1", 300, 100),
                new DrawingBlockItem("Detail 2", 300, 100),
                new DrawingBlockItem("Detail 3", 300, 100)
            };

            var results = SheetArrangerCalculator.ArrangeBlocks(blocks, sheetDef, tb);

            double tbMinX = sheetDef.WidthMm - sheetDef.MarginRightMm - tb.WidthMm;
            double tbMaxY = sheetDef.MarginBottomMm + tb.HeightMm;

            foreach (var b in results[0].PlacedBlocks)
            {
                bool inTbX = b.X < (sheetDef.WidthMm - sheetDef.MarginRightMm) && (b.X + b.WidthMm) > tbMinX;
                bool inTbY = b.Y < tbMaxY && (b.Y + b.HeightMm) > sheetDef.MarginBottomMm;
                Assert.False(inTbX && inTbY, $"Block {b.Title} intersects title block!");
            }
        }

        [Fact]
        public void ArrangeBlocks_EmptyList_ReturnsEmptyResult()
        {
            var sheetDef = SheetSizeDefinition.Create(SheetFormat.A1, true);
            var results = SheetArrangerCalculator.ArrangeBlocks(new List<DrawingBlockItem>(), sheetDef);
            Assert.Empty(results);
        }

        [Fact]
        public void ArrangeBlocks_NullArguments_ThrowsArgumentNull()
        {
            var sheetDef = SheetSizeDefinition.Create(SheetFormat.A1, true);
            Assert.Throws<ArgumentNullException>(() => SheetArrangerCalculator.ArrangeBlocks(null!, sheetDef));
            Assert.Throws<ArgumentNullException>(() => SheetArrangerCalculator.ArrangeBlocks(new List<DrawingBlockItem>(), null!));
        }
    }
}
