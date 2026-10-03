using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.FoundationRebar.Calculators;
using HPRebar.Core.FoundationRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.FoundationRebar;

public sealed class FoundationPlanFrameCalculatorTests
{
    private const double BottomZ = -1500.0;

    [Fact]
    public void Compute_AxisAlignedRectangle_FramesItOnTheProjectAxes()
    {
        // Arrange
        var edges = Loop(At(1000, 500), At(4000, 500), At(4000, 2500), At(1000, 2500));

        // Act
        var frame = FoundationPlanFrameCalculator.Compute(edges, edges[2].Start, BottomZ);

        // Assert
        Assert.Equal(At(1000, 500), frame.Origin);
        Assert.Equal(Vector3.UnitX, frame.LocalX);
        Assert.Equal(Vector3.UnitY, frame.LocalY);
        Assert.Equal(3000.0, frame.Length, 6);
        Assert.Equal(2000.0, frame.Width, 6);
    }

    [Fact]
    public void Compute_RectangleTurnedThirtyDegrees_FollowsItsLongSide()
    {
        // Arrange: 4000 × 2500 footing turned 30° about its corner at (200, 300)
        var along = new Vector3(Math.Cos(Math.PI / 6), Math.Sin(Math.PI / 6), 0);
        var across = new Vector3(-along.Y, along.X, 0);
        var corner = At(200, 300);
        var far = corner + (along * 4000);
        var edges = Loop(corner, far, far + (across * 2500), corner + (across * 2500));

        // Act
        var frame = FoundationPlanFrameCalculator.Compute(edges, edges[1].Start, BottomZ);

        // Assert
        Assert.Equal(corner, frame.Origin);
        Assert.Equal(along, frame.LocalX);
        Assert.Equal(across, frame.LocalY);
        Assert.Equal(4000.0, frame.Length, 6);
        Assert.Equal(2500.0, frame.Width, 6);
    }

    [Fact]
    public void Compute_LShapedOutline_BoundsEveryCorner()
    {
        // Arrange: 6000 long leg along X, 1500 wide, with a 1500 × 4000 leg rising at its left end
        var edges = Loop(At(0, 0), At(6000, 0), At(6000, 1500), At(1500, 1500), At(1500, 4000), At(0, 4000));

        // Act
        var frame = FoundationPlanFrameCalculator.Compute(edges, edges[3].Start, BottomZ);

        // Assert
        Assert.Equal(At(0, 0), frame.Origin);
        Assert.Equal(6000.0, frame.Length, 6);
        Assert.Equal(4000.0, frame.Width, 6);
    }

    [Theory]
    [InlineData(5000, 0, 1, 0)]   // drawn towards −X → +X
    [InlineData(0, 5000, 0, 1)]   // drawn towards −Y → +Y
    public void DominantDirection_LongestEdgeDrawnBackwards_PointsToThePositiveAxis(
        double startX, double startY, double expectedX, double expectedY)
    {
        var edges = new[] { Straight(At(startX, startY), At(0, 0)), Straight(At(0, 0), At(300, 300)) };

        Assert.Equal(new Vector3(expectedX, expectedY, 0), FoundationPlanFrameCalculator.DominantDirection(edges));
    }

    [Fact]
    public void DominantDirection_LongerEdgeAfterShorterOnes_TakesTheLonger()
    {
        var edges = new[] { Straight(At(0, 0), At(1000, 0)), Straight(At(1000, 0), At(1000, 3000)) };

        Assert.Equal(Vector3.UnitY, FoundationPlanFrameCalculator.DominantDirection(edges));
    }

    /// <summary>Only exact ties are pinned: lengths a rounding step apart are not treated as equal.</summary>
    [Fact]
    public void DominantDirection_SquareFootprint_KeepsTheFirstOfTheEqualEdges()
    {
        var edges = Loop(At(0, 0), At(0, 2000), At(2000, 2000), At(2000, 0));

        Assert.Equal(Vector3.UnitY, FoundationPlanFrameCalculator.DominantDirection(edges));
    }

    [Fact]
    public void DominantDirection_LongSteepEdgeBeforeAShortHorizontalOne_TakesTheHorizontal()
    {
        var edges = new[] { Straight(At(0, 0), new Point3(1, 0, 5000)), Straight(At(0, 0), At(0, 1000)) };

        Assert.Equal(Vector3.UnitY, FoundationPlanFrameCalculator.DominantDirection(edges));
    }

    [Fact]
    public void DominantDirection_LongerCurvedEdge_IsIgnored()
    {
        var edges = new[] { Straight(At(0, 0), At(0, 1000)), Curved(At(0, 1000), At(3000, 1000), length: 9000) };

        Assert.Equal(Vector3.UnitY, FoundationPlanFrameCalculator.DominantDirection(edges));
    }

    [Fact]
    public void DominantDirection_OnlySteepOrCurvedEdges_FallsBackToPlusX()
    {
        var edges = new[] { Straight(At(0, 0), new Point3(1, 0, 1000)), Curved(At(0, 0), At(0, 3000), length: 3500) };

        Assert.Equal(Vector3.UnitX, FoundationPlanFrameCalculator.DominantDirection(edges));
    }

    [Fact]
    public void Fit_CornerTakesTheBottomLevelNotTheReferenceLevel()
    {
        var points = new List<Point3> { new(0, 0, 0), new(2000, 1000, 0) };

        var frame = FoundationPlanFrameCalculator.Fit(Vector3.UnitX, points, new Point3(500, 500, 300), BottomZ);

        Assert.Equal(At(0, 0), frame.Origin);
    }

    [Fact]
    public void Fit_DirectionNotUnitOrNotHorizontal_UsesItsHorizontalUnitDirection()
    {
        var points = new List<Point3> { At(0, 0), At(2000, 1000) };

        var frame = FoundationPlanFrameCalculator.Fit(new Vector3(2, 0, 3), points, At(0, 0), BottomZ);

        Assert.Equal(Vector3.UnitX, frame.LocalX);
        Assert.Equal(2000.0, frame.Length, 6);
        Assert.Equal(1000.0, frame.Width, 6);
    }

    [Fact]
    public void Fit_VerticalDirection_Throws()
    {
        var points = new List<Point3> { At(0, 0) };

        Assert.Throws<ArgumentException>(
            () => FoundationPlanFrameCalculator.Fit(Vector3.UnitZ, points, At(0, 0), BottomZ));
    }

    [Fact]
    public void Compute_NoEdges_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => FoundationPlanFrameCalculator.Compute(Array.Empty<FoundationOutlineEdge>(), Point3.Zero, BottomZ));
    }

    [Fact]
    public void Fit_NoPoints_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => FoundationPlanFrameCalculator.Fit(Vector3.UnitX, Array.Empty<Point3>(), Point3.Zero, BottomZ));
    }

    private static Point3 At(double x, double y) => new(x, y, BottomZ);

    private static FoundationOutlineEdge Straight(Point3 start, Point3 end) =>
        new(start, end, IsStraight: true, start.DistanceTo(end));

    private static FoundationOutlineEdge Curved(Point3 start, Point3 end, double length) =>
        new(start, end, IsStraight: false, length);

    /// <summary>Straight edges joining the corners in order and closing back to the first.</summary>
    private static FoundationOutlineEdge[] Loop(params Point3[] corners) =>
        corners.Select((corner, i) => Straight(corner, corners[(i + 1) % corners.Length])).ToArray();
}
