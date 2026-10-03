using System.Collections.Generic;
using HPRebar.Core.BeamRebar;
using HPRebar.Core.BeamRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.BeamRebar;

public sealed class BeamGeometryPrimitivesTests
{
    private const int Precision = 9;

    [Fact]
    public void TotalLength_OpenPolyline_SumsSegmentsOnly()
    {
        var polyline = new Polyline3(new List<Point3> { new(0, 0, 0), new(3, 4, 0), new(3, 4, 10) });

        Assert.Equal(15.0, polyline.TotalLength, Precision);
    }

    [Fact]
    public void TotalLength_ClosedPolyline_AddsClosingSegment()
    {
        var square = new Polyline3(
            new List<Point3> { new(0, 0, 0), new(10, 0, 0), new(10, 10, 0), new(0, 10, 0) },
            isClosed: true);

        Assert.Equal(40.0, square.TotalLength, Precision);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void TotalLength_FewerThanTwoPoints_IsZero(int pointCount)
    {
        var points = new List<Point3>();
        for (int i = 0; i < pointCount; i++)
        {
            points.Add(new Point3(i, 0, 0));
        }

        Assert.Equal(0.0, new Polyline3(points).TotalLength, Precision);
    }

    [Fact]
    public void Simplify_VertexCloserThanMinimum_IsDropped()
    {
        var polyline = new Polyline3(new List<Point3> { new(0, 0, 0), new(0.4, 0, 0), new(100, 0, 0) });

        var simplified = polyline.Simplify(minSegmentLength: 1.0);

        Assert.Equal(new[] { new Point3(0, 0, 0), new Point3(100, 0, 0) }, simplified.Points);
    }

    [Fact]
    public void Simplify_ClosedPolylineWithShortClosingSegment_DropsLastVertex()
    {
        var ring = new Polyline3(
            new List<Point3> { new(0, 0, 0), new(100, 0, 0), new(100, 100, 0), new(0.5, 0, 0) },
            isClosed: true);

        var simplified = ring.Simplify(minSegmentLength: 1.0);

        Assert.Equal(3, simplified.Points.Count);
        Assert.True(simplified.IsClosed);
    }

    [Fact]
    public void Simplify_DoesNotChangeTheOriginalPolyline()
    {
        var polyline = new Polyline3(new List<Point3> { new(0, 0, 0), new(0.4, 0, 0), new(100, 0, 0) });

        polyline.Simplify();

        Assert.Equal(3, polyline.Points.Count);
    }

    [Fact]
    public void Translate_MovesEveryVertexByTheOffset()
    {
        var polyline = new Polyline3(new List<Point3> { new(0, 0, 0), new(10, 0, 0) });

        var moved = polyline.Translate(new Vector3(1, 2, 3));

        Assert.Equal(new[] { new Point3(1, 2, 3), new Point3(11, 2, 3) }, moved.Points);
    }

    [Fact]
    public void DistanceTo_ThreeFourFiveTriangle_ReturnsHypotenuse()
    {
        Assert.Equal(5.0, new Point3(0, 0, 0).DistanceTo(new Point3(3, 4, 0)), Precision);
    }

    [Fact]
    public void IsAlmostEqualTo_DifferenceWithinTolerance_IsTrue()
    {
        var point = new Point3(1, 2, 3);

        Assert.True(point.IsAlmostEqualTo(new Point3(1 + 1e-10, 2, 3)));
        Assert.False(point.IsAlmostEqualTo(new Point3(1.001, 2, 3)));
    }

    [Theory]
    [InlineData(1.0, 1.0 + 1e-10, true)]
    [InlineData(1.0, 1.0 + 1e-6, false)]
    public void AreEqual_DefaultTolerance_ComparesWithinOneNanometre(double first, double second, bool expected)
    {
        Assert.Equal(expected, Tolerance.AreEqual(first, second));
    }

    [Theory]
    [InlineData(1e-10, true)]
    [InlineData(-1e-10, true)]
    [InlineData(1e-6, false)]
    public void IsZero_DefaultTolerance_TreatsTinyValuesAsZero(double value, bool expected)
    {
        Assert.Equal(expected, Tolerance.IsZero(value));
    }

    [Fact]
    public void IsGreaterOrEqual_ValueJustBelowWithinTolerance_IsTrue()
    {
        Assert.True(Tolerance.IsGreaterOrEqual(1.0 - 1e-10, 1.0));
        Assert.False(Tolerance.IsGreaterOrEqual(0.999, 1.0));
    }

    [Fact]
    public void IsLessOrEqual_ValueJustAboveWithinTolerance_IsTrue()
    {
        Assert.True(Tolerance.IsLessOrEqual(1.0 + 1e-10, 1.0));
        Assert.False(Tolerance.IsLessOrEqual(1.001, 1.0));
    }

    [Fact]
    public void IsPositiveAndIsNegative_ValueInsideTolerance_AreBothFalse()
    {
        Assert.False(Tolerance.IsPositive(1e-10));
        Assert.False(Tolerance.IsNegative(-1e-10));
        Assert.True(Tolerance.IsPositive(1e-3));
        Assert.True(Tolerance.IsNegative(-1e-3));
    }
}
