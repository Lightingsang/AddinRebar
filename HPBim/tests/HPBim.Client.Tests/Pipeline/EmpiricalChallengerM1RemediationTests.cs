namespace HPBim.Client.Tests.Pipeline;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HPBim.Client.Pipeline;
using HPBim.Client.Tests.Fakes;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

/// <summary>
/// Empirical Challenger tests for R1.2: 1:1 Cardinality, Truncation (>64KB),
/// Deficit Resolution, and Non-Synthesis of Created status.
/// </summary>
public sealed class EmpiricalChallengerM1RemediationTests
{
    [Fact]
    public async Task Challenge_Truncation_WhenTruncated_EnforcesOutcomeUnknownOnAllItems()
    {
        var items = Enumerable.Range(1, 10).Select(i => new ElementPlanItem
        {
            SourceKey = CompositeSourceKey.Create("Doc1", CoordinationCategory.Column, $"C{i}"),
            Category = CoordinationCategory.Column,
            Mark = $"C{i}",
            SequenceOrder = i
        }).ToList();

        var plan = new BimExecutionPlan { PlanId = Guid.NewGuid(), PlanHash = "hash-trunc", Items = items };
        var fakeClient = new FakeRevitPipeClient
        {
            CustomExecuteHandler = _ => new ExecuteResult
            {
                IsError = false,
                Truncated = true,
                RolledBack = false,
                Value = JsonDocument.Parse("\"...[64KB buffer exceeded]\"").RootElement
            }
        };

        var engine = new BimPipelineEngine(fakeClient);
        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        Assert.True(result.IsOutcomeUnknown);
        Assert.Equal(10, result.ElementRecords.Count);
        Assert.Equal(10, result.OutcomeUnknownCount);
        Assert.Equal(0, result.CreatedCount);
        Assert.All(result.ElementRecords, r =>
        {
            Assert.Equal(ElementExecutionStatus.OutcomeUnknown, r.Status);
            Assert.Equal("ERR_RESPONSE_TRUNCATED", r.ErrorCode);
        });
    }

    [Fact]
    public async Task Challenge_Deficit_WhenFewerRecordsReturned_PreservesExactCardinalityAndMarksDeficitUnknown()
    {
        var items = Enumerable.Range(1, 5).Select(i => new ElementPlanItem
        {
            SourceKey = CompositeSourceKey.Create("Doc1", CoordinationCategory.Beam, $"B{i}"),
            Category = CoordinationCategory.Beam,
            Mark = $"B{i}",
            SequenceOrder = i
        }).ToList();

        var plan = new BimExecutionPlan { PlanId = Guid.NewGuid(), PlanHash = "hash-def", Items = items };
        var payload = new
        {
            records = new[]
            {
                new { sourceKey = "[Doc1]#[Beam]:B1", category = "Beam", mark = "B1", status = "Created", targetUniqueId = "u1", targetElementId = 201L },
                new { sourceKey = "[Doc1]#[Beam]:B2", category = "Beam", mark = "B2", status = "Created", targetUniqueId = "u2", targetElementId = 202L }
            }
        };

        var fakeClient = new FakeRevitPipeClient
        {
            CustomExecuteHandler = _ => new ExecuteResult
            {
                IsError = false,
                RolledBack = false,
                Value = JsonDocument.Parse(JsonSerializer.Serialize(payload)).RootElement
            }
        };

        var engine = new BimPipelineEngine(fakeClient);
        var result = await engine.ExecutePlanAsync(plan);

        Assert.Equal(5, result.ElementRecords.Count);
        Assert.False(result.Success);
        Assert.True(result.IsOutcomeUnknown);
        Assert.Equal(2, result.CreatedCount);
        Assert.Equal(3, result.OutcomeUnknownCount);

        for (int i = 2; i < 5; i++)
        {
            var rec = result.ElementRecords[i];
            Assert.Equal(ElementExecutionStatus.OutcomeUnknown, rec.Status);
            Assert.Equal("ERR_RECORD_DEFICIT", rec.ErrorCode);
            Assert.Equal(items[i].SourceKey, rec.SourceKey);
        }
    }

    [Fact]
    public async Task Challenge_NoRecords_ResponseEmptyOrNull_NeverSynthesizesCreated()
    {
        var items = Enumerable.Range(1, 4).Select(i => new ElementPlanItem
        {
            SourceKey = CompositeSourceKey.Create("Doc1", CoordinationCategory.Wall, $"W{i}"),
            Category = CoordinationCategory.Wall,
            Mark = $"W{i}",
            SequenceOrder = i
        }).ToList();

        var plan = new BimExecutionPlan { PlanId = Guid.NewGuid(), PlanHash = "hash-empty", Items = items };

        var fakeClientNull = new FakeRevitPipeClient
        {
            AutoEnrichRecords = false,
            CustomExecuteHandler = _ => new ExecuteResult { IsError = false, Value = null }
        };
        var engine1 = new BimPipelineEngine(fakeClientNull);
        var result1 = await engine1.ExecutePlanAsync(plan);

        Assert.False(result1.Success);
        Assert.True(result1.IsOutcomeUnknown);
        Assert.Equal(0, result1.CreatedCount);
        Assert.Equal(4, result1.OutcomeUnknownCount);
        Assert.All(result1.ElementRecords, r => Assert.Equal("ERR_NO_RECORDS", r.ErrorCode));

        var fakeClientEmpty = new FakeRevitPipeClient
        {
            CustomExecuteHandler = _ => new ExecuteResult
            {
                IsError = false,
                Value = JsonDocument.Parse("{\"records\":[]}").RootElement
            }
        };
        var engine2 = new BimPipelineEngine(fakeClientEmpty);
        var result2 = await engine2.ExecutePlanAsync(plan);

        Assert.False(result2.Success);
        Assert.True(result2.IsOutcomeUnknown);
        Assert.Equal(0, result2.CreatedCount);
        Assert.Equal(4, result2.OutcomeUnknownCount);
        Assert.All(result2.ElementRecords, r => Assert.Equal("ERR_RECORD_DEFICIT", r.ErrorCode));
    }

    [Fact]
    public async Task Challenge_Deficit_OnFailedRollback_DowngradesCreatedToFailedAndMarksMissingFailed()
    {
        var items = Enumerable.Range(1, 3).Select(i => new ElementPlanItem
        {
            SourceKey = CompositeSourceKey.Create("Doc1", CoordinationCategory.Column, $"C{i}"),
            Category = CoordinationCategory.Column,
            Mark = $"C{i}",
            SequenceOrder = i
        }).ToList();

        var plan = new BimExecutionPlan { PlanId = Guid.NewGuid(), PlanHash = "hash-rb", Items = items, Options = new ExecutionOptions { DryRun = false } };
        var payload = new
        {
            records = new[]
            {
                new { sourceKey = "[Doc1]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "u1", targetElementId = 101L }
            }
        };

        var fakeClient = new FakeRevitPipeClient
        {
            CustomExecuteHandler = _ => new ExecuteResult
            {
                IsError = true,
                RolledBack = true,
                Message = "Simulated Revit API fatal error",
                Value = JsonDocument.Parse(JsonSerializer.Serialize(payload)).RootElement
            }
        };

        var engine = new BimPipelineEngine(fakeClient);
        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        Assert.Equal(3, result.ElementRecords.Count);
        Assert.Equal(0, result.CreatedCount);
        Assert.All(result.ElementRecords, r =>
        {
            Assert.Equal(ElementExecutionStatus.Failed, r.Status);
            Assert.Equal("ERR_BATCH_ROLLED_BACK", r.ErrorCode);
        });
    }

    [Fact]
    public async Task Challenge_Stress_LargeBatchDeficitAndTruncation_StrictCardinality()
    {
        int totalItems = 500;
        int returnedItems = 250;
        var items = Enumerable.Range(1, totalItems).Select(i => new ElementPlanItem
        {
            SourceKey = CompositeSourceKey.Create("BigModel", CoordinationCategory.Slab, $"S{i:D4}"),
            Category = CoordinationCategory.Slab,
            Mark = $"S{i:D4}",
            SequenceOrder = i
        }).ToList();

        var plan = new BimExecutionPlan { PlanId = Guid.NewGuid(), PlanHash = "hash-stress", Items = items };
        var returnRecords = Enumerable.Range(1, returnedItems).Select(i => new
        {
            sourceKey = $"[BigModel]#[Slab]:S{i:D4}",
            category = "Slab",
            mark = $"S{i:D4}",
            status = "Created",
            targetUniqueId = $"uid-s-{i}",
            targetElementId = 5000L + i
        }).ToArray();

        var fakeClient = new FakeRevitPipeClient
        {
            CustomExecuteHandler = _ => new ExecuteResult
            {
                IsError = false,
                RolledBack = false,
                Value = JsonDocument.Parse(JsonSerializer.Serialize(new { records = returnRecords })).RootElement
            }
        };

        var engine = new BimPipelineEngine(fakeClient);
        var result = await engine.ExecutePlanAsync(plan);

        Assert.Equal(totalItems, result.ElementRecords.Count);
        Assert.False(result.Success);
        Assert.True(result.IsOutcomeUnknown);
        Assert.Equal(returnedItems, result.CreatedCount);
        Assert.Equal(totalItems - returnedItems, result.OutcomeUnknownCount);
        Assert.Equal(0, result.FailedCount);
    }

    [Fact]
    public async Task FakeRevitPipeClient_AutoEnrichRecords_NeverOverwritesExplicitRecordsProperty()
    {
        var request = new ExecuteRequest("test")
        {
            Args = JsonDocument.Parse("{\"items\":[{\"sourceKey\":\"[Doc]#[Wall]:W1\",\"category\":\"Wall\",\"mark\":\"W1\"}]}").RootElement
        };

        var clientExplicitEmpty = new FakeRevitPipeClient
        {
            AutoEnrichRecords = true,
            CustomExecuteHandler = _ => new ExecuteResult
            {
                IsError = false,
                Value = JsonDocument.Parse("{\"records\":[]}").RootElement
            }
        };

        var resEmpty = await clientExplicitEmpty.ExecuteAsync(request);
        Assert.NotNull(resEmpty.Value);
        Assert.True(resEmpty.Value!.Value.TryGetProperty("records", out var rProp));
        Assert.Equal(0, rProp.GetArrayLength());

        var clientNoRecordsProp = new FakeRevitPipeClient
        {
            AutoEnrichRecords = true,
            CustomExecuteHandler = _ => new ExecuteResult
            {
                IsError = false,
                Value = null
            }
        };

        var resEnriched = await clientNoRecordsProp.ExecuteAsync(request);
        Assert.NotNull(resEnriched.Value);
        Assert.True(resEnriched.Value!.Value.TryGetProperty("records", out var enrichedProp));
        Assert.Equal(1, enrichedProp.GetArrayLength());
    }
}
