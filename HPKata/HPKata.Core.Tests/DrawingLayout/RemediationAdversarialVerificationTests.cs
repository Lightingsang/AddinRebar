using System;
using System.Collections.Generic;
using System.Linq;
using HPKata.Core.CadUtilities.Calculators;
using HPKata.Core.CadUtilities.Models;
using HPKata.Core.DrawingLayout.Calculators;
using HPKata.Core.DrawingLayout.Models;
using HPKata.Core.QuantitySurveying.Calculators;
using HPKata.Core.QuantitySurveying.Models;
using Xunit;

namespace HPKata.Core.Tests.DrawingLayout
{
    /// <summary>
    /// Adversarial empirical stress tests verifying Milestone M4 Remediation:
    /// SheetArranger boundary clamping, TitleBlock collision avoidance,
    /// phantom sheet elimination, continuous dimension point logic, and QS dimension clamping.
    /// </summary>
    public sealed class RemediationAdversarialVerificationTests
    {
        [Fact]
        public void OversizedBlock_1500x1000_OnA2_NeverProtrudesPrintableBounds()
        {
            var sheetDef = SheetSizeDefinition.Create(SheetFormat.A2, true); // 594 x 420
            var tb = new TitleBlockModel { WidthMm = 180, HeightMm = 120 };

            var blocks = new List<DrawingBlockItem>
            {
                new("Giant_1500x1000", 1500.0, 1000.0),
                new("Wide_1500x200", 1500.0, 200.0),
                new("Tall_200x1000", 200.0, 1000.0),
                new("Normal_300x200", 300.0, 200.0)
            };

            var sheets = SheetArrangerCalculator.ArrangeBlocks(blocks, sheetDef, tb);

            Assert.Single(sheets);
            var placed = sheets[0].PlacedBlocks;
            Assert.Single(placed);
            Assert.Equal("Normal_300x200", placed[0].Title);

            double maxX = sheetDef.WidthMm - sheetDef.MarginRightMm;
            double maxY = sheetDef.HeightMm - sheetDef.MarginTopMm;

            Assert.True(placed[0].X >= sheetDef.MarginLeftMm);
            Assert.True(placed[0].Y >= sheetDef.MarginBottomMm);
            Assert.True(placed[0].X + placed[0].WidthMm <= maxX);
            Assert.True(placed[0].Y + placed[0].HeightMm <= maxY);
        }

        [Fact]
        public void PhantomSheets_OnlyUnplaceableBlocks_YieldsZeroSheets()
        {
            var sheetDef = SheetSizeDefinition.Create(SheetFormat.A2, true);
            var tb = new TitleBlockModel { WidthMm = 180, HeightMm = 120 };

            var invalidBlocks = new List<DrawingBlockItem>
            {
                new("Giant", 1500.0, 1000.0),
                new("NegativeW", -100.0, 200.0),
                new("ZeroH", 200.0, 0.0),
                new("OversizedW", 800.0, 300.0)
            };

            var sheets = SheetArrangerCalculator.ArrangeBlocks(invalidBlocks, sheetDef, tb);

            Assert.Empty(sheets);
            Assert.True(sheets.All(s => s.PlacedBlocks.Count > 0));
        }

        [Fact]
        public void PhantomSheets_UnplaceableDueToTitleBlock_YieldsZeroSheets()
        {
            var sheetDef = SheetSizeDefinition.Create(SheetFormat.A2, true); // 594x420, printable 559x400
            var tb = new TitleBlockModel { WidthMm = 180, HeightMm = 120 };

            // Fits printable bounds (500 <= 559, 350 <= 400), but collides with TB at bottom (W 500 > 379)
            // and cannot fit above TB (Y=130 + 350 = 480 > 410)
            var unplaceable = new List<DrawingBlockItem>
            {
                new("CollidesWithTitleBlockEverywhere", 500.0, 350.0)
            };

            var sheets = SheetArrangerCalculator.ArrangeBlocks(unplaceable, sheetDef, tb);

            Assert.Empty(sheets);
            Assert.DoesNotContain(sheets, s => s.PlacedBlocks.Count == 0);
        }

        [Fact]
        public void TitleBlockCollision_OverlappingBlockAdvancesToShelfAboveTitleBlock()
        {
            var sheetDef = SheetSizeDefinition.Create(SheetFormat.A2, true);
            var tb = new TitleBlockModel { WidthMm = 180, HeightMm = 120 };

            // Width 500 collides with TB on bottom shelf (25 + 500 = 525 > 404). Height 150 fits above TB (130 + 150 = 280 <= 410).
            var blocks = new List<DrawingBlockItem>
            {
                new("WideBlockAboveTB", 500.0, 150.0)
            };

            var sheets = SheetArrangerCalculator.ArrangeBlocks(blocks, sheetDef, tb);

            Assert.Single(sheets);
            var block = Assert.Single(sheets[0].PlacedBlocks);

            double tbTopY = sheetDef.MarginBottomMm + tb.HeightMm; // 10 + 120 = 130
            Assert.True(block.Y >= tbTopY, $"Block Y ({block.Y}) must advance to or above TitleBlock top ({tbTopY}).");
            Assert.True(block.Y + block.HeightMm <= sheetDef.HeightMm - sheetDef.MarginTopMm);

            double tbMinX = sheetDef.WidthMm - sheetDef.MarginRightMm - tb.WidthMm; // 404
            double tbMaxX = sheetDef.WidthMm - sheetDef.MarginRightMm;             // 584
            bool xOverlap = block.X < tbMaxX && (block.X + block.WidthMm) > tbMinX;
            bool yOverlap = block.Y < tbTopY && (block.Y + block.HeightMm) > sheetDef.MarginBottomMm;
            Assert.False(xOverlap && yOverlap, "Block must not intersect TitleBlock area.");
        }

        [Fact]
        public void TitleBlockCollision_MultipleBlocksFillAroundTitleBlockCleanly()
        {
            var sheetDef = SheetSizeDefinition.Create(SheetFormat.A2, true);
            var tb = new TitleBlockModel { WidthMm = 180, HeightMm = 120 };

            // Block1 fits in bottom shelf next to TB: 25 + 300 = 325 < 404
            // Block2 at 325 + 20 = 345, 345 + 200 = 545 > 404 (collides with TB!) -> must advance to Y >= 130
            var blocks = new List<DrawingBlockItem>
            {
                new("Block1_BottomShelf", 300.0, 80.0),
                new("Block2_PushedAboveTB", 200.0, 80.0)
            };

            var sheets = SheetArrangerCalculator.ArrangeBlocks(blocks, sheetDef, tb);

            Assert.Single(sheets);
            Assert.Equal(2, sheets[0].PlacedBlocks.Count);

            var b1 = sheets[0].PlacedBlocks.First(b => b.Title == "Block1_BottomShelf");
            var b2 = sheets[0].PlacedBlocks.First(b => b.Title == "Block2_PushedAboveTB");

            Assert.Equal(sheetDef.MarginBottomMm, b1.Y);
            Assert.True(b2.Y >= sheetDef.MarginBottomMm + tb.HeightMm);
        }

        [Fact]
        public void ContinuousDimensionPoints_CollinearReverseDegenerate_OracleBehavior()
        {
            var start = new Point2D(0, 0);
            var pCollinear = new Point2D(1000, 0);
            var pReverse = new Point2D(-500, 0);
            var pDegenerate = new Point2D(0, 0);

            Assert.True(pDegenerate.DistanceTo(start) < 1e-4, "Degenerate point must be detected under 1e-4 tolerance.");
            Assert.False(pCollinear.DistanceTo(start) < 1e-4, "Collinear separated point must not be degenerate.");
            Assert.False(pReverse.DistanceTo(start) < 1e-4, "Reverse separated point must not be degenerate.");

            var dimLinePt = new Point2D(500, 200);
            var (t1, t2) = VectorProjectionHelper.TrimWitnessLines(start, pCollinear, dimLinePt, 50.0);
            Assert.Equal(0.0, Math.Round(t1.X, 2));
            Assert.Equal(150.0, Math.Round(t1.Y, 2));
            Assert.Equal(1000.0, Math.Round(t2.X, 2));
            Assert.Equal(150.0, Math.Round(t2.Y, 2));

            var (r1, r2) = VectorProjectionHelper.TrimWitnessLines(start, pReverse, dimLinePt, 50.0);
            Assert.Equal(0.0, Math.Round(r1.X, 2));
            Assert.Equal(150.0, Math.Round(r1.Y, 2));
            Assert.Equal(-500.0, Math.Round(r2.X, 2));
            Assert.Equal(150.0, Math.Round(r2.Y, 2));
        }

        [Fact]
        public void QsTakeoff_NegativeDimensions_SanitizedClampingPreventsSpuriousValues()
        {
            var element = new QsElementModel
            {
                MemberType = QsMemberType.Beam,
                WidthMm = -300.0,
                HeightMm = -500.0,
                LengthMm = 4000.0
            };

            double volume = QsTakeoffCalculator.CalculateGrossVolume(element);
            var (soffit, side) = QsTakeoffCalculator.CalculateGrossFormwork(element);

            Assert.Equal(0.0, volume);
            Assert.Equal(0.0, soffit);
            Assert.Equal(0.0, side);
        }
    }
}
