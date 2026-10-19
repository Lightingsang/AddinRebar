namespace HPBim.Core.Tests.Verification;

using System;
using System.Collections.Generic;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Geometry;
using HPBim.Core.Verification;
using Xunit;

/// <summary>
/// Empirical adversarial challenger suite for Requirement R5 (Strict Inconclusive Threshold).
/// Stress-tests missing attributes across all structural categories and verifies Rule 0 contracts.
/// Complies with Rule C2 (<= 300 lines) and Rule M3 (<= 50 lines per method).
/// </summary>
public sealed class EmpiricalChallengerM3StrictInconclusiveTests
{
    private readonly MeasuredGeometricEngine _engine = new();

    private static ElementPlanItem CreatePlan(
        CoordinationCategory cat,
        string mark,
        ElementGeometrySpec geom,
        Dictionary<string, double>? dims = null) => new()
    {
        SourceKey = new CompositeSourceKey("Doc", cat, mark),
        Category = cat,
        Mark = mark,
        Geometry = geom,
        Resource = dims != null ? new ResourceResolutionSpec { DimensionParametersMm = dims } : new()
    };

    private static ActualElementGeometry CreateAct(
        ElementPlanItem item,
        Point3D? centroid = null,
        double? w = null,
        double? h = null,
        Point3D? start = null,
        Point3D? end = null,
        double? topElev = null,
        double? thk = null) => new()
    {
        Key = item.SourceKey.ToString(),
        CategoryName = item.Category.ToString(),
        HasGeometry = true,
        Centroid = centroid,
        WidthMm = w,
        HeightMm = h,
        CurveStart = start,
        CurveEnd = end,
        TopElevationMm = topElev,
        ThicknessMm = thk
    };

    [Theory]
    [InlineData(CoordinationCategory.Pile, false, false, false, false, false, false)]
    [InlineData(CoordinationCategory.Column, false, true, true, false, false, false)]
    [InlineData(CoordinationCategory.Column, true, false, true, false, false, false)]
    [InlineData(CoordinationCategory.Column, true, true, false, false, false, false)]
    [InlineData(CoordinationCategory.Column, true, false, false, false, false, false)]
    [InlineData(CoordinationCategory.Beam, false, false, false, false, true, true)]
    [InlineData(CoordinationCategory.Beam, false, false, false, true, false, true)]
    [InlineData(CoordinationCategory.Beam, false, false, false, true, true, false)]
    [InlineData(CoordinationCategory.GradeBeam, false, false, false, false, true, true)]
    [InlineData(CoordinationCategory.GradeBeam, false, false, false, true, true, false)]
    [InlineData(CoordinationCategory.Slab, false, false, false, false, false, false)]
    [InlineData(CoordinationCategory.Footing, false, false, false, false, false, false)]
    [InlineData(CoordinationCategory.Wall, false, false, false, false, true, false)]
    [InlineData(CoordinationCategory.Wall, false, false, false, true, false, false)]
    [InlineData(CoordinationCategory.Level, false, false, false, false, false, false)]
    [InlineData(CoordinationCategory.Grid, false, false, false, false, true, false)]
    [InlineData(CoordinationCategory.Grid, false, false, false, true, false, false)]
    public void StressTest_MissingAttributes_NeverReturnsVerified(
        CoordinationCategory cat,
        bool hasCentroid,
        bool hasW,
        bool hasH,
        bool hasStart,
        bool hasEnd,
        bool hasTopElev)
    {
        var item = CreateItemForCategory(cat);
        var act = CreateAct(
            item,
            centroid: hasCentroid ? new Point3D(0, 0, 0) : null,
            w: hasW ? 220.0 : null,
            h: hasH ? 220.0 : null,
            start: hasStart ? new Point3D(0, 0, 0) : null,
            end: hasEnd ? new Point3D(1000, 0, 0) : null,
            topElev: hasTopElev ? 0.0 : null,
            thk: cat is CoordinationCategory.Slab or CoordinationCategory.Footing ? null : 150.0);

        var audit = _engine.AuditItem(item, new Dictionary<string, ActualElementGeometry> { [act.Key] = act });

        Assert.Equal(GeometricVerificationStatus.Inconclusive, audit.Status);
        Assert.False(audit.IsVerified);
        Assert.NotNull(audit.DefectReason);
        Assert.Contains("Rule 0", audit.DefectReason);
        Assert.NotEmpty(audit.Discrepancies);
        Assert.Contains("Rule 0", audit.Discrepancies[0]);
    }

    [Fact]
    public void StressTest_SlabWithTopElevation_MissingThickness_ReturnsInconclusiveRule0()
    {
        var item = CreatePlan(CoordinationCategory.Slab, "S1", new ProfileGeometrySpec { ElevationMm = 0.0, ThicknessMm = 150.0 });
        var act = CreateAct(item, topElev: 0.0, thk: null);

        var audit = _engine.AuditItem(item, new Dictionary<string, ActualElementGeometry> { [act.Key] = act });

        Assert.Equal(GeometricVerificationStatus.Inconclusive, audit.Status);
        Assert.Contains("Rule 0: Slab thiếu thuộc tính Thickness từ Revit API.", audit.DefectReason);
    }

    [Fact]
    public void StressTest_FootingWithTopElevation_MissingThickness_ReturnsInconclusiveRule0()
    {
        var item = CreatePlan(CoordinationCategory.Footing, "F1", new ProfileGeometrySpec { ElevationMm = -1600.0, ThicknessMm = 1200.0 });
        var act = CreateAct(item, topElev: -1600.0, thk: null);

        var audit = _engine.AuditItem(item, new Dictionary<string, ActualElementGeometry> { [act.Key] = act });

        Assert.Equal(GeometricVerificationStatus.Inconclusive, audit.Status);
        Assert.Contains("Rule 0: Footing thiếu thuộc tính Thickness từ Revit API.", audit.DefectReason);
    }

    [Fact]
    public void StressTest_AuditPlan_WithMissingMeasurements_ReportsInconclusiveCountAndOverallFailed()
    {
        var p1 = CreatePlan(CoordinationCategory.Pile, "P1", new PointGeometrySpec());
        var c1 = CreatePlan(CoordinationCategory.Column, "C1", new PointGeometrySpec(), new() { ["b"] = 220, ["h"] = 220 });
        var b1 = CreatePlan(CoordinationCategory.Beam, "B1", new LineGeometrySpec());
        var plan = new BimExecutionPlan { Items = new[] { p1, c1, b1 } };

        // P1 missing centroid (inconclusive), C1 valid (verified), B1 missing top elev (inconclusive)
        var actP1 = CreateAct(p1, centroid: null);
        var actC1 = CreateAct(c1, centroid: new Point3D(0, 0, 0), w: 220.0, h: 220.0);
        var actB1 = CreateAct(b1, start: new Point3D(0, 0, 0), end: new Point3D(1000, 0, 0), topElev: null);

        var report = _engine.AuditPlan(plan, new Dictionary<string, ActualElementGeometry>
        {
            [actP1.Key] = actP1,
            [actC1.Key] = actC1,
            [actB1.Key] = actB1
        });

        Assert.False(report.OverallPassed);
        Assert.Equal(1, report.VerifiedCount);
        Assert.Equal(2, report.InconclusiveCount);
        Assert.Equal(0, report.DefectCount);
        Assert.Equal(0, report.MissingCount);
    }

    [Fact]
    public void StressTest_ToCoordinationAuditRecord_InconclusiveMapsToChuaDuCoSo()
    {
        var item = CreatePlan(CoordinationCategory.Column, "C1", new PointGeometrySpec(), new() { ["b"] = 220, ["h"] = 220 });
        var act = CreateAct(item, centroid: null, w: 220.0, h: 220.0);

        var audit = _engine.AuditItem(item, new Dictionary<string, ActualElementGeometry> { [act.Key] = act });
        var record = audit.ToCoordinationAuditRecord();

        Assert.Equal(AuditStatus.ChuaDuCoSo, record.Status);
        Assert.Equal(DefectRoute.None, record.DefectRoute);
        Assert.Contains("Rule 0", record.Notes);
    }

    [Fact]
    public void StressTest_ColumnWithPlanDimensions_WhenBothActualDimensionsNull_ReturnsInconclusiveRule0()
    {
        var item = CreatePlan(CoordinationCategory.Column, "C1", new PointGeometrySpec(), new() { ["b"] = 220, ["h"] = 220 });
        var act = CreateAct(item, centroid: new Point3D(0, 0, 0), w: null, h: null);

        var audit = _engine.AuditItem(item, new Dictionary<string, ActualElementGeometry> { [act.Key] = act });

        Assert.Equal(GeometricVerificationStatus.Inconclusive, audit.Status);
        Assert.False(audit.IsVerified);
        Assert.Contains("Rule 0: Column thiếu thuộc tính Width/Height từ Revit API.", audit.DefectReason);
    }

    private static ElementPlanItem CreateItemForCategory(CoordinationCategory cat) => cat switch
    {
        CoordinationCategory.Pile => CreatePlan(cat, "P", new PointGeometrySpec { OffsetZMm = -2500 }),
        CoordinationCategory.Column => CreatePlan(cat, "C", new PointGeometrySpec(), new() { ["b"] = 220, ["h"] = 220 }),
        CoordinationCategory.Beam or CoordinationCategory.GradeBeam => CreatePlan(cat, "B", new LineGeometrySpec()),
        CoordinationCategory.Wall => CreatePlan(cat, "W", new LineGeometrySpec(), new() { ["thickness"] = 200 }),
        CoordinationCategory.Slab or CoordinationCategory.Footing => CreatePlan(cat, "S", new ProfileGeometrySpec { ThicknessMm = 150 }),
        CoordinationCategory.Level => CreatePlan(cat, "L", new LevelGeometrySpec { ElevationM = 3.8 }),
        CoordinationCategory.Grid => CreatePlan(cat, "G", new LineGeometrySpec()),
        _ => throw new ArgumentOutOfRangeException(nameof(cat))
    };
}
