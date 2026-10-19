using System;
using System.Collections.Generic;
using HPDcePro.Core.Geometry;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public class AdvancedGeometryTests
    {
        [Fact]
        public void Plane3D_From3PointsAndDistance_ComputesCorrectly()
        {
            var p1 = new Point3D(0.0, 0.0, 0.0);
            var p2 = new Point3D(100.0, 0.0, 0.0);
            var p3 = new Point3D(0.0, 100.0, 0.0);

            var plane = Plane3D.From3Points(p1, p2, p3);
            Assert.Equal(0.0, plane.Normal.X, 4);
            Assert.Equal(0.0, plane.Normal.Y, 4);
            Assert.Equal(1.0, Math.Abs(plane.Normal.Z), 4);

            var testPt = new Point3D(50.0, 50.0, 250.0);
            Assert.Equal(250.0, plane.AbsoluteDistance(testPt), 4);

            bool intersects = plane.IntersectLine(
                new Point3D(50.0, 50.0, -100.0),
                new Vector3D(0.0, 0.0, 1.0),
                out Point3D inter);

            Assert.True(intersects);
            Assert.Equal(50.0, inter.X, 4);
            Assert.Equal(50.0, inter.Y, 4);
            Assert.Equal(0.0, inter.Z, 4);
        }

        [Fact]
        public void MathUtils_EquationSolversAndInterpolation_Work()
        {
            // 2x2: 2x + y = 5, x - y = 1 => x = 2, y = 1
            bool solved = MathUtils.Solve2x2(2.0, 1.0, 5.0, 1.0, -1.0, 1.0, out double x, out double y);
            Assert.True(solved);
            Assert.Equal(2.0, x, 4);
            Assert.Equal(1.0, y, 4);

            // Quadratic: x^2 - 5x + 6 = 0 => x = 2, 3
            int roots = MathUtils.SolveQuadratic(1.0, -5.0, 6.0, out double? r1, out double? r2);
            Assert.Equal(2, roots);
            Assert.True(Math.Abs(r1!.Value - 2.0) < 1e-4 || Math.Abs(r1.Value - 3.0) < 1e-4);

            // Linear interpolation
            double[] xs = new[] { 0.0, 10.0, 20.0 };
            double[] ys = new[] { 0.0, 100.0, 400.0 };
            double val = MathUtils.LinearInterpolate(xs, ys, 5.0);
            Assert.Equal(50.0, val, 4);
        }

        [Fact]
        public void FramingTopology_CircumcircleAndSorting_Work()
        {
            var p1 = new Point2D(0.0, 0.0);
            var p2 = new Point2D(4.0, 0.0);
            var p3 = new Point2D(0.0, 3.0);

            bool ok = FramingTopology.CalculateCircumcircle(p1, p2, p3, out Point2D center, out double radius);
            Assert.True(ok);
            Assert.Equal(2.0, center.X, 4);
            Assert.Equal(1.5, center.Y, 4);
            Assert.Equal(2.5, radius, 4);

            Assert.True(FramingTopology.InCircumcircle(p1, p2, p3, new Point2D(1.0, 1.0)));
            Assert.False(FramingTopology.InCircumcircle(p1, p2, p3, new Point2D(10.0, 10.0)));
        }

        [Fact]
        public void MathExpressionEvaluator_ComplexExpressions_EvaluatesCorrectly()
        {
            double res1 = MathExpressionEvaluator.Evaluate("100 + 50 * 2 - (30 / 3)");
            Assert.Equal(190.0, res1, 4);

            var vars = new Dictionary<string, double> { { "L", 6000.0 }, { "col", 400.0 } };
            double res2 = MathExpressionEvaluator.EvaluateWithContext("(L - col) / 4", vars);
            Assert.Equal(1400.0, res2, 4);

            double res3 = MathExpressionEvaluator.EvaluateParametric("200 + t * 50", 4.0);
            Assert.Equal(400.0, res3, 4);

            double res4 = MathExpressionEvaluator.Evaluate("sqrt(16) + abs(-10)");
            Assert.Equal(14.0, res4, 4);
        }
    }
}
