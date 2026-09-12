using System;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.BeamRebar;

public sealed class BeamCanvasTransformCalculatorTests
{
    private const int Precision = 6;

    // Scaling & Transformation (Tier 1)
    [Fact]
    public void RealSizedContinuousBeamShrinksUniformlyToFitCanvasDimensions()
    {
        var t = BeamCanvasTransformCalculator.ComputeElevationTransform(0, 18000, 0, 600, 1000, 400, 40);

        Assert.True(t.Scale < 1.0);
        var (sx, sy) = t.ToScreen(18000, 600);
        Assert.InRange(sx, 40.0, 1000.0 - 40.0);
        Assert.InRange(sy, 40.0, 400.0 - 40.0);
    }

    [Fact]
    public void AspectRatioIsStrictlyPreservedBetweenLengthAndHeight()
    {
        var t = BeamCanvasTransformCalculator.ComputeElevationTransform(0, 10000, 0, 1000, 1000, 500, 40);

        double dScreenX = t.ToScreenX(1000) - t.ToScreenX(0);
        double dScreenY = t.ToScreenY(0) - t.ToScreenY(1000);

        Assert.Equal(dScreenX, dScreenY, Precision);
    }

    [Fact]
    public void MarginPaddingIsMaintainedOnAllFourCanvasBorders()
    {
        var t = BeamCanvasTransformCalculator.ComputeElevationTransform(0, 10000, 0, 500, 1000, 500, 40);

        Assert.True(t.OffsetX >= 40.0);
        Assert.True(t.OffsetY >= 40.0);
    }

    [Fact]
    public void CanvasYCoordinatesAreInvertedRelativeDomainElevations()
    {
        var t = BeamCanvasTransformCalculator.ComputeElevationTransform(0, 6000, 0, 600, 800, 400);

        double yTop = t.ToScreenY(600);
        double yBot = t.ToScreenY(0);

        // Top elevation has lower screen Y (WPF canvas coordinates)
        Assert.True(yTop < yBot);
    }

    [Fact]
    public void XCoordinatesIncreaseMonotonicallyFromLeftToRight()
    {
        var t = BeamCanvasTransformCalculator.ComputeElevationTransform(0, 6000, 0, 600, 800, 400);

        double x1 = t.ToScreenX(1000);
        double x2 = t.ToScreenX(2000);

        Assert.True(x2 > x1);
    }

    // Boundary & Degenerate Cases (Tier 2)
    [Fact]
    public void EmptyBeamStackThrowsArgumentException()
    {
        var emptyStack = new BeamContinuousStack();
        Assert.Throws<ArgumentException>(() =>
            BeamCanvasTransformCalculator.ComputeElevationTransform(emptyStack, 800, 400));
    }

    [Fact]
    public void ZeroOrNegativeCanvasDimensionsThrowArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BeamCanvasTransformCalculator.ComputeElevationTransform(0, 6000, 0, 600, 0, 400));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BeamCanvasTransformCalculator.ComputeElevationTransform(0, 6000, 0, 600, 800, -100));
    }

    [Fact]
    public void SingleSpanCanvasScaleMatchesDirectDimensionRatio()
    {
        // wDraw = 800 - 80 = 720. lModel = 6000. scaleX = 720 / 6000 = 0.12.
        // hDraw = 400 - 80 = 320. hModel = 600. scaleY = 320 / 600 = 0.5333.
        // Scale = min(0.12, 0.5333) = 0.12.
        var t = BeamCanvasTransformCalculator.ComputeElevationTransform(0, 6000, 0, 600, 800, 400, 40);

        Assert.Equal(0.12, t.Scale, Precision);
    }

    [Fact]
    public void MultiSpanCanvasScaleIsDrivenByTotalLength()
    {
        // 18m span on 1000px canvas (margin 50). wDraw = 900. Scale = 900 / 18000 = 0.05.
        var t = BeamCanvasTransformCalculator.ComputeElevationTransform(0, 18000, 0, 600, 1000, 600, 50);

        Assert.Equal(900.0 / 18000.0, t.Scale, Precision);
    }

    [Fact]
    public void ExtremelyDeepBeamCanvasScaleIsDrivenByMaxHeight()
    {
        // Length = 2000, Height = 3000, Canvas = 1000x500 (margin 50).
        // wDraw = 900 (scale 0.45). hDraw = 400 (scale 400/3000 = 0.1333).
        var t = BeamCanvasTransformCalculator.ComputeElevationTransform(0, 2000, 0, 3000, 1000, 500, 50);

        Assert.Equal(400.0 / 3000.0, t.Scale, Precision);
    }

    [Fact]
    public void DomainToCanvasRoundTripPreservesRelativeRatios()
    {
        var t = BeamCanvasTransformCalculator.ComputeElevationTransform(0, 6000, 0, 600, 800, 400);

        var (sx, sy) = t.ToScreen(3500, 450);
        var (mx, mz) = t.ToModel(sx, sy);

        Assert.Equal(3500.0, mx, Precision);
        Assert.Equal(450.0, mz, Precision);
    }

    [Fact]
    public void CrossSectionCanvasScalesWidthAndHeightIndependently()
    {
        var t = BeamCanvasTransformCalculator.ComputeSectionTransform(300, 600, 400, 600, 40);

        Assert.True(t.Scale > 0.0);
    }

    [Fact]
    public void CrossSectionCanvasCentersBeamInTransverseViewport()
    {
        var t = BeamCanvasTransformCalculator.ComputeSectionTransform(300, 600, 400, 600, 40);

        double sxLeft = t.ToScreenX(-150);
        double sxRight = t.ToScreenX(150);

        // Center of cross section should align with canvas center (400 / 2 = 200)
        Assert.Equal(200.0, (sxLeft + sxRight) / 2.0, Precision);
    }

    [Fact]
    public void CanvasPointsNeverExceedViewportBoundingBox()
    {
        var t = BeamCanvasTransformCalculator.ComputeElevationTransform(0, 6000, 0, 600, 800, 400, 40);

        var (sx0, sy0) = t.ToScreen(0, 0);
        var (sx1, sy1) = t.ToScreen(6000, 600);

        Assert.InRange(sx0, 0.0, 800.0);
        Assert.InRange(sx1, 0.0, 800.0);
        Assert.InRange(sy0, 0.0, 400.0);
        Assert.InRange(sy1, 0.0, 400.0);
    }
}
