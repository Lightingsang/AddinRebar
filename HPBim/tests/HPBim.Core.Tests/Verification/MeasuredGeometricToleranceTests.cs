namespace HPBim.Core.Tests.Verification;

using System;
using System.Collections.Generic;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Geometry;
using HPBim.Core.Verification;
using Xunit;

public class MeasuredGeometricToleranceTests
{
    [Fact]
    public void AuditPlan_WhenBeamElevationExceedsTolerance_ReturnsOverallPassedFalse()
    {
        var plan = new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "hash-beam",
            Items = new[]
            {
                new ElementPlanItem
                {
                    SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Beam, "B1"),
                    Category = CoordinationCategory.Beam,
                    Mark = "B1",
                    Geometry = new LineGeometrySpec
                    {
                        Start = new Point3D(0, 0, 3800),
                        End = new Point3D(4000, 0, 3800),
                        ElevationMm = 3800
                    }
                }
            }
        };

        var actuals = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Beam:B1"] = new()
            {
                Key = "Doc#Beam:B1",
                CategoryName = "Beam",
                HasGeometry = true,
                CurveStart = new Point3D(0, 0, 3802.5),
                CurveEnd = new Point3D(4000, 0, 3802.5),
                TopElevationMm = 3802.5
            }
        };

        var engine = new MeasuredGeometricEngine();
        var report = engine.AuditPlan(plan, actuals);

        Assert.False(report.OverallPassed);
        var audit = report.Audits[0];
        Assert.Equal(GeometricVerificationStatus.Defect, audit.Status);
        Assert.True(audit.IsElevationOutOfTolerance);
        Assert.Equal(2.5, audit.DeltaElevationMm);
    }

    [Fact]
    public void AuditPlan_WhenPileCenterlineExceedsTolerance_ReturnsDefect()
    {
        var plan = new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "hash-pile",
            Items = new[]
            {
                new ElementPlanItem
                {
                    SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Pile, "P1"),
                    Category = CoordinationCategory.Pile,
                    Mark = "P1",
                    Geometry = new PointGeometrySpec { Location = new Point3D(1500, 3000, -2500), OffsetZMm = -2500.0 }
                }
            }
        };

        var actuals = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Pile:P1"] = new()
            {
                Key = "Doc#Pile:P1",
                CategoryName = "Pile",
                HasGeometry = true,
                Centroid = new Point3D(1500, 3002.5, -2500),
                TopElevationMm = -2500.0
            }
        };

        var engine = new MeasuredGeometricEngine();
        var report = engine.AuditPlan(plan, actuals);

        Assert.False(report.OverallPassed);
        var audit = report.Audits[0];
        Assert.Equal(GeometricVerificationStatus.Defect, audit.Status);
        Assert.True(audit.IsPositionOutOfTolerance);
        Assert.Equal(2.5, audit.DeltaCenterLineMm);
    }

    [Fact]
    public void AuditPlan_WhenFootingThicknessExceedsTolerance_ReturnsDefect()
    {
        var plan = new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "hash-footing",
            Items = new[]
            {
                new ElementPlanItem
                {
                    SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Footing, "F1"),
                    Category = CoordinationCategory.Footing,
                    Mark = "F1",
                    Geometry = new ProfileGeometrySpec { ThicknessMm = 1000.0, ElevationMm = -1600.0 }
                }
            }
        };

        var actuals = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Footing:F1"] = new()
            {
                Key = "Doc#Footing:F1",
                CategoryName = "Footing",
                HasGeometry = true,
                Centroid = new Point3D(0, 0, -1600),
                TopElevationMm = -1600.0,
                ThicknessMm = 1050.0
            }
        };

        var engine = new MeasuredGeometricEngine();
        var report = engine.AuditPlan(plan, actuals);

        Assert.False(report.OverallPassed);
        var audit = report.Audits[0];
        Assert.Equal(GeometricVerificationStatus.Defect, audit.Status);
        Assert.True(audit.IsDimensionOutOfTolerance);
    }

    [Fact]
    public void AuditPlan_BoundaryValues_StrictToleranceValidation()
    {
        var tolerances = new CoordinationTolerances
        {
            SectionDimensionMm = 0.0,
            ElevationMm = 1.0,
            PlanPositionMm = 1.0,
            PileCenterMm = 2.0
        };

        var engine = new MeasuredGeometricEngine(tolerances);
        var pileItem = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Pile, "P1"),
            Category = CoordinationCategory.Pile,
            Mark = "P1",
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0), OffsetZMm = 0.0 }
        };

        var passActual = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Pile:P1"] = new() { Key = "Doc#Pile:P1", CategoryName = "Pile", HasGeometry = true, Centroid = new Point3D(2.0, 0, 0), TopElevationMm = 0.0 }
        };
        var failActual = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Pile:P1"] = new() { Key = "Doc#Pile:P1", CategoryName = "Pile", HasGeometry = true, Centroid = new Point3D(2.05, 0, 0), TopElevationMm = 0.0 }
        };

        Assert.Equal(GeometricVerificationStatus.Verified, engine.AuditItem(pileItem, passActual).Status);
        Assert.Equal(GeometricVerificationStatus.Defect, engine.AuditItem(pileItem, failActual).Status);
    }
}
