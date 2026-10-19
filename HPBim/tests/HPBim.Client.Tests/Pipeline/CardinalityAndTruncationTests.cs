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

public sealed class CardinalityAndTruncationTests
{
    private static BimExecutionPlan CreateTestPlan(int itemCount)
    {
        var items = new List<ElementPlanItem>();
        for (int i = 1; i <= itemCount; i++)
        {
            items.Add(new ElementPlanItem
            {
                SourceKey = CompositeSourceKey.Create("Doc", CoordinationCategory.Column, $"C{i}"),
                Category = CoordinationCategory.Column,
                Mark = $"C{i}",
                SequenceOrder = i
            });
        }

        return new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "plan-hash-123",
            Items = items,
            Options = new ExecutionOptions { DryRun = false }
        };
    }

    [Fact]
    public async Task ExecutePlanAsync_WhenTruncated_MarksAllItemsOutcomeUnknownAndSetsIsOutcomeUnknownTrue()
    {
        var plan = CreateTestPlan(5);
        var fakeClient = new FakeRevitPipeClient
        {
            CustomExecuteHandler = _ => new ExecuteResult
            {
                IsError = false,
                Truncated = true,
                RolledBack = false,
                Value = JsonDocument.Parse("\"...[truncated]\"").RootElement
            }
        };

        var engine = new BimPipelineEngine(fakeClient);
        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        Assert.True(result.IsOutcomeUnknown);
        Assert.Equal(5, result.ElementRecords.Count);
        Assert.Equal(5, result.OutcomeUnknownCount);
        Assert.Equal(0, result.CreatedCount);
        Assert.All(result.ElementRecords, r =>
        {
            Assert.Equal(ElementExecutionStatus.OutcomeUnknown, r.Status);
            Assert.Equal("ERR_RESPONSE_TRUNCATED", r.ErrorCode);
        });
    }

    [Fact]
    public async Task ExecutePlanAsync_WhenResponseLacksRecords_NeverSynthesizesCreatedStatus()
    {
        var plan = CreateTestPlan(4);
        var fakeClient = new FakeRevitPipeClient
        {
            AutoEnrichRecords = false,
            CustomExecuteHandler = _ => new ExecuteResult
            {
                IsError = false,
                RolledBack = false,
                Value = null
            }
        };

        var engine = new BimPipelineEngine(fakeClient);
        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        Assert.True(result.IsOutcomeUnknown);
        Assert.Equal(4, result.ElementRecords.Count);
        Assert.Equal(4, result.OutcomeUnknownCount);
        Assert.Equal(0, result.CreatedCount);
        Assert.All(result.ElementRecords, r =>
        {
            Assert.Equal(ElementExecutionStatus.OutcomeUnknown, r.Status);
            Assert.Equal("ERR_NO_RECORDS", r.ErrorCode);
        });
    }

    [Fact]
    public async Task ExecutePlanAsync_WhenRecordDeficitOccurs_MarksMissingItemsOutcomeUnknown()
    {
        var plan = CreateTestPlan(5);

        // Only return 3 records for C1, C2, C3; C4 and C5 are missing (deficit)
        var responsePayload = new
        {
            records = new[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-1", targetElementId = 101L },
                new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "Created", targetUniqueId = "uid-2", targetElementId = 102L },
                new { sourceKey = "[Doc]#[Column]:C3", category = "Column", mark = "C3", status = "Created", targetUniqueId = "uid-3", targetElementId = 103L }
            }
        };

        var fakeClient = new FakeRevitPipeClient
        {
            CustomExecuteHandler = _ => new ExecuteResult
            {
                IsError = false,
                RolledBack = false,
                Value = JsonDocument.Parse(JsonSerializer.Serialize(responsePayload)).RootElement
            }
        };

        var engine = new BimPipelineEngine(fakeClient);
        var result = await engine.ExecutePlanAsync(plan);

        // 1:1 cardinality enforced: 5 items returned
        Assert.Equal(5, result.ElementRecords.Count);
        Assert.False(result.Success);
        Assert.True(result.IsOutcomeUnknown);
        Assert.Equal(3, result.CreatedCount);
        Assert.Equal(2, result.OutcomeUnknownCount);

        // C1..C3 are Created
        Assert.Equal(ElementExecutionStatus.Created, result.ElementRecords[0].Status);
        Assert.Equal(ElementExecutionStatus.Created, result.ElementRecords[1].Status);
        Assert.Equal(ElementExecutionStatus.Created, result.ElementRecords[2].Status);

        // C4..C5 are OutcomeUnknown
        Assert.Equal(ElementExecutionStatus.OutcomeUnknown, result.ElementRecords[3].Status);
        Assert.Equal("ERR_RECORD_DEFICIT", result.ElementRecords[3].ErrorCode);
        Assert.Equal(ElementExecutionStatus.OutcomeUnknown, result.ElementRecords[4].Status);
        Assert.Equal("ERR_RECORD_DEFICIT", result.ElementRecords[4].ErrorCode);
    }

    [Fact]
    public async Task ExecutePlanAsync_Strict1To1_PreservesItemSequenceOrderAndSourceKeyIdentity()
    {
        var plan = CreateTestPlan(10);
        var fakeClient = new FakeRevitPipeClient();

        var engine = new BimPipelineEngine(fakeClient);
        var result = await engine.ExecutePlanAsync(plan);

        Assert.True(result.Success);
        Assert.Equal(10, result.ElementRecords.Count);
        for (int i = 0; i < 10; i++)
        {
            Assert.Equal(plan.Items[i].SourceKey, result.ElementRecords[i].SourceKey);
            Assert.Equal(plan.Items[i].Mark, result.ElementRecords[i].Mark);
        }
    }
}
