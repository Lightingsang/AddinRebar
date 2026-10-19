namespace HPBim.Client.Tests.Verification;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HPBim.Client.Connection;
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

public sealed class StandardizedGeometricQueryR5Tests
{
    [Fact]
    public void BuildQueryScript_IncludesTargetUniqueIdLookup()
    {
        var plan = new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "test-hash-uid",
            Items = new[]
            {
                new ElementPlanItem
                {
                    SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1"),
                    TargetUniqueId = "revit-unique-id-abc-123",
                    Category = CoordinationCategory.Column,
                    Mark = "C1",
                    Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0) }
                }
            }
        };

        string script = RevitGeometryQueryScriptTemplate.GetScriptCode();

        Assert.Contains("targetUniqueId", script);
        Assert.Contains("doc.GetElement(targetUniqueId)", script);
    }

    [Fact]
    public void BuildQueryScript_ExtractsPileCutoffElevation()
    {
        var plan = new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "test-hash-pile",
            Items = new[]
            {
                new ElementPlanItem
                {
                    SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Pile, "P1"),
                    Category = CoordinationCategory.Pile,
                    Mark = "P1",
                    Geometry = new PointGeometrySpec { Location = new Point3D(1000, 2000, -2500), OffsetZMm = -2500.0 }
                }
            }
        };

        string script = RevitGeometryQueryScriptTemplate.GetScriptCode();

        Assert.Contains("bbPile.Max.Z", script);
        Assert.Contains("record[\"topElevationMm\"]", script);
    }

    [Fact]
    public void BuildQueryScript_ExtractsFloorDimensionsAndThickness()
    {
        var loop = new[]
        {
            new Point3D(0, 0, 0),
            new Point3D(5000, 0, 0),
            new Point3D(5000, 4000, 0),
            new Point3D(0, 4000, 0)
        };

        var plan = new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "test-hash-slab",
            Items = new[]
            {
                new ElementPlanItem
                {
                    SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Slab, "S1"),
                    Category = CoordinationCategory.Slab,
                    Mark = "S1",
                    Geometry = new ProfileGeometrySpec { OuterLoop = loop, ThicknessMm = 200.0, ElevationMm = 0.0 }
                }
            }
        };

        string script = RevitGeometryQueryScriptTemplate.GetScriptCode();

        Assert.Contains("elem is Floor floorObj", script);
        Assert.Contains("record[\"widthMm\"]", script);
        Assert.Contains("record[\"heightMm\"]", script);
        Assert.Contains("record[\"thicknessMm\"]", script);
    }

    [Fact]
    public async Task VerifyPlanGeometryAsync_PrioritizesTargetUniqueIdAndExtractsMeasurements()
    {
        var fake = new FakeRevitPipeClient();
        var payload = new
        {
            total = 1,
            actuals = new[]
            {
                new
                {
                    key = "mapped-guid-pile-01",
                    uniqueId = "mapped-guid-pile-01",
                    category = "Pile",
                    mark = "P1",
                    hasGeometry = true,
                    centroid = new { x = 1000.0, y = 2000.0, z = -2500.0 },
                    topElevationMm = -2500.0
                }
            }
        };

        fake.CustomExecuteHandler = req => new ExecuteResult
        {
            IsError = false,
            Value = BridgeJson.ToElement(payload)
        };

        var plan = new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "plan-uid-verify",
            Items = new[]
            {
                new ElementPlanItem
                {
                    SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Pile, "P1"),
                    TargetUniqueId = "mapped-guid-pile-01",
                    Category = CoordinationCategory.Pile,
                    Mark = "P1",
                    Geometry = new PointGeometrySpec { Location = new Point3D(1000, 2000, -2500), OffsetZMm = -2500.0 }
                }
            }
        };

        var verifier = new MeasuredGeometricVerifier(fake);
        var report = await verifier.VerifyPlanGeometryAsync(plan);

        Assert.True(report.OverallPassed);
        Assert.Equal(1, report.VerifiedCount);
        Assert.Equal("mapped-guid-pile-01", report.Audits[0].TargetUniqueId);
    }
}
