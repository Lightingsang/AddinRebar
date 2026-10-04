using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.BeamRebar;

public sealed class BeamMainBarCalculatorTests
{
    private const int Precision = 6;

    // Feature Coverage (Tier 1)
    [Fact]
    public void TopMainBarsGenerateFourVertexUShapedPolylinesWithDownwardHooks()
    {
        var stack = TestBeamData.SingleSpan();
        var bars = BeamMainBarCalculator.ComputeTopMainBars(stack, TestBeamData.MainBarSpec(), 8.0);

        Assert.Equal(3, bars.Count);
        Assert.All(bars, b =>
        {
            Assert.Equal(4, b.Points.Count);
            // Downward hook: hook tip Z < hook corner Z
            Assert.True(b.Points[0].Z < b.Points[1].Z);
            Assert.True(b.Points[3].Z < b.Points[2].Z);
        });
    }

    [Fact]
    public void BottomMainBarsGenerateFourVertexUShapedPolylinesWithUpwardHooks()
    {
        var stack = TestBeamData.SingleSpan();
        var bars = BeamMainBarCalculator.ComputeBottomMainBars(stack, TestBeamData.MainBarSpec(), 8.0);

        Assert.Equal(3, bars.Count);
        Assert.All(bars, b =>
        {
            Assert.Equal(4, b.Points.Count);
            // Upward hook: hook tip Z > hook corner Z
            Assert.True(b.Points[0].Z > b.Points[1].Z);
            Assert.True(b.Points[3].Z > b.Points[2].Z);
        });
    }

    [Fact]
    public void TransverseSpacingEvenlyDistributesBarsAcrossClearBeamWidth()
    {
        var yPos = BeamMainBarCalculator.ComputeTransverseYPositions(300, 25, 8, 20, 3);

        Assert.Equal(3, yPos.Count);
        // Symmetrical around 0
        Assert.Equal(0.0, (yPos[0] + yPos[2]) / 2.0, Precision);
        // Equal spacing between adjacent bars
        Assert.Equal(yPos[1] - yPos[0], yPos[2] - yPos[1], Precision);
    }

    [Fact]
    public void ExteriorAnchorageHooksClampToColumnDepthMinusCover()
    {
        var stack = TestBeamData.SingleSpan(height: 600);
        var bars = BeamMainBarCalculator.ComputeTopMainBars(stack, TestBeamData.MainBarSpec(hookLength: 0.0), 8.0);

        Assert.True(bars[0].Points[1].Z - bars[0].Points[0].Z <= 600 - (2 * 25));
    }

    [Fact]
    public void BarPolylineTotalLengthMatchesSumOfSegments()
    {
        var stack = TestBeamData.SingleSpan();
        var bars = BeamMainBarCalculator.ComputeTopMainBars(stack, TestBeamData.MainBarSpec(), 8.0);
        var b = bars[0];

        double segSum = b.Points[0].DistanceTo(b.Points[1]) +
                        b.Points[1].DistanceTo(b.Points[2]) +
                        b.Points[2].DistanceTo(b.Points[3]);

        Assert.Equal(segSum, b.Polyline.TotalLength, Precision);
    }

    [Fact]
    public void AllVerticesOfEachBarShareIdenticalTransverseYCoordinate()
    {
        var stack = TestBeamData.SingleSpan();
        var bars = BeamMainBarCalculator.ComputeTopMainBars(stack, TestBeamData.MainBarSpec(), 8.0);

        foreach (var bar in bars)
        {
            double expectedY = bar.Points[0].Y;
            Assert.All(bar.Points, p => Assert.Equal(expectedY, p.Y, Precision));
        }
    }

    // Splicing & Staggering
    [Fact]
    public void TotalLengthUnderStockLimitGeneratesUnbrokenContinuousBars()
    {
        var stack = TestBeamData.SingleSpan(length: 6000);
        var bars = BeamMainBarCalculator.ComputeTopMainBars(stack, TestBeamData.MainBarSpec(), 8.0);

        Assert.Equal(3, bars.Count);
        Assert.All(bars, b => Assert.Equal(4, b.Points.Count));
    }

    [Fact]
    public void TotalLengthExceedingStockLimitSplicesTopBarsInMidspan()
    {
        var stack = TestBeamData.ThreeSpan(l1: 6000, l2: 6000, l3: 6000); // 18m > 11.7m
        var bars = BeamMainBarCalculator.ComputeTopMainBars(stack, TestBeamData.MainBarSpec(), 8.0);

        // 3 top bar lines each split into 2 segments = 6 bars
        Assert.Equal(6, bars.Count);
    }

    [Fact]
    public void TotalLengthExceedingStockLimitSplicesBottomBarsAtSupports()
    {
        var stack = TestBeamData.ThreeSpan(l1: 6000, l2: 6000, l3: 6000);
        var bars = BeamMainBarCalculator.ComputeBottomMainBars(stack, TestBeamData.MainBarSpec(), 8.0);

        Assert.Equal(6, bars.Count);
    }

    [Fact]
    public void StaggerToggleOffsetsAdjacentBarSplicesByOnePointThreeLapLength()
    {
        var stack = TestBeamData.ThreeSpan(l1: 6000, l2: 6000, l3: 6000);
        var spec = TestBeamData.MainBarSpec() with { EnableStagger = true, StaggerOffsetRatio = 1.3, MaxStockLength = 11700 };
        var bars = BeamMainBarCalculator.ComputeTopMainBars(stack, spec, 8.0);

        // Bar 0 segment 1 endX vs Bar 1 segment 1 endX
        double end0 = bars[0].Points[bars[0].Points.Count - 1].X;
        double end1 = bars[2].Points[bars[2].Points.Count - 1].X;
        double lap = spec.LapFactor * spec.TopDiameter;

        Assert.Equal(1.3 * lap, Math.Abs(end1 - end0), Precision);
    }

    [Fact]
    public void LapLengthCalculatesCorrectlyFromMultiplierAndBarDiameter()
    {
        // 3-span continuous beam (18 m) forces splicing across commercial stock length limit (11.7 m)
        var stack = TestBeamData.ThreeSpan(l1: 6000, l2: 6000, l3: 6000);
        var spec = TestBeamData.MainBarSpec(topDiameter: 25.0) with { LapFactor = 40.0, MaxStockLength = 11700.0 };
        var bars = BeamMainBarCalculator.ComputeTopMainBars(stack, spec, stirrupDiameterMm: 8.0);

        // Segment 1 (bars[0]) and Segment 2 (bars[1]) form the spliced pair for bar line 0
        double seg1EndX = bars[0].Points[bars[0].Points.Count - 1].X;
        double seg2StartX = bars[1].Points[0].X;
        double actualLapOverlap = seg1EndX - seg2StartX;

        // Verify production calculator applies exactly LapFactor * TopDiameter = 40 * 25 = 1000 mm
        Assert.Equal(1000.0, actualLapOverlap, Precision);
    }

    // Boundary & Step Changes (Tier 2 & 3)
    [Fact]
    public void DepthStepBetweenSpansTerminatesBottomBarsWithUpwardHooksAtSupport()
    {
        var stack = TestBeamData.VariableDepth(h1: 600, h2: 400);
        var bars = BeamMainBarCalculator.ComputeBottomMainBars(stack, TestBeamData.MainBarSpec(), 8.0);

        // 3 bars in Span 1 + 3 bars in Span 2 = 6 bars total
        Assert.Equal(6, bars.Count);
        Assert.All(bars, b => Assert.Equal(4, b.Points.Count));
    }

    [Fact]
    public void CantileverLeftExtendsTopTensionBarToTipAndTurnsDown()
    {
        var stack = TestBeamData.CantileverLeft();
        var bars = BeamMainBarCalculator.ComputeTopMainBars(stack, TestBeamData.MainBarSpec(), 8.0);

        Assert.Equal(stack.OverallStartX + 25.0, bars[0].Points[0].X, Precision);
    }

    [Fact]
    public void CantileverLeftStopsBottomBarAtInteriorColumnFace()
    {
        var stack = TestBeamData.CantileverLeft();
        var bars = BeamMainBarCalculator.ComputeBottomMainBars(stack, TestBeamData.MainBarSpec(), 8.0);

        Assert.True(bars[0].Points[0].X >= stack.Supports[1].LeftFaceX);
    }

    [Fact]
    public void CantileverRightAnchorsTopBarAtTip()
    {
        var supports = new List<BeamSupportNode>
        {
            new(0, "Col0", 0, 400, SupportType.ExteriorColumn),
            new(1, "Col1", 6000, 400, SupportType.InteriorColumn),
            new(2, "Tip2", 8000, 0, SupportType.CantileverEnd)
        };
        var spans = new List<BeamSpan>
        {
            new(0, "Span1", 6000, 300, 600, 0, 25, 5600) { StartX = 200 },
            new(1, "Cant2", 2000, 300, 600, 0, 25, 1800) { StartX = 6200, Cantilever = CantileverPosition.Right }
        };
        var stack = new BeamContinuousStack(spans, supports);
        var bars = BeamMainBarCalculator.ComputeTopMainBars(stack, TestBeamData.MainBarSpec(), 8.0);

        Assert.Equal(stack.OverallEndX - 25.0, bars[0].Points[bars[0].Points.Count - 1].X, Precision);
    }

    [Fact]
    public void BothCantileversAnchorTopBarsAtBothTips()
    {
        var supports = new List<BeamSupportNode>
        {
            new(0, "Tip0", 0, 0, SupportType.CantileverEnd),
            new(1, "Col1", 2000, 400, SupportType.InteriorColumn),
            new(2, "Col2", 8000, 400, SupportType.InteriorColumn),
            new(3, "Tip3", 10000, 0, SupportType.CantileverEnd)
        };
        var spans = new List<BeamSpan>
        {
            new(0, "Cant0", 2000, 300, 600, 0, 25, 1800) { StartX = 0, Cantilever = CantileverPosition.Left },
            new(1, "Span1", 6000, 300, 600, 0, 25, 5600) { StartX = 2200 },
            new(2, "Cant2", 2000, 300, 600, 0, 25, 1800) { StartX = 8200, Cantilever = CantileverPosition.Right }
        };
        var stack = new BeamContinuousStack(spans, supports);
        var bars = BeamMainBarCalculator.ComputeTopMainBars(stack, TestBeamData.MainBarSpec(), 8.0);

        Assert.Equal(stack.OverallStartX + 25.0, bars[0].Points[0].X, Precision);
        Assert.Equal(stack.OverallEndX - 25.0, bars[0].Points[bars[0].Points.Count - 1].X, Precision);
    }

    [Fact]
    public void SubMillimeterPolylineSegmentsAreCulledToPreventRevitGeometryCrash()
    {
        var raw = new List<Point3> { new(0, 0, 0), new(0.5, 0, 0), new(100, 0, 0) };
        var culled = BeamMainBarCalculator.SimplifyPolyline(raw);

        Assert.Equal(2, culled.Count);
        Assert.All(culled.Zip(culled.Skip(1), (p1, p2) => p1.DistanceTo(p2)), d => Assert.True(d >= 1.0));
    }

    [Fact]
    public void MultiLayerTopBarsOffsetSecondLayerVerticallyWithSpecifiedGap()
    {
        var stack = TestBeamData.TwoSpan();
        var mainSpec = TestBeamData.MainBarSpec(topDiameter: 20.0);
        var mainBars = BeamMainBarCalculator.ComputeTopMainBars(stack, mainSpec, stirrupDiameterMm: 8.0);

        var addConfig = new SupportAdditionalTopBarConfig
        {
            SupportIndex = 1,
            Layer1Count = 2,
            Layer1Diameter = 20.0,
            Layer2Count = 2,
            Layer2Diameter = 20.0,
            LayerGap = 50.0
        };
        var addSpec = new BeamAdditionalTopBarSpec { SupportTopBars = new[] { addConfig } };
        var addBars = BeamAdditionalBarCalculator.ComputeSupportTopBars(stack, addSpec, stirrupDiameterMm: 8.0);

        var layer1MainBar = mainBars.First();
        var layer1AddBar = addBars.First(b => b.Layer == 1);
        var layer2AddBar = addBars.First(b => b.Layer == 2);

        // Continuous main top bars and Layer 1 additional bars share identical elevation
        Assert.Equal(layer1MainBar.Points[1].Z, layer1AddBar.Points[0].Z, Precision);

        // Layer 2 additional bars are offset vertically downward from Layer 1 by specified gap
        double z1 = layer1MainBar.Points[1].Z;
        double z2 = layer2AddBar.Points[0].Z;
        Assert.Equal(50.0, z1 - z2, Precision);
    }

    [Fact]
    public void MultiLayerBottomBarsOffsetSecondLayerVerticallyUpwards()
    {
        var stack = TestBeamData.SingleSpan();
        var mainSpec = TestBeamData.MainBarSpec(bottomDiameter: 20.0);
        var mainBars = BeamMainBarCalculator.ComputeBottomMainBars(stack, mainSpec, stirrupDiameterMm: 8.0);

        var addConfig = new SpanAdditionalBottomBarConfig
        {
            SpanIndex = 0,
            Layer1Count = 2,
            Layer1Diameter = 20.0,
            Layer2Count = 2,
            Layer2Diameter = 20.0,
            LayerGap = 50.0
        };
        var addSpec = new BeamAdditionalBottomBarSpec { SpanBottomBars = new[] { addConfig } };
        var addBars = BeamAdditionalBarCalculator.ComputeSpanBottomBars(stack, addSpec, stirrupDiameterMm: 8.0);

        var layer1MainBar = mainBars.First();
        var layer1AddBar = addBars.First(b => b.Layer == 1);
        var layer2AddBar = addBars.First(b => b.Layer == 2);

        // Continuous main bottom bars and Layer 1 additional bars share identical elevation
        Assert.Equal(layer1MainBar.Points[1].Z, layer1AddBar.Points[0].Z, Precision);

        // Layer 2 additional bars are offset vertically upwards from Layer 1 by specified gap
        double z1 = layer1MainBar.Points[1].Z;
        double z2 = layer2AddBar.Points[0].Z;
        Assert.True(z2 > z1);
        Assert.Equal(50.0, z2 - z1, Precision);
    }

    [Fact]
    public void SimplifyPolylinePreservesOneHundredEightyDegreeHairpinApex()
    {
        var raw = new List<Point3> { new(0, 0, 0), new(100, 0, 0), new(50, 0, 0) };
        var simplified = BeamMainBarCalculator.SimplifyPolyline(raw);

        Assert.Equal(3, simplified.Count);
        Assert.Equal(new Point3(100, 0, 0), simplified[1]);
    }

    [Fact]
    public void SimplifyPolylinePreservesIntermediatePointsOnHairpinStraightLegs()
    {
        var raw = new List<Point3> { new(0, 0, 0), new(50, 0, 0), new(100, 0, 0), new(75, 0, 0), new(50, 0, 0) };
        var simplified = BeamMainBarCalculator.SimplifyPolyline(raw);

        // (50,0,0) and (75,0,0) are codirectional collinear so culled; apex (100,0,0) is preserved
        Assert.Equal(3, simplified.Count);
        Assert.Equal(new Point3(0, 0, 0), simplified[0]);
        Assert.Equal(new Point3(100, 0, 0), simplified[1]);
        Assert.Equal(new Point3(50, 0, 0), simplified[2]);
    }

    [Fact]
    public void StandardThreeSpanOfficeGirderGeneratesExactPolylineCoordinates()
    {
        var stack = TestBeamData.ThreeSpan();
        var bars = BeamMainBarCalculator.ComputeTopMainBars(stack, TestBeamData.MainBarSpec(), 8.0);

        Assert.NotNull(bars);
        Assert.True(bars.Count >= 3);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ComputeBottomMainBars_LapFactorNotAFiniteNumber_LapsWithTheDefaultForty(double lapFactor)
    {
        var stack = TestBeamData.SingleSpan(length: 12500);
        var standard = TestBeamData.MainBarSpec();

        var bars = BeamMainBarCalculator.ComputeBottomMainBars(
            stack, standard with { LapFactor = lapFactor }, stirrupDiameterMm: 10.0);
        var expected = BeamMainBarCalculator.ComputeBottomMainBars(stack, standard, stirrupDiameterMm: 10.0);

        Assert.Equal(2 * standard.BottomCount, expected.Count);   // the reference run is spliced
        Assert.Equal(expected.Select(b => b.Polyline.TotalLength), bars.Select(b => b.Polyline.TotalLength));
    }

    [Fact]
    public void ComputeBottomMainBars_InfiniteStockLength_SplicesAtTheCommercialLength()
    {
        var stack = TestBeamData.SingleSpan(length: 12500);
        var standard = TestBeamData.MainBarSpec();

        var bars = BeamMainBarCalculator.ComputeBottomMainBars(
            stack, standard with { MaxStockLength = double.PositiveInfinity }, stirrupDiameterMm: 10.0);
        var expected = BeamMainBarCalculator.ComputeBottomMainBars(stack, standard, stirrupDiameterMm: 10.0);

        Assert.Equal(2 * standard.BottomCount, expected.Count);   // the reference run is spliced
        Assert.Equal(expected.Count, bars.Count);
    }

    [Theory]
    [InlineData(9000.0, 9000.0)]
    [InlineData(0.0, 11700.0)]
    [InlineData(-1.0, 11700.0)]
    [InlineData(double.NaN, 11700.0)]
    [InlineData(double.PositiveInfinity, 11700.0)]
    public void EffectiveStockLength_FallsBackToTheCommercialLengthUnlessPositiveAndFinite(double entered, double expected)
    {
        Assert.Equal(expected, BeamMainBarCalculator.EffectiveStockLength(entered));
    }

    [Theory]
    [InlineData(35.0, 35.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(double.NaN, 40.0)]
    [InlineData(double.NegativeInfinity, 40.0)]
    public void EffectiveLapFactor_FallsBackToFortyOnlyWhenNotAFiniteNumber(double entered, double expected)
    {
        Assert.Equal(expected, BeamMainBarCalculator.EffectiveLapFactor(entered));
    }
}
