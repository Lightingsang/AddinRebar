namespace HPBim.Core.Tests.Adversarial;

using System;
using System.Collections.Generic;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Geometry;
using HPBim.Core.Verification;
using Xunit;

public sealed class MeasuredGeometricVerifierAdversarialCoverageTests
{
    private readonly MeasuredGeometricEngine _engine = new(new CoordinationTolerances
    {
        SectionDimensionMm = 0.0,
        ElevationMm = 1.0,
        PlanPositionMm = 1.0,
        PileCenterMm = 2.0
    });

    [Fact]
    public void AuditColumn_DecimalRoundingNearToleranceBoundaries_StrictZeroMm()
    {
        var item = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1"),
            Category = CoordinationCategory.Column,
            Mark = "C1",
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0) },
            Resource = new ResourceResolutionSpec
            {
                DimensionParametersMm = new Dictionary<string, double> { ["b"] = 220.0, ["h"] = 220.0 }
            }
        };

        var actualPass = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Column:C1"] = new()
            {
                Key = "Doc#Column:C1", HasGeometry = true, Centroid = new Point3D(0, 0, 0), WidthMm = 220.0, HeightMm = 220.0
            }
        };
        var auditPass = _engine.AuditItem(item, actualPass);
        Assert.Equal(GeometricVerificationStatus.Verified, auditPass.Status);
        Assert.False(auditPass.IsDimensionOutOfTolerance);

        var actualFail = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Column:C1"] = new()
            {
                Key = "Doc#Column:C1", HasGeometry = true, Centroid = new Point3D(0, 0, 0), WidthMm = 220.001, HeightMm = 220.0
            }
        };
        var auditFail = _engine.AuditItem(item, actualFail);
        Assert.Equal(GeometricVerificationStatus.Defect, auditFail.Status);
        Assert.True(auditFail.IsDimensionOutOfTolerance);
    }

    [Fact]
    public void AuditBeam_ElevationBoundary_Strict1mmThreshold()
    {
        var item = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Beam, "B1"),
            Category = CoordinationCategory.Beam,
            Mark = "B1",
            Geometry = new LineGeometrySpec { Start = new(0, 0, 3800), End = new(4000, 0, 3800), ElevationMm = 3800.0 }
        };

        var actualAtLimit = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Beam:B1"] = new()
            {
                Key = "Doc#Beam:B1", HasGeometry = true, CurveStart = new(0, 0, 3800), CurveEnd = new(4000, 0, 3800), TopElevationMm = 3801.000
            }
        };
        Assert.Equal(GeometricVerificationStatus.Verified, _engine.AuditItem(item, actualAtLimit).Status);

        var actualOverLimit = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Beam:B1"] = new()
            {
                Key = "Doc#Beam:B1", HasGeometry = true, CurveStart = new(0, 0, 3800), CurveEnd = new(4000, 0, 3800), TopElevationMm = 3801.002
            }
        };
        var auditOver = _engine.AuditItem(item, actualOverLimit);
        Assert.Equal(GeometricVerificationStatus.Defect, auditOver.Status);
        Assert.True(auditOver.IsElevationOutOfTolerance);
    }

    [Fact]
    public void AuditPile_CenterBoundary_Strict2mmThreshold()
    {
        var item = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Pile, "P1"),
            Category = CoordinationCategory.Pile,
            Mark = "P1",
            Geometry = new PointGeometrySpec { Location = new Point3D(1000, 2000, -2500) }
        };

        var actualAtLimit = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Pile:P1"] = new() { Key = "Doc#Pile:P1", HasGeometry = true, Centroid = new Point3D(1000, 2002.000, -2500), TopElevationMm = 0.0 }
        };
        Assert.Equal(GeometricVerificationStatus.Verified, _engine.AuditItem(item, actualAtLimit).Status);

        var actualOverLimit = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Pile:P1"] = new() { Key = "Doc#Pile:P1", HasGeometry = true, Centroid = new Point3D(1000, 2002.005, -2500), TopElevationMm = 0.0 }
        };
        var auditOver = _engine.AuditItem(item, actualOverLimit);
        Assert.Equal(GeometricVerificationStatus.Defect, auditOver.Status);
        Assert.True(auditOver.IsPositionOutOfTolerance);
    }

    [Fact]
    public void AuditLinearStructure_VN2000_ExtremeCoordinatesScaleAndOffsets()
    {
        double baseX = 584_320_000.0;
        double baseY = 1_195_450_000.0;

        var item = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("VN2000", CoordinationCategory.GradeBeam, "GB1"),
            Category = CoordinationCategory.GradeBeam,
            Mark = "GB1",
            Geometry = new LineGeometrySpec
            {
                Start = new Point3D(baseX, baseY, 0),
                End = new Point3D(baseX + 6000, baseY, 0),
                ElevationMm = 0.0
            }
        };

        var actualWithinTol = new Dictionary<string, ActualElementGeometry>
        {
            ["VN2000#GradeBeam:GB1"] = new()
            {
                Key = "VN2000#GradeBeam:GB1",
                HasGeometry = true,
                CurveStart = new Point3D(baseX + 0.5, baseY, 0),
                CurveEnd = new Point3D(baseX + 6000.5, baseY, 0),
                TopElevationMm = 0.5
            }
        };

        var audit = _engine.AuditItem(item, actualWithinTol);
        Assert.Equal(GeometricVerificationStatus.Verified, audit.Status);
        Assert.False(audit.IsPositionOutOfTolerance);
        Assert.False(audit.IsElevationOutOfTolerance);
    }

    [Fact]
    public void AuditWall_EndpointOffsetExceedsTolerance_FlaggedAsDefect()
    {
        var item = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Wall, "W1"),
            Category = CoordinationCategory.Wall,
            Mark = "W1",
            Geometry = new LineGeometrySpec { Start = new(0, 0, 0), End = new(5000, 0, 0) }
        };

        var actual = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Wall:W1"] = new()
            {
                Key = "Doc#Wall:W1", HasGeometry = true, CurveStart = new(0.5, 0, 0), CurveEnd = new(5000, 1.25, 0)
            }
        };

        var audit = _engine.AuditItem(item, actual);
        Assert.Equal(GeometricVerificationStatus.Defect, audit.Status);
        Assert.True(audit.IsPositionOutOfTolerance);
    }

    [Fact]
    public void AuditLevel_ScaleConversionFromMetersToMillimeters()
    {
        var item = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Level, "L2"),
            Category = CoordinationCategory.Level,
            Mark = "L2",
            Geometry = new LevelGeometrySpec { ElevationM = 7.200, FloorNumber = 2 }
        };

        var passActual = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Level:L2"] = new() { Key = "Doc#Level:L2", HasGeometry = true, TopElevationMm = 7200.75 }
        };
        Assert.Equal(GeometricVerificationStatus.Verified, _engine.AuditItem(item, passActual).Status);

        var failActual = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Level:L2"] = new() { Key = "Doc#Level:L2", HasGeometry = true, TopElevationMm = 7201.50 }
        };
        var failAudit = _engine.AuditItem(item, failActual);
        Assert.Equal(GeometricVerificationStatus.Defect, failAudit.Status);
        Assert.True(failAudit.IsElevationOutOfTolerance);
    }

    [Fact]
    public void AuditItem_UnmeasurableCurve_Rule0_Inconclusive_NeverPass()
    {
        var item = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Beam, "B1"),
            Category = CoordinationCategory.Beam,
            Mark = "B1",
            Geometry = new LineGeometrySpec { Start = new(0, 0, 0), End = new(3000, 0, 0) }
        };

        var actualNoGeom = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Beam:B1"] = new() { Key = "Doc#Beam:B1", HasGeometry = false }
        };

        var audit = _engine.AuditItem(item, actualNoGeom);
        Assert.Equal(GeometricVerificationStatus.Inconclusive, audit.Status);
        Assert.Contains("Rule 0", audit.DefectReason);

        var auditRecord = audit.ToCoordinationAuditRecord();
        Assert.Equal(AuditStatus.ChuaDuCoSo, auditRecord.Status);
        Assert.Equal(DefectRoute.None, auditRecord.DefectRoute);
    }

    [Fact]
    public void AuditPlan_ListOverload_DictionaryMappingByUniqueIdAndKey()
    {
        var plan = new BimExecutionPlan
        {
            Items = new[]
            {
                new ElementPlanItem
                {
                    SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1"),
                    Category = CoordinationCategory.Column,
                    Mark = "C1",
                    Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0) }
                }
            }
        };

        var actualsList = new List<ActualElementGeometry>
        {
            new() { Key = "Doc#Column:C1", UniqueId = "uid-12345", HasGeometry = true, Centroid = new Point3D(0, 0, 0) }
        };

        var report = _engine.AuditPlan(plan, actualsList);
        Assert.True(report.OverallPassed);
        Assert.Equal(1, report.VerifiedCount);
    }
}
