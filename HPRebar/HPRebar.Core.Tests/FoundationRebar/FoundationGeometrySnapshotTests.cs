using System;
using HPRebar.Core.FoundationRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.FoundationRebar;

public sealed class FoundationGeometrySnapshotTests
{
    private const double Tolerance = 1.0e-6;

    #region 1. Axis-Aligned Construction & Aliases

    [Fact]
    public void CreateAxisAligned_ConstructsExpectedBasisAndCoordinates()
    {
        // Arrange
        var origin = new Point3(150.0, 250.0, 100.0);
        double length = 3500.0;
        double width = 2500.0;
        double thickness = 600.0;

        // Act
        var snapshot = FoundationGeometrySnapshot.CreateAxisAligned(origin, length, width, thickness);

        // Assert
        Assert.Equal(origin, snapshot.Origin);
        Assert.Equal(length, snapshot.Length, Tolerance);
        Assert.Equal(width, snapshot.Width, Tolerance);
        Assert.Equal(thickness, snapshot.Thickness, Tolerance);
        Assert.Equal(100.0, snapshot.BottomZ, Tolerance);
        Assert.Equal(700.0, snapshot.TopZ, Tolerance); // 100 + 600

        // Orthonormal standard basis
        Assert.Equal(Vector3.UnitX, snapshot.LocalX);
        Assert.Equal(Vector3.UnitY, snapshot.LocalY);
        Assert.Equal(Vector3.UnitZ, snapshot.LocalZ);

        // Aliases
        Assert.Equal(snapshot.Length, snapshot.LengthMm, Tolerance);
        Assert.Equal(snapshot.Width, snapshot.WidthMm, Tolerance);
        Assert.Equal(snapshot.Thickness, snapshot.ThicknessMm, Tolerance);
        Assert.Equal(snapshot.TopZ, snapshot.TopElevation, Tolerance);
        Assert.Equal(snapshot.BottomZ, snapshot.BottomElevation, Tolerance);
        Assert.Equal(snapshot.LocalZ, snapshot.NormalZ);
        Assert.Equal(snapshot.LocalX, snapshot.DirectionX);
        Assert.Equal(snapshot.LocalY, snapshot.DirectionY);
        Assert.Equal(snapshot.LocalZ, snapshot.DirectionZ);
    }

    #endregion

    #region 2. Orthonormal Basis Vectors Across Arbitrary Angles

    [Theory]
    [InlineData(0.0)]
    [InlineData(15.0)]
    [InlineData(30.0)]
    [InlineData(45.0)]
    [InlineData(60.0)]
    [InlineData(90.0)]
    [InlineData(120.0)]
    [InlineData(135.0)]
    [InlineData(180.0)]
    [InlineData(215.0)]
    [InlineData(270.0)]
    [InlineData(315.0)]
    [InlineData(-45.0)]
    public void CreateOriented_GeneratesStrictlyOrthonormalRightHandedBasis(double angleDegrees)
    {
        // Arrange
        var origin = new Point3(500.0, -300.0, 50.0);

        // Act
        var snapshot = FoundationGeometrySnapshot.CreateOriented(origin, angleDegrees, 3000.0, 2000.0, 500.0);

        // Assert: 1. Unit length for all basis vectors
        Assert.Equal(1.0, snapshot.LocalX.Length, Tolerance);
        Assert.Equal(1.0, snapshot.LocalY.Length, Tolerance);
        Assert.Equal(1.0, snapshot.LocalZ.Length, Tolerance);

        // Assert: 2. Mutual orthogonality (dot products must be zero)
        Assert.Equal(0.0, snapshot.LocalX.Dot(snapshot.LocalY), Tolerance);
        Assert.Equal(0.0, snapshot.LocalX.Dot(snapshot.LocalZ), Tolerance);
        Assert.Equal(0.0, snapshot.LocalY.Dot(snapshot.LocalZ), Tolerance);

        // Assert: 3. Right-handed coordinate system: LocalX x LocalY == LocalZ
        Vector3 rightHandedZ = snapshot.LocalX.Cross(snapshot.LocalY);
        Assert.True(rightHandedZ.IsAlmostEqualTo(snapshot.LocalZ, Tolerance),
            $"Cross product LocalX x LocalY must equal LocalZ for angle {angleDegrees}°.");
    }

    #endregion

    #region 3. Coordinate Transformations Round-Trip Accuracy

    [Theory]
    [InlineData(0.0)]
    [InlineData(30.0)]
    [InlineData(45.0)]
    [InlineData(90.0)]
    [InlineData(137.5)]
    [InlineData(240.0)]
    [InlineData(-60.0)]
    public void ToWorld_And_ToLocal_RoundTripIsIdentityWithinTolerance(double angleDegrees)
    {
        // Arrange
        var origin = new Point3(1234.56, -7890.12, 345.67);
        double length = 4000.0;
        double width = 3000.0;
        double thickness = 700.0;

        var snapshot = FoundationGeometrySnapshot.CreateOriented(origin, angleDegrees, length, width, thickness);

        // Test grid of local points spanning the boundary and interior
        var localTestPoints = new[]
        {
            Point3.Zero,
            new Point3(length, 0.0, 0.0),
            new Point3(0.0, width, 0.0),
            new Point3(0.0, 0.0, thickness),
            new Point3(length, width, thickness),
            new Point3(length / 2.0, width / 2.0, thickness / 2.0),
            new Point3(123.4, 567.8, 90.1)
        };

        foreach (var localPoint in localTestPoints)
        {
            // Act: Local -> World -> Local
            Point3 worldPoint = snapshot.ToWorld(localPoint);
            Point3 localRoundTrip = snapshot.ToLocal(worldPoint);

            // Assert
            Assert.True(localRoundTrip.IsAlmostEqualTo(localPoint, Tolerance),
                $"Round-trip transformation failed for angle {angleDegrees}° at local point {localPoint}. Result: {localRoundTrip}.");
        }
    }

    [Fact]
    public void ToWorld_WithIndividualCoordinates_MatchesPoint3Overload()
    {
        // Arrange
        var snapshot = FoundationGeometrySnapshot.CreateOriented(new Point3(10, 20, 30), 37.0, 3000, 2000, 500);
        double u = 150.0, v = 250.0, w = 70.0;

        // Act
        Point3 fromCoords = snapshot.ToWorld(u, v, w);
        Point3 fromPoint = snapshot.ToWorld(new Point3(u, v, w));

        // Assert
        Assert.True(fromCoords.IsAlmostEqualTo(fromPoint, Tolerance));
    }

    #endregion

    #region 4. Foundational Models Operations (Point3, Vector3, Polyline3)

    [Fact]
    public void Point3_And_Vector3_VectorArithmetic_OperatesAccurately()
    {
        var p1 = new Point3(10.0, 20.0, 30.0);
        var v = new Vector3(5.0, -10.0, 15.0);

        // Point + Vector
        var p2 = p1 + v;
        Assert.Equal(15.0, p2.X, Tolerance);
        Assert.Equal(10.0, p2.Y, Tolerance);
        Assert.Equal(45.0, p2.Z, Tolerance);

        // Point - Vector
        var p3 = p2 - v;
        Assert.True(p3.IsAlmostEqualTo(p1, Tolerance));

        // Point - Point = Vector
        var diff = p2 - p1;
        Assert.True(diff.IsAlmostEqualTo(v, Tolerance));

        // Scalar ops
        var scaled = p1 * 2.0;
        Assert.Equal(20.0, scaled.X, Tolerance);
        Assert.Equal(40.0, scaled.Y, Tolerance);
        Assert.Equal(60.0, scaled.Z, Tolerance);

        var divided = scaled / 2.0;
        Assert.True(divided.IsAlmostEqualTo(p1, Tolerance));

        // Distance
        Assert.Equal(Math.Sqrt(25 + 100 + 225), p1.DistanceTo(p2), Tolerance);
    }

    [Fact]
    public void Vector3_DotAndCrossProducts_SatisfyGeometricProperties()
    {
        var vx = Vector3.UnitX;
        var vy = Vector3.UnitY;
        var vz = Vector3.UnitZ;

        Assert.Equal(0.0, vx.Dot(vy), Tolerance);
        Assert.Equal(1.0, vx.Dot(vx), Tolerance);

        // vx cross vy == vz
        Assert.True(vx.Cross(vy).IsAlmostEqualTo(vz, Tolerance));
        // vy cross vz == vx
        Assert.True(vy.Cross(vz).IsAlmostEqualTo(vx, Tolerance));
        // vz cross vx == vy
        Assert.True(vz.Cross(vx).IsAlmostEqualTo(vy, Tolerance));

        // Anti-commutativity
        Assert.True(vy.Cross(vx).IsAlmostEqualTo(-vz, Tolerance));
    }

    [Fact]
    public void Polyline3_TotalLength_And_Simplify_OperateCorrectly()
    {
        // Polyline: (0,0,0) -> (1000, 0, 0) -> (1000, 500, 0) -> length = 1500
        var pts = new[]
        {
            new Point3(0, 0, 0),
            new Point3(1000, 0, 0),
            new Point3(1000, 500, 0)
        };
        var poly = new Polyline3(pts);
        Assert.Equal(1500.0, poly.TotalLength, Tolerance);

        // Simplify micro-segment (less than 1.0 mm)
        var microPts = new[]
        {
            new Point3(0, 0, 0),
            new Point3(0.5, 0, 0), // < 1.0 mm from previous vertex
            new Point3(1000, 0, 0)
        };
        var simplified = new Polyline3(microPts).Simplify(1.0);
        Assert.Equal(2, simplified.Points.Count);
        Assert.Equal(1000.0, simplified.TotalLength, Tolerance);

        // Translate
        var translated = poly.Translate(new Vector3(10, 20, 30));
        Assert.Equal(1500.0, translated.TotalLength, Tolerance);
        Assert.Equal(10.0, translated.Points[0].X, Tolerance);
        Assert.Equal(20.0, translated.Points[0].Y, Tolerance);
        Assert.Equal(30.0, translated.Points[0].Z, Tolerance);
    }

    #endregion
}
