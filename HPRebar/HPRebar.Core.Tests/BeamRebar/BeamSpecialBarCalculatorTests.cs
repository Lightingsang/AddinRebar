using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.BeamRebar;

public sealed class BeamSpecialBarCalculatorTests
{
    private const int Precision = 6;

    // Hanging Stirrups (Tier 1 & 4)
    [Fact]
    public void SecondaryBeamIntersectionGeneratesSymmetricHangingStirrupsFlankingJoint()
    {
        var stations = BeamSpecialBarCalculator.ComputeHangingStirrupStations(3500, 250, countPerSide: 3, spacingMm: 50);

        Assert.Equal(6, stations.Count);
        double leftFace = 3500.0 - (250.0 / 2.0); // 3375
        double rightFace = 3500.0 + (250.0 / 2.0); // 3625

        var leftGroup = stations.Where(x => x < 3500).ToList();
        var rightGroup = stations.Where(x => x > 3500).ToList();

        Assert.Equal(3, leftGroup.Count);
        Assert.Equal(3, rightGroup.Count);

        // Verify symmetry relative to joint faces
        Assert.Equal(Math.Abs(leftGroup[0] - leftFace), Math.Abs(rightGroup[2] - rightFace), Precision);
    }

    [Fact]
    public void HangingStirrupSpacingMatchesSpecifiedDistance()
    {
        var stations = BeamSpecialBarCalculator.ComputeHangingStirrupStations(3500, 250, countPerSide: 3, spacingMm: 50);
        var sorted = stations.OrderBy(s => s).ToList();

        // Check 50mm spacing within left group
        Assert.Equal(50.0, sorted[1] - sorted[0], Precision);
        Assert.Equal(50.0, sorted[2] - sorted[1], Precision);

        // Check 50mm spacing within right group
        Assert.Equal(50.0, sorted[4] - sorted[3], Precision);
        Assert.Equal(50.0, sorted[5] - sorted[4], Precision);
    }

    [Fact]
    public void HangingStirrupDimensionsMatchPrimaryBeamCrossSection()
    {
        var stack = TestBeamData.SingleSpan(width: 400, height: 700);
        var sec = new SecondaryBeamIntersection(0, 0, centerX: 3500, width: 250, height: 500);
        stack = stack with { SecondaryIntersections = new[] { sec } };

        var stirrups = BeamSpecialBarCalculator.ComputeHangingStirrups(stack, new BeamSpecialBarSpec { EnableHangingStirrups = true });

        Assert.NotEmpty(stirrups);
        var s0 = stirrups[0];
        // Out-to-out width = 400 - 2*25 = 350
        double width = s0.Points[1].Y - s0.Points[0].Y;
        Assert.Equal(350.0, width, Precision);
    }

    [Fact]
    public void NumberOfHangingStirrupPairsMatchesUserSpecification()
    {
        var stations = BeamSpecialBarCalculator.ComputeHangingStirrupStations(3500, 250, countPerSide: 4, spacingMm: 50);

        // 4 on left + 4 on right = 8 stirrups
        Assert.Equal(8, stations.Count);
    }

    // Overlapping Secondary Beams
    [Fact]
    public void AdjacentSecondaryBeamsMergeOverlappingHangingZones()
    {
        var st1 = BeamSpecialBarCalculator.ComputeHangingStirrupStations(3500, 250, 3, 50);
        var st2 = BeamSpecialBarCalculator.ComputeHangingStirrupStations(3600, 250, 3, 50);
        var all = st1.Concat(st2).ToList();

        var merged = BeamSpecialBarCalculator.MergeHangingStations(all, minimumClearMm: 20.0);

        Assert.True(merged.Count < all.Count);
    }

    [Fact]
    public void SecondaryBeamOutsideClearSpanSafelySkipped()
    {
        var stack = TestBeamData.SingleSpan(length: 6000); // clear: 200 to 5800
        var secOutside = new SecondaryBeamIntersection(0, 0, centerX: 9000, width: 250, height: 500);
        var invalidStack = stack with { SecondaryIntersections = new[] { secOutside } };

        var stirrups = BeamSpecialBarCalculator.ComputeHangingStirrups(invalidStack, new BeamSpecialBarSpec { EnableHangingStirrups = true });
        Assert.Empty(stirrups);
    }

    // Diagonal Bent Bars (Thép vai bò)
    [Fact]
    public void DiagonalBentBarsGenerateFortyFiveDegreeInclinationLegs()
    {
        var pts = BeamSpecialBarCalculator.ComputeDiagonalTiePolyline(
            secondaryCenterXMm: 3500,
            secondaryWidthMm: 250,
            primaryZTopMm: 3600,
            primaryZBotMm: 2900,
            coverMm: 25,
            barDiameterMm: 14);

        Assert.Equal(6, pts.Count);

        double deltaX = pts[2].X - pts[1].X;
        double deltaZ = pts[1].Z - pts[2].Z;

        // For 45°, deltaX must equal deltaZ
        Assert.Equal(deltaZ, deltaX, Precision);
    }

    [Fact]
    public void DiagonalBentBarsPositionDirectlyBeneathSecondaryBeamSoffit()
    {
        var pts = BeamSpecialBarCalculator.ComputeDiagonalTiePolyline(3500, 250, 3600, 2900, 25, 14);

        // Bottom horizontal run points are pts[2] and pts[3]
        Assert.Equal(pts[2].Z, pts[3].Z, Precision);
        Assert.Equal(2900.0 + 25.0 + 7.0, pts[2].Z, Precision);
    }

    [Fact]
    public void DiagonalBentBarsToggleDisabledProducesZeroBentBars()
    {
        var stack = TestBeamData.SingleSpan();
        var sec = new SecondaryBeamIntersection(0, 0, centerX: 3500, width: 250, height: 500);
        stack = stack with { SecondaryIntersections = new[] { sec } };

        var bars = BeamSpecialBarCalculator.ComputeDiagonalTies(stack, new BeamSpecialBarSpec { EnableDiagonalTies = false });

        Assert.Empty(bars);
    }

    [Fact]
    public void SecondaryBeamDepthBelowThresholdOmitsDiagonalBentBars()
    {
        var stack = TestBeamData.SingleSpan();
        var secShallow = new SecondaryBeamIntersection(0, 0, centerX: 3500, width: 250, height: 200); // < 300 mm
        stack = stack with { SecondaryIntersections = new[] { secShallow } };

        var bars = BeamSpecialBarCalculator.ComputeDiagonalTies(stack, new BeamSpecialBarSpec { EnableDiagonalTies = true });

        Assert.Empty(bars);
    }

    [Fact]
    public void FramingCaseBSpecialBarGeometryMatchesNominalDrawingCoordinates()
    {
        // Framing Case B: secondary beam at X = 3500 with b_s = 250 mm.
        // Left group: 3325, 3275, 3225.
        // Right group: 3675, 3725, 3775.
        var stations = BeamSpecialBarCalculator.ComputeHangingStirrupStations(3500, 250, 3, 50);
        var sorted = stations.OrderBy(x => x).ToList();

        Assert.Equal(3225.0, sorted[0], Precision);
        Assert.Equal(3275.0, sorted[1], Precision);
        Assert.Equal(3325.0, sorted[2], Precision);
        Assert.Equal(3675.0, sorted[3], Precision);
        Assert.Equal(3725.0, sorted[4], Precision);
        Assert.Equal(3775.0, sorted[5], Precision);
    }

    [Fact]
    public void HangingStirrupsDoNotConflictWithPrimaryStirrupRuns()
    {
        var stations = BeamSpecialBarCalculator.ComputeHangingStirrupStations(3500, 250, 3, 50);

        Assert.All(stations, s => Assert.True(s > 0.0));
    }

    [Fact]
    public void SecondaryBeamNearLeftColumnClampsHangingStirrupsInsideHostSpan()
    {
        // Secondary beam at X = 350 mm (w = 250 mm) in span starting at X = 200 mm with cover = 25 mm.
        // Left joint face = 350 - 125 = 225 mm. Left flanking stations unclamped would be 175, 125, 75 mm (< 225 mm).
        // Clear host span minX = 200 + 25 = 225 mm.
        // Stations < 225 mm are culled so zero stirrups penetrate column or violate cover.
        var stations = BeamSpecialBarCalculator.ComputeHangingStirrupStations(
            secondaryCenterXMm: 350,
            secondaryWidthMm: 250,
            countPerSide: 3,
            spacingMm: 50,
            minXMm: 225.0,
            maxXMm: 5800.0);

        Assert.NotEmpty(stations);
        Assert.All(stations, x => Assert.True(x >= 225.0));
    }

    [Fact]
    public void SecondaryBeamNearRightColumnClampsHangingStirrupsInsideHostSpan()
    {
        // Span ends at 5800 mm with cover = 25 mm (maxX = 5775 mm). Secondary at 5650 mm (w = 250 mm).
        // Right face = 5650 + 125 = 5775 mm. Right flanking stations would be 5825, 5875, 5925 mm (> 5775 mm).
        var stations = BeamSpecialBarCalculator.ComputeHangingStirrupStations(
            secondaryCenterXMm: 5650,
            secondaryWidthMm: 250,
            countPerSide: 3,
            spacingMm: 50,
            minXMm: 200.0,
            maxXMm: 5775.0);

        Assert.NotEmpty(stations);
        Assert.All(stations, x => Assert.True(x <= 5775.0));
    }

    [Fact]
    public void SecondaryBeamNearColumnOmitsDiagonalTieWhenBendCannotClearSpan()
    {
        // Joint at X = 500, w = 250. xSecL = 375.
        // primaryZTop = 3600, primaryZBot = 2900, cover = 25, barDia = 14 => deltaZ = 672 => deltaX = 672.
        // xBendL = 375 - 672 = -297 < minXMm = 225 mm.
        var pts = BeamSpecialBarCalculator.ComputeDiagonalTiePolyline(
            secondaryCenterXMm: 500,
            secondaryWidthMm: 250,
            primaryZTopMm: 3600,
            primaryZBotMm: 2900,
            coverMm: 25,
            barDiameterMm: 14,
            minXMm: 225.0,
            maxXMm: 5775.0);

        Assert.Empty(pts);
    }

    [Fact]
    public void DiagonalBentTieAnchorLegsClampToSpanBounds()
    {
        // Joint at X = 1100, w = 250 => xSecL = 975.
        // deltaX = 672 => xBendL = 975 - 672 = 303 >= 225 mm (clears bend).
        // anchor = 30 * 14 = 420 => unclamped x0 = 303 - 420 = -117 mm (< 225 mm).
        // Clamped x0 must equal minXMm = 225 mm.
        var pts = BeamSpecialBarCalculator.ComputeDiagonalTiePolyline(
            secondaryCenterXMm: 1100,
            secondaryWidthMm: 250,
            primaryZTopMm: 3600,
            primaryZBotMm: 2900,
            coverMm: 25,
            barDiameterMm: 14,
            minXMm: 225.0,
            maxXMm: 5775.0);

        Assert.NotEmpty(pts);
        Assert.Equal(225.0, pts[0].X, Precision);
        Assert.True(pts[0].X >= 225.0);
        Assert.True(pts[pts.Count - 1].X <= 5775.0);
    }
}
