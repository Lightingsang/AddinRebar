namespace HPBim.Core.Tests.Verification;

using System;
using System.Collections.Generic;
using System.Linq;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Geometry;
using HPBim.Core.Verification;
using Xunit;

public class MeasuredGeometricEngineTests
{
    private static ElementPlanItem CreateColumnItem(string mark, double x, double y, double width = 220, double height = 220) => new()
    {
        SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, mark),
        Category = CoordinationCategory.Column,
        Mark = mark,
        Geometry = new PointGeometrySpec { Location = new Point3D(x, y, 0) },
        Resource = new ResourceResolutionSpec
        {
            DimensionParametersMm = new Dictionary<string, double> { ["b"] = width, ["h"] = height }
        }
    };

    private static ElementPlanItem CreateBeamItem(string mark, double x1, double x2, double z, double width = 300, double height = 500) => new()
    {
        SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Beam, mark),
        Category = CoordinationCategory.Beam,
        Mark = mark,
        Geometry = new LineGeometrySpec
        {
            Start = new Point3D(x1, 0, z),
            End = new Point3D(x2, 0, z),
            ElevationMm = z
        },
        Resource = new ResourceResolutionSpec
        {
            DimensionParametersMm = new Dictionary<string, double> { ["b"] = width, ["h"] = height }
        }
    };

    [Fact]
    public void AuditPlan_WhenAllElementsWithinTolerances_ReturnsOverallPassedTrue()
    {
        var plan = new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "hash-1",
            Items = new[] { CreateColumnItem("C1", 0, 0), CreateBeamItem("B1", 0, 5000, 3800) }
        };

        var actuals = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Column:C1"] = new()
            {
                Key = "Doc#Column:C1",
                CategoryName = "Column",
                HasGeometry = true,
                Centroid = new Point3D(0.5, 0.5, 0),
                WidthMm = 220.0,
                HeightMm = 220.0
            },
            ["Doc#Beam:B1"] = new()
            {
                Key = "Doc#Beam:B1",
                CategoryName = "Beam",
                HasGeometry = true,
                CurveStart = new Point3D(0.5, 0, 3800),
                CurveEnd = new Point3D(5000.5, 0, 3800),
                TopElevationMm = 3800.5,
                WidthMm = 300.0,
                HeightMm = 500.0
            }
        };

        var engine = new MeasuredGeometricEngine();
        var report = engine.AuditPlan(plan, actuals);

        Assert.True(report.OverallPassed);
        Assert.Equal(2, report.TotalPlannedElements);
        Assert.Equal(2, report.VerifiedCount);
        Assert.Equal(0, report.DefectCount);
        Assert.Empty(report.SummaryDiscrepancies);
    }

    [Fact]
    public void AuditPlan_WhenColumnWidthHasDefect_ReturnsOverallPassedFalse_EvenIfTotalCountMatches100Percent()
    {
        var items = new List<ElementPlanItem>();
        var actuals = new Dictionary<string, ActualElementGeometry>();

        for (int i = 1; i <= 10; i++)
        {
            string mark = $"C{i}";
            items.Add(CreateColumnItem(mark, i * 1000, 0, 220, 220));

            double measuredWidth = (i == 5) ? 200.0 : 220.0;
            actuals[$"Doc#Column:{mark}"] = new()
            {
                Key = $"Doc#Column:{mark}",
                CategoryName = "Column",
                HasGeometry = true,
                Centroid = new Point3D(i * 1000, 0, 0),
                WidthMm = measuredWidth,
                HeightMm = 220.0
            };
        }

        var plan = new BimExecutionPlan { PlanId = Guid.NewGuid(), PlanHash = "hash-10cols", Items = items };
        var engine = new MeasuredGeometricEngine();
        var report = engine.AuditPlan(plan, actuals);

        Assert.Equal(10, report.TotalPlannedElements);
        Assert.Equal(10, report.TotalMeasuredElements);
        Assert.Equal(9, report.VerifiedCount);
        Assert.Equal(1, report.DefectCount);
        Assert.False(report.OverallPassed);

        var c5Audit = report.Audits.First(a => a.Mark == "C5");
        Assert.Equal(GeometricVerificationStatus.Defect, c5Audit.Status);
        Assert.True(c5Audit.IsDimensionOutOfTolerance);
        Assert.Equal(20.0, c5Audit.DeltaWidthMm);
    }

    [Fact]
    public void AuditPlan_WhenElementHasNoGeometry_EnforcesRule0_ReturnsInconclusive()
    {
        var plan = new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "hash-empty-geom",
            Items = new[] { CreateColumnItem("C1", 0, 0) }
        };

        var actuals = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Column:C1"] = new()
            {
                Key = "Doc#Column:C1",
                CategoryName = "Column",
                HasGeometry = false
            }
        };

        var engine = new MeasuredGeometricEngine();
        var report = engine.AuditPlan(plan, actuals);

        Assert.False(report.OverallPassed);
        Assert.Equal(1, report.InconclusiveCount);
        var audit = report.Audits.Single();
        Assert.Equal(GeometricVerificationStatus.Inconclusive, audit.Status);
        Assert.Contains("Rule 0", audit.DefectReason);
    }

    [Fact]
    public void AuditPlan_WhenPlannedElementMissingInRevit_ReturnsMissing_AndOverallPassedFalse()
    {
        var plan = new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "hash-missing",
            Items = new[] { CreateColumnItem("C1", 0, 0), CreateColumnItem("C2", 1000, 0) }
        };

        var actuals = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Column:C1"] = new()
            {
                Key = "Doc#Column:C1",
                CategoryName = "Column",
                HasGeometry = true,
                Centroid = new Point3D(0, 0, 0),
                WidthMm = 220.0,
                HeightMm = 220.0
            }
        };

        var engine = new MeasuredGeometricEngine();
        var report = engine.AuditPlan(plan, actuals);

        Assert.False(report.OverallPassed);
        Assert.Equal(1, report.MissingCount);
        Assert.Equal(1, report.VerifiedCount);
        var c2Audit = report.Audits.First(a => a.Mark == "C2");
        Assert.Equal(GeometricVerificationStatus.Missing, c2Audit.Status);
    }

    [Fact]
    public void ElementGeometricAudit_ToCoordinationAuditRecord_TranslatesStatusesAndRoutesCorrectly()
    {
        var verified = new ElementGeometricAudit { Status = GeometricVerificationStatus.Verified };
        var defect = new ElementGeometricAudit { Status = GeometricVerificationStatus.Defect, DeltaWidthMm = 5.0 };
        var inconclusive = new ElementGeometricAudit { Status = GeometricVerificationStatus.Inconclusive };

        Assert.Equal(AuditStatus.Dung, verified.ToCoordinationAuditRecord().Status);
        Assert.Equal(DefectRoute.None, verified.ToCoordinationAuditRecord().DefectRoute);

        Assert.Equal(AuditStatus.SaiCanSua, defect.ToCoordinationAuditRecord().Status);
        Assert.Equal(DefectRoute.RevitFix, defect.ToCoordinationAuditRecord().DefectRoute);
        Assert.Equal(5.0, defect.ToCoordinationAuditRecord().DeltaDimensionMm);

        Assert.Equal(AuditStatus.ChuaDuCoSo, inconclusive.ToCoordinationAuditRecord().Status);
    }

    [Fact]
    public void AuditPlan_WhenElementHasUnmeasurableSectionDimension_FlagsDiscrepancyAndDefect()
    {
        var plan = new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "hash-unmeasurable-dim",
            Items = new[] { CreateBeamItem("B1", 0, 5000, 3800, width: 300, height: 500) }
        };

        var actuals = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Beam:B1"] = new()
            {
                Key = "Doc#Beam:B1",
                CategoryName = "Beam",
                HasGeometry = true,
                CurveStart = new Point3D(0, 0, 3800),
                CurveEnd = new Point3D(5000, 0, 3800),
                TopElevationMm = 3800,
                WidthMm = null,
                HeightMm = 500.0
            }
        };

        var engine = new MeasuredGeometricEngine();
        var report = engine.AuditPlan(plan, actuals);

        Assert.False(report.OverallPassed);
        Assert.Equal(1, report.DefectCount);
        var audit = report.Audits.Single();
        Assert.Equal(GeometricVerificationStatus.Defect, audit.Status);
        Assert.Contains("Beam width: Expected 300.0mm, but actual parameter was unmeasurable (null)", audit.DefectReason);
    }
}
