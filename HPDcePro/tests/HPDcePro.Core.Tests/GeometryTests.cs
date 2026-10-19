using System;
using System.Collections.Generic;
using HPDcePro.Core.Geometry;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public class GeometryTests
    {
        [Fact]
        public void Point2D_DistanceAndOffsets_BehaveCorrectly()
        {
            var p1 = new Point2D(0.0, 0.0);
            var p2 = new Point2D(300.0, 400.0);

            Assert.Equal(500.0, p1.DistanceTo(p2), 4);
            Assert.Equal(250000.0, p1.DistanceToSquared(p2), 4);

            var mid = p1.MidPoint(p2);
            Assert.Equal(150.0, mid.X, 4);
            Assert.Equal(200.0, mid.Y, 4);

            var offset = p1.Offset(50.0, -20.0);
            Assert.Equal(50.0, offset.X, 4);
            Assert.Equal(-20.0, offset.Y, 4);

            var polar = p1.Polar(100.0, 0.0);
            Assert.Equal(100.0, polar.X, 4);
            Assert.Equal(0.0, polar.Y, 4);
        }

        [Fact]
        public void Vector2D_Operations_BehaveCorrectly()
        {
            var v1 = new Vector2D(3.0, 4.0);
            Assert.Equal(5.0, v1.Length, 4);

            var norm = v1.Normalize();
            Assert.Equal(0.6, norm.X, 4);
            Assert.Equal(0.8, norm.Y, 4);

            var v2 = new Vector2D(-4.0, 3.0);
            Assert.Equal(0.0, v1.Dot(v2), 4);
            Assert.Equal(Math.PI * 0.5, v1.AngleBetween(v2), 4);

            var perp = v1.Perpendicular();
            Assert.Equal(-4.0, perp.X, 4);
            Assert.Equal(3.0, perp.Y, 4);
        }

        [Fact]
        public void Segment2D_IntersectionAndContainment_BehaveCorrectly()
        {
            var s1 = new Segment2D(0.0, 0.0, 1000.0, 1000.0);
            var s2 = new Segment2D(0.0, 1000.0, 1000.0, 0.0);

            bool intersects = s1.Intersect(s2, out Point2D pt);
            Assert.True(intersects);
            Assert.Equal(500.0, pt.X, 4);
            Assert.Equal(500.0, pt.Y, 4);

            Assert.True(s1.ContainsPointOnLine(new Point2D(500.0, 500.0)));
            Assert.False(s1.ContainsPointOnLine(new Point2D(500.0, 600.0)));
        }

        [Fact]
        public void Polygon2D_ShoelaceAreaAndCentroid_AreExact()
        {
            var box = new Polygon2D(new[]
            {
                new Point2D(0.0, 0.0),
                new Point2D(1000.0, 0.0),
                new Point2D(1000.0, 500.0),
                new Point2D(0.0, 500.0)
            });

            Assert.Equal(500000.0, box.Area, 4);
            Assert.Equal(3000.0, box.Perimeter, 4);
            Assert.Equal(500.0, box.Centroid.X, 4);
            Assert.Equal(250.0, box.Centroid.Y, 4);
            Assert.False(box.IsClockwise);

            Assert.True(box.ContainsPoint(new Point2D(500.0, 250.0)));
            Assert.False(box.ContainsPoint(new Point2D(1500.0, 250.0)));
            Assert.True(box.IsPointOnBoundary(new Point2D(500.0, 0.0)));
        }

        [Fact]
        public void Polygon2D_ClipSegmentLength_CalculatesCorrectly()
        {
            var box = new Polygon2D(new[]
            {
                new Point2D(0.0, 0.0),
                new Point2D(1000.0, 0.0),
                new Point2D(1000.0, 1000.0),
                new Point2D(0.0, 1000.0)
            });

            // Segment passing through middle: from (-500, 500) to (1500, 500)
            var seg = new Segment2D(-500.0, 500.0, 1500.0, 500.0);
            double clipped = box.ClipSegmentLength(seg);
            Assert.Equal(1000.0, clipped, 2);
        }

        [Fact]
        public void ConvexHull2D_MonotoneChain_BuildsCorrectEnvelope()
        {
            var pts = new[]
            {
                new Point2D(0.0, 0.0),
                new Point2D(100.0, 0.0),
                new Point2D(100.0, 100.0),
                new Point2D(0.0, 100.0),
                new Point2D(50.0, 50.0), // Interior point
                new Point2D(25.0, 75.0)  // Interior point
            };

            var hull = ConvexHull2D.Compute(pts);
            Assert.Equal(4, hull.VertexCount);
            Assert.Equal(10000.0, hull.Area, 2);
        }

        [Fact]
        public void SpatialGrid2D_FindNearestAndRadius_Works()
        {
            var grid = new SpatialGrid2D<string>(200.0);
            grid.InsertPoint(new Point2D(100.0, 100.0), "A");
            grid.InsertPoint(new Point2D(500.0, 500.0), "B");
            grid.InsertPoint(new Point2D(1000.0, 1000.0), "C");

            bool found = grid.FindNearest(new Point2D(120.0, 110.0), 100.0, out string? item, out double dist);
            Assert.True(found);
            Assert.Equal("A", item);

            var inRadius = new List<string>(grid.QueryRadius(new Point2D(100.0, 100.0), 300.0));
            Assert.Single(inRadius);
            Assert.Contains("A", inRadius);
        }
    }
}
