using System;
using System.Collections.Generic;
using System.Linq;
using HPKata.Core.Beams.Calculators;
using HPKata.Core.Beams.Etabs;
using HPKata.Core.Beams.Models;
using Xunit;

namespace HPKata.Core.Tests.Beams
{
    public class BeamCoreStressTests
    {
        // ====================================================================
        // 1. TcvnBeamRebarOptimizer STRESS TESTS
        // ====================================================================

        [Fact]
        public void Stress_DualDiameter_PairingsObey4mmDifferenceRule()
        {
            // Test intermediate As demand that favors dual-diameter combinations
            // e.g. As = 1350 mm2 on width 300mm: 2Ø22 (760) + 2Ø20 (628) = 1388 mm2 (diff = 2mm <= 4mm)
            var opt = TcvnBeamRebarOptimizer.Optimize(1350.0, 300.0, isTopBar: true);

            if (opt.SecondaryCount > 0)
            {
                int diff = Math.Abs(opt.PrimaryDiameterMm - opt.SecondaryDiameterMm);
                Assert.True(diff <= 4, $"Dual-diameter difference {diff}mm exceeds 4mm limit! Primary: {opt.PrimaryDiameterMm}, Secondary: {opt.SecondaryDiameterMm}");
                Assert.True(opt.PrimaryDiameterMm >= opt.SecondaryDiameterMm, "Corner bars should be larger than or equal to interior bars");
            }
        }

        [Theory]
        [InlineData(1200.0, 300.0, false, 25.0)] // Bottom bars min gap >= 25mm
        [InlineData(1200.0, 300.0, true, 30.0)]  // Top bars min gap >= 30mm
        public void Stress_ClearGap_EnforcesTopVsBottomLimits(
            double asReq, double width, bool isTop, double expectedMinGap)
        {
            var opt = TcvnBeamRebarOptimizer.Optimize(asReq, width, isTopBar: isTop);
            Assert.True(opt.IsCompliant);
            Assert.True(opt.ClearGapMm >= expectedMinGap,
                $"Clear gap {opt.ClearGapMm}mm is less than expected minimum {expectedMinGap}mm for {(isTop ? "Top" : "Bottom")} bars");
            Assert.True(opt.ClearGapMm >= opt.PrimaryDiameterMm,
                $"Clear gap {opt.ClearGapMm}mm is less than bar diameter {opt.PrimaryDiameterMm}mm");
        }

        [Fact]
        public void Stress_ExtremeLoads_HighAsDemandBehavior()
        {
            // As = 5000 mm2 on 300mm beam
            // In 1 layer, max 5x32 = 4021 mm2 (cannot fit). Must split to 2 layers.
            var opt = TcvnBeamRebarOptimizer.Optimize(5000.0, 300.0, isTopBar: true);

            // Empirical observation:
            // Check whether 2-layer fallback provides required As
            Assert.True(opt.AreaProvidedMm2 >= 5000.0,
                $"Provided area {opt.AreaProvidedMm2}mm2 is less than required 5000mm2");
            Assert.Equal(2, opt.LayerCount);

            // Check clear gap and compliance
            // Note: Does 14 bars of Ø22 fit into 300mm width?
            // usableWidth = 300 - 66 = 234mm. Layer1 = 7 bars of Ø22 = 154mm.
            // Gap = (234 - 154) / 6 = 13.3mm < 20mm!
            // This exposes that CreateTwoLayerFallback hardcodes Ø22 instead of choosing larger diameters like Ø28/Ø32!
        }

        [Fact]
        public void Stress_ExtremeLoads_8000mm2_ExposesNegativeClearGapBug()
        {
            // Extreme load: 8000 mm2 on 300mm beam width
            var opt = TcvnBeamRebarOptimizer.Optimize(8000.0, 300.0, isTopBar: true);

            // 8000 mm2 / 380.13 = 22 bars of Ø22. Layer 1 = 11 bars.
            // 11 * 22 = 242mm > usable width 234mm.
            // Gap = (234 - 242) / 10 = -0.8 mm (NEGATIVE CLEAR GAP!)
            // Assert that clear gap is physically positive:
            Assert.True(opt.ClearGapMm > 0,
                $"CRITICAL BUG: Optimizer returned negative clear gap {opt.ClearGapMm}mm! Physically impossible bar layout.");
        }

        [Fact]
        public void Stress_NarrowBeam_ImpossibleLayoutRejected()
        {
            // Beam width b = 100mm, cover = 25mm, stirrup = 8mm
            // Usable width = 100 - 66 = 34mm.
            // Default 2Ø18 takes 36mm > 34mm. Clear gap = -2.0mm!
            var opt = TcvnBeamRebarOptimizer.Optimize(0.0, 100.0, isTopBar: true);

            // The optimizer should NOT mark an impossible layout with negative clear gap as compliant!
            Assert.True(opt.ClearGapMm > 0,
                $"CRITICAL BUG: Default layout on 100mm beam width has negative clear gap {opt.ClearGapMm}mm, but IsCompliant was {opt.IsCompliant}");
            Assert.False(opt.IsCompliant && opt.ClearGapMm < 20.0,
                $"CRITICAL BUG: Marked compliant despite insufficient gap {opt.ClearGapMm}mm");
        }

        [Fact]
        public void Stress_MinimumReinforcementRatio_Mu01Percent_NotEnforced()
        {
            // Beam section 500x1200 mm. h0 approx 1150mm.
            // TCVN 5574:2018 §10.3.1.1 requires As_min >= 0.10% * b * h0 = 0.001 * 500 * 1150 = 575 mm2.
            // If required As from load analysis is very low (e.g. 50 mm2):
            double b = 500.0;
            double h0 = 1150.0;
            double asMinTcvn = 0.001 * b * h0; // 575 mm2

            var opt = TcvnBeamRebarOptimizer.Optimize(50.0, b, isTopBar: false, beamHeightMm: 1200.0);

            // Assert whether optimizer enforces the TCVN minimum reinforcement ratio:
            Assert.True(opt.AreaProvidedMm2 >= asMinTcvn,
                $"DEFECT: Area provided {opt.AreaProvidedMm2}mm2 is less than TCVN 5574:2018 minimum reinforcement area {asMinTcvn}mm2 (mu < 0.10%)! Optimizer lacks beam depth parameter.");
        }

        [Theory]
        [InlineData(150.0, 600.0)]
        [InlineData(200.0, 1200.0)]
        [InlineData(200.0, 2500.0)]
        public void Stress_NarrowBeams_TieringAndClearance(double width, double asReq)
        {
            var opt = TcvnBeamRebarOptimizer.Optimize(asReq, width, isTopBar: true);

            if (opt.IsCompliant)
            {
                Assert.True(opt.ClearGapMm >= 20.0,
                    $"Compliant result on width {width}mm has clear gap {opt.ClearGapMm}mm < 20mm");
                Assert.True(opt.AreaProvidedMm2 >= asReq,
                    $"Compliant result provides {opt.AreaProvidedMm2}mm2 < required {asReq}mm2");
            }
        }

        // ====================================================================
        // 2. BeamSkinBarCalculator STRESS TESTS (Deep Beams h >= 700mm)
        // ====================================================================

        [Theory]
        [InlineData(650.0, 0)]
        [InlineData(700.0, 1)]
        [InlineData(750.0, 1)]
        [InlineData(1100.0, 2)]
        [InlineData(1500.0, 3)]
        [InlineData(2000.0, 4)]
        public void Stress_DeepBeams_SkinBarRequirementsAndSpacing(double height, int expectedPairs)
        {
            int pairs = BeamSkinBarCalculator.CalculateRequiredSideBarPairs(height, coverMm: 30.0);
            Assert.Equal(expectedPairs, pairs);

            if (pairs > 0)
            {
                var elevations = BeamSkinBarCalculator.CalculateSkinBarElevations(height, coverMm: 30.0);
                Assert.Equal(pairs, elevations.Count);

                // Verify that vertical spacing between adjacent elevations <= 400mm (TCVN limit)
                for (int i = 0; i < elevations.Count - 1; i++)
                {
                    double spacing = elevations[i + 1] - elevations[i];
                    Assert.True(spacing <= BeamSkinBarCalculator.MaximumVerticalSpacingMm,
                        $"Skin bar vertical spacing {spacing}mm exceeds 400mm max at height {height}mm");
                }
            }
        }

        // ====================================================================
        // 3. BeamCurtailmentCalculator STRESS TESTS
        // ====================================================================

        [Fact]
        public void Stress_CommercialBars_3Span24mBeamRun_DivisionAndSplices()
        {
            // 3-span 24m beam run (total length = 24000 mm, dia = 20 mm)
            double totalLength = 24000.0;
            int dia = 20;
            double expectedLap = 30.0 * dia; // 600mm

            var segsUnstaggered = BeamCurtailmentCalculator.SegmentCommercialBars(
                totalLength, dia, isTopBar: true, staggerOffsetMm: 0.0);

            // Must divide 24m into at least 3 segments (each <= 11700mm)
            Assert.True(segsUnstaggered.Count >= 3);
            Assert.All(segsUnstaggered, s => Assert.True(s.LengthMm <= BeamCurtailmentCalculator.CommercialStockLengthMm));

            // Verify total reach spans from 0 to 24000mm
            var lastSeg = segsUnstaggered.Last();
            Assert.Equal(totalLength, lastSeg.StartXMm + lastSeg.LengthMm);

            // Verify lap lengths on splices
            for (int i = 1; i < segsUnstaggered.Count; i++)
            {
                Assert.True(segsUnstaggered[i].IsLapSplice);
                Assert.Equal(expectedLap, segsUnstaggered[i].LapLengthMm);
            }

            // Test staggered splices
            var segsStaggered = BeamCurtailmentCalculator.SegmentCommercialBars(
                totalLength, dia, isTopBar: true, staggerOffsetMm: 2000.0);

            Assert.True(segsStaggered.Count >= 3);
            Assert.Equal(BeamCurtailmentCalculator.CommercialStockLengthMm - 2000.0, segsStaggered[0].LengthMm);

            // Stagger difference between bar lines
            double splicePos1 = segsUnstaggered[0].LengthMm; // 11700
            double splicePos2 = segsStaggered[0].LengthMm;   // 9700
            Assert.Equal(2000.0, Math.Abs(splicePos1 - splicePos2));
        }

        [Fact]
        public void Stress_CommercialBars_DeadParameter_IsTopBarIgnored()
        {
            // Verify empirical fact: Does isTopBar change the splice locations?
            // In structural mechanics, top bars splice at midspan, bottom bars splice at supports.
            var topSegs = BeamCurtailmentCalculator.SegmentCommercialBars(24000.0, 20, isTopBar: true);
            var botSegs = BeamCurtailmentCalculator.SegmentCommercialBars(24000.0, 20, isTopBar: false);

            // Empirical check: are they identical?
            bool identical = topSegs.Count == botSegs.Count &&
                             topSegs[0].LengthMm == botSegs[0].LengthMm &&
                             topSegs[1].StartXMm == botSegs[1].StartXMm;

            // This demonstrates empirically whether isTopBar is wired or dead code
            Assert.True(identical, "Empirical observation: isTopBar produces identical segments because it is currently dead code in BeamCurtailmentCalculator.");
        }

        // ====================================================================
        // 4. BeamStirrupZoneCalculator STRESS TESTS
        // ====================================================================

        [Theory]
        [InlineData(6000.0, 600.0, 1500.0, 3000.0)] // Normal span: dense L/4 = 1500, midspan = 3000
        [InlineData(4000.0, 600.0, 1200.0, 1600.0)] // 2h = 1200 > L/4 = 1000: dense = 1200, midspan = 1600
        [InlineData(2400.0, 600.0, 1200.0, 0.0)]    // Short span: capped at L/2 = 1200, midspan = 0
        public void Stress_StirrupZones_DenseAndSparseLengths(
            double span, double height, double expectedDense, double expectedMidspan)
        {
            var zones = BeamStirrupZoneCalculator.CalculateZones(0, span, 300.0, height);
            Assert.Equal(expectedDense, zones.LeftDenseZoneLengthMm);
            Assert.Equal(expectedDense, zones.RightDenseZoneLengthMm);
            Assert.Equal(expectedMidspan, zones.MidspanSparseZoneLengthMm);
        }

        [Fact]
        public void Stress_StirrupZones_RoundingOverflowBug_DenseExceedsHalfSpan()
        {
            // When clear span is 1350mm, h = 500mm:
            // 2h = 1000mm. Capped at span/2 = 675mm.
            // Math.Round(675 / 50) * 50 = 14 * 50 = 700mm!
            // 700mm > 675mm (dense zone exceeds half span!)
            // Total dense = 1400mm > 1350mm (clear span)!
            var zones = BeamStirrupZoneCalculator.CalculateZones(0, 1350.0, 300.0, 500.0);

            // Assert that sum of dense zones does not exceed total span:
            double totalDense = zones.LeftDenseZoneLengthMm + zones.RightDenseZoneLengthMm;
            Assert.True(totalDense <= 1350.0,
                $"CRITICAL DEFECT: Left dense ({zones.LeftDenseZoneLengthMm}mm) + Right dense ({zones.RightDenseZoneLengthMm}mm) = {totalDense}mm EXCEEDS clear span 1350mm!");
        }
    }
}
