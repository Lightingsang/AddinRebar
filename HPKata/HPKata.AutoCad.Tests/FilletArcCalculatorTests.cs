using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using HPKata.AutoCad.Interop;
using Xunit;

namespace HPKata.AutoCad.Tests
{
    public class FilletArcCalculatorTests
    {
        [Fact]
        public void BulgeConstants_MatchAnalyticalFormulas()
        {
            Assert.Equal(FilletArcCalculator.Bulge90Deg, Math.Tan(Math.PI / 8.0), 6);
            Assert.Equal(FilletArcCalculator.Bulge180Deg, 1.0, 6);
            Assert.Equal(FilletArcCalculator.Bulge45Deg, Math.Tan((Math.PI / 4.0) / 4.0), 6);
            Assert.Equal(FilletArcCalculator.Bulge135Deg, Math.Tan((3.0 * Math.PI / 4.0) / 4.0), 6);
        }

        [Fact]
        public void ComputeTurn_LeftTurnCCW_ProducesPositiveCrossZ()
        {
            var p1 = new Point2d(0, 0);
            var p2 = new Point2d(100, 0);
            var p3 = new Point2d(100, 100);

            FilletArcCalculator.ComputeTurn(p1, p2, p3, out double turnRad, out double crossZ);

            Assert.True(crossZ > 0.0);
            Assert.Equal(Math.PI / 2.0, turnRad, 4);
        }

        [Fact]
        public void ComputeTurn_RightTurnCW_ProducesNegativeCrossZ()
        {
            var p1 = new Point2d(0, 0);
            var p2 = new Point2d(100, 0);
            var p3 = new Point2d(100, -100);

            FilletArcCalculator.ComputeTurn(p1, p2, p3, out double turnRad, out double crossZ);

            Assert.True(crossZ < 0.0);
            Assert.Equal(-Math.PI / 2.0, turnRad, 4);
        }

        [Fact]
        public void ClipTangent_WhenExceedsMax_ClipsToFactorTimesMinLength()
        {
            double tangentDist = 80.0;
            double len1 = 100.0;
            double len2 = 50.0;

            double clipped = FilletArcCalculator.ClipTangent(tangentDist, len1, len2, 0.45);

            // 0.45 * 50.0 = 22.5
            Assert.Equal(22.5, clipped, 4);
        }

        [Fact]
        public void ClipTangent_WhenWithinLimit_KeepsOriginalValue()
        {
            double tangentDist = 15.0;
            double len1 = 100.0;
            double len2 = 50.0;

            double clipped = FilletArcCalculator.ClipTangent(tangentDist, len1, len2, 0.45);

            Assert.Equal(15.0, clipped, 4);
        }

        [Fact]
        public void GenerateFilletedVertices_CollinearPoints_NoFilletBulges()
        {
            var points = new List<Point2d>
            {
                new(0, 0),
                new(50, 0),
                new(100, 0)
            };

            var vertices = FilletArcCalculator.GenerateFilletedVertices(points, bendRadius: 20.0);

            Assert.Equal(3, vertices.Count);
            Assert.All(vertices, v => Assert.Equal(0.0, v.Bulge, 5));
        }

        [Fact]
        public void GenerateFilletedVertices_LShapeCorner_GeneratesFilletVerticesWithBulge()
        {
            var points = new List<Point2d>
            {
                new(0, 0),
                new(500, 0),
                new(500, 500)
            };

            var vertices = FilletArcCalculator.GenerateFilletedVertices(points, bendRadius: 40.0);

            // Expect: start vertex + 2 corner tangent vertices + end vertex = 4 vertices
            Assert.Equal(4, vertices.Count);
            Assert.Equal(0.0, vertices[0].Bulge, 5);
            Assert.True(vertices[1].Bulge > 0.0);
            Assert.Equal(FilletArcCalculator.Bulge90Deg, vertices[1].Bulge, 4);
            Assert.Equal(0.0, vertices[2].Bulge, 5);
            Assert.Equal(0.0, vertices[3].Bulge, 5);
        }
    }
}
