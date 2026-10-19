using System;
using HPKata.Core.Beams.Calculators;
using HPKata.Core.Beams.Models;
using Xunit;

namespace HPKata.Core.Tests.Beams
{
    public class BeamHangerBarCalculatorTests
    {
        [Fact]
        public void CalculateHangers_StandardIntersection_GeneratesSymmetricOffsets()
        {
            var hanger = BeamHangerBarCalculator.CalculateHangers(
                spanIndex: 0, distanceAlongSpanMm: 3000.0, secondaryBeamName: "D_SEC",
                secondaryBeamWidthMm: 220.0, secondaryBeamHeightMm: 450.0,
                hangerCount: 5, hangerDiaMm: 10, hangerSpacingMm: 50.0);

            Assert.Equal(5, hanger.HangerStirrupCount);
            Assert.Equal(10, hanger.HangerStirrupDiameterMm);
            Assert.Equal(10, hanger.HangerOffsetsMm.Count); // 5 left + 5 right
            Assert.True(hanger.RequiresAntiCrackBars); // h = 450 >= 400
        }

        [Fact]
        public void CalculateHangers_ShallowSecondaryBeam_DoesNotRequireAntiCrack()
        {
            var hanger = BeamHangerBarCalculator.CalculateHangers(
                spanIndex: 0, distanceAlongSpanMm: 2000.0, secondaryBeamName: "D_Phu",
                secondaryBeamWidthMm: 200.0, secondaryBeamHeightMm: 300.0);

            Assert.False(hanger.RequiresAntiCrackBars);
        }

        [Fact]
        public void SplitBeamSpan_ValidCutPoint_DividesSpanCleanly()
        {
            var original = new BeamSpanModel
            {
                SpanIndex = 1,
                SpanName = "D1_N1",
                ClearLengthMm = 6000.0,
                LeftSupportWidthMm = 400.0,
                RightSupportWidthMm = 400.0,
                WidthMm = 300.0,
                HeightMm = 600.0
            };

            var (left, right) = BeamHangerBarCalculator.SplitBeamSpan(original, splitDistanceMm: 3000.0, supportWidthMm: 300.0);

            // Left clear: 3000 - 150 = 2850
            Assert.Equal(2850.0, left.ClearLengthMm);
            Assert.Equal(400.0, left.LeftSupportWidthMm);
            Assert.Equal(300.0, left.RightSupportWidthMm);

            // Right clear: 6000 - 3000 - 150 = 2850
            Assert.Equal(2850.0, right.ClearLengthMm);
            Assert.Equal(300.0, right.LeftSupportWidthMm);
            Assert.Equal(400.0, right.RightSupportWidthMm);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(-500.0)]
        [InlineData(6000.0)]
        [InlineData(7000.0)]
        public void SplitBeamSpan_InvalidSplitPoints_ThrowsArgumentException(double splitDistance)
        {
            var original = new BeamSpanModel
            {
                SpanIndex = 0,
                ClearLengthMm = 6000.0
            };

            Assert.Throws<ArgumentException>(() =>
                BeamHangerBarCalculator.SplitBeamSpan(original, splitDistance));
        }
    }
}
