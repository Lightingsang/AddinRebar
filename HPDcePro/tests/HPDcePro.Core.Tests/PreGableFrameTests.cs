using System;
using HPDcePro.Core.Calculations.Steel;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public sealed class PreGableFrameTests
    {
        [Fact]
        public void Calculate_StandardGableFrame_ComputesCorrectCenterlineGeometryAndApex()
        {
            var input = new GableFrameInput
            {
                SpanMm = 24000.0,
                EaveHeightMm = 7000.0,
                RoofSlopePercent = 10.0,
                BaySpacingMm = 6000.0
            };

            var result = PreGableFrameCalculator.Calculate(input);

            Assert.Equal(24000.0 * 0.5, result.Apex.X, precision: 1);
            // Rise = 12000 * 0.10 = 1200 mm -> Apex height = 7000 + 1200 = 8200 mm
            Assert.Equal(8200.0, result.ApexHeightMm, precision: 1);
            Assert.Equal(8200.0, result.Apex.Y, precision: 1);
            Assert.True(result.RafterSlopeLengthMm > 12000.0);
            Assert.True(result.RafterSlopeAngleDeg > 5.0 && result.RafterSlopeAngleDeg < 6.0);
        }

        [Fact]
        public void Calculate_TaperedSection_ReturnsCorrectInterpolatedDimensionsAndProperties()
        {
            double startH = 400.0;
            double endH = 800.0;
            double len = 10000.0;

            var secMid = PreGableFrameCalculator.ComputeTaperedSection(5000.0, len, startH, endH, 250.0, 12.0, 8.0);

            Assert.Equal(600.0, secMid.DepthMm, precision: 2);
            Assert.True(secMid.AreaMm2 > 0);
            Assert.True(secMid.IxMm4 > 0);
            Assert.True(secMid.WxMm3 > 0);

            var secStart = PreGableFrameCalculator.ComputeTaperedSection(0.0, len, startH, endH, 250.0, 12.0, 8.0);
            Assert.Equal(400.0, secStart.DepthMm, precision: 2);

            var secEnd = PreGableFrameCalculator.ComputeTaperedSection(len, len, startH, endH, 250.0, 12.0, 8.0);
            Assert.Equal(800.0, secEnd.DepthMm, precision: 2);
        }

        [Fact]
        public void Calculate_PurlinLayout_ProducesSpacingBetween1200And1500Mm()
        {
            var input = new GableFrameInput
            {
                SpanMm = 30000.0,
                EaveHeightMm = 8000.0,
                RoofSlopePercent = 10.0,
                TargetPurlinSpacingMm = 1350.0
            };

            var result = PreGableFrameCalculator.Calculate(input);

            Assert.NotEmpty(result.Purlins);
            Assert.True(result.ActualPurlinSpacingMm >= 1200.0 && result.ActualPurlinSpacingMm <= 1500.0,
                $"Purlin spacing {result.ActualPurlinSpacingMm} should be within 1.2-1.5m");
            Assert.True(result.PurlinCountPerRafter >= 10);
            Assert.Equal(result.PurlinCountPerRafter * 2, result.Purlins.Count);
        }

        [Fact]
        public void Calculate_InternalForcesAndStress_EvaluatesKneeAndApexMoments()
        {
            var input = new GableFrameInput
            {
                SpanMm = 24000.0,
                EaveHeightMm = 7000.0,
                RoofSlopePercent = 10.0,
                BaySpacingMm = 6000.0,
                DeadLoadKnM2 = 0.20,
                LiveLoadKnM2 = 0.30
            };

            var result = PreGableFrameCalculator.Calculate(input);

            Assert.True(result.FactoredLoadKnM > 0);
            Assert.True(result.MaxKneeMomentKnm > 0);
            Assert.True(result.MaxApexMomentKnm > 0);
            Assert.True(result.BaseVerticalReactionKn > 0);
            Assert.True(result.BaseHorizontalThrustKn > 0);
            Assert.True(result.KneeStressRatio > 0);
            Assert.True(result.ApexStressRatio > 0);
        }

        [Fact]
        public void Calculate_Deflection_EvaluatesRidgeDeflectionAgainstSpanLimit()
        {
            var input = new GableFrameInput
            {
                SpanMm = 20000.0,
                EaveHeightMm = 6500.0,
                RoofSlopePercent = 12.0,
                RafterStartDepthMm = 700.0,
                RafterEndDepthMm = 450.0
            };

            var result = PreGableFrameCalculator.Calculate(input);

            Assert.True(result.EstimatedRidgeDeflectionMm > 0);
            Assert.Equal(20000.0 / 180.0, result.AllowableRidgeDeflectionMm, precision: 2);
            Assert.True(result.IsDeflectionCompliant);
        }

        [Fact]
        public void Calculate_InvalidInput_ThrowsArgumentOutOfRangeException()
        {
            var badSpan = new GableFrameInput { SpanMm = -100 };
            Assert.Throws<ArgumentOutOfRangeException>(() => PreGableFrameCalculator.Calculate(badSpan));

            var badEave = new GableFrameInput { EaveHeightMm = 0 };
            Assert.Throws<ArgumentOutOfRangeException>(() => PreGableFrameCalculator.Calculate(badEave));

            var badSlope = new GableFrameInput { RoofSlopePercent = -5 };
            Assert.Throws<ArgumentOutOfRangeException>(() => PreGableFrameCalculator.Calculate(badSlope));
        }

        [Fact]
        public void Calculate_SteepSlopeRoof_AccuratelyCalculatesPurlinRunCoordinates()
        {
            var input = new GableFrameInput
            {
                SpanMm = 18000.0,
                EaveHeightMm = 6000.0,
                RoofSlopePercent = 20.0, // steep slope
                TargetPurlinSpacingMm = 1400.0
            };

            var result = PreGableFrameCalculator.Calculate(input);

            Assert.True(result.RafterSlopeAngleDeg > 11.0);
            // Verify left purlin coordinates ascend in X and Y
            for (int i = 1; i < result.PurlinCountPerRafter; i++)
            {
                var prev = result.Purlins[i - 1];
                var cur = result.Purlins[i];
                if (prev.Side == FrameRafterSide.Left && cur.Side == FrameRafterSide.Left)
                {
                    Assert.True(cur.Coordinate.X > prev.Coordinate.X);
                    Assert.True(cur.Coordinate.Y > prev.Coordinate.Y);
                }
            }
        }
    }
}
