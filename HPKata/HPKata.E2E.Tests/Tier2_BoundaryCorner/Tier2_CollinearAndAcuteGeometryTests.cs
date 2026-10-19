using System;
using Autodesk.AutoCAD.Geometry;
using HPKata.AutoCad.Interop;
using Xunit;

namespace HPKata.E2E.Tests.Tier2_BoundaryCorner;

/// <summary>
/// Tier 2: Boundary tests for collinear vertices, acute angles, degenerate segments, and zero-radius fillets.
/// </summary>
public sealed class Tier2_CollinearAndAcuteGeometryTests
{
    [Fact]
    public void CollinearVertices_180Degrees_ProducesZeroTurnAndZeroTangent()
    {
        // Arrange: 3 collinear points along X axis
        var p0 = new Point2d(0, 0);
        var p1 = new Point2d(100, 0);
        var p2 = new Point2d(200, 0);

        // Act
        FilletArcCalculator.ComputeTurn(p0, p1, p2, out double turnRad, out double crossZ);
        double tangentDist = FilletArcCalculator.ComputeTangentDistance(40.0, turnRad);
        double bulge = FilletArcCalculator.ComputeBulge(turnRad, crossZ);

        // Assert: zero turn, zero tangent, zero bulge
        Assert.Equal(0.0, turnRad, 5);
        Assert.Equal(0.0, crossZ, 5);
        Assert.Equal(0.0, tangentDist, 5);
        Assert.Equal(0.0, bulge, 5);
    }

    [Fact]
    public void DegenerateZeroLengthSegment_HandlesWithoutDivisionByZero()
    {
        // Arrange: p0 and p1 are identical points
        var p0 = new Point2d(50, 50);
        var p1 = new Point2d(50, 50);
        var p2 = new Point2d(150, 50);

        // Act
        FilletArcCalculator.ComputeTurn(p0, p1, p2, out double turnRad, out double crossZ);

        // Assert
        Assert.Equal(0.0, turnRad);
        Assert.Equal(0.0, crossZ);
    }

    [Fact]
    public void AcuteTurn_SharpCorner_ClipsTangentToPreventSegmentOverlap()
    {
        // Arrange: Sharp 15-degree turn
        double rad15 = 15.0 * Math.PI / 180.0;
        double tangent = FilletArcCalculator.ComputeTangentDistance(50.0, rad15);

        // Act: Clip against short 10mm segments
        double clipped = FilletArcCalculator.ClipTangent(tangent, 10.0, 10.0, 0.45);

        // Assert: Clipped <= 4.5mm (0.45 * 10)
        Assert.True(clipped <= 4.5);
    }

    [Fact]
    public void ZeroOrNegativeBendRadius_ProducesZeroBulgePolyline()
    {
        // Arrange: 90 degree corner with 0 radius
        var points = new[]
        {
            new Point2d(0, 0),
            new Point2d(100, 0),
            new Point2d(100, 100)
        };

        // Act
        var filleted = FilletArcCalculator.GenerateFilletedVertices(points, 0.0);

        // Assert: exact same 3 points with 0 bulge
        Assert.Equal(3, filleted.Count);
        Assert.All(filleted, v => Assert.Equal(0.0, v.Bulge));
    }

    [Fact]
    public void Orthogonal90DegreeCorner_ComputesStandardBulgeValue()
    {
        // Arrange: Standard 90 deg corner (turn = pi/2)
        double turnRad = Math.PI / 2.0;
        double crossZ = 1.0;

        // Act
        double bulge = FilletArcCalculator.ComputeBulge(turnRad, crossZ);

        // Assert: tan(pi / 8) = sqrt(2) - 1 approx 0.41421356
        Assert.Equal(FilletArcCalculator.Bulge90Deg, bulge, 5);
    }

    [Fact]
    public void ReverseDirection_180Turn_DoesNotThrowNaN()
    {
        // Arrange: folding back along the same line (p2 == p0)
        var p0 = new Point2d(0, 0);
        var p1 = new Point2d(100, 0);
        var p2 = new Point2d(0, 0);

        // Act
        FilletArcCalculator.ComputeTurn(p0, p1, p2, out double turnRad, out double crossZ);
        double tangentDist = FilletArcCalculator.ComputeTangentDistance(30.0, turnRad);

        // Assert: turnRad is pi, tangent returns 0.0 cleanly
        Assert.False(double.IsNaN(turnRad));
        Assert.False(double.IsNaN(tangentDist));
    }
}
