using System.Collections.Generic;
using HPKata.Core.Beams.Calculators;
using Xunit;

namespace HPKata.Core.Tests.Beams
{
    public class BeamCurtailmentCalculatorTests
    {
        [Theory]
        [InlineData(6000.0, 0.25, 1500.0)]
        [InlineData(6000.0, 0.20, 1200.0)]
        [InlineData(6000.0, 0.33, 1980.0)]
        public void CalculateTopCutoff_VariousRatios_CalculatesCorrectLength(
            double clearSpan, double ratio, double expected)
        {
            double cutoff = BeamCurtailmentCalculator.CalculateTopCutoff(clearSpan, ratio);
            Assert.Equal(expected, cutoff);
        }

        [Fact]
        public void CalculateBottomCutoffs_StandardOffsets_CalculatesStartAndEnd()
        {
            var (start, end) = BeamCurtailmentCalculator.CalculateBottomCutoffs(6000.0, 0.10, 0.10);
            Assert.Equal(600.0, start);
            Assert.Equal(5400.0, end);
        }

        [Fact]
        public void CalculateAnchorCog_DeepVsShallow_ClampsToAvailableDepth()
        {
            // Dia 20 standard 12*20 = 240. In 600mm beam (room = 600 - 60 = 540) => 240
            double cogDeep = BeamCurtailmentCalculator.CalculateAnchorCog(20, 600.0);
            Assert.Equal(240.0, cogDeep);

            // In 250mm shallow beam (room = 250 - 60 = 190) => 190
            double cogShallow = BeamCurtailmentCalculator.CalculateAnchorCog(20, 250.0);
            Assert.Equal(190.0, cogShallow);
        }

        [Fact]
        public void SegmentCommercialBars_ShortSpan_ReturnsSingleSegment()
        {
            var segments = BeamCurtailmentCalculator.SegmentCommercialBars(8000.0, 20, isTopBar: true);
            Assert.Single(segments);
            Assert.Equal(8000.0, segments[0].LengthMm);
            Assert.False(segments[0].IsLapSplice);
        }

        [Fact]
        public void SegmentCommercialBars_LongSpan_DividesWithLapSplice()
        {
            double totalSpan = 15000.0;
            int dia = 20;
            var segments = BeamCurtailmentCalculator.SegmentCommercialBars(totalSpan, dia, isTopBar: true);

            Assert.Equal(2, segments.Count);
            Assert.Equal(BeamCurtailmentCalculator.CommercialStockLengthMm, segments[0].LengthMm);
            Assert.True(segments[1].IsLapSplice);
            Assert.Equal(600.0, segments[1].LapLengthMm); // 30 * 20 = 600
        }

        [Fact]
        public void SegmentCommercialBars_StaggerOffset_OffsetsFirstBarLength()
        {
            double totalSpan = 15000.0;
            int dia = 20;
            var segments = BeamCurtailmentCalculator.SegmentCommercialBars(
                totalSpan, dia, isTopBar: false, staggerOffsetMm: 2000.0);

            Assert.Equal(2, segments.Count);
            Assert.Equal(BeamCurtailmentCalculator.CommercialStockLengthMm - 2000.0, segments[0].LengthMm);
        }
    }
}
