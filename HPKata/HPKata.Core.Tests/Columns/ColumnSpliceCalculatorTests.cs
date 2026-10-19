using HPKata.Core.Columns.Calculators;
using Xunit;

namespace HPKata.Core.Tests.Columns
{
    public class ColumnSpliceCalculatorTests
    {
        [Fact]
        public void CalculateLapLength_CompressionAndTension_ReturnsExpectedMultipliers()
        {
            // Compression 35d: dia 20 => 35 * 20 = 700mm
            double compLap = ColumnSpliceCalculator.CalculateLapLength(20, isTension: false);
            Assert.Equal(700.0, compLap);

            // Tension 45d: dia 20 => 45 * 20 = 900mm
            double tensLap = ColumnSpliceCalculator.CalculateLapLength(20, isTension: true);
            Assert.Equal(900.0, tensLap);

            // Minimum 400mm check for small diameter: dia 10 compression 35*10 = 350 < 400 => 400
            double smallLap = ColumnSpliceCalculator.CalculateLapLength(10, isTension: false);
            Assert.Equal(400.0, smallLap);
        }

        [Fact]
        public void CalculateStaggerOffset_ClampsToMinimum600Mm()
        {
            // Lap 400mm => 1.3 * 400 = 520 < 600 => 600mm
            double offsetSmall = ColumnSpliceCalculator.CalculateStaggerOffset(400.0);
            Assert.Equal(600.0, offsetSmall);

            // Lap 700mm => 1.3 * 700 = 910 >= 600 => 910mm
            double offsetLarge = ColumnSpliceCalculator.CalculateStaggerOffset(700.0);
            Assert.Equal(910.0, offsetLarge);
        }

        [Fact]
        public void CalculateSpliceElevation_LocatesOutsideBeamAndConfinement()
        {
            double floorZ = 0.0;
            double beamDepth = 600.0;
            double l0 = 500.0;

            double spliceZ = ColumnSpliceCalculator.CalculateSpliceElevation(floorZ, beamDepth, l0);
            Assert.Equal(1100.0, spliceZ);
        }

        [Fact]
        public void SegmentCommercialColumnBars_ShortRun_ReturnsSingleSegment()
        {
            var segments = ColumnSpliceCalculator.SegmentCommercialColumnBars(
                0.0, 7200.0, 20, 3600.0, isStaggeredTier: false);

            Assert.Single(segments);
            Assert.Equal(0.0, segments[0].StartZMm);
            Assert.Equal(7200.0, segments[0].EndZMm);
            Assert.False(segments[0].IsLapSplice);
        }

        [Fact]
        public void SegmentCommercialColumnBars_TallRun_SegmentsWithLapSplices()
        {
            // 5 stories x 3.6m = 18000mm > 11700mm
            var segments = ColumnSpliceCalculator.SegmentCommercialColumnBars(
                0.0, 18000.0, 20, 3600.0, isStaggeredTier: false);

            Assert.True(segments.Count >= 2);
            Assert.Equal(0.0, segments[0].StartZMm);
            Assert.True(segments[1].IsLapSplice);
            Assert.Equal(700.0, segments[1].LapLengthMm);
        }

        [Fact]
        public void SegmentCommercialColumnBars_ShortStoryHeight_TerminatesCleanlyWithoutInfiniteLoop()
        {
            // 2 * storyHeight <= lapLengthMm: e.g. storyHeight = 400mm, dia = 25 => lap = 875mm (2*400 = 800 <= 875)
            // Run length 15000mm
            var segments = ColumnSpliceCalculator.SegmentCommercialColumnBars(
                0.0, 15000.0, 25, 400.0, isStaggeredTier: false);

            Assert.NotEmpty(segments);
            Assert.Equal(0.0, segments[0].StartZMm);
            Assert.Equal(15000.0, segments[segments.Count - 1].EndZMm);

            for (int i = 0; i < segments.Count - 1; i++)
            {
                Assert.True(segments[i + 1].StartZMm > segments[i].StartZMm,
                    $"Segment {i + 1} StartZ must advance strictly beyond Segment {i} StartZ");
            }
        }
    }
}
