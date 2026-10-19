namespace HPBim.Core.Tests.Execution;

using System;
using System.Collections.Generic;
using System.Text.Json;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Geometry;
using Xunit;

public sealed class BimExecutionPlanTests
{
    [Fact]
    public void BimExecutionPlan_ComputeDeterministicHash_ReturnsConsistentSha256()
    {
        var items1 = new List<ElementPlanItem>
        {
            new()
            {
                SourceKey = new CompositeSourceKey("doc1", CoordinationCategory.Level, "L1"),
                Category = CoordinationCategory.Level,
                Mark = "TRET",
                Geometry = new LevelGeometrySpec { ElevationM = 0.0, FloorNumber = 1, IsBuildingStory = true }
            },
            new()
            {
                SourceKey = new CompositeSourceKey("doc1", CoordinationCategory.Grid, "G1"),
                Category = CoordinationCategory.Grid,
                Mark = "1",
                Geometry = new LineGeometrySpec
                {
                    Start = new Point3D(0, 0, 0),
                    End = new Point3D(0, 5000, 0)
                }
            }
        };

        var items2 = new List<ElementPlanItem>
        {
            new()
            {
                SourceKey = new CompositeSourceKey("doc1", CoordinationCategory.Level, "L1"),
                Category = CoordinationCategory.Level,
                Mark = "TRET",
                Geometry = new LevelGeometrySpec { ElevationM = 0.0, FloorNumber = 1, IsBuildingStory = true }
            },
            new()
            {
                SourceKey = new CompositeSourceKey("doc1", CoordinationCategory.Grid, "G1"),
                Category = CoordinationCategory.Grid,
                Mark = "1",
                Geometry = new LineGeometrySpec
                {
                    Start = new Point3D(0, 0, 0),
                    End = new Point3D(0, 5000, 0)
                }
            }
        };

        var fixedId = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        var plan1 = new BimExecutionPlan
        {
            PlanId = fixedId,
            SourceModelName = "TestModel",
            TargetDoc = new DocumentIdentitySnapshot { Title = "Project1.rvt", VersionGuid = Guid.Parse("11111111-1111-1111-1111-111111111111") },
            Items = items1
        };

        var plan2 = new BimExecutionPlan
        {
            PlanId = fixedId,
            SourceModelName = "TestModel",
            TargetDoc = new DocumentIdentitySnapshot { Title = "Project1.rvt", VersionGuid = Guid.Parse("11111111-1111-1111-1111-111111111111") },
            Items = items2
        };

        Assert.Equal(plan1.PlanHash, plan2.PlanHash);
        Assert.False(string.IsNullOrWhiteSpace(plan1.PlanHash));
        Assert.Equal(64, plan1.PlanHash.Length); // 256 bits = 64 hex characters
    }

    [Fact]
    public void BimExecutionPlan_DifferentItems_ProduceDifferentHash()
    {
        var plan1 = new BimExecutionPlan
        {
            SourceModelName = "TestModel",
            Items = new[]
            {
                new ElementPlanItem
                {
                    SourceKey = new CompositeSourceKey("doc1", CoordinationCategory.Level, "L1"),
                    Category = CoordinationCategory.Level,
                    Mark = "TRET",
                    Geometry = new LevelGeometrySpec { ElevationM = 0.0, FloorNumber = 1 }
                }
            }
        };

        var plan2 = new BimExecutionPlan
        {
            SourceModelName = "TestModel",
            Items = new[]
            {
                new ElementPlanItem
                {
                    SourceKey = new CompositeSourceKey("doc1", CoordinationCategory.Level, "L1"),
                    Category = CoordinationCategory.Level,
                    Mark = "LAU 1",
                    Geometry = new LevelGeometrySpec { ElevationM = 3.6, FloorNumber = 2 }
                }
            }
        };

        Assert.NotEqual(plan1.PlanHash, plan2.PlanHash);
    }

    [Fact]
    public void CompositeSourceKey_ToStringAndParse_RoundTripsAccurately()
    {
        var key = new CompositeSourceKey("Drawing1.dwg", CoordinationCategory.Column, "2B4F", new[] { "H1", "H2" });
        string formatted = key.ToString();

        Assert.Equal("[Drawing1.dwg]#[Column]:2B4F", formatted);

        var parsed = CompositeSourceKey.Parse(formatted);
        Assert.Equal(key.DocIdentifier, parsed.DocIdentifier);
        Assert.Equal(key.Category, parsed.Category);
        Assert.Equal(key.PrimaryHandle, parsed.PrimaryHandle);
    }

    [Fact]
    public void ElementExecutionRecord_StatusAndDiagnostics_RetainedProperly()
    {
        var record = new ElementExecutionRecord
        {
            SourceKey = new CompositeSourceKey("DocA", CoordinationCategory.Beam, "D1_01"),
            Category = CoordinationCategory.Beam,
            Mark = "D1_01",
            Status = ElementExecutionStatus.Created,
            TargetElementId = 123456L,
            TargetUniqueId = "abc-guid-1234",
            ErrorCode = null,
            ErrorReason = null
        };

        Assert.Equal(ElementExecutionStatus.Created, record.Status);
        Assert.Equal(123456L, record.TargetElementId);
        Assert.Equal("abc-guid-1234", record.TargetUniqueId);
        Assert.Null(record.ErrorCode);
    }

    [Fact]
    public void ElementExecutionRecord_FailedStatus_RetainsErrorReport()
    {
        var record = new ElementExecutionRecord
        {
            SourceKey = new CompositeSourceKey("DocA", CoordinationCategory.Wall, "W1_01"),
            Category = CoordinationCategory.Wall,
            Mark = "W1_01",
            Status = ElementExecutionStatus.Failed,
            ErrorCode = "ERR_HOST_MISSING",
            ErrorReason = "Base level 'UnknownLevel' not found in active document."
        };

        Assert.Equal(ElementExecutionStatus.Failed, record.Status);
        Assert.Equal("ERR_HOST_MISSING", record.ErrorCode);
        Assert.Contains("UnknownLevel", record.ErrorReason);
    }

    [Fact]
    public void BimExecutionPlan_JsonSerialization_RoundTripsCleanly()
    {
        var plan = new BimExecutionPlan
        {
            SourceModelName = "RoundTripModel",
            TargetDoc = new DocumentIdentitySnapshot
            {
                Title = "Hospital.rvt",
                PathName = "C:\\Bim\\Hospital.rvt",
                VersionGuid = Guid.Parse("99999999-9999-9999-9999-999999999999")
            },
            Options = new ExecutionOptions
            {
                DryRun = true,
                TimeoutSeconds = 180,
                CreatePlanViews = true
            },
            Items = new[]
            {
                new ElementPlanItem
                {
                    SourceKey = new CompositeSourceKey("Doc1", CoordinationCategory.Level, "L1"),
                    Category = CoordinationCategory.Level,
                    Mark = "TRET",
                    Geometry = new LevelGeometrySpec { ElevationM = 0.15, FloorNumber = 1 }
                }
            }
        };

        string json = JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true });
        var restored = JsonSerializer.Deserialize<BimExecutionPlan>(json);

        Assert.NotNull(restored);
        Assert.Equal(plan.PlanHash, restored.PlanHash);
        Assert.Equal(plan.SourceModelName, restored.SourceModelName);
        Assert.Equal(plan.TargetDoc.Title, restored.TargetDoc.Title);
        Assert.Equal(plan.Items.Count, restored.Items.Count);
        Assert.Equal(plan.Items[0].Mark, restored.Items[0].Mark);
        Assert.True(restored.Options.DryRun);
    }

    [Fact]
    public void BimExecutionPlan_PlanHash_IsSensitiveToProfileOuterLoopHolesAndThickness()
    {
        var ptsBase = new[] { new Point3D(0, 0, 0), new Point3D(5000, 0, 0), new Point3D(5000, 5000, 0), new Point3D(0, 5000, 0) };
        var ptsShifted = new[] { new Point3D(0, 0, 0), new Point3D(5001, 0, 0), new Point3D(5000, 5000, 0), new Point3D(0, 5000, 0) };
        var hole = new[] { new Point3D(1000, 1000, 0), new Point3D(2000, 1000, 0), new Point3D(2000, 2000, 0), new Point3D(1000, 2000, 0) };

        var itemBase = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("doc", CoordinationCategory.Slab, "S1"),
            Category = CoordinationCategory.Slab,
            Geometry = new ProfileGeometrySpec { OuterLoop = ptsBase, ThicknessMm = 150 }
        };

        var planBase = new BimExecutionPlan { SourceModelName = "M", Items = new[] { itemBase } };
        var planShifted = new BimExecutionPlan { SourceModelName = "M", Items = new[] { itemBase with { Geometry = new ProfileGeometrySpec { OuterLoop = ptsShifted, ThicknessMm = 150 } } } };
        var planHole = new BimExecutionPlan { SourceModelName = "M", Items = new[] { itemBase with { Geometry = new ProfileGeometrySpec { OuterLoop = ptsBase, InnerLoops = new[] { hole }, ThicknessMm = 150 } } } };
        var planThick = new BimExecutionPlan { SourceModelName = "M", Items = new[] { itemBase with { Geometry = new ProfileGeometrySpec { OuterLoop = ptsBase, ThicknessMm = 200 } } } };

        Assert.NotEqual(planBase.PlanHash, planShifted.PlanHash);
        Assert.NotEqual(planBase.PlanHash, planHole.PlanHash);
        Assert.NotEqual(planBase.PlanHash, planThick.PlanHash);
    }

    [Fact]
    public void BimExecutionPlan_PlanHash_IsSensitiveToTargetDocAndSectionParameters()
    {
        var itemCol = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("doc", CoordinationCategory.Column, "C1"),
            Category = CoordinationCategory.Column,
            Resource = new ResourceResolutionSpec { FamilyName = "ColFam", TypeName = "300x300", DimensionParametersMm = new Dictionary<string, double> { ["b"] = 300, ["h"] = 300 } }
        };

        var plan1 = new BimExecutionPlan { SourceModelName = "M", TargetDoc = new DocumentIdentitySnapshot { Title = "A.rvt" }, Items = new[] { itemCol } };
        var plan2 = new BimExecutionPlan { SourceModelName = "M", TargetDoc = new DocumentIdentitySnapshot { Title = "B.rvt" }, Items = new[] { itemCol } };
        var plan3 = new BimExecutionPlan { SourceModelName = "M", TargetDoc = new DocumentIdentitySnapshot { Title = "A.rvt", PathName = "C:\\A.rvt" }, Items = new[] { itemCol } };
        var plan4 = new BimExecutionPlan
        {
            SourceModelName = "M", TargetDoc = new DocumentIdentitySnapshot { Title = "A.rvt" },
            Items = new[] { itemCol with { Resource = new ResourceResolutionSpec { FamilyName = "ColFam", TypeName = "300x400", DimensionParametersMm = new Dictionary<string, double> { ["b"] = 300, ["h"] = 400 } } } }
        };

        Assert.NotEqual(plan1.PlanHash, plan2.PlanHash);
        Assert.NotEqual(plan1.PlanHash, plan3.PlanHash);
        Assert.NotEqual(plan1.PlanHash, plan4.PlanHash);
    }
}
