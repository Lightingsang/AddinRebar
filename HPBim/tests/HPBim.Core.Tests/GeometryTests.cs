namespace HPBim.Core.Tests;

using System;
using System.Collections.Generic;
using HPBim.Core.Algorithms;
using HPBim.Core.Geometry;
using HPBim.Core.Models;
using Xunit;

public sealed class GeometryTests
{
    [Theory]
    [InlineData(1.0, 1.0, 1.0)]
    [InlineData(5.5, 2.5, 13.75)]
    [InlineData(10.0, 20.0, 200.0)]
    [InlineData(100.0, 50.0, 5000.0)]
    public void Shoelace_Rectangles_CalculatesExactArea(double width, double height, double expectedArea)
    {
        var poly = new Polygon2D([
            new Point2D(0, 0),
            new Point2D(width, 0),
            new Point2D(width, height),
            new Point2D(0, height)
        ]);

        Assert.Equal(expectedArea, poly.Area, precision: 6);
    }

    [Fact]
    public void Shoelace_TriangleAndTrapezoid_CalculatesExactArea()
    {
        var rightTriangle = new Polygon2D([
            new Point2D(0, 0),
            new Point2D(6, 0),
            new Point2D(0, 8)
        ]);
        Assert.Equal(24.0, rightTriangle.Area, precision: 6);

        var trapezoid = new Polygon2D([
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(7, 4),
            new Point2D(3, 4)
        ]);
        // Area = (10 + 4) * 4 / 2 = 28
        Assert.Equal(28.0, trapezoid.Area, precision: 6);
    }

    [Fact]
    public void Polygon2D_SignedAreaAndOrientation_DistinguishesCcwAndCw()
    {
        var ccw = new Polygon2D([
            new Point2D(0, 0),
            new Point2D(4, 0),
            new Point2D(4, 3),
            new Point2D(0, 3)
        ]);
        Assert.True(ccw.SignedArea > 0);
        Assert.True(ccw.IsCounterClockwise);
        Assert.False(ccw.IsClockwise);

        var cw = new Polygon2D([
            new Point2D(0, 0),
            new Point2D(0, 3),
            new Point2D(4, 3),
            new Point2D(4, 0)
        ]);
        Assert.True(cw.SignedArea < 0);
        Assert.False(cw.IsCounterClockwise);
        Assert.True(cw.IsClockwise);
        Assert.Equal(ccw.Area, cw.Area, precision: 6);
    }

    [Fact]
    public void Polygon2D_Centroid_CalculatesAccurately()
    {
        var rect = new Polygon2D([
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 6),
            new Point2D(0, 6)
        ]);
        Assert.Equal(5.0, rect.Centroid.X, precision: 4);
        Assert.Equal(3.0, rect.Centroid.Y, precision: 4);

        var triangle = new Polygon2D([
            new Point2D(0, 0),
            new Point2D(6, 0),
            new Point2D(0, 6)
        ]);
        Assert.Equal(2.0, triangle.Centroid.X, precision: 4);
        Assert.Equal(2.0, triangle.Centroid.Y, precision: 4);
    }

    [Fact]
    public void Polygon2D_Perimeter_CalculatesAccurately()
    {
        var rect = new Polygon2D([
            new Point2D(0, 0),
            new Point2D(5, 0),
            new Point2D(5, 12),
            new Point2D(0, 12)
        ]);
        Assert.Equal(34.0, rect.Perimeter, precision: 6);

        var tri = new Polygon2D([
            new Point2D(0, 0),
            new Point2D(3, 0),
            new Point2D(0, 4)
        ]);
        Assert.Equal(12.0, tri.Perimeter, precision: 6);
    }

    [Fact]
    public void Polygon2D_Containment_RayCastingAndBoundary()
    {
        var poly = new Polygon2D([
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10),
            new Point2D(5, 5),
            new Point2D(0, 10)
        ]);

        Assert.True(poly.Contains(new Point2D(2, 2)));
        Assert.True(poly.Contains(new Point2D(8, 2)));
        Assert.False(poly.Contains(new Point2D(5, 7))); // In the upper V notch
        Assert.False(poly.Contains(new Point2D(-1, 5)));
        Assert.False(poly.Contains(new Point2D(12, 5)));

        Assert.True(poly.Contains(new Point2D(5, 0), includeBoundary: true));
        Assert.False(poly.Contains(new Point2D(5, 0), includeBoundary: false));
    }

    [Fact]
    public void LiangBarsky_Clipping_HandlesAllScenarios()
    {
        var box = new BoundingBox2D(new Point2D(0, 0), new Point2D(10, 10));

        // Inside segment
        Assert.True(LiangBarskyClipping.Clip(
            new Point2D(2, 2), new Point2D(8, 8), box,
            out Point2D cp0, out Point2D cp1));
        Assert.Equal(new Point2D(2, 2), cp0);
        Assert.Equal(new Point2D(8, 8), cp1);

        // Outside segments (cardinal directions)
        Assert.False(LiangBarskyClipping.Intersects(new Point2D(-10, 5), new Point2D(-1, 5), box));
        Assert.False(LiangBarskyClipping.Intersects(new Point2D(11, 5), new Point2D(20, 5), box));
        Assert.False(LiangBarskyClipping.Intersects(new Point2D(5, -10), new Point2D(5, -1), box));
        Assert.False(LiangBarskyClipping.Intersects(new Point2D(5, 11), new Point2D(5, 20), box));

        // Piercing horizontal segment
        Assert.True(LiangBarskyClipping.Clip(
            new Point2D(-5, 4), new Point2D(15, 4), box,
            out Point2D hp0, out Point2D hp1));
        Assert.Equal(0.0, hp0.X, precision: 4);
        Assert.Equal(4.0, hp0.Y, precision: 4);
        Assert.Equal(10.0, hp1.X, precision: 4);
        Assert.Equal(4.0, hp1.Y, precision: 4);

        // Piercing vertical segment
        Assert.True(LiangBarskyClipping.Clip(
            new Point2D(3, -5), new Point2D(3, 15), box,
            out Point2D vp0, out Point2D vp1));
        Assert.Equal(3.0, vp0.X, precision: 4);
        Assert.Equal(0.0, vp0.Y, precision: 4);
        Assert.Equal(3.0, vp1.X, precision: 4);
        Assert.Equal(10.0, vp1.Y, precision: 4);
    }

    [Fact]
    public void Vector2D_And_Point2D_Operations()
    {
        var p1 = new Point2D(1, 2);
        var p2 = new Point2D(4, 6);
        Assert.Equal(5.0, p1.DistanceTo(p2));
        Assert.Equal(25.0, p1.DistanceSquaredTo(p2));

        var v1 = new Vector2D(3, 4);
        Assert.Equal(5.0, v1.Length);
        var norm = v1.Normalize();
        Assert.Equal(0.6, norm.X, precision: 4);
        Assert.Equal(0.8, norm.Y, precision: 4);

        var leftNormal = v1.LeftNormal;
        Assert.Equal(0.0, v1.Dot(leftNormal), precision: 6);
        var rightNormal = v1.RightNormal;
        Assert.Equal(0.0, v1.Dot(rightNormal), precision: 6);

        var v2 = new Vector2D(-4, 3);
        Assert.Equal(0.0, v1.Dot(v2), precision: 6); // Orthogonal
    }

    [Fact]
    public void BoundingBox2D_IntersectionsAndUnions()
    {
        var b1 = new BoundingBox2D(0, 0, 10, 10);
        var b2 = new BoundingBox2D(5, 5, 15, 15);
        var b3 = new BoundingBox2D(20, 20, 30, 30);

        Assert.True(b1.Intersects(b2));
        Assert.False(b1.Intersects(b3));

        var union = b1.Union(b2);
        Assert.Equal(0.0, union.MinX);
        Assert.Equal(0.0, union.MinY);
        Assert.Equal(15.0, union.MaxX);
        Assert.Equal(15.0, union.MaxY);

        var inflated = b1.Inflate(2.0, 2.0);
        Assert.Equal(-2.0, inflated.MinX);
        Assert.Equal(-2.0, inflated.MinY);
        Assert.Equal(12.0, inflated.MaxX);
        Assert.Equal(12.0, inflated.MaxY);
    }

    [Fact]
    public void RoomAreaCalculator_CalculatesAndValidates()
    {
        var calc = new RoomAreaCalculator();
        var poly = new Polygon2D([
            new Point2D(0, 0),
            new Point2D(5000, 0),
            new Point2D(5000, 4000),
            new Point2D(0, 4000)
        ]);

        double areaM2 = calc.ComputeAreaM2(poly);
        Assert.Equal(20.0, areaM2, precision: 4);
        Assert.True(calc.IsEnclosed(poly));

        var room = new RoomDef
        {
            Name = "Phong test",
            TagLocation = new Point2D(2500, 2000),
            TheoreticalAreaM2 = 20.0,
            BoundaryPolygon = poly
        };

        var result = calc.ValidateRoom(room, areaToleranceRatio: 0.05);
        Assert.True(result.IsValid);
        Assert.True(result.IsEnclosed);
        Assert.True(result.IsTagInside);
        Assert.Null(result.Message);

        var outsideTagRoom = room with { TagLocation = new Point2D(6000, 2000) };
        var outsideResult = calc.ValidateRoom(outsideTagRoom);
        Assert.False(outsideResult.IsValid);
        Assert.False(outsideResult.IsTagInside);
    }
}
