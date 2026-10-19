namespace HPBim.Core.Tests;

using System;
using HPBim.Core.Geometry;
using Xunit;

public sealed class PolygonAndClippingStressTests
{
    [Fact]
    public void LiangBarsky_LineCompletelyInside_PreservesEndpoints()
    {
        var box = new BoundingBox2D(0, 0, 1000, 1000);
        var p0 = new Point2D(200, 200);
        var p1 = new Point2D(800, 800);

        bool intersects = LiangBarskyClipping.Clip(p0, p1, box, out var c0, out var c1);

        Assert.True(intersects);
        Assert.True(p0.IsAlmostEqualTo(c0));
        Assert.True(p1.IsAlmostEqualTo(c1));
    }

    [Theory]
    [InlineData(-500, 500, -100, 500)]   // Completely left
    [InlineData(1100, 500, 1500, 500)]   // Completely right
    [InlineData(500, -500, 500, -100)]   // Completely below
    [InlineData(500, 1100, 500, 1500)]   // Completely above
    [InlineData(-200, -200, -50, -50)]   // Diagonal bottom-left
    [InlineData(1200, 1200, 1500, 1500)] // Diagonal top-right
    [InlineData(-200, 1200, -50, 1500)]  // Diagonal top-left
    [InlineData(1200, -200, 1500, -50)]  // Diagonal bottom-right
    public void LiangBarsky_LineCompletelyOutside_ReturnsFalse(
        double x0, double y0, double x1, double y1)
    {
        var box = new BoundingBox2D(0, 0, 1000, 1000);
        var p0 = new Point2D(x0, y0);
        var p1 = new Point2D(x1, y1);

        bool intersects = LiangBarskyClipping.Intersects(p0, p1, box);
        Assert.False(intersects);
    }

    [Fact]
    public void LiangBarsky_PiercingLines_ClipsToExactBoxBoundaries()
    {
        var box = new BoundingBox2D(0, 0, 1000, 1000);

        // 1. Horizontal pierce from (-500, 500) to (1500, 500)
        bool hOk = LiangBarskyClipping.Clip(
            new Point2D(-500, 500), new Point2D(1500, 500), box, out var h0, out var h1);
        Assert.True(hOk);
        Assert.Equal(0.0, h0.X, precision: 4);
        Assert.Equal(500.0, h0.Y, precision: 4);
        Assert.Equal(1000.0, h1.X, precision: 4);
        Assert.Equal(500.0, h1.Y, precision: 4);

        // 2. Vertical pierce from (500, -500) to (500, 1500)
        bool vOk = LiangBarskyClipping.Clip(
            new Point2D(500, -500), new Point2D(500, 1500), box, out var v0, out var v1);
        Assert.True(vOk);
        Assert.Equal(500.0, v0.X, precision: 4);
        Assert.Equal(0.0, v0.Y, precision: 4);
        Assert.Equal(500.0, v1.X, precision: 4);
        Assert.Equal(1000.0, v1.Y, precision: 4);

        // 3. Diagonal pierce from (-200, -200) to (1200, 1200)
        bool dOk = LiangBarskyClipping.Clip(
            new Point2D(-200, -200), new Point2D(1200, 1200), box, out var d0, out var d1);
        Assert.True(dOk);
        Assert.Equal(0.0, d0.X, precision: 4);
        Assert.Equal(0.0, d0.Y, precision: 4);
        Assert.Equal(1000.0, d1.X, precision: 4);
        Assert.Equal(1000.0, d1.Y, precision: 4);
    }

    [Fact]
    public void LiangBarsky_CollinearWithEdges_And_DegeneratePoints()
    {
        var box = new BoundingBox2D(0, 0, 1000, 1000);

        // Line exactly on left edge: X = 0, Y from 200 to 800
        bool leftEdge = LiangBarskyClipping.Clip(
            new Point2D(0, 200), new Point2D(0, 800), box, out var le0, out var le1);
        Assert.True(leftEdge);
        Assert.Equal(0.0, le0.X, precision: 4);
        Assert.Equal(200.0, le0.Y, precision: 4);

        // Degenerate point inside box: p0 == p1 == (500, 500)
        bool pointInside = LiangBarskyClipping.Intersects(
            new Point2D(500, 500), new Point2D(500, 500), box);
        Assert.True(pointInside);

        // Degenerate point outside box: p0 == p1 == (-50, 500)
        bool pointOutside = LiangBarskyClipping.Intersects(
            new Point2D(-50, 500), new Point2D(-50, 500), box);
        Assert.False(pointOutside);
    }

    [Fact]
    public void LiangBarsky_NegativeAndExtremeCoordinates()
    {
        var box = new BoundingBox2D(-5000, -3000, -1000, -1000);
        var p0 = new Point2D(-6000, -2000);
        var p1 = new Point2D(0, -2000);

        bool ok = LiangBarskyClipping.Clip(p0, p1, box, out var c0, out var c1);
        Assert.True(ok);
        Assert.Equal(-5000.0, c0.X, precision: 3);
        Assert.Equal(-1000.0, c1.X, precision: 3);
    }

    [Fact]
    public void Polygon2D_AreaAndOrientation_ShoelaceExact()
    {
        // 1. Rectangle 4000 x 3000 (Area = 12,000,000 mm^2)
        var ccwVertices = new[]
        {
            new Point2D(0, 0),
            new Point2D(4000, 0),
            new Point2D(4000, 3000),
            new Point2D(0, 3000)
        };
        var ccwPoly = new Polygon2D(ccwVertices);

        Assert.Equal(12_000_000.0, ccwPoly.Area, precision: 4);
        Assert.Equal(12_000_000.0, ccwPoly.SignedArea, precision: 4);
        Assert.True(ccwPoly.IsCounterClockwise);
        Assert.False(ccwPoly.IsClockwise);
        Assert.Equal(14000.0, ccwPoly.Perimeter, precision: 4);

        // 2. Clockwise rectangle (reversed vertices)
        var cwVertices = new[]
        {
            new Point2D(0, 0),
            new Point2D(0, 3000),
            new Point2D(4000, 3000),
            new Point2D(4000, 0)
        };
        var cwPoly = new Polygon2D(cwVertices);

        Assert.Equal(12_000_000.0, cwPoly.Area, precision: 4);
        Assert.Equal(-12_000_000.0, cwPoly.SignedArea, precision: 4);
        Assert.False(cwPoly.IsCounterClockwise);
        Assert.True(cwPoly.IsClockwise);

        // 3. Right triangle: Base 3000, Height 4000 (Area = 6,000,000)
        var triPoly = new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(3000, 0),
            new Point2D(0, 4000)
        });
        Assert.Equal(6_000_000.0, triPoly.Area, precision: 4);
    }

    [Fact]
    public void Polygon2D_Centroid_SymmetricAndTriangle()
    {
        // Symmetric rectangle centered at origin
        var rect = new Polygon2D(new[]
        {
            new Point2D(-2000, -1000),
            new Point2D(2000, -1000),
            new Point2D(2000, 1000),
            new Point2D(-2000, 1000)
        });
        Assert.Equal(0.0, rect.Centroid.X, precision: 4);
        Assert.Equal(0.0, rect.Centroid.Y, precision: 4);

        // Triangle (0,0), (6000, 0), (0, 6000) -> centroid is (2000, 2000)
        var tri = new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(6000, 0),
            new Point2D(0, 6000)
        });
        Assert.Equal(2000.0, tri.Centroid.X, precision: 4);
        Assert.Equal(2000.0, tri.Centroid.Y, precision: 4);
    }

    [Fact]
    public void Polygon2D_Containment_InsideOutsideBoundaryAndNotch()
    {
        // L-shaped concave polygon:
        // (0,0) -> (4000,0) -> (4000, 2000) -> (2000, 2000) -> (2000, 4000) -> (0, 4000)
        var lShape = new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(4000, 0),
            new Point2D(4000, 2000),
            new Point2D(2000, 2000),
            new Point2D(2000, 4000),
            new Point2D(0, 4000)
        });

        // 1. Strictly inside
        Assert.True(lShape.Contains(new Point2D(1000, 1000)));
        Assert.True(lShape.Contains(new Point2D(3000, 1000)));
        Assert.True(lShape.Contains(new Point2D(1000, 3000)));

        // 2. The "Notch" - inside the AABB [0..4000, 0..4000] but OUTSIDE the L-shape
        Assert.False(lShape.Contains(new Point2D(3000, 3000)));
        Assert.False(lShape.Contains(new Point2D(2500, 2500)));

        // 3. Strictly outside AABB
        Assert.False(lShape.Contains(new Point2D(-500, 1000)));
        Assert.False(lShape.Contains(new Point2D(5000, 5000)));

        // 4. Exact vertex boundary
        var vertex = new Point2D(2000, 2000);
        Assert.True(lShape.Contains(vertex, includeBoundary: true));
        Assert.False(lShape.Contains(vertex, includeBoundary: false));

        // 5. Exact edge boundary
        var edgePt = new Point2D(2000, 1000); // On bottom arm
        Assert.True(lShape.Contains(edgePt, includeBoundary: true));
    }

    [Fact]
    public void Polygon2D_DuplicateClosingVertex_PrunedInConstructor()
    {
        // GeoJSON standard provides duplicate closing vertex: 5 vertices for a quadrilateral
        var verticesWithClosingDuplicate = new[]
        {
            new Point2D(0, 0),
            new Point2D(4000, 0),
            new Point2D(4000, 3000),
            new Point2D(0, 3000),
            new Point2D(0, 0) // Duplicate closing vertex
        };

        var poly = new Polygon2D(verticesWithClosingDuplicate);
        Assert.Equal(4, poly.VertexCount);
        Assert.Equal(12_000_000.0, poly.Area, precision: 4);
    }

    [Fact]
    public void Polygon2D_DegenerateInputs_ThrowExpectedExceptions()
    {
        Assert.Throws<ArgumentNullException>(() => new Polygon2D((Point2D[])null!));

        // Less than 3 vertices
        Assert.Throws<ArgumentException>(() => new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(100, 100)
        }));

        // Empty polygon
        var empty = Polygon2D.Empty;
        Assert.Equal(0, empty.VertexCount);
        Assert.Equal(0.0, empty.Area);
        Assert.Equal(0.0, empty.Perimeter);
        Assert.Equal(Point2D.Zero, empty.Centroid);
        Assert.False(empty.Contains(new Point2D(10, 10)));
    }
}
