namespace HPBim.Core.Tests.Adversarial;

using System;
using System.Collections.Generic;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Geometry;
using HPBim.Core.Verification;
using Xunit;

public sealed class Milestone5AdversarialChallengeTests
{
    private readonly MeasuredGeometricEngine _defaultEngine = new(new CoordinationTolerances
    {
        SectionDimensionMm = 0.0,
        ElevationMm = 1.0,
        PlanPositionMm = 1.0,
        PileCenterMm = 2.0
    });

    [Fact]
    public void PileCutoffElevation_NullElevation_ReturnsInconclusive()
    {
        var item = CreatePileItem("P1", -2500.0);
        var actuals = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Pile:P1"] = new()
            {
                Key = "Doc#Pile:P1",
                CategoryName = "Pile",
                HasGeometry = true,
                Centroid = new Point3D(1000, 2000, -2500),
                TopElevationMm = null
            }
        };

        var audit = _defaultEngine.AuditItem(item, actuals);

        Assert.Equal(GeometricVerificationStatus.Inconclusive, audit.Status);
        Assert.NotNull(audit.DefectReason);
        Assert.Contains("Rule 0", audit.DefectReason);
        Assert.Contains("TopElevationMm", audit.DefectReason);
    }

    [Theory]
    [InlineData(-2500.0, 0.0, GeometricVerificationStatus.Verified, false)]
    [InlineData(-2499.0, 1.0, GeometricVerificationStatus.Verified, false)]
    [InlineData(-2501.0, 1.0, GeometricVerificationStatus.Verified, false)]
    [InlineData(-2498.999, 1.001, GeometricVerificationStatus.Defect, true)]
    [InlineData(-2501.001, 1.001, GeometricVerificationStatus.Defect, true)]
    public void PileCutoffElevation_ToleranceBoundaries_StrictVerification(
        double actualElev,
        double expectedDelta,
        GeometricVerificationStatus expectedStatus,
        bool expectedOutOfTol)
    {
        var item = CreatePileItem("P1", -2500.0);
        var actuals = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Pile:P1"] = new()
            {
                Key = "Doc#Pile:P1",
                CategoryName = "Pile",
                HasGeometry = true,
                Centroid = new Point3D(1000, 2000, -2500),
                TopElevationMm = actualElev
            }
        };

        var audit = _defaultEngine.AuditItem(item, actuals);

        Assert.Equal(expectedStatus, audit.Status);
        Assert.Equal(expectedOutOfTol, audit.IsElevationOutOfTolerance);
        Assert.Equal(expectedDelta, audit.DeltaElevationMm, 3);
    }

    [Fact]
    public void Pile_CenterPositionExceedsTolerance_ReturnsDefectEvenIfElevationMatches()
    {
        var item = CreatePileItem("P1", -2500.0);
        var actuals = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Pile:P1"] = new()
            {
                Key = "Doc#Pile:P1",
                CategoryName = "Pile",
                HasGeometry = true,
                Centroid = new Point3D(1000, 2002.001, -2500),
                TopElevationMm = -2500.0
            }
        };

        var audit = _defaultEngine.AuditItem(item, actuals);

        Assert.Equal(GeometricVerificationStatus.Defect, audit.Status);
        Assert.True(audit.IsPositionOutOfTolerance);
        Assert.False(audit.IsElevationOutOfTolerance);
    }

    [Theory]
    [InlineData(CoordinationCategory.Opening)]
    [InlineData(CoordinationCategory.Room)]
    [InlineData(CoordinationCategory.MepFixture)]
    [InlineData(CoordinationCategory.PipeRun)]
    [InlineData(CoordinationCategory.Rebar)]
    [InlineData((CoordinationCategory)999)]
    public void GenericCategory_AlwaysReturnsInconclusiveNeverVerified(CoordinationCategory category)
    {
        var item = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", category, "ELEM-01"),
            Category = category,
            Mark = "ELEM-01",
            Geometry = new PointGeometrySpec { Location = new Point3D(100, 200, 300) }
        };
        var actuals = new Dictionary<string, ActualElementGeometry>
        {
            [item.SourceKey.ToString()] = new()
            {
                Key = item.SourceKey.ToString(),
                HasGeometry = true,
                Centroid = new Point3D(100, 200, 300),
                TopElevationMm = 300.0,
                WidthMm = 500.0,
                HeightMm = 500.0
            }
        };

        var audit = _defaultEngine.AuditItem(item, actuals);

        Assert.Equal(GeometricVerificationStatus.Inconclusive, audit.Status);
        Assert.NotNull(audit.DefectReason);
        Assert.Contains("Rule 0", audit.DefectReason);
    }

    [Fact]
    public void ProfileSlab_CentroidOffsetBoundaries_StrictPlanPositionTolerance()
    {
        var item = CreateSlabItem("S1", 200.0, 0.0);
        var actualPass = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Slab:S1"] = new()
            {
                Key = "Doc#Slab:S1",
                CategoryName = "Slab",
                HasGeometry = true,
                Centroid = new Point3D(2001.0, 1500.0, 0.0),
                TopElevationMm = 0.0,
                ThicknessMm = 200.0
            }
        };

        var passAudit = _defaultEngine.AuditItem(item, actualPass);
        Assert.Equal(GeometricVerificationStatus.Verified, passAudit.Status);
        Assert.False(passAudit.IsPositionOutOfTolerance);

        var actualFail = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Slab:S1"] = new()
            {
                Key = "Doc#Slab:S1",
                CategoryName = "Slab",
                HasGeometry = true,
                Centroid = new Point3D(2001.001, 1500.0, 0.0),
                TopElevationMm = 0.0,
                ThicknessMm = 200.0
            }
        };

        var failAudit = _defaultEngine.AuditItem(item, actualFail);
        Assert.Equal(GeometricVerificationStatus.Defect, failAudit.Status);
        Assert.True(failAudit.IsPositionOutOfTolerance);
    }

    [Fact]
    public void ProfileSlab_ThicknessOffsetBoundaries_StrictZeroMmDefaultTolerance()
    {
        var item = CreateSlabItem("S1", 200.0, 0.0);
        var actualExact = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Slab:S1"] = new()
            {
                Key = "Doc#Slab:S1",
                CategoryName = "Slab",
                HasGeometry = true,
                Centroid = new Point3D(2000.0, 1500.0, 0.0),
                TopElevationMm = 0.0,
                ThicknessMm = 200.0
            }
        };

        var exactAudit = _defaultEngine.AuditItem(item, actualExact);
        Assert.Equal(GeometricVerificationStatus.Verified, exactAudit.Status);
        Assert.False(exactAudit.IsDimensionOutOfTolerance);

        var actualSlightDiff = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Slab:S1"] = new()
            {
                Key = "Doc#Slab:S1",
                CategoryName = "Slab",
                HasGeometry = true,
                Centroid = new Point3D(2000.0, 1500.0, 0.0),
                TopElevationMm = 0.0,
                ThicknessMm = 200.001
            }
        };

        var slightAudit = _defaultEngine.AuditItem(item, actualSlightDiff);
        Assert.Equal(GeometricVerificationStatus.Defect, slightAudit.Status);
        Assert.True(slightAudit.IsDimensionOutOfTolerance);
    }

    [Fact]
    public void ProfileSlab_MissingCentroidOrTopElevation_ReturnsInconclusive()
    {
        var item = CreateSlabItem("S1", 200.0, 0.0);
        var missingCentroid = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Slab:S1"] = new()
            {
                Key = "Doc#Slab:S1",
                HasGeometry = true,
                Centroid = null,
                TopElevationMm = 0.0,
                ThicknessMm = 200.0
            }
        };

        var audit1 = _defaultEngine.AuditItem(item, missingCentroid);
        Assert.Equal(GeometricVerificationStatus.Inconclusive, audit1.Status);

        var missingElev = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Slab:S1"] = new()
            {
                Key = "Doc#Slab:S1",
                HasGeometry = true,
                Centroid = new Point3D(2000, 1500, 0),
                TopElevationMm = null,
                ThicknessMm = 200.0
            }
        };

        var audit2 = _defaultEngine.AuditItem(item, missingElev);
        Assert.Equal(GeometricVerificationStatus.Inconclusive, audit2.Status);
    }

    [Fact]
    public void TargetUniqueId_DuplicateMarkDisambiguation_CorrectlyResolved()
    {
        var item1 = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1_North"),
            TargetUniqueId = "rvt-col-north-uid",
            Category = CoordinationCategory.Column,
            Mark = "COL-1",
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0) },
            Resource = new ResourceResolutionSpec
            {
                DimensionParametersMm = new Dictionary<string, double> { ["b"] = 220.0, ["h"] = 220.0 }
            }
        };

        var item2 = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1_South"),
            TargetUniqueId = "rvt-col-south-uid",
            Category = CoordinationCategory.Column,
            Mark = "COL-1",
            Geometry = new PointGeometrySpec { Location = new Point3D(5000, 5000, 0) },
            Resource = new ResourceResolutionSpec
            {
                DimensionParametersMm = new Dictionary<string, double> { ["b"] = 220.0, ["h"] = 220.0 }
            }
        };

        var plan = new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "plan-duplicate-mark",
            Items = new[] { item1, item2 }
        };

        var actualsList = new[]
        {
            new ActualElementGeometry
            {
                Key = "Doc#Column:C1_North",
                UniqueId = "rvt-col-north-uid",
                HasGeometry = true,
                Centroid = new Point3D(0, 0, 0),
                WidthMm = 220.0,
                HeightMm = 220.0
            },
            new ActualElementGeometry
            {
                Key = "Doc#Column:C1_South",
                UniqueId = "rvt-col-south-uid",
                HasGeometry = true,
                Centroid = new Point3D(5000, 5000, 0),
                WidthMm = 220.0,
                HeightMm = 220.0
            }
        };

        var report = _defaultEngine.AuditPlan(plan, actualsList);

        Assert.True(report.OverallPassed);
        Assert.Equal(2, report.VerifiedCount);
        Assert.Equal("rvt-col-north-uid", report.Audits[0].TargetUniqueId);
        Assert.Equal("rvt-col-south-uid", report.Audits[1].TargetUniqueId);
    }

    [Fact]
    public void TargetUniqueId_TakesPriorityOverDefectiveMarkMatch()
    {
        var item = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1"),
            TargetUniqueId = "correct-uid",
            Category = CoordinationCategory.Column,
            Mark = "C1",
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0) },
            Resource = new ResourceResolutionSpec
            {
                DimensionParametersMm = new Dictionary<string, double> { ["b"] = 220.0, ["h"] = 220.0 }
            }
        };

        var actuals = new Dictionary<string, ActualElementGeometry>
        {
            ["correct-uid"] = new()
            {
                Key = "correct-uid",
                UniqueId = "correct-uid",
                HasGeometry = true,
                Centroid = new Point3D(0, 0, 0),
                WidthMm = 220.0,
                HeightMm = 220.0
            },
            ["C1"] = new()
            {
                Key = "C1",
                UniqueId = "wrong-uid",
                HasGeometry = true,
                Centroid = new Point3D(999, 999, 0),
                WidthMm = 500.0,
                HeightMm = 500.0
            }
        };

        var audit = _defaultEngine.AuditItem(item, actuals);

        Assert.Equal(GeometricVerificationStatus.Verified, audit.Status);
        Assert.Equal("correct-uid", audit.TargetUniqueId);
    }

    private static ElementPlanItem CreatePileItem(string mark, double offsetZMm) => new()
    {
        SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Pile, mark),
        Category = CoordinationCategory.Pile,
        Mark = mark,
        Geometry = new PointGeometrySpec
        {
            Location = new Point3D(1000, 2000, offsetZMm),
            OffsetZMm = offsetZMm
        }
    };

    private static ElementPlanItem CreateSlabItem(string mark, double thickMm, double elevMm) => new()
    {
        SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Slab, mark),
        Category = CoordinationCategory.Slab,
        Mark = mark,
        Geometry = new ProfileGeometrySpec
        {
            OuterLoop = new[]
            {
                new Point3D(0, 0, 0),
                new Point3D(4000, 0, 0),
                new Point3D(4000, 3000, 0),
                new Point3D(0, 3000, 0)
            },
            ThicknessMm = thickMm,
            ElevationMm = elevMm
        }
    };
}
