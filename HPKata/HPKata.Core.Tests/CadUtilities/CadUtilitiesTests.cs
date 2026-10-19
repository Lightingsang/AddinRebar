using System;
using System.Collections.Generic;
using HPKata.Core.CadUtilities.Calculators;
using HPKata.Core.CadUtilities.Models;
using Xunit;

namespace HPKata.Core.Tests.CadUtilities
{
    public class CadUtilitiesTests
    {
        [Fact]
        public void Point2D_DistanceAndVectorOperations()
        {
            var p1 = new Point2D(0, 0);
            var p2 = new Point2D(300, 400);

            Assert.Equal(500.0, p1.DistanceTo(p2));

            var vec = p2.Subtract(p1);
            Assert.Equal(300.0, vec.X);
            Assert.Equal(400.0, vec.Y);
            Assert.Equal(500.0, vec.Length);

            var p3 = p1.Add(vec);
            Assert.Equal(p2, p3);
        }

        [Fact]
        public void Vector2D_NormalizeAndNormal()
        {
            var v = new Vector2D(300, 400);
            var u = v.Normalize();
            Assert.Equal(0.6, Math.Round(u.X, 4));
            Assert.Equal(0.8, Math.Round(u.Y, 4));

            var n = u.Normal();
            Assert.Equal(-0.8, Math.Round(n.X, 4));
            Assert.Equal(0.6, Math.Round(n.Y, 4));
            Assert.Equal(0.0, Math.Round(u.Dot(n), 6)); // Perpendicular
        }

        [Fact]
        public void TrimWitnessLines_OffsetsEndpointsCorrectly()
        {
            var ext1 = new Point2D(0, 0);
            var ext2 = new Point2D(1000, 0);
            var dimLinePt = new Point2D(500, 200);

            var (new1, new2) = VectorProjectionHelper.TrimWitnessLines(ext1, ext2, dimLinePt, 50.0);

            // Witness line from y=200 down towards y=0 offset 50mm -> y=150
            Assert.Equal(0.0, Math.Round(new1.X, 2));
            Assert.Equal(150.0, Math.Round(new1.Y, 2));
            Assert.Equal(1000.0, Math.Round(new2.X, 2));
            Assert.Equal(150.0, Math.Round(new2.Y, 2));
        }

        [Fact]
        public void SlopeCalculator_StandardSlope_CalculatesPercentageAndRatio()
        {
            var p1 = new Point2D(0, 0);
            var p2 = new Point2D(1000, 20); // 20 / 1000 = 2%

            var slope = SlopeCalculator.Calculate(p1, p2);
            Assert.Equal(2.0, slope.GradientPercent);
            Assert.Equal("1:50", slope.RatioText);
            Assert.Equal("i = 2%", slope.FormattedText);
            Assert.False(slope.IsVertical);
        }

        [Fact]
        public void SlopeCalculator_VerticalLine_HandlesGracefully()
        {
            var p1 = new Point2D(100, 0);
            var p2 = new Point2D(100, 500);

            var slope = SlopeCalculator.Calculate(p1, p2);
            Assert.True(slope.IsVertical);
            Assert.Equal("Đứng", slope.FormattedText);
        }

        [Fact]
        public void SlopeCalculator_AntiUpsideDownTextAngle_FlipsQuadrants2And3()
        {
            // Pointing left and up: 135 deg (3pi/4 rad)
            double angle135 = 3.0 * Math.PI / 4.0;
            double flipped = SlopeCalculator.NormalizeTextAngle(angle135);

            // Should be flipped by 180 deg (pi) -> -45 deg or 315 deg (7pi/4)
            Assert.True(flipped < Math.PI / 2.0 || flipped > 3.0 * Math.PI / 2.0);
        }

        [Fact]
        public void ElevationFormatter_FormattingRules()
        {
            Assert.Equal("±0.000", ElevationFormatter.Format(0.0));
            Assert.Equal("±0.000", ElevationFormatter.Format(0.0002));
            Assert.Equal("%%p0.000", ElevationFormatter.Format(0.0, forAutoCad: true));
            Assert.Equal("+3.600", ElevationFormatter.Format(3.6));
            Assert.Equal("-1.250", ElevationFormatter.Format(-1.25));
        }

        [Fact]
        public void ElevationFormatter_Parse_ReadsFormattedElevation()
        {
            Assert.Equal(3.6, ElevationFormatter.Parse("+3.600"));
            Assert.Equal(-1.25, ElevationFormatter.Parse("-1.250"));
            Assert.Equal(0.0, ElevationFormatter.Parse("±0.000"));
            Assert.Equal(0.0, ElevationFormatter.Parse("%%p0.000"));
        }

        [Fact]
        public void TextAlignmentCalculator_AlignHorizontalLeft()
        {
            var pts = new List<Point2D>
            {
                new Point2D(100, 10),
                new Point2D(250, 20),
                new Point2D(180, 30)
            };

            var aligned = TextAlignmentCalculator.AlignHorizontal(pts, TextHorizontalAlignment.Left);
            Assert.All(aligned, p => Assert.Equal(100.0, p.X));
        }

        [Fact]
        public void TextAlignmentCalculator_DistributeVertically()
        {
            var pts = new List<Point2D>
            {
                new Point2D(50, 300),
                new Point2D(50, 100),
                new Point2D(50, 200)
            };

            var distributed = TextAlignmentCalculator.DistributeVertically(pts);
            Assert.Equal(300.0, distributed[0].Y);
            Assert.Equal(200.0, distributed[1].Y);
            Assert.Equal(100.0, distributed[2].Y);
        }
    }
}
