namespace HPBim.Core.Tests.Verification;

using System;
using System.Collections.Generic;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Geometry;
using HPBim.Core.Verification;
using Xunit;

public sealed class StandardizedGeometricMeasurementR5Tests
{
    private readonly MeasuredGeometricEngine _engine = new(new CoordinationTolerances
    {
        SectionDimensionMm = 0.0,
        ElevationMm = 1.0,
        PlanPositionMm = 1.0,
        PileCenterMm = 2.0
    });

    [Fact]
    public void AuditPile_MissingCutoffElevation_ReturnsInconclusiveRule0()
    {
        var item = CreatePileItem("P1", -2500.0);
        var actuals = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Pile:P1"] = new() { Key = "Doc#Pile:P1", CategoryName = "Pile", HasGeometry = true, Centroid = new Point3D(1000, 2000, -2500), TopElevationMm = null }
        };

        var audit = _engine.AuditItem(item, actuals);
        Assert.Equal(GeometricVerificationStatus.Inconclusive, audit.Status);
        Assert.NotNull(audit.DefectReason);
        Assert.Contains("Rule 0", audit.DefectReason);
        Assert.Contains("TopElevationMm", audit.DefectReason);
    }

    [Fact]
    public void AuditPile_ValidCutoffElevationWithinTolerance_ReturnsVerified()
    {
        var item = CreatePileItem("P1", -2500.0);
        var actuals = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Pile:P1"] = new() { Key = "Doc#Pile:P1", CategoryName = "Pile", HasGeometry = true, Centroid = new Point3D(1000, 2001.0, -2500), TopElevationMm = -2500.5 }
        };

        var audit = _engine.AuditItem(item, actuals);
        Assert.Equal(GeometricVerificationStatus.Verified, audit.Status);
        Assert.Equal(0.5, audit.DeltaElevationMm);
        Assert.False(audit.IsElevationOutOfTolerance);
    }

    [Fact]
    public void AuditPile_CutoffElevationExceedsTolerance_ReturnsDefect()
    {
        var item = CreatePileItem("P1", -2500.0);
        var actuals = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Pile:P1"] = new() { Key = "Doc#Pile:P1", CategoryName = "Pile", HasGeometry = true, Centroid = new Point3D(1000, 2000, -2500), TopElevationMm = -2502.5 }
        };

        var audit = _engine.AuditItem(item, actuals);
        Assert.Equal(GeometricVerificationStatus.Defect, audit.Status);
        Assert.True(audit.IsElevationOutOfTolerance);
        Assert.Equal(2.5, audit.DeltaElevationMm);
    }

    [Fact]
    public void AuditGeneric_UnmeasuredCategory_ReturnsInconclusiveRule0()
    {
        var item = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", (CoordinationCategory)999, "GEN1"),
            Category = (CoordinationCategory)999,
            Mark = "GEN1",
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0) }
        };
        var actuals = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#999:GEN1"] = new() { Key = "Doc#999:GEN1", HasGeometry = true, Centroid = new Point3D(0, 0, 0) }
        };

        var audit = _engine.AuditItem(item, actuals);
        Assert.Equal(GeometricVerificationStatus.Inconclusive, audit.Status);
        Assert.NotNull(audit.DefectReason);
        Assert.Contains("Rule 0", audit.DefectReason);
        Assert.Contains("measurement rules", audit.DefectReason);
    }

    [Fact]
    public void AuditProfileStructure_SlabCentroidAndThicknessWithinTolerance_ReturnsVerified()
    {
        var item = CreateSlabItem("S1", 200.0, 0.0);
        var actuals = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Slab:S1"] = new() { Key = "Doc#Slab:S1", CategoryName = "Slab", HasGeometry = true, Centroid = new Point3D(2000.5, 1500.5, 0.0), TopElevationMm = 0.5, ThicknessMm = 200.0 }
        };

        var audit = _engine.AuditItem(item, actuals);
        Assert.Equal(GeometricVerificationStatus.Verified, audit.Status);
        Assert.False(audit.IsPositionOutOfTolerance);
        Assert.False(audit.IsDimensionOutOfTolerance);
    }

    [Fact]
    public void AuditProfileStructure_SlabCentroidExceedsTolerance_ReturnsDefect()
    {
        var item = CreateSlabItem("S1", 200.0, 0.0);
        var actuals = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Slab:S1"] = new() { Key = "Doc#Slab:S1", CategoryName = "Slab", HasGeometry = true, Centroid = new Point3D(2005.0, 1500.0, 0.0), TopElevationMm = 0.0, ThicknessMm = 200.0 }
        };

        var audit = _engine.AuditItem(item, actuals);
        Assert.Equal(GeometricVerificationStatus.Defect, audit.Status);
        Assert.True(audit.IsPositionOutOfTolerance);
    }

    [Fact]
    public void AuditProfileStructure_MissingRequiredThickness_ReturnsInconclusive()
    {
        var item = CreateSlabItem("S1", 200.0, 0.0);
        var actuals = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Slab:S1"] = new() { Key = "Doc#Slab:S1", CategoryName = "Slab", HasGeometry = true, Centroid = new Point3D(2000.0, 1500.0, 0.0), TopElevationMm = 0.0, ThicknessMm = null }
        };

        var audit = _engine.AuditItem(item, actuals);
        Assert.Equal(GeometricVerificationStatus.Inconclusive, audit.Status);
        Assert.Contains("Thickness", audit.DefectReason);
    }

    [Fact]
    public void FindActual_PrioritizesTargetUniqueIdOverSourceKey()
    {
        var item = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Pile, "P1"),
            TargetUniqueId = "target-guid-xyz",
            Category = CoordinationCategory.Pile,
            Mark = "P1",
            Geometry = new PointGeometrySpec { Location = new Point3D(1000, 2000, -2500), OffsetZMm = -2500.0 }
        };

        var actuals = new Dictionary<string, ActualElementGeometry>
        {
            ["target-guid-xyz"] = new() { Key = "target-guid-xyz", UniqueId = "target-guid-xyz", CategoryName = "Pile", HasGeometry = true, Centroid = new Point3D(1000, 2000, -2500), TopElevationMm = -2500.0 },
            ["Doc#Pile:P1"] = new() { Key = "Doc#Pile:P1", UniqueId = "wrong-guid", CategoryName = "Pile", HasGeometry = true, Centroid = new Point3D(9999, 9999, 0), TopElevationMm = 0.0 }
        };

        var audit = _engine.AuditItem(item, actuals);
        Assert.Equal(GeometricVerificationStatus.Verified, audit.Status);
        Assert.Equal("target-guid-xyz", audit.TargetUniqueId);
    }

    [Theory]
    [InlineData(CoordinationCategory.Column, "C1")]
    [InlineData(CoordinationCategory.Beam, "B1")]
    [InlineData(CoordinationCategory.Slab, "S1")]
    [InlineData(CoordinationCategory.Pile, "P1")]
    public void AuditItem_NullPlannedGeometry_ReturnsInconclusiveRule0(CoordinationCategory category, string mark)
    {
        var item = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", category, mark),
            Category = category,
            Mark = mark,
            Geometry = null!
        };
        var actuals = new Dictionary<string, ActualElementGeometry>
        {
            [$"Doc#{category}:{mark}"] = new()
            {
                Key = $"Doc#{category}:{mark}",
                CategoryName = category.ToString(),
                HasGeometry = true,
                Centroid = new Point3D(0, 0, 0),
                CurveStart = new Point3D(0, 0, 0),
                CurveEnd = new Point3D(1000, 0, 0),
                TopElevationMm = 0.0,
                ThicknessMm = 100.0,
                WidthMm = 200.0,
                HeightMm = 200.0
            }
        };

        var audit = _engine.AuditItem(item, actuals);
        Assert.Equal(GeometricVerificationStatus.Inconclusive, audit.Status);
        Assert.Contains("lacks planned geometry specification", audit.DefectReason);
    }

    [Fact]
    public void AuditItem_MismatchedGeometrySpec_ReturnsInconclusiveRule0()
    {
        var item = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1"),
            Category = CoordinationCategory.Column,
            Mark = "C1",
            Geometry = new LineGeometrySpec { Start = new Point3D(0, 0, 0), End = new Point3D(0, 0, 3000) }
        };
        var actuals = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Column:C1"] = new() { Key = "Doc#Column:C1", CategoryName = "Column", HasGeometry = true, Centroid = new Point3D(0, 0, 0), WidthMm = 220, HeightMm = 220 }
        };

        var audit = _engine.AuditItem(item, actuals);
        Assert.Equal(GeometricVerificationStatus.Inconclusive, audit.Status);
        Assert.Contains("expects PointGeometrySpec geometry specification", audit.DefectReason);
    }

    [Fact]
    public void AuditItem_NaNMeasurements_ReturnsInconclusiveRule0()
    {
        var item = CreatePileItem("P1", -2500.0);
        var nanCentroidActual = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Pile:P1"] = new() { Key = "Doc#Pile:P1", CategoryName = "Pile", HasGeometry = true, Centroid = new Point3D(double.NaN, 2000, -2500), TopElevationMm = -2500.0 }
        };
        var auditCentroid = _engine.AuditItem(item, nanCentroidActual);
        Assert.Equal(GeometricVerificationStatus.Inconclusive, auditCentroid.Status);

        var nanElevActual = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Pile:P1"] = new() { Key = "Doc#Pile:P1", CategoryName = "Pile", HasGeometry = true, Centroid = new Point3D(1000, 2000, -2500), TopElevationMm = double.NaN }
        };
        var auditElev = _engine.AuditItem(item, nanElevActual);
        Assert.Equal(GeometricVerificationStatus.Inconclusive, auditElev.Status);
    }

    [Fact]
    public void AuditItem_InfiniteElevation_ReturnsDefect()
    {
        var item = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Beam, "B1"),
            Category = CoordinationCategory.Beam,
            Mark = "B1",
            Geometry = new LineGeometrySpec { Start = new Point3D(0, 0, 0), End = new Point3D(5000, 0, 0), ElevationMm = 3800 }
        };
        var actuals = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Beam:B1"] = new() { Key = "Doc#Beam:B1", CategoryName = "Beam", HasGeometry = true, CurveStart = new Point3D(0, 0, 0), CurveEnd = new Point3D(5000, 0, 0), TopElevationMm = double.PositiveInfinity }
        };

        var audit = _engine.AuditItem(item, actuals);
        Assert.Equal(GeometricVerificationStatus.Defect, audit.Status);
    }

    [Fact]
    public void AuditProfileStructure_DegenerateClosedLoop_ComputesCentroidWithoutCrash()
    {
        var item = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Slab, "S1"),
            Category = CoordinationCategory.Slab,
            Mark = "S1",
            Geometry = new ProfileGeometrySpec
            {
                OuterLoop = new[] { new Point3D(0, 0, 0), new Point3D(1000, 0, 0), new Point3D(0, 0, 0) },
                ElevationMm = 0,
                ThicknessMm = 150
            }
        };
        var actuals = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Slab:S1"] = new() { Key = "Doc#Slab:S1", CategoryName = "Slab", HasGeometry = true, Centroid = new Point3D(500, 0, 0), TopElevationMm = 0, ThicknessMm = 150 }
        };

        var audit = _engine.AuditItem(item, actuals);
        Assert.Equal(GeometricVerificationStatus.Verified, audit.Status);
        Assert.Equal(0.0, audit.DeltaCenterLineMm);
    }

    private static ElementPlanItem CreatePileItem(string mark, double offsetZMm) => new()
    {
        SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Pile, mark),
        Category = CoordinationCategory.Pile,
        Mark = mark,
        Geometry = new PointGeometrySpec { Location = new Point3D(1000, 2000, offsetZMm), OffsetZMm = offsetZMm }
    };

    private static ElementPlanItem CreateSlabItem(string mark, double thickMm, double elevMm) => new()
    {
        SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Slab, mark),
        Category = CoordinationCategory.Slab,
        Mark = mark,
        Geometry = new ProfileGeometrySpec
        {
            OuterLoop = new[] { new Point3D(0, 0, 0), new Point3D(4000, 0, 0), new Point3D(4000, 3000, 0), new Point3D(0, 3000, 0) },
            ThicknessMm = thickMm,
            ElevationMm = elevMm
        }
    };
}
