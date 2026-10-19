namespace HPBim.Core.Tests.Verification;

using System;
using System.Collections.Generic;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Geometry;
using HPBim.Core.Verification;
using Xunit;

public sealed class MeasuredGeometricEngineRequiredMeasurementsTests
{
    private readonly MeasuredGeometricEngine _engine = new();

    private static ElementPlanItem CreatePlanItem(
        CoordinationCategory category,
        string mark,
        ElementGeometrySpec geometry,
        Dictionary<string, double>? dimensions = null) => new()
    {
        SourceKey = new CompositeSourceKey("Doc", category, mark),
        Category = category,
        Mark = mark,
        Geometry = geometry,
        Resource = dimensions != null ? new ResourceResolutionSpec { DimensionParametersMm = dimensions } : new()
    };

    private static ActualElementGeometry CreateActual(
        ElementPlanItem item,
        Point3D? centroid = null,
        double? width = null,
        double? height = null,
        Point3D? curveStart = null,
        Point3D? curveEnd = null,
        double? topElev = null,
        double? thickness = null) => new()
    {
        Key = item.SourceKey.ToString(),
        CategoryName = item.Category.ToString(),
        HasGeometry = true,
        Centroid = centroid,
        WidthMm = width,
        HeightMm = height,
        CurveStart = curveStart,
        CurveEnd = curveEnd,
        TopElevationMm = topElev,
        ThicknessMm = thickness
    };

    [Fact]
    public void AuditItem_PileWithGeometry_WhenCentroidNull_ReturnsInconclusiveRule0()
    {
        var item = CreatePlanItem(CoordinationCategory.Pile, "P1", new PointGeometrySpec { Location = new Point3D(1000, 2000, -2500), OffsetZMm = -2500 });
        var actual = CreateActual(item, centroid: null, topElev: -2500.0);
        var audit = _engine.AuditItem(item, new Dictionary<string, ActualElementGeometry> { [actual.Key] = actual });

        Assert.Equal(GeometricVerificationStatus.Inconclusive, audit.Status);
        Assert.NotNull(audit.DefectReason);
        Assert.Contains("Rule 0", audit.DefectReason);
    }

    [Fact]
    public void AuditItem_ColumnWithGeometry_WhenCentroidNull_ReturnsInconclusive()
    {
        var item = CreatePlanItem(CoordinationCategory.Column, "C1", new PointGeometrySpec { Location = new Point3D(0, 0, 0) }, new() { ["b"] = 220, ["h"] = 220 });
        var actual = CreateActual(item, centroid: null, width: 220.0, height: 220.0);
        var audit = _engine.AuditItem(item, new Dictionary<string, ActualElementGeometry> { [actual.Key] = actual });

        Assert.Equal(GeometricVerificationStatus.Inconclusive, audit.Status);
        Assert.NotNull(audit.DefectReason);
        Assert.Contains("Rule 0", audit.DefectReason);
    }

    [Theory]
    [InlineData(null, 220.0)]
    [InlineData(220.0, null)]
    public void AuditItem_ColumnWithGeometry_WhenWidthOrHeightNull_ReturnsInconclusive(double? width, double? height)
    {
        var item = CreatePlanItem(CoordinationCategory.Column, "C1", new PointGeometrySpec { Location = new Point3D(0, 0, 0) }, new() { ["b"] = 220, ["h"] = 220 });
        var actual = CreateActual(item, centroid: new Point3D(0, 0, 0), width: width, height: height);
        var audit = _engine.AuditItem(item, new Dictionary<string, ActualElementGeometry> { [actual.Key] = actual });

        Assert.Equal(GeometricVerificationStatus.Inconclusive, audit.Status);
        Assert.NotNull(audit.DefectReason);
        Assert.Contains("Rule 0", audit.DefectReason);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void AuditItem_BeamWithGeometry_WhenCurveEndpointNull_ReturnsInconclusive(bool hasStart, bool hasEnd)
    {
        var item = CreatePlanItem(CoordinationCategory.Beam, "B1", new LineGeometrySpec { Start = new Point3D(0, 0, 3800), End = new Point3D(4000, 0, 3800), ElevationMm = 3800 });
        var actual = CreateActual(item, curveStart: hasStart ? new Point3D(0, 0, 3800) : null, curveEnd: hasEnd ? new Point3D(4000, 0, 3800) : null, topElev: 3800.0, width: 300.0, height: 500.0);
        var audit = _engine.AuditItem(item, new Dictionary<string, ActualElementGeometry> { [actual.Key] = actual });

        Assert.Equal(GeometricVerificationStatus.Inconclusive, audit.Status);
        Assert.NotNull(audit.DefectReason);
        Assert.Contains("Rule 0", audit.DefectReason);
    }

    [Fact]
    public void AuditItem_BeamWithGeometry_WhenTopElevationNull_ReturnsInconclusive()
    {
        var item = CreatePlanItem(CoordinationCategory.Beam, "B1", new LineGeometrySpec { Start = new Point3D(0, 0, 3800), End = new Point3D(4000, 0, 3800), ElevationMm = 3800 });
        var actual = CreateActual(item, curveStart: new Point3D(0, 0, 3800), curveEnd: new Point3D(4000, 0, 3800), topElev: null, width: 300.0, height: 500.0);
        var audit = _engine.AuditItem(item, new Dictionary<string, ActualElementGeometry> { [actual.Key] = actual });

        Assert.Equal(GeometricVerificationStatus.Inconclusive, audit.Status);
        Assert.NotNull(audit.DefectReason);
        Assert.Contains("Rule 0", audit.DefectReason);
    }

    [Fact]
    public void AuditItem_SlabWithGeometry_WhenTopElevationNull_ReturnsInconclusive()
    {
        var item = CreatePlanItem(CoordinationCategory.Slab, "S1", new ProfileGeometrySpec { ElevationMm = 3800.0, ThicknessMm = 120.0 });
        var actual = CreateActual(item, topElev: null, thickness: 120.0);
        var audit = _engine.AuditItem(item, new Dictionary<string, ActualElementGeometry> { [actual.Key] = actual });

        Assert.Equal(GeometricVerificationStatus.Inconclusive, audit.Status);
        Assert.NotNull(audit.DefectReason);
        Assert.Contains("Rule 0", audit.DefectReason);
    }

    [Fact]
    public void AuditItem_SlabWithGeometry_WhenThicknessNull_ReturnsInconclusive()
    {
        var item = CreatePlanItem(CoordinationCategory.Slab, "S1", new ProfileGeometrySpec { ElevationMm = 3800.0, ThicknessMm = 120.0 });
        var actual = CreateActual(item, topElev: 3800.0, thickness: null);
        var audit = _engine.AuditItem(item, new Dictionary<string, ActualElementGeometry> { [actual.Key] = actual });

        Assert.Equal(GeometricVerificationStatus.Inconclusive, audit.Status);
        Assert.NotNull(audit.DefectReason);
        Assert.Contains("Rule 0", audit.DefectReason);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void AuditItem_WallWithGeometry_WhenCurveEndpointNull_ReturnsInconclusive(bool hasStart, bool hasEnd)
    {
        var item = CreatePlanItem(CoordinationCategory.Wall, "W1", new LineGeometrySpec { Start = new Point3D(0, 0, 0), End = new Point3D(5000, 0, 0) }, new() { ["thickness"] = 200.0 });
        var actual = CreateActual(item, curveStart: hasStart ? new Point3D(0, 0, 0) : null, curveEnd: hasEnd ? new Point3D(5000, 0, 0) : null, thickness: 200.0);
        var audit = _engine.AuditItem(item, new Dictionary<string, ActualElementGeometry> { [actual.Key] = actual });

        Assert.Equal(GeometricVerificationStatus.Inconclusive, audit.Status);
        Assert.NotNull(audit.DefectReason);
        Assert.Contains("Rule 0", audit.DefectReason);
    }

    [Fact]
    public void AuditItem_LevelWithGeometry_WhenTopElevationNull_ReturnsInconclusive()
    {
        var item = CreatePlanItem(CoordinationCategory.Level, "L1", new LevelGeometrySpec { ElevationM = 3.8, FloorNumber = 1 });
        var actual = CreateActual(item, topElev: null);
        var audit = _engine.AuditItem(item, new Dictionary<string, ActualElementGeometry> { [actual.Key] = actual });

        Assert.Equal(GeometricVerificationStatus.Inconclusive, audit.Status);
        Assert.NotNull(audit.DefectReason);
        Assert.Contains("Rule 0", audit.DefectReason);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void AuditItem_GridWithGeometry_WhenCurveEndpointNull_ReturnsInconclusive(bool hasStart, bool hasEnd)
    {
        var item = CreatePlanItem(CoordinationCategory.Grid, "X1", new LineGeometrySpec { Start = new Point3D(0, 0, 0), End = new Point3D(0, 10000, 0) });
        var actual = CreateActual(item, curveStart: hasStart ? new Point3D(0, 0, 0) : null, curveEnd: hasEnd ? new Point3D(0, 10000, 0) : null);
        var audit = _engine.AuditItem(item, new Dictionary<string, ActualElementGeometry> { [actual.Key] = actual });

        Assert.Equal(GeometricVerificationStatus.Inconclusive, audit.Status);
        Assert.NotNull(audit.DefectReason);
        Assert.Contains("Rule 0", audit.DefectReason);
    }
}
