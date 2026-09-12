using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.BeamRebar;

public sealed class BeamAdditionalBarCalculatorTests
{
    private const int Precision = 6;

    // Support Top Bars (Tier 1 & 2)
    [Fact]
    public void SupportTopBarsCenterOverInteriorColumnBetweenEqualSpans()
    {
        var stack = TestBeamData.TwoSpan(l1: 6000, l2: 6000, colWidth: 400);
        var config = new SupportAdditionalTopBarConfig
        {
            SupportIndex = 1,
            Layer1Count = 2,
            Layer1Diameter = 20.0,
            Layer1ExtensionRatio = 1.0 / 3.0
        };
        var spec = new BeamAdditionalTopBarSpec { SupportTopBars = new[] { config } };
        var bars = BeamAdditionalBarCalculator.ComputeSupportTopBars(stack, spec, 8.0);

        Assert.Equal(2, bars.Count);
        double midX = (bars[0].StartX + bars[0].EndX) / 2.0;
        Assert.Equal(stack.Supports[1].CenterX, midX, Precision);
    }

    [Fact]
    public void SupportTopBarsExtendL3IntoAdjacentSpansForLayerOne()
    {
        var stack = TestBeamData.TwoSpan(l1: 6000, l2: 6000, colWidth: 400); // clear = 5600 mm
        var config = new SupportAdditionalTopBarConfig
        {
            SupportIndex = 1,
            Layer1Count = 2,
            Layer1Diameter = 20.0,
            Layer1ExtensionRatio = 1.0 / 3.0
        };
        var spec = new BeamAdditionalTopBarSpec { SupportTopBars = new[] { config } };
        var bars = BeamAdditionalBarCalculator.ComputeSupportTopBars(stack, spec, 8.0);

        Assert.Equal(5600.0 / 3.0, bars[0].LeftExtension, Precision);
        Assert.Equal(5600.0 / 3.0, bars[0].RightExtension, Precision);
    }

    [Fact]
    public void SupportTopBarsExtendL4IntoAdjacentSpansForLayerTwo()
    {
        var stack = TestBeamData.TwoSpan(l1: 6000, l2: 6000, colWidth: 400);
        var config = new SupportAdditionalTopBarConfig
        {
            SupportIndex = 1,
            Layer1Count = 2,
            Layer1Diameter = 20.0,
            Layer1ExtensionRatio = 1.0 / 3.0,
            Layer2Count = 2,
            Layer2Diameter = 20.0,
            Layer2ExtensionRatio = 1.0 / 4.0
        };
        var spec = new BeamAdditionalTopBarSpec { SupportTopBars = new[] { config } };
        var bars = BeamAdditionalBarCalculator.ComputeSupportTopBars(stack, spec, 8.0);

        var layer2Bars = bars.Where(b => b.Layer == 2).ToList();
        Assert.Equal(2, layer2Bars.Count);
        Assert.Equal(5600.0 / 4.0, layer2Bars[0].LeftExtension, Precision);
    }

    [Fact]
    public void SupportTopBarsLayerTwoOffsetBelowLayerOneWithClearance()
    {
        var stack = TestBeamData.TwoSpan();
        var config = new SupportAdditionalTopBarConfig
        {
            SupportIndex = 1,
            Layer1Count = 2,
            Layer1Diameter = 20.0,
            Layer2Count = 2,
            Layer2Diameter = 20.0,
            LayerGap = 50.0
        };
        var spec = new BeamAdditionalTopBarSpec { SupportTopBars = new[] { config } };
        var bars = BeamAdditionalBarCalculator.ComputeSupportTopBars(stack, spec, 8.0);

        double z1 = bars.First(b => b.Layer == 1).Points[0].Z;
        double z2 = bars.First(b => b.Layer == 2).Points[0].Z;

        Assert.True(z1 - z2 >= 30.0);
        Assert.Equal(50.0, z1 - z2, Precision);
    }

    [Fact]
    public void ExteriorSupportTopBarsAnchorIntoEndColumnWithDownwardHook()
    {
        var stack = TestBeamData.SingleSpan(height: 600);
        var config = new SupportAdditionalTopBarConfig
        {
            SupportIndex = 0,
            Layer1Count = 2,
            Layer1Diameter = 20.0,
            Layer1ExtensionRatio = 1.0 / 3.0
        };
        var spec = new BeamAdditionalTopBarSpec { SupportTopBars = new[] { config } };
        var bars = BeamAdditionalBarCalculator.ComputeSupportTopBars(stack, spec, 8.0);

        Assert.Equal(2, bars.Count);
        var bar = bars[0];
        Assert.Equal(3, bar.Points.Count);
        // Hook tip Z < Corner Z
        Assert.True(bar.Points[0].Z < bar.Points[1].Z);
        Assert.Equal(HookAngle.Hook90, bar.StartHookAngle);
    }

    [Fact]
    public void UnequalAdjacentSpansExtendAsymmetricLengthsIntoRespectiveSpans()
    {
        // Span 1 clear = 5600 mm, Span 2 clear = 4600 mm
        var stack = TestBeamData.ThreeSpan(l1: 6000, l2: 5000, l3: 6000, colWidth: 400);
        var config = new SupportAdditionalTopBarConfig
        {
            SupportIndex = 1,
            Layer1Count = 2,
            Layer1Diameter = 20.0,
            Layer1ExtensionRatio = 1.0 / 3.0
        };
        var spec = new BeamAdditionalTopBarSpec { SupportTopBars = new[] { config } };
        var bars = BeamAdditionalBarCalculator.ComputeSupportTopBars(stack, spec, 8.0);

        var bar = bars[0];
        Assert.NotEqual(bar.LeftExtension, bar.RightExtension);
        Assert.Equal(5600.0 / 3.0, bar.LeftExtension, Precision);
        Assert.Equal(4600.0 / 3.0, bar.RightExtension, Precision);
    }

    // Midspan Bottom Bars (Tier 1 & 2)
    [Fact]
    public void MidspanBottomBarsStartAtOneSeventhClearSpanFromSupportFace()
    {
        var stack = TestBeamData.SingleSpan(length: 6000, leftCol: 400, rightCol: 400); // clear = 5600 mm
        var config = new SpanAdditionalBottomBarConfig
        {
            SpanIndex = 0,
            Layer1Count = 2,
            Layer1Diameter = 20.0,
            CutoffRatio = 1.0 / 7.0
        };
        var spec = new BeamAdditionalBottomBarSpec { SpanBottomBars = new[] { config } };
        var bars = BeamAdditionalBarCalculator.ComputeSpanBottomBars(stack, spec, 8.0);

        double expectedStartX = 200.0 + (5600.0 / 7.0); // left column face = 200 mm; 200 + 800 = 1000 mm
        Assert.Equal(expectedStartX, bars[0].StartX, Precision);
    }

    [Fact]
    public void MidspanBottomBarsEndAtOneSeventhClearSpanFromRightSupportFace()
    {
        var stack = TestBeamData.SingleSpan(length: 6000, leftCol: 400, rightCol: 400); // clear = 5600 mm
        var config = new SpanAdditionalBottomBarConfig
        {
            SpanIndex = 0,
            Layer1Count = 2,
            Layer1Diameter = 20.0,
            CutoffRatio = 1.0 / 7.0
        };
        var spec = new BeamAdditionalBottomBarSpec { SpanBottomBars = new[] { config } };
        var bars = BeamAdditionalBarCalculator.ComputeSpanBottomBars(stack, spec, 8.0);

        double expectedEndX = 5800.0 - (5600.0 / 7.0); // right column face = 5800 mm; 5800 - 800 = 5000 mm
        Assert.Equal(expectedEndX, bars[0].EndX, Precision);
    }

    [Fact]
    public void MidspanBottomBarsLayerTwoOffsetAboveLayerOneWithClearance()
    {
        var stack = TestBeamData.SingleSpan();
        var config = new SpanAdditionalBottomBarConfig
        {
            SpanIndex = 0,
            Layer1Count = 2,
            Layer1Diameter = 20.0,
            Layer2Count = 2,
            Layer2Diameter = 20.0,
            LayerGap = 50.0
        };
        var spec = new BeamAdditionalBottomBarSpec { SpanBottomBars = new[] { config } };
        var bars = BeamAdditionalBarCalculator.ComputeSpanBottomBars(stack, spec, 8.0);

        double z1 = bars.First(b => b.Layer == 1).Points[0].Z;
        double z2 = bars.First(b => b.Layer == 2).Points[0].Z;

        Assert.True(z2 > z1);
        Assert.Equal(50.0, z2 - z1, Precision);
    }

    [Fact]
    public void StraightMidspanBarsHaveExactlyTwoVertices()
    {
        var stack = TestBeamData.SingleSpan();
        var config = new SpanAdditionalBottomBarConfig
        {
            SpanIndex = 0,
            Layer1Count = 2,
            Layer1Diameter = 20.0
        };
        var spec = new BeamAdditionalBottomBarSpec { SpanBottomBars = new[] { config } };
        var bars = BeamAdditionalBarCalculator.ComputeSpanBottomBars(stack, spec, 8.0);

        Assert.All(bars, b => Assert.Equal(2, b.Points.Count));
    }

    // Boundaries & Combinations (Tier 2 & 3)
    [Fact]
    public void CantileverInteriorSupportAdditionExtendsFromCantileverIntoSpan()
    {
        var stack = TestBeamData.CantileverLeft();
        var config = new SupportAdditionalTopBarConfig
        {
            SupportIndex = 1,
            Layer1Count = 2,
            Layer1Diameter = 20.0,
            Layer1ExtensionRatio = 1.0 / 3.0
        };
        var spec = new BeamAdditionalTopBarSpec { SupportTopBars = new[] { config } };
        var bars = BeamAdditionalBarCalculator.ComputeSupportTopBars(stack, spec, 8.0);

        Assert.NotNull(bars);
        Assert.Equal(2, bars.Count);
    }

    [Fact]
    public void ZeroAdditionalBarsRequestedGeneratesEmptyCollection()
    {
        var stack = TestBeamData.SingleSpan();
        var specTop = new BeamAdditionalTopBarSpec();
        var specBot = new BeamAdditionalBottomBarSpec();

        Assert.Empty(BeamAdditionalBarCalculator.ComputeSupportTopBars(stack, specTop, 8.0));
        Assert.Empty(BeamAdditionalBarCalculator.ComputeSpanBottomBars(stack, specBot, 8.0));
    }

    [Fact]
    public void HighBarCountAutomaticallyDistributesExcessIntoSecondLayer()
    {
        var stack = TestBeamData.SingleSpan();
        var config = new SupportAdditionalTopBarConfig
        {
            SupportIndex = 0,
            Layer1Count = 3,
            Layer1Diameter = 20.0,
            Layer2Count = 2,
            Layer2Diameter = 20.0
        };
        var spec = new BeamAdditionalTopBarSpec { SupportTopBars = new[] { config } };
        var bars = BeamAdditionalBarCalculator.ComputeSupportTopBars(stack, spec, 8.0);

        Assert.Equal(3, bars.Count(b => b.Layer == 1));
        Assert.Equal(2, bars.Count(b => b.Layer == 2));
    }

    [Fact]
    public void FramingCaseASupportBarsMatchNominalCutLengths()
    {
        // Support 1: Column width = 400, left clear = 5600, right clear = 4600
        // Left ext = 5600/3 = 1866.667, Right ext = 4600/3 = 1533.333
        // Total = 1866.666667 + 400 + 1533.333333 = 3800.0 mm
        var stack = TestBeamData.ThreeSpan(l1: 6000, l2: 5000, l3: 6000, colWidth: 400);
        var config = new SupportAdditionalTopBarConfig
        {
            SupportIndex = 1,
            Layer1Count = 2,
            Layer1Diameter = 20.0,
            Layer1ExtensionRatio = 1.0 / 3.0
        };
        var spec = new BeamAdditionalTopBarSpec { SupportTopBars = new[] { config } };
        var bars = BeamAdditionalBarCalculator.ComputeSupportTopBars(stack, spec, 8.0);

        Assert.Equal(3800.0, bars[0].TotalLength, Precision);
    }

    [Fact]
    public void FramingCaseAMidspanBarsMatchNominalCutLengths()
    {
        // Span 1: Ln = 5600 mm. Length = 5600 * (1 - 2/7) = 4000.0 mm
        var stack = TestBeamData.ThreeSpan(l1: 6000, l2: 5000, l3: 6000, colWidth: 400);
        var config = new SpanAdditionalBottomBarConfig
        {
            SpanIndex = 0,
            Layer1Count = 2,
            Layer1Diameter = 20.0,
            CutoffRatio = 1.0 / 7.0
        };
        var spec = new BeamAdditionalBottomBarSpec { SpanBottomBars = new[] { config } };
        var bars = BeamAdditionalBarCalculator.ComputeSpanBottomBars(stack, spec, 8.0);

        Assert.Equal(4000.0, bars[0].TotalLength, Precision);
    }

    [Fact]
    public void TransversePositionsFitBetweenMainLongitudinalBars()
    {
        var stack = TestBeamData.SingleSpan(width: 300);
        var config = new SpanAdditionalBottomBarConfig
        {
            SpanIndex = 0,
            Layer1Count = 2,
            Layer1Diameter = 20.0
        };
        var spec = new BeamAdditionalBottomBarSpec { SpanBottomBars = new[] { config } };
        var bars = BeamAdditionalBarCalculator.ComputeSpanBottomBars(stack, spec, 8.0);

        double bound = 300.0 / 2.0;
        Assert.All(bars, b => Assert.InRange(b.TransverseY, -bound, bound));
    }

    [Fact]
    public void ExteriorSupportZeroGeneratesBothLayerOneAndLayerTwoBars()
    {
        var stack = TestBeamData.SingleSpan();
        var config = new SupportAdditionalTopBarConfig
        {
            SupportIndex = 0,
            Layer1Count = 3,
            Layer1Diameter = 20.0,
            Layer2Count = 2,
            Layer2Diameter = 20.0,
            LayerGap = 50.0
        };
        var spec = new BeamAdditionalTopBarSpec { SupportTopBars = new[] { config } };
        var bars = BeamAdditionalBarCalculator.ComputeSupportTopBars(stack, spec, 8.0);

        var layer1Bars = bars.Where(b => b.Layer == 1).ToList();
        var layer2Bars = bars.Where(b => b.Layer == 2).ToList();

        Assert.Equal(3, layer1Bars.Count);
        Assert.Equal(2, layer2Bars.Count);
        Assert.Equal(50.0, layer1Bars[0].Points[1].Z - layer2Bars[0].Points[1].Z, Precision);
        Assert.Equal(HookAngle.Hook90, layer1Bars[0].StartHookAngle);
        Assert.Equal(HookAngle.Hook90, layer2Bars[0].StartHookAngle);
        Assert.Equal(HookAngle.None, layer1Bars[0].EndHookAngle);
        Assert.Equal(HookAngle.None, layer2Bars[0].EndHookAngle);
    }

    [Fact]
    public void ExteriorSupportNGreatestSupportIndexGeneratesLayerTwoBars()
    {
        var stack = TestBeamData.TwoSpan();
        int lastSupportIdx = stack.Supports.Count - 1;
        var config = new SupportAdditionalTopBarConfig
        {
            SupportIndex = lastSupportIdx,
            Layer1Count = 2,
            Layer1Diameter = 20.0,
            Layer2Count = 2,
            Layer2Diameter = 20.0,
            LayerGap = 60.0
        };
        var spec = new BeamAdditionalTopBarSpec { SupportTopBars = new[] { config } };
        var bars = BeamAdditionalBarCalculator.ComputeSupportTopBars(stack, spec, 8.0);

        var layer1Bars = bars.Where(b => b.Layer == 1).ToList();
        var layer2Bars = bars.Where(b => b.Layer == 2).ToList();

        Assert.Equal(2, layer1Bars.Count);
        Assert.Equal(2, layer2Bars.Count);
        Assert.Equal(60.0, layer1Bars[0].Points[0].Z - layer2Bars[0].Points[0].Z, Precision);
        Assert.Equal(HookAngle.None, layer2Bars[0].StartHookAngle);
        Assert.Equal(HookAngle.Hook90, layer2Bars[0].EndHookAngle);
    }

    [Fact]
    public void LayerTwoHookLengthClampedToAvailableClearHeight()
    {
        // Shallow beam H = 350 mm, cover = 25 mm, stirrup = 8 mm => zBotFloor = 0 + 25 + 8 = 33 mm.
        var stack = TestBeamData.SingleSpan(height: 350);
        var config = new SupportAdditionalTopBarConfig
        {
            SupportIndex = 0,
            Layer1Count = 2,
            Layer1Diameter = 20.0,
            Layer2Count = 2,
            Layer2Diameter = 20.0,
            LayerGap = 50.0,
            ExteriorHookLength = 500.0 // deliberately exceeds available height
        };
        var spec = new BeamAdditionalTopBarSpec { SupportTopBars = new[] { config } };
        var bars = BeamAdditionalBarCalculator.ComputeSupportTopBars(stack, spec, 8.0);

        var layer2 = bars.First(b => b.Layer == 2);
        // Hook tip Z must not penetrate bottom cover
        double zBotFloor = stack.Spans[0].BottomElevation + 25.0 + 8.0;
        Assert.True(layer2.Points[0].Z >= zBotFloor);
    }
}
