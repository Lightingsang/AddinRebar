using System;
using System.Collections.Generic;
using HPDcePro.Core.Calculations.Beam;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public sealed class ContinuousBeamTests
    {
        private readonly ConcreteProperties _concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
        private readonly RebarProperties _steel = MaterialLibrary.GetRebar(RebarGrade.CB400V);

        [Fact]
        public void Calculate_MultiSpanMomentRedistribution_ClampsToTwentyPercentOnEqualSpans()
        {
            var spans = new List<BeamSpanInput>
            {
                new BeamSpanInput { SpanIndex = 0, ClearSpanMm = 6000, SectionWidthMm = 300, SectionHeightMm = 600, TopMomentRightKnm = 100, BottomMomentMidKnm = 80 },
                new BeamSpanInput { SpanIndex = 1, ClearSpanMm = 6000, SectionWidthMm = 300, SectionHeightMm = 600, TopMomentLeftKnm = 100, BottomMomentMidKnm = 80 }
            };

            var input = new ContinuousBeamInput("D1", _concrete, _steel, spans) { MomentRedistributionRatio = 0.20 };
            var result = ContinuousBeamCalculator.Calculate(input);

            Assert.Equal(80.0, result.Spans[0].RedistributedTopMomentRightKnm, 1);
            Assert.True(result.Spans[0].RedistributedBottomMomentMidKnm > 80.0);
        }

        [Fact]
        public void Calculate_MultiSpanMomentRedistribution_ClampsToTenPercentWhenSpanRatioExceedsTwo()
        {
            var spans = new List<BeamSpanInput>
            {
                new BeamSpanInput { SpanIndex = 0, ClearSpanMm = 3000, SectionWidthMm = 300, SectionHeightMm = 600, TopMomentRightKnm = 100 },
                new BeamSpanInput { SpanIndex = 1, ClearSpanMm = 7000, SectionWidthMm = 300, SectionHeightMm = 600, TopMomentLeftKnm = 100 }
            };

            var input = new ContinuousBeamInput("D2", _concrete, _steel, spans) { MomentRedistributionRatio = 0.20 };
            var result = ContinuousBeamCalculator.Calculate(input);

            Assert.Equal(90.0, result.Spans[0].RedistributedTopMomentRightKnm, 1);
        }

        [Fact]
        public void Calculate_TopNegativeSupportBars_CutoffIncludesSpanQuarterPlusAnchorage()
        {
            var spans = new List<BeamSpanInput>
            {
                new BeamSpanInput { SpanIndex = 0, ClearSpanMm = 6000, SectionWidthMm = 300, SectionHeightMm = 600, LeftSupportWidthMm = 400, RightSupportWidthMm = 400 }
            };

            var input = new ContinuousBeamInput("D3", _concrete, _steel, spans);
            var result = ContinuousBeamCalculator.Calculate(input);

            Assert.NotEmpty(result.SupportExtraTopBars);
            double expectedExt = 6000.0 * 0.25 + (30.0 * 20.0); // 1500 + 600 = 2100
            Assert.True(result.SupportExtraTopBars[0].LengthMm >= expectedExt);
        }

        [Fact]
        public void Calculate_TopNegativeSupportBars_ExtendsToThirdSpanForUnequalSpans()
        {
            var spans = new List<BeamSpanInput>
            {
                new BeamSpanInput { SpanIndex = 0, ClearSpanMm = 3000, SectionWidthMm = 300, SectionHeightMm = 600, RightSupportWidthMm = 400 },
                new BeamSpanInput { SpanIndex = 1, ClearSpanMm = 7000, SectionWidthMm = 300, SectionHeightMm = 600, LeftSupportWidthMm = 400 }
            };

            var input = new ContinuousBeamInput("D4", _concrete, _steel, spans);
            var result = ContinuousBeamCalculator.Calculate(input);

            var interiorSupportBar = result.SupportExtraTopBars[1];
            double expectedExt = 7000.0 * (1.0 / 3.0) + (30.0 * 20.0);
            double expectedMinTotal = 2.0 * expectedExt + 400.0;
            Assert.True(interiorSupportBar.LengthMm >= expectedMinTotal - 1.0);
        }

        [Fact]
        public void Calculate_BottomPositiveSpanBars_TerminatesAtPointFifteenFromSupport()
        {
            var spans = new List<BeamSpanInput>
            {
                new BeamSpanInput { SpanIndex = 0, ClearSpanMm = 6000, SectionWidthMm = 300, SectionHeightMm = 600, LeftSupportWidthMm = 400, RightSupportWidthMm = 400 }
            };

            var input = new ContinuousBeamInput("D5", _concrete, _steel, spans);
            var result = ContinuousBeamCalculator.Calculate(input);

            Assert.NotEmpty(result.SpanExtraBottomBars);
            var botBar = result.SpanExtraBottomBars[0];
            Assert.Equal(6000.0 * 0.70, botBar.LengthMm, 1);
            Assert.Equal(400.0 + 6000.0 * 0.15, botBar.StartX, 1);
        }

        [Fact]
        public void Calculate_CommercialBarSplice_StaggersInLowStressZonesWhenExceeding11700Mm()
        {
            var spans = new List<BeamSpanInput>
            {
                new BeamSpanInput { SpanIndex = 0, ClearSpanMm = 7000, SectionWidthMm = 300, SectionHeightMm = 600 },
                new BeamSpanInput { SpanIndex = 1, ClearSpanMm = 7000, SectionWidthMm = 300, SectionHeightMm = 600 }
            };

            var input = new ContinuousBeamInput("D6", _concrete, _steel, spans) { CommercialBarLengthMm = 11700.0 };
            var result = ContinuousBeamCalculator.Calculate(input);

            Assert.NotEmpty(result.LapSplices);
            var topSplice = Assert.Single(result.LapSplices, s => s.Layer == RebarPlacementType.MainTop);
            var botSplice = Assert.Single(result.LapSplices, s => s.Layer == RebarPlacementType.MainBottom);

            Assert.True(topSplice.LapLengthMm >= 30.0 * 18.0);
            Assert.True(topSplice.StaggerOffsetMm >= 1.3 * topSplice.LapLengthMm);
            Assert.NotEqual(topSplice.SpliceX, botSplice.SpliceX);
        }

        [Fact]
        public void Calculate_StirrupZones_ArrangesDenseZonesAtSupportFaces()
        {
            var spans = new List<BeamSpanInput>
            {
                new BeamSpanInput { SpanIndex = 0, ClearSpanMm = 6000, SectionWidthMm = 300, SectionHeightMm = 600, ShearLeftKn = 150.0 }
            };

            var input = new ContinuousBeamInput("D7", _concrete, _steel, spans);
            var result = ContinuousBeamCalculator.Calculate(input);

            Assert.Equal(3, result.StirrupZones.Count);
            Assert.Equal(1500.0, result.StirrupZones[0].EndX);
            Assert.True(result.StirrupZones[0].SpacingMm <= 150.0);
            Assert.Equal(200.0, result.StirrupZones[1].SpacingMm);
        }

        [Fact]
        public void Calculate_SideSkinBars_GeneratesSideBarsWhenHeightReaches700Mm()
        {
            var spans = new List<BeamSpanInput>
            {
                new BeamSpanInput { SpanIndex = 0, ClearSpanMm = 6000, SectionWidthMm = 300, SectionHeightMm = 800 }
            };

            var input = new ContinuousBeamInput("D8", _concrete, _steel, spans);
            var result = ContinuousBeamCalculator.Calculate(input);

            Assert.NotEmpty(result.SideBars);
            Assert.Equal(14.0, result.SideBars[0].DiameterMm);
        }

        [Fact]
        public void FoundationBeam_InvertedMomentProfile_TensionAtTopMidspan()
        {
            var input = new FoundationBeamDesignInput("GB1", _concrete, _steel)
            {
                WidthMm = 400,
                HeightMm = 800,
                CoverMm = 50,
                UpwardMidspanMomentKnm = 200.0,
                SupportBottomMomentKnm = 150.0
            };

            var result = FoundationBeamCalculator.Calculate(input);

            Assert.True(result.MidSpanTopTensionAsMm2 > 0);
            Assert.NotEmpty(result.MainTopBars);
            Assert.NotEmpty(result.MainBottomBars);
            Assert.Equal(50.0, result.ConcreteCoverMm);
        }

        [Fact]
        public void FoundationBeam_Under30MmCover_ThrowsInvalidOperationException()
        {
            var input = new FoundationBeamDesignInput("GB2", _concrete, _steel) { CoverMm = 25.0 };
            Assert.Throws<InvalidOperationException>(() => FoundationBeamCalculator.Calculate(input));
        }

        [Fact]
        public void CurvedBeam_DiscretizationAndTorsion_CalculatesChordPointsAndAsw()
        {
            var input = new CurvedBeamDesignInput("CB1", _concrete, _steel)
            {
                RadiusMm = 6000.0,
                StartAngleRad = 0.0,
                EndAngleRad = Math.PI * 0.5,
                TorsionKnm = 30.0,
                ShearKn = 100.0,
                MaxChordLengthMm = 1000.0
            };

            var result = CurvedBeamCalculator.Calculate(input);

            Assert.True(result.ArcLengthMm > 9400.0);
            Assert.True(result.DiscretizedChordPoints.Count >= 10);
            Assert.True(result.TotalCombinedStirrupAswPerS > result.RequiredTorsionStirrupAsw1PerS);
            Assert.Equal("NHAN_XDATA_DAM_CONG", result.XDataNhanDamCong.RegAppName);
        }

        [Fact]
        public void BeamTrimmer_HangerReinforcement_CalculatesConcentratedAsh()
        {
            var input = new BeamTrimmerInput
            {
                PrimaryBeamWidthMm = 300,
                PrimaryBeamHeightMm = 600,
                SecondaryBeamWidthMm = 220,
                SecondaryBeamHeightMm = 400,
                SecondaryShearReactionKn = 100.0,
                StirrupRswMpa = 225.0
            };

            var result = BeamTrimmerCalculator.Calculate(input);

            Assert.True(result.IsHangerRequired);
            Assert.True(result.RequiredHangerAreaAshMm2 > 0);
            Assert.True(result.RecommendedStirrupPairs >= 1);
        }
    }
}
