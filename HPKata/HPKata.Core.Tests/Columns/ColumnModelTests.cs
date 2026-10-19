using System;
using HPKata.Core.Columns.Models;
using Xunit;

namespace HPKata.Core.Tests.Columns
{
    public class ColumnModelTests
    {
        [Fact]
        public void ColumnStorySegment_Validation_ThrowsOnInvalidParameters()
        {
            var segment = new ColumnStorySegment
            {
                StoryHeightMm = -100.0,
                WidthMm = 400.0,
                DepthMm = 400.0
            };
            Assert.Throws<ArgumentException>(() => segment.Validate());

            segment.StoryHeightMm = 3600.0;
            segment.WidthMm = 0.0;
            Assert.Throws<ArgumentException>(() => segment.Validate());

            segment.WidthMm = 400.0;
            segment.BeamDepthMm = 3800.0;
            Assert.Throws<ArgumentException>(() => segment.Validate());
        }

        [Fact]
        public void ColumnStorySegment_ClearHeight_CalculatesDifference()
        {
            var segment = new ColumnStorySegment
            {
                StoryHeightMm = 3600.0,
                BeamDepthMm = 600.0
            };
            Assert.Equal(3000.0, segment.ClearHeightMm);
        }

        [Fact]
        public void ColumnSectionModel_TotalBarCount_And_SteelArea_CalculatesCorrectly()
        {
            var sec = new ColumnSectionModel
            {
                WidthMm = 500.0,
                DepthMm = 500.0,
                CornerDiameterMm = 25,
                SideCountB = 2,
                SideDiameterB = 20,
                SideCountH = 2,
                SideDiameterH = 20
            };

            // Total bars = 4 corner + 2*2 on B + 2*2 on H = 12 bars
            Assert.Equal(12, sec.TotalBarCount);

            double expectedCornerArea = 4.0 * (Math.PI * 25.0 * 25.0 / 4.0);
            double expectedSideArea = 8.0 * (Math.PI * 20.0 * 20.0 / 4.0);
            double expectedTotalArea = expectedCornerArea + expectedSideArea;

            Assert.Equal(expectedTotalArea, sec.TotalSteelAreaMm2, 2);
            double expectedRho = (expectedTotalArea / (500.0 * 500.0)) * 100.0;
            Assert.Equal(expectedRho, sec.ReinforcementRatioPercent, 2);
        }

        [Fact]
        public void ColumnSectionModel_GetBarCoordinates_CalculatesCornersAndSideBars()
        {
            var sec = new ColumnSectionModel
            {
                WidthMm = 400.0,
                DepthMm = 400.0,
                CornerDiameterMm = 20,
                SideCountB = 1,
                SideDiameterB = 18,
                SideCountH = 1,
                SideDiameterH = 18
            };

            var coords = sec.GetBarCoordinates(coverMm: 30.0, tieDiaMm: 8.0);
            // 4 corners + 2 along B + 2 along H = 8 bars
            Assert.Equal(8, coords.Count);

            double expectedCoreX = 200.0 - 30.0 - 8.0 - 10.0; // 152.0
            double expectedCoreY = 200.0 - 30.0 - 8.0 - 10.0; // 152.0

            Assert.Contains(coords, c => Math.Abs(c.X - expectedCoreX) < 0.1 && Math.Abs(c.Y - expectedCoreY) < 0.1);
            Assert.Contains(coords, c => Math.Abs(c.X - (-expectedCoreX)) < 0.1 && Math.Abs(c.Y - (-expectedCoreY)) < 0.1);
        }

        [Fact]
        public void ColumnSectionModel_ClearSpacing_ValidatesMinimumClearGap()
        {
            var sec = new ColumnSectionModel
            {
                WidthMm = 400.0,
                DepthMm = 400.0,
                CornerDiameterMm = 20,
                SideCountB = 1,
                SideDiameterB = 18,
                SideCountH = 1,
                SideDiameterH = 18
            };

            double minClear = sec.CalculateMinClearSpacingMm(coverMm: 30.0, tieDiaMm: 8.0);
            Assert.True(minClear >= 50.0);
            Assert.True(sec.ValidateClearSpacing(coverMm: 30.0, tieDiaMm: 8.0, minClearMm: 50.0));
        }

        [Fact]
        public void ColumnTieDistributionModel_CalculateTotalSets_CalculatesDenseSparseJoint()
        {
            var tie = new ColumnTieDistributionModel
            {
                TieDiameterMm = 8,
                DenseSpacingMm = 100.0,
                SparseSpacingMm = 200.0,
                JointSpacingMm = 150.0,
                ConfinementHeightMm = 600.0
            };

            int sets = tie.CalculateTotalSets(clearHeightMm: 3000.0, beamDepthMm: 600.0);
            // Bottom dense: ceil(600/100) = 6
            // Top dense: ceil(600/100) = 6
            // Mid sparse: (3000 - 1200) = 1800 => ceil(1800/200) = 9
            // Joint: ceil(600/150) = 4
            // Total = 6 + 6 + 9 + 4 = 25
            Assert.Equal(25, sets);
        }
    }
}
