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

public sealed partial class BimPipelineEngineRecordReconciliationTests
{
    private static BimExecutionPlan CreatePlan(params (CoordinationCategory cat, string mark)[] items)
    {
        var planItems = items.Select((it, idx) => new ElementPlanItem
        {
            SourceKey = CompositeSourceKey.Create("Doc", it.cat, it.mark),
            Category = it.cat,
            Mark = it.mark,
            SequenceOrder = idx + 1
        }).ToList();

        return new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "plan-reconciliation-hash",
            Items = planItems,
            Options = new ExecutionOptions { DryRun = false }
        };
    }

    private static FakeRevitPipeClient CreatePipeClient(object recordsPayload) =>
        new()
        {
            CustomExecuteHandler = _ => new ExecuteResult
            {
                IsError = false,
                RolledBack = false,
                Value = JsonDocument.Parse(JsonSerializer.Serialize(new { records = recordsPayload })).RootElement
            }
        };

    [Fact]
    public async Task ExecutePlanAsync_ForeignRecordReturned_RejectsBatchWithForeignRecord()
    {
        var plan = CreatePlan((CoordinationCategory.Column, "C1"), (CoordinationCategory.Column, "C2"));
        var payload = new[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-1" },
            new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "Created", targetUniqueId = "uid-2" },
            new { sourceKey = "[Doc]#[Column]:C99_FOREIGN", category = "Column", mark = "C99_FOREIGN", status = "Created", targetUniqueId = "uid-99" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload));
        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        Assert.Equal(2, result.ElementRecords.Count);
        Assert.All(result.ElementRecords, r =>
        {
            Assert.Equal(ElementExecutionStatus.Failed, r.Status);
            Assert.Equal("ERR_FOREIGN_RECORD", r.ErrorCode);
        });
    }

    [Fact]
    public async Task ExecutePlanAsync_DuplicateRecordsReturned_RejectsBatchWithDuplicateRecord()
    {
        var plan = CreatePlan((CoordinationCategory.Column, "C1"), (CoordinationCategory.Column, "C2"));
        var payload = new[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-1a" },
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-1b" },
            new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "Created", targetUniqueId = "uid-2" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload));
        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        Assert.Equal(2, result.ElementRecords.Count);
        Assert.All(result.ElementRecords, r =>
        {
            Assert.Equal(ElementExecutionStatus.Failed, r.Status);
            Assert.Equal("ERR_DUPLICATE_RECORD", r.ErrorCode);
        });
    }

    [Fact]
    public async Task ExecutePlanAsync_InvalidStatusEnumString_RejectsBatchWithInvalidStatusEnum()
    {
        var plan = CreatePlan((CoordinationCategory.Column, "C1"), (CoordinationCategory.Column, "C2"));
        var payload = new[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "BogusStatus", targetUniqueId = "uid-1" },
            new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "Created", targetUniqueId = "uid-2" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload));
        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        Assert.True(result.IsOutcomeUnknown);
        var c1 = result.ElementRecords.First(r => r.Mark == "C1");
        Assert.Equal(ElementExecutionStatus.Failed, c1.Status);
        Assert.Equal("ERR_INVALID_STATUS_ENUM", c1.ErrorCode);

        var c2 = result.ElementRecords.First(r => r.Mark == "C2");
        Assert.Equal(ElementExecutionStatus.OutcomeUnknown, c2.Status);
        Assert.Equal("ERR_POST_DISPATCH_INTEGRITY_DEFECT", c2.ErrorCode);
    }

    [Fact]
    public async Task ExecutePlanAsync_CreatedStatusWithMissingUniqueId_RejectsBatchWithMissingTargetUniqueId()
    {
        var plan = CreatePlan((CoordinationCategory.Column, "C1"), (CoordinationCategory.Column, "C2"));
        var payload = new[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "" },
            new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "Created", targetUniqueId = "uid-2" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload));
        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        Assert.True(result.IsOutcomeUnknown);
        var c1 = result.ElementRecords.First(r => r.Mark == "C1");
        Assert.Equal(ElementExecutionStatus.Failed, c1.Status);
        Assert.Equal("ERR_MISSING_TARGET_UNIQUE_ID", c1.ErrorCode);

        var c2 = result.ElementRecords.First(r => r.Mark == "C2");
        Assert.Equal(ElementExecutionStatus.OutcomeUnknown, c2.Status);
        Assert.Equal("ERR_POST_DISPATCH_INTEGRITY_DEFECT", c2.ErrorCode);
    }

    [Fact]
    public async Task Reconciliation_CreatedRecordWithOmittedTargetUniqueIdProperty_FailsIntegrityCheck()
    {
        var plan = CreatePlan((CoordinationCategory.Column, "C1"), (CoordinationCategory.Column, "C2"));
        var payload = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created" },
            new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "Created", targetUniqueId = "uid-2" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload));
        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        Assert.True(result.IsOutcomeUnknown);
        var c1 = result.ElementRecords.First(r => r.Mark == "C1");
        Assert.Equal(ElementExecutionStatus.Failed, c1.Status);
        Assert.Equal("ERR_MISSING_TARGET_UNIQUE_ID", c1.ErrorCode);
        Assert.Null(c1.TargetUniqueId);

        var c2 = result.ElementRecords.First(r => r.Mark == "C2");
        Assert.Equal(ElementExecutionStatus.OutcomeUnknown, c2.Status);
        Assert.Equal("ERR_POST_DISPATCH_INTEGRITY_DEFECT", c2.ErrorCode);
    }

    [Fact]
    public async Task ExecutePlanAsync_UpdatedStatusWithMissingUniqueId_RejectsBatchWithMissingTargetUniqueId()
    {
        var plan = CreatePlan((CoordinationCategory.Column, "C1"));
        var payload = new[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Updated", targetUniqueId = (string?)null }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload));
        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        var c1 = result.ElementRecords[0];
        Assert.Equal(ElementExecutionStatus.Failed, c1.Status);
        Assert.Equal("ERR_MISSING_TARGET_UNIQUE_ID", c1.ErrorCode);
    }

    [Fact]
    public async Task ExecutePlanAsync_UpdatedStatusWithOmittedUniqueIdProperty_RejectsBatchWithMissingTargetUniqueId()
    {
        var plan = CreatePlan((CoordinationCategory.Column, "C1"));
        var payload = new[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Updated" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload));
        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        var c1 = result.ElementRecords[0];
        Assert.Equal(ElementExecutionStatus.Failed, c1.Status);
        Assert.Equal("ERR_MISSING_TARGET_UNIQUE_ID", c1.ErrorCode);
        Assert.Null(c1.TargetUniqueId);
    }

    [Fact]
    public async Task ExecutePlanAsync_CategoryMismatch_RejectsBatchWithCategoryMismatch()
    {
        var plan = CreatePlan((CoordinationCategory.Column, "C1"), (CoordinationCategory.Column, "C2"));
        var payload = new[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "StructuralFraming", mark = "C1", status = "Created", targetUniqueId = "uid-1" },
            new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "Created", targetUniqueId = "uid-2" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload));
        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        Assert.True(result.IsOutcomeUnknown);
        var c1 = result.ElementRecords.First(r => r.Mark == "C1");
        Assert.Equal(ElementExecutionStatus.Failed, c1.Status);
        Assert.Equal("ERR_CATEGORY_MISMATCH", c1.ErrorCode);

        var c2 = result.ElementRecords.First(r => r.Mark == "C2");
        Assert.Equal(ElementExecutionStatus.OutcomeUnknown, c2.Status);
        Assert.Equal("ERR_POST_DISPATCH_INTEGRITY_DEFECT", c2.ErrorCode);
    }

    [Fact]
    public async Task ExecutePlanAsync_MarkMismatch_RejectsBatchWithMarkMismatch()
    {
        var plan = CreatePlan((CoordinationCategory.Column, "C1"));
        var payload = new[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C2_MISMATCH", status = "Created", targetUniqueId = "uid-1" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload));
        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        var c1 = result.ElementRecords[0];
        Assert.Equal(ElementExecutionStatus.Failed, c1.Status);
        Assert.Equal("ERR_MARK_MISMATCH", c1.ErrorCode);
    }
}
