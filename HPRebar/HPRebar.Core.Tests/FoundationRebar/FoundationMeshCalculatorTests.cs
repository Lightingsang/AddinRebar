using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.FoundationRebar.Calculators;
using HPRebar.Core.FoundationRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.FoundationRebar;

public sealed class FoundationMeshCalculatorTests
{
    private const double Precision = 1.0e-5;

    #region 1. Spacing Divisibility & Bar Positions

    [Fact]
    public void CalculateBarPositions_WhenSpanExactlyDivisibleBySpacing_StartsAndEndsAtBoundaries()
    {
        // Arrange: Span of 1000 mm with 200 mm spacing -> exactly 5 intervals, 6 bars
        double start = 50.0;
        double end = 1050.0;
        double spacing = 200.0;

        // Act
        var positions = FoundationMeshCalculator.CalculateBarPositions(start, end, spacing);

        // Assert
        Assert.Equal(6, positions.Count);
        Assert.Equal(50.0, positions[0], Precision);
        Assert.Equal(250.0, positions[1], Precision);
        Assert.Equal(450.0, positions[2], Precision);
        Assert.Equal(650.0, positions[3], Precision);
        Assert.Equal(850.0, positions[4], Precision);
        Assert.Equal(1050.0, positions[5], Precision);

        // First bar at start, last bar at end
        Assert.Equal(start, positions.First(), Precision);
        Assert.Equal(end, positions.Last(), Precision);
    }

    [Fact]
    public void CalculateBarPositions_WhenNotDivisible_CentersRemainingSlackEquallyOnBothSides()
    {
        // Arrange: Span of 950 mm with 200 mm spacing
        // intervals = floor(950 / 200) = 4
        // slack = 950 - 4*200 = 150 mm
        // delta = slack / 2 = 75 mm
        double start = 100.0;
        double end = 1050.0;
        double spacing = 200.0;

        // Act
        var positions = FoundationMeshCalculator.CalculateBarPositions(start, end, spacing);

        // Assert
        Assert.Equal(5, positions.Count); // N + 1 = 4 + 1 = 5 bars

        double leftMargin = positions.First() - start;
        double rightMargin = end - positions.Last();

        Assert.Equal(75.0, leftMargin, Precision);
        Assert.Equal(75.0, rightMargin, Precision);
        Assert.Equal(leftMargin, rightMargin, Precision);

        // Spacing between internal bars is exactly 200 mm
        for (int i = 0; i < positions.Count - 1; i++)
        {
            Assert.Equal(200.0, positions[i + 1] - positions[i], Precision);
        }
    }

    [Fact]
    public void CalculateBarPositions_WhenSpanSmallerThanSpacing_PlacesSingleCenteredBar()
    {
        // Arrange: Span of 120 mm with 200 mm spacing
        double start = 50.0;
        double end = 170.0;
        double spacing = 200.0;

        // Act
        var positions = FoundationMeshCalculator.CalculateBarPositions(start, end, spacing);

        // Assert
        Assert.Single(positions);
        Assert.Equal(110.0, positions[0], Precision); // 50 + 120/2 = 110
    }

    [Theory]
    [InlineData(100.0, 50.0, 150.0)]  // Negative span
    [InlineData(100.0, 100.0, 150.0)] // Zero span
    [InlineData(50.0, 200.0, 0.0)]    // Zero spacing
    [InlineData(50.0, 200.0, -50.0)]  // Negative spacing
    public void CalculateBarPositions_WithInvalidInputs_ReturnsEmpty(double start, double end, double spacing)
    {
        var positions = FoundationMeshCalculator.CalculateBarPositions(start, end, spacing);
        Assert.Empty(positions);
    }

    [Fact]
    public void CalculateBarPositions_WithEqualSpacingMode_SubdividesSpanEvenly()
    {
        // Arrange: Span 1000 mm with max spacing 300 mm
        // intervals = ceil(1000 / 300) = 4
        // actualSpacing = 1000 / 4 = 250 mm
        double start = 0.0;
        double end = 1000.0;

        // Act
        var positions = FoundationMeshCalculator.CalculateBarPositions(start, end, 300.0, equalSpacing: true);

        // Assert
        Assert.Equal(5, positions.Count);
        Assert.Equal(0.0, positions[0], Precision);
        Assert.Equal(250.0, positions[1], Precision);
        Assert.Equal(500.0, positions[2], Precision);
        Assert.Equal(750.0, positions[3], Precision);
        Assert.Equal(1000.0, positions[4], Precision);
    }

    #endregion

    #region 2. 4-Layer Vertical Stacking

    [Fact]
    public void Calculate_FourLayerVerticalStacking_MaintainsExactElevationsAndClearanceGap()
    {
        // Arrange
        double H = 600.0;
        var snapshot = FoundationTestData.StandardSnapshot(length: 3000.0, width: 2000.0, thickness: H);

        double cBot = 60.0;
        double cTop = 50.0;
        double dBX = 20.0;
        double dBY = 16.0;
        double dTY = 16.0;
        double dTX = 12.0;

        var spec = new FoundationRebarSpec
        {
            CoverBottom = cBot,
            CoverTop = cTop,
            DiameterBottomX = dBX,
            DiameterBottomY = dBY,
            DiameterTopY = dTY,
            DiameterTopX = dTX,
            SpacingBottomX = 200.0,
            SpacingBottomY = 200.0,
            SpacingTopX = 200.0,
            SpacingTopY = 200.0,
            IsTopMatEnabled = true,
            HookType = FoundationHookType.None
        };

        // Expected local Z coordinates
        double z1 = cBot + (dBX / 2.0);                      // 60 + 10 = 70 mm
        double z2 = cBot + dBX + (dBY / 2.0);                // 60 + 20 + 8 = 88 mm
        double z3 = H - cTop - dTX - (dTY / 2.0);            // 600 - 50 - 12 - 8 = 530 mm
        double z4 = H - cTop - (dTX / 2.0);                  // 600 - 50 - 6 = 544 mm

        // Act
        var result = FoundationMeshCalculator.Calculate(snapshot, spec);

        // Assert: Layer heights strictly ordered z1 < z2 < z3 < z4
        Assert.True(z1 < z2);
        Assert.True(z2 < z3);
        Assert.True(z3 < z4);

        // Clearance gap between bottom mat top and top mat bottom
        double bottomMatTopElevation = z2 + (dBY / 2.0);
        double topMatBottomElevation = z3 - (dTY / 2.0);
        double clearanceGap = topMatBottomElevation - bottomMatTopElevation;
        Assert.True(clearanceGap > 0, $"Clearance gap must be positive, but was {clearanceGap:F1} mm.");

        // Verify all Bottom X bars are at z1
        var bottomXBars = result.Bars.Where(b => b.Layer == FoundationBarLayer.BottomX).ToList();
        Assert.NotEmpty(bottomXBars);
        Assert.All(bottomXBars, b =>
        {
            Assert.All(b.LocalPolyline.Points, p => Assert.Equal(z1, p.Z, Precision));
        });

        // Verify all Bottom Y bars are at z2
        var bottomYBars = result.Bars.Where(b => b.Layer == FoundationBarLayer.BottomY).ToList();
        Assert.NotEmpty(bottomYBars);
        Assert.All(bottomYBars, b =>
        {
            Assert.All(b.LocalPolyline.Points, p => Assert.Equal(z2, p.Z, Precision));
        });

        // Verify all Top Y bars are at z3
        var topYBars = result.Bars.Where(b => b.Layer == FoundationBarLayer.TopY).ToList();
        Assert.NotEmpty(topYBars);
        Assert.All(topYBars, b =>
        {
            Assert.All(b.LocalPolyline.Points, p => Assert.Equal(z3, p.Z, Precision));
        });

        // Verify all Top X bars are at z4
        var topXBars = result.Bars.Where(b => b.Layer == FoundationBarLayer.TopX).ToList();
        Assert.NotEmpty(topXBars);
        Assert.All(topXBars, b =>
        {
            Assert.All(b.LocalPolyline.Points, p => Assert.Equal(z4, p.Z, Precision));
        });
    }

    #endregion

    #region 3. Top Mat Disabled vs Enabled

    [Fact]
    public void Calculate_WhenTopMatDisabled_GeneratesOnlyBottomLayers()
    {
        // Arrange
        var snapshot = FoundationTestData.StandardSnapshot();
        var spec = FoundationTestData.StandardSpec(isTopMatEnabled: false);

        // Act
        var result = FoundationMeshCalculator.Calculate(snapshot, spec);

        // Assert
        Assert.NotEmpty(result.BottomBarsX);
        Assert.NotEmpty(result.BottomBarsY);
        Assert.Empty(result.TopBarsX);
        Assert.Empty(result.TopBarsY);

        Assert.Equal(0, result.Statistics.TopBarCountX);
        Assert.Equal(0, result.Statistics.TopBarCountY);
        Assert.Equal(result.BottomBarsX.Count + result.BottomBarsY.Count, result.TotalBarCount);

        Assert.All(result.Bars, b =>
        {
            Assert.True(b.Layer == FoundationBarLayer.BottomX || b.Layer == FoundationBarLayer.BottomY);
        });
    }

    [Fact]
    public void Calculate_WhenTopMatEnabled_GeneratesAllFourLayers()
    {
        // Arrange
        var snapshot = FoundationTestData.StandardSnapshot();
        var spec = FoundationTestData.StandardSpec(isTopMatEnabled: true);

        // Act
        var result = FoundationMeshCalculator.Calculate(snapshot, spec);

        // Assert
        Assert.NotEmpty(result.BottomBarsX);
        Assert.NotEmpty(result.BottomBarsY);
        Assert.NotEmpty(result.TopBarsX);
        Assert.NotEmpty(result.TopBarsY);

        Assert.True(result.Statistics.TopBarCountX > 0);
        Assert.True(result.Statistics.TopBarCountY > 0);
        Assert.Equal(
            result.BottomBarsX.Count + result.BottomBarsY.Count + result.TopBarsX.Count + result.TopBarsY.Count,
            result.TotalBarCount);
    }

    #endregion

    #region 4. Arbitrary Rotation in Plan

    [Theory]
    [InlineData(0.0)]
    [InlineData(30.0)]
    [InlineData(45.0)]
    [InlineData(90.0)]
    [InlineData(137.0)]
    public void Calculate_ArbitraryRotationInPlan_BarCountsAndLengthsAreInvariant(double angleDegrees)
    {
        // Arrange: Compare rotated mesh with baseline unrotated (0 degrees) mesh
        var spec = FoundationTestData.StandardSpec(hookType: FoundationHookType.Hook90Degrees, hookLength: 150.0);

        var baselineSnapshot = FoundationTestData.OrientedSnapshot(0.0, 3200.0, 2400.0, 500.0);
        var rotatedSnapshot = FoundationTestData.OrientedSnapshot(angleDegrees, 3200.0, 2400.0, 500.0);

        // Act
        var baselineResult = FoundationMeshCalculator.Calculate(baselineSnapshot, spec);
        var rotatedResult = FoundationMeshCalculator.Calculate(rotatedSnapshot, spec);

        // Assert: Bar counts across each layer must be strictly invariant under rotation
        Assert.Equal(baselineResult.BottomBarsX.Count, rotatedResult.BottomBarsX.Count);
        Assert.Equal(baselineResult.BottomBarsY.Count, rotatedResult.BottomBarsY.Count);
        Assert.Equal(baselineResult.TopBarsX.Count, rotatedResult.TopBarsX.Count);
        Assert.Equal(baselineResult.TopBarsY.Count, rotatedResult.TopBarsY.Count);
        Assert.Equal(baselineResult.TotalBarCount, rotatedResult.TotalBarCount);

        // Individual bar lengths must be identical
        Assert.Equal(baselineResult.Bars.Count, rotatedResult.Bars.Count);
        for (int i = 0; i < baselineResult.Bars.Count; i++)
        {
            Assert.Equal(baselineResult.Bars[i].LengthMm, rotatedResult.Bars[i].LengthMm, Precision);
        }

        // Total cumulative length and estimated weight must be invariant
        Assert.Equal(baselineResult.TotalLengthMm, rotatedResult.TotalLengthMm, Precision);
        Assert.Equal(baselineResult.Statistics.EstimatedWeightKg, rotatedResult.Statistics.EstimatedWeightKg, Precision);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(30.0)]
    [InlineData(45.0)]
    [InlineData(90.0)]
    [InlineData(137.0)]
    public void Calculate_ArbitraryRotationInPlan_AllBarCurvesAreCoplanarIn3DWorldSpace(double angleDegrees)
    {
        // Arrange
        var snapshot = FoundationTestData.OrientedSnapshot(angleDegrees, 3000.0, 2200.0, 500.0);
        var spec = FoundationTestData.StandardSpec(hookType: FoundationHookType.Hook90Degrees, hookLength: 180.0);

        // Act
        var result = FoundationMeshCalculator.Calculate(snapshot, spec);

        // Assert:
        // For X-direction bars, the normal vector to the rebar curve plane is LocalY.
        // For Y-direction bars, the normal vector to the rebar curve plane is LocalX.
        // Coplanarity condition: for every vertex P on bar, (P - P0) . Normal == 0.
        foreach (var bar in result.Bars)
        {
            var points = bar.Polyline.Points;
            Assert.True(points.Count >= 2);
            var p0 = points[0];

            Vector3 planeNormal = (bar.Layer == FoundationBarLayer.BottomX || bar.Layer == FoundationBarLayer.TopX)
                ? snapshot.LocalY
                : snapshot.LocalX;

            for (int i = 1; i < points.Count; i++)
            {
                Vector3 chord = points[i] - p0;
                double projectionOnNormal = Math.Abs(chord.Dot(planeNormal));
                Assert.True(projectionOnNormal < Precision,
                    $"Bar {bar.BarIndex} on layer {bar.LayerName} is not coplanar: dot product with normal is {projectionOnNormal:E3}.");
            }
        }
    }

    #endregion

    #region 5. Anchorage Hooks & Safety Clamping

    [Fact]
    public void Calculate_AnchorageHooks_BottomBarsBendUpAndTopBarsBendDown()
    {
        // Arrange
        var snapshot = FoundationTestData.StandardSnapshot(thickness: 500.0);
        var spec = FoundationTestData.StandardSpec(
            hookType: FoundationHookType.Hook90Degrees,
            hookLength: 200.0,
            isTopMatEnabled: true);

        // Act
        var result = FoundationMeshCalculator.Calculate(snapshot, spec);

        // Assert
        // Bottom X bars: 4 points, hook start and end points bend UP (+Z) relative to corners
        var bX = result.Bars.First(b => b.Layer == FoundationBarLayer.BottomX);
        Assert.Equal(4, bX.LocalPolyline.Points.Count);
        Assert.True(bX.LocalPolyline.Points[0].Z > bX.LocalPolyline.Points[1].Z, "Bottom bar hook start must bend UP (+Z).");
        Assert.True(bX.LocalPolyline.Points[3].Z > bX.LocalPolyline.Points[2].Z, "Bottom bar hook end must bend UP (+Z).");

        // Bottom Y bars: bend UP (+Z)
        var bY = result.Bars.First(b => b.Layer == FoundationBarLayer.BottomY);
        Assert.Equal(4, bY.LocalPolyline.Points.Count);
        Assert.True(bY.LocalPolyline.Points[0].Z > bY.LocalPolyline.Points[1].Z, "Bottom bar hook start must bend UP (+Z).");
        Assert.True(bY.LocalPolyline.Points[3].Z > bY.LocalPolyline.Points[2].Z, "Bottom bar hook end must bend UP (+Z).");

        // Top Y bars: 4 points, hook start and end points bend DOWN (-Z) relative to corners
        var tY = result.Bars.First(b => b.Layer == FoundationBarLayer.TopY);
        Assert.Equal(4, tY.LocalPolyline.Points.Count);
        Assert.True(tY.LocalPolyline.Points[0].Z < tY.LocalPolyline.Points[1].Z, "Top bar hook start must bend DOWN (-Z).");
        Assert.True(tY.LocalPolyline.Points[3].Z < tY.LocalPolyline.Points[2].Z, "Top bar hook end must bend DOWN (-Z).");

        // Top X bars: bend DOWN (-Z)
        var tX = result.Bars.First(b => b.Layer == FoundationBarLayer.TopX);
        Assert.Equal(4, tX.LocalPolyline.Points.Count);
        Assert.True(tX.LocalPolyline.Points[0].Z < tX.LocalPolyline.Points[1].Z, "Top bar hook start must bend DOWN (-Z).");
        Assert.True(tX.LocalPolyline.Points[3].Z < tX.LocalPolyline.Points[2].Z, "Top bar hook end must bend DOWN (-Z).");
    }

    [Fact]
    public void Calculate_OversizedHooks_ClampedSafelyToPreventCoverBreach()
    {
        // Arrange: Slab thickness = 400 mm, CoverTop = 50 mm, CoverBottom = 50 mm.
        // Bottom X: z1 = 50 + 16/2 = 58 mm.
        // Max rise for Bottom X = 400 - 58 - 50 = 292 mm.
        // Request massive hook of 1000 mm.
        var snapshot = FoundationTestData.StandardSnapshot(thickness: 400.0);
        var spec = FoundationTestData.StandardSpec(
            coverTop: 50.0,
            coverBottom: 50.0,
            diameterBottomX: 16.0,
            diameterTopX: 12.0,
            hookType: FoundationHookType.Hook90Degrees,
            hookLength: 1000.0, // Oversized!
            isTopMatEnabled: true);

        // Act
        var result = FoundationMeshCalculator.Calculate(snapshot, spec);

        // Assert:
        // Bottom X: hook tip Z must be exactly H - coverTop = 350 mm (clamped to 292 mm)
        var bX = result.Bars.First(b => b.Layer == FoundationBarLayer.BottomX);
        Assert.Equal(292.0, bX.HookLength, Precision);
        Assert.Equal(350.0, bX.LocalPolyline.Points[0].Z, Precision);
        Assert.True(bX.LocalPolyline.Points[0].Z <= snapshot.Thickness - spec.CoverTop);

        // Top X: z4 = 400 - 50 - 6 = 344 mm.
        // Max drop for Top X = 344 - 50 = 294 mm.
        // Hook tip Z must be exactly coverBottom = 50 mm (clamped to 294 mm)
        var tX = result.Bars.First(b => b.Layer == FoundationBarLayer.TopX);
        Assert.Equal(294.0, tX.HookLength, Precision);
        Assert.Equal(50.0, tX.LocalPolyline.Points[0].Z, Precision);
        Assert.True(tX.LocalPolyline.Points[0].Z >= spec.CoverBottom);
    }

    [Fact]
    public void Calculate_StraightBars_WhenHookTypeNone_GeneratesTwoPointPolylines()
    {
        // Arrange
        var snapshot = FoundationTestData.StandardSnapshot();
        var spec = FoundationTestData.StandardSpec(hookType: FoundationHookType.None);

        // Act
        var result = FoundationMeshCalculator.Calculate(snapshot, spec);

        // Assert
        Assert.All(result.Bars, b =>
        {
            Assert.Equal(2, b.Polyline.Points.Count);
            Assert.Equal(2, b.LocalPolyline.Points.Count);
            Assert.Equal(0.0, b.HookLength);
        });
    }

    [Fact]
    public void Calculate_WhenValidationFails_ThrowsInvalidOperationException()
    {
        // Arrange: Invalid spec with negative spacing
        var snapshot = FoundationTestData.StandardSnapshot();
        var spec = FoundationTestData.StandardSpec(spacingBottomX: -100.0);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => FoundationMeshCalculator.Calculate(snapshot, spec));
    }

    [Fact]
    public void Calculate_NullArguments_ThrowsArgumentNullException()
    {
        var snapshot = FoundationTestData.StandardSnapshot();
        var spec = FoundationTestData.StandardSpec();

        Assert.Throws<ArgumentNullException>(() => FoundationMeshCalculator.Calculate(null!, spec));
        Assert.Throws<ArgumentNullException>(() => FoundationMeshCalculator.Calculate(snapshot, null!));
    }

    #endregion
}
