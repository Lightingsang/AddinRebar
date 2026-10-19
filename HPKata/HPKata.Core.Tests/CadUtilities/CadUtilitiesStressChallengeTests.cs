using System;
using HPKata.Core.CadUtilities.Calculators;
using HPKata.Core.CadUtilities.Models;
using Xunit;

namespace HPKata.Core.Tests.CadUtilities
{
    public class CadUtilitiesStressChallengeTests
    {
        [Fact]
        public void Slope_Full360Circle_TextAngleAlwaysInQuadrants1Or4()
        {
            // For every angle from 0 to 359 degrees, text angle must NEVER be upside-down
            // Upside-down means pointing into Quadrant 2 or 3: (pi/2, 3pi/2]
            for (int deg = 0; deg < 360; deg++)
            {
                double rad = deg * Math.PI / 180.0;
                double normalized = SlopeCalculator.NormalizeTextAngle(rad);

                Assert.True(normalized >= 0.0 && normalized < 2.0 * Math.PI, $"Angle {deg} out of range [0, 2pi): {normalized}");
                bool isUpsideDown = normalized > Math.PI / 2.0 && normalized <= 3.0 * Math.PI / 2.0;
                Assert.False(isUpsideDown, $"Angle {deg} resulted in upside-down text angle {normalized * 180.0 / Math.PI} deg");
            }
        }

        [Fact]
        public void Slope_NegativeAndMultiTurnAngles_NormalizesProperly()
        {
            double neg45 = -Math.PI / 4.0;
            double normNeg45 = SlopeCalculator.NormalizeTextAngle(neg45);
            Assert.True(normNeg45 >= 0.0 && normNeg45 < 2.0 * Math.PI);
            Assert.False(normNeg45 > Math.PI / 2.0 && normNeg45 <= 3.0 * Math.PI / 2.0);

            double turn720 = 4.0 * Math.PI + Math.PI / 4.0; // 720 + 45 deg
            double norm720 = SlopeCalculator.NormalizeTextAngle(turn720);
            Assert.Equal(Math.PI / 4.0, norm720, 5);
        }

        [Fact]
        public void Slope_AllGradientsAndDirections_CalculatesCorrectPercentageAndRatios()
        {
            // 2% slope
            var s2 = SlopeCalculator.Calculate(new Point2D(0, 0), new Point2D(1000, 20));
            Assert.Equal(2.0, s2.GradientPercent);
            Assert.Equal("1:50", s2.RatioText);
            Assert.Equal("i = 2%", s2.FormattedText);
            Assert.False(s2.IsVertical);

            // 100% slope (45 deg)
            var s100 = SlopeCalculator.Calculate(new Point2D(0, 0), new Point2D(500, 500));
            Assert.Equal(100.0, s100.GradientPercent);
            Assert.Equal("1:1", s100.RatioText);

            // Horizontal (0%)
            var s0 = SlopeCalculator.Calculate(new Point2D(0, 0), new Point2D(1000, 0));
            Assert.Equal(0.0, s0.GradientPercent);
            Assert.Equal("Bằng", s0.RatioText);

            // Vertical (dx = 0)
            var sVert = SlopeCalculator.Calculate(new Point2D(200, 100), new Point2D(200, 800));
            Assert.True(sVert.IsVertical);
            Assert.True(double.IsPositiveInfinity(sVert.GradientPercent));
            Assert.Equal("1:0", sVert.RatioText);
            Assert.Equal("Đứng", sVert.FormattedText);

            // Coincident points (dx = 0, dy = 0)
            var sCoin = SlopeCalculator.Calculate(new Point2D(50, 50), new Point2D(50, 50));
            Assert.True(sCoin.IsVertical);
            Assert.Equal("Đứng", sCoin.FormattedText);
        }

        [Fact]
        public void Elevation_VietnameseStandards_FormatsPositiveNegativeZero()
        {
            Assert.Equal("+3.600", ElevationFormatter.Format(3.6));
            Assert.Equal("+0.150", ElevationFormatter.Format(0.15));
            Assert.Equal("-1.200", ElevationFormatter.Format(-1.2));
            Assert.Equal("-0.050", ElevationFormatter.Format(-0.05));
            Assert.Equal("±0.000", ElevationFormatter.Format(0.0));
            Assert.Equal("%%p0.000", ElevationFormatter.Format(0.0, forAutoCad: true));
        }

        [Fact]
        public void Elevation_ZeroThreshold_AppliesHalfMillimeterBand()
        {
            // Values with |Z| < 0.0005 should round to ±0.000
            Assert.Equal("±0.000", ElevationFormatter.Format(0.00049));
            Assert.Equal("±0.000", ElevationFormatter.Format(-0.00049));

            // Values with |Z| >= 0.0005 should format with explicit + or - sign
            Assert.Equal("+0.001", ElevationFormatter.Format(0.00051));
            Assert.Equal("-0.001", ElevationFormatter.Format(-0.00051));
        }

        [Fact]
        public void Elevation_RoundTripStress_100RandomElevations_ParsesAccurately()
        {
            var rnd = new Random(12345);
            for (int i = 0; i < 100; i++)
            {
                double val = Math.Round((rnd.NextDouble() * 300.0) - 50.0, 3); // -50.0 to +250.0
                string formatted = ElevationFormatter.Format(val, forAutoCad: false);
                double parsed = ElevationFormatter.Parse(formatted);

                if (Math.Abs(val) < 0.0005)
                {
                    Assert.Equal(0.0, parsed);
                }
                else
                {
                    Assert.Equal(val, parsed, 3);
                }
            }

            Assert.Equal(0.0, ElevationFormatter.Parse("%%p0.000"));
            Assert.Equal(0.0, ElevationFormatter.Parse(""));
            Assert.Equal(0.0, ElevationFormatter.Parse("invalid_text"));
        }

        [Fact]
        public void TrimWitnessLines_HorizontalVerticalAngled_TrimsCorrectly()
        {
            // Horizontal dimension line, offset 40mm
            var ext1 = new Point2D(100, 0);
            var ext2 = new Point2D(600, 0);
            var dimLinePt = new Point2D(350, 200);

            var (new1, new2) = VectorProjectionHelper.TrimWitnessLines(ext1, ext2, dimLinePt, 40.0);

            Assert.Equal(100.0, Math.Round(new1.X, 2));
            Assert.Equal(160.0, Math.Round(new1.Y, 2)); // 200 - 40
            Assert.Equal(600.0, Math.Round(new2.X, 2));
            Assert.Equal(160.0, Math.Round(new2.Y, 2));

            // Vertical dimension line
            var vExt1 = new Point2D(0, 100);
            var vExt2 = new Point2D(0, 800);
            var vDimPt = new Point2D(150, 400);

            var (vNew1, vNew2) = VectorProjectionHelper.TrimWitnessLines(vExt1, vExt2, vDimPt, 30.0);
            Assert.Equal(120.0, Math.Round(vNew1.X, 2)); // 150 - 30
            Assert.Equal(100.0, Math.Round(vNew1.Y, 2));
            Assert.Equal(120.0, Math.Round(vNew2.X, 2));
            Assert.Equal(800.0, Math.Round(vNew2.Y, 2));
        }

        [Fact]
        public void DistancePointToLine_StandardAndDegenerateCases_MatchesGeometricOracle()
        {
            var lineStart = new Point2D(0, 0);
            var lineEnd = new Point2D(100, 0);

            // Point perpendicular at distance 50
            double dist1 = VectorProjectionHelper.DistancePointToLine(new Point2D(50, 50), lineStart, lineEnd);
            Assert.Equal(50.0, dist1, 5);

            // Point on line
            double dist2 = VectorProjectionHelper.DistancePointToLine(new Point2D(25, 0), lineStart, lineEnd);
            Assert.Equal(0.0, dist2, 5);

            // Degenerate line (start == end)
            double dist3 = VectorProjectionHelper.DistancePointToLine(new Point2D(30, 40), lineStart, lineStart);
            Assert.Equal(50.0, dist3, 5); // Distance from (0,0) to (30,40)
        }

        [Fact]
        public void ProjectPointOnLine_CollinearAndArbitraryPoints_MatchesOracle()
        {
            var linePt = new Point2D(0, 0);
            var lineDir = new Vector2D(1, 0);

            var ptOnLine = new Point2D(75, 0);
            var proj1 = VectorProjectionHelper.ProjectPointOnLine(ptOnLine, linePt, lineDir);
            Assert.Equal(ptOnLine, proj1);

            var ptOffLine = new Point2D(50, 80);
            var proj2 = VectorProjectionHelper.ProjectPointOnLine(ptOffLine, linePt, lineDir);
            Assert.Equal(new Point2D(50, 0), proj2);
        }
    }
}
