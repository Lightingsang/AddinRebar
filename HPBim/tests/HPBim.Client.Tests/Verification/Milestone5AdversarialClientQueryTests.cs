namespace HPBim.Client.Tests.Verification;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HPBim.Client.Tests.Fakes;
using HPBim.Client.Verification;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Geometry;
using HPBim.Core.Verification;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

public sealed class Milestone5AdversarialClientQueryTests
{
    [Fact]
    public async Task VerifyPlanGeometryAsync_PileMissingCutoffElevation_FailsOverallPassedWithInconclusive()
    {
        var fake = new FakeRevitPipeClient();
        var payload = new
        {
            total = 1,
            actuals = new[]
            {
                new
                {
                    key = "Doc#Pile:P1",
                    uniqueId = "uid-pile-missing-elev",
                    category = "Pile",
                    mark = "P1",
                    hasGeometry = true,
                    centroid = new { x = 1000.0, y = 2000.0, z = -2500.0 },
                    topElevationMm = (double?)null
                }
            }
        };

        fake.CustomExecuteHandler = _ => new ExecuteResult
        {
            IsError = false,
            Value = BridgeJson.ToElement(payload)
        };

        var plan = new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "plan-pile-missing-elev",
            Items = new[]
            {
                new ElementPlanItem
                {
                    SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Pile, "P1"),
                    TargetUniqueId = "uid-pile-missing-elev",
                    Category = CoordinationCategory.Pile,
                    Mark = "P1",
                    Geometry = new PointGeometrySpec { Location = new Point3D(1000, 2000, -2500), OffsetZMm = -2500.0 }
                }
            }
        };

        var verifier = new MeasuredGeometricVerifier(fake);
        var report = await verifier.VerifyPlanGeometryAsync(plan);

        Assert.False(report.OverallPassed);
        Assert.Equal(0, report.VerifiedCount);
        Assert.Equal(1, report.InconclusiveCount);
        Assert.Equal(0, report.DefectCount);
        Assert.Contains("TopElevationMm", report.Audits[0].DefectReason);
    }

    [Fact]
    public async Task VerifyPlanGeometryAsync_UnmeasuredCategory_FailsOverallPassedWithInconclusive()
    {
        var fake = new FakeRevitPipeClient();
        var payload = new
        {
            total = 1,
            actuals = new[]
            {
                new
                {
                    key = "Doc#Room:R101",
                    uniqueId = "uid-room-101",
                    category = "Room",
                    mark = "R101",
                    hasGeometry = true,
                    centroid = new { x = 2000.0, y = 3000.0, z = 0.0 }
                }
            }
        };

        fake.CustomExecuteHandler = _ => new ExecuteResult
        {
            IsError = false,
            Value = BridgeJson.ToElement(payload)
        };

        var plan = new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "plan-unmeasured-room",
            Items = new[]
            {
                new ElementPlanItem
                {
                    SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Room, "R101"),
                    TargetUniqueId = "uid-room-101",
                    Category = CoordinationCategory.Room,
                    Mark = "R101",
                    Geometry = new PointGeometrySpec { Location = new Point3D(2000, 3000, 0) }
                }
            }
        };

        var verifier = new MeasuredGeometricVerifier(fake);
        var report = await verifier.VerifyPlanGeometryAsync(plan);

        Assert.False(report.OverallPassed);
        Assert.Equal(1, report.InconclusiveCount);
        Assert.Equal(GeometricVerificationStatus.Inconclusive, report.Audits[0].Status);
        Assert.Contains("Rule 0", report.Audits[0].DefectReason);
    }

    [Fact]
    public async Task VerifyPlanGeometryAsync_DuplicateMarkDisambiguatedByTargetUniqueId()
    {
        var fake = new FakeRevitPipeClient();
        var payload = new
        {
            total = 2,
            actuals = new[]
            {
                new
                {
                    key = "uid-c1-north",
                    uniqueId = "uid-c1-north",
                    category = "Column",
                    mark = "C1",
                    hasGeometry = true,
                    centroid = new { x = 0.0, y = 0.0, z = 0.0 },
                    widthMm = 220.0,
                    heightMm = 220.0
                },
                new
                {
                    key = "uid-c1-south",
                    uniqueId = "uid-c1-south",
                    category = "Column",
                    mark = "C1",
                    hasGeometry = true,
                    centroid = new { x = 4000.0, y = 0.0, z = 0.0 },
                    widthMm = 220.0,
                    heightMm = 220.0
                }
            }
        };

        fake.CustomExecuteHandler = _ => new ExecuteResult
        {
            IsError = false,
            Value = BridgeJson.ToElement(payload)
        };

        var itemNorth = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1_N"),
            TargetUniqueId = "uid-c1-north",
            Category = CoordinationCategory.Column,
            Mark = "C1",
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0) },
            Resource = new ResourceResolutionSpec
            {
                DimensionParametersMm = new Dictionary<string, double> { ["b"] = 220.0, ["h"] = 220.0 }
            }
        };

        var itemSouth = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1_S"),
            TargetUniqueId = "uid-c1-south",
            Category = CoordinationCategory.Column,
            Mark = "C1",
            Geometry = new PointGeometrySpec { Location = new Point3D(4000, 0, 0) },
            Resource = new ResourceResolutionSpec
            {
                DimensionParametersMm = new Dictionary<string, double> { ["b"] = 220.0, ["h"] = 220.0 }
            }
        };

        var plan = new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "plan-dup-mark-uid",
            Items = new[] { itemNorth, itemSouth }
        };

        var verifier = new MeasuredGeometricVerifier(fake);
        var report = await verifier.VerifyPlanGeometryAsync(plan);

        Assert.True(report.OverallPassed);
        Assert.Equal(2, report.VerifiedCount);
        Assert.Equal("uid-c1-north", report.Audits[0].TargetUniqueId);
        Assert.Equal("uid-c1-south", report.Audits[1].TargetUniqueId);
    }

    [Fact]
    public async Task VerifyPlanGeometryAsync_TargetUniqueIdPointsToDefect_AuditsDefectRegardlessOfMark()
    {
        var fake = new FakeRevitPipeClient();
        var payload = new
        {
            total = 1,
            actuals = new[]
            {
                new
                {
                    key = "uid-defective-column",
                    uniqueId = "uid-defective-column",
                    category = "Column",
                    mark = "C1",
                    hasGeometry = true,
                    centroid = new { x = 0.0, y = 0.0, z = 0.0 },
                    widthMm = 300.0,
                    heightMm = 300.0
                }
            }
        };

        fake.CustomExecuteHandler = _ => new ExecuteResult
        {
            IsError = false,
            Value = BridgeJson.ToElement(payload)
        };

        var plan = new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "plan-target-uid-defect",
            Items = new[]
            {
                new ElementPlanItem
                {
                    SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1"),
                    TargetUniqueId = "uid-defective-column",
                    Category = CoordinationCategory.Column,
                    Mark = "C1",
                    Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0) },
                    Resource = new ResourceResolutionSpec
                    {
                        DimensionParametersMm = new Dictionary<string, double> { ["b"] = 220.0, ["h"] = 220.0 }
                    }
                }
            }
        };

        var verifier = new MeasuredGeometricVerifier(fake);
        var report = await verifier.VerifyPlanGeometryAsync(plan);

        Assert.False(report.OverallPassed);
        Assert.Equal(1, report.DefectCount);
        Assert.Equal(GeometricVerificationStatus.Defect, report.Audits[0].Status);
        Assert.True(report.Audits[0].IsDimensionOutOfTolerance);
    }
}
