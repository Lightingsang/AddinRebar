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

public sealed class EmpiricalChallenger2M4SemanticsTests
{
    private static BimExecutionPlan CreatePlan(params (CoordinationCategory cat, string mark, string? targetId)[] items)
    {
        var planItems = items.Select((it, idx) => new ElementPlanItem
        {
            SourceKey = CompositeSourceKey.Create("Doc", it.cat, it.mark),
            Category = it.cat,
            Mark = it.mark,
            TargetUniqueId = it.targetId,
            SequenceOrder = idx + 1
        }).ToList();

        return new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "adversarial-m4-hash",
            Items = planItems,
            Options = new ExecutionOptions { DryRun = false }
        };
    }

    private static FakeRevitPipeClient CreatePipeClient(object recordsPayload, bool rolledBack = false) =>
        new()
        {
            CustomExecuteHandler = _ => new ExecuteResult
            {
                IsError = false,
                RolledBack = rolledBack,
                Value = JsonDocument.Parse(JsonSerializer.Serialize(new { records = recordsPayload })).RootElement
            }
        };

    [Theory]
    [InlineData("Cannot skip: SKIP_FROZEN_ZONE disabled")]
    [InlineData("Error occurred while evaluating SKIP_PRECONDITION_FAILED")]
    [InlineData("SKIP_STAGE_DISABLED_NOW")]
    [InlineData("NOT_SKIP_USER_EXCLUDED")]
    [InlineData("SKIP_PREREQUISITE_FAILED ")]
    [InlineData("SKIP_FROZEN")]
    public async Task ExecutePlanAsync_AdversarialSkipErrorReasonsAndCodes_RejectedAsIntegrityDefect(string adversarialCodeOrReason)
    {
        var plan = CreatePlan((CoordinationCategory.Column, "C1", null), (CoordinationCategory.Column, "C2", null));
        var payload = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Skipped", errorCode = adversarialCodeOrReason, errorReason = adversarialCodeOrReason, targetUniqueId = "" },
            new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "Created", targetUniqueId = "uid-c2" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload, rolledBack: false));
        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        Assert.True(result.IsOutcomeUnknown);

        var c1 = result.ElementRecords.First(r => r.Mark == "C1");
        Assert.Equal(ElementExecutionStatus.Failed, c1.Status);
        Assert.Equal("ERR_INTEGRITY_DEFECT_SKIPPED", c1.ErrorCode);

        var c2 = result.ElementRecords.First(r => r.Mark == "C2");
        Assert.Equal(ElementExecutionStatus.OutcomeUnknown, c2.Status);
        Assert.Equal("ERR_POST_DISPATCH_INTEGRITY_DEFECT", c2.ErrorCode);
        Assert.Contains("ERR_INTEGRITY_DEFECT_SKIPPED", c2.ErrorReason);
    }

    [Fact]
    public async Task ExecutePlanAsync_SkipWithNullErrorCodeAndSubstringInReason_RejectedAsIntegrityDefect()
    {
        var plan = CreatePlan((CoordinationCategory.Column, "C1", null), (CoordinationCategory.Column, "C2", null));
        var payload = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Skipped", errorCode = (string?)null, errorReason = "Cannot skip: SKIP_FROZEN_ZONE disabled", targetUniqueId = "" },
            new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "Created", targetUniqueId = "uid-c2" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload, rolledBack: false));
        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        Assert.True(result.IsOutcomeUnknown);

        var c1 = result.ElementRecords.First(r => r.Mark == "C1");
        Assert.Equal(ElementExecutionStatus.Failed, c1.Status);
        Assert.Equal("ERR_INTEGRITY_DEFECT_SKIPPED", c1.ErrorCode);

        var c2 = result.ElementRecords.First(r => r.Mark == "C2");
        Assert.Equal(ElementExecutionStatus.OutcomeUnknown, c2.Status);
        Assert.Equal("ERR_POST_DISPATCH_INTEGRITY_DEFECT", c2.ErrorCode);
    }

    [Theory]
    [InlineData("SKIP_FROZEN_ZONE")]
    [InlineData("skip_frozen_zone")]
    [InlineData("SKIP_STAGE_DISABLED")]
    [InlineData("skip_stage_disabled")]
    [InlineData("SKIP_PRECONDITION_FAILED")]
    [InlineData("skip_precondition_failed")]
    [InlineData("SKIP_PREREQUISITE_FAILED")]
    [InlineData("skip_prerequisite_failed")]
    [InlineData("SKIP_USER_EXCLUDED")]
    [InlineData("skip_user_excluded")]
    public async Task ExecutePlanAsync_ValidSkipCodesCaseInsensitive_PreservesSkippedStatus(string validCode)
    {
        var plan = CreatePlan((CoordinationCategory.Column, "C1", null), (CoordinationCategory.Column, "C2", null));
        var payload = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Skipped", errorCode = validCode, errorReason = "Intentional valid skip reason", targetUniqueId = "" },
            new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "Created", targetUniqueId = "uid-c2" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload, rolledBack: false));
        var result = await engine.ExecutePlanAsync(plan);

        Assert.True(result.Success);
        Assert.False(result.IsOutcomeUnknown);

        var c1 = result.ElementRecords.First(r => r.Mark == "C1");
        Assert.Equal(ElementExecutionStatus.Skipped, c1.Status);
        Assert.Equal(validCode, c1.ErrorCode);

        var c2 = result.ElementRecords.First(r => r.Mark == "C2");
        Assert.Equal(ElementExecutionStatus.Created, c2.Status);
    }

    [Fact]
    public async Task ExecutePlanAsync_NoOpTargetMismatch_ReportsPostDispatchIntegrityDefectWhenRolledBackFalse()
    {
        var plan = CreatePlan((CoordinationCategory.Column, "C1", "ledger-uid-100"), (CoordinationCategory.Column, "C2", null));
        var payload = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "NoOp", targetUniqueId = "revit-uid-999" },
            new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "Created", targetUniqueId = "uid-c2" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload, rolledBack: false));
        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        Assert.True(result.IsOutcomeUnknown);

        var c1 = result.ElementRecords.First(r => r.Mark == "C1");
        Assert.Equal(ElementExecutionStatus.Failed, c1.Status);
        Assert.Equal("ERR_NOOP_TARGET_MISMATCH", c1.ErrorCode);

        var c2 = result.ElementRecords.First(r => r.Mark == "C2");
        Assert.Equal(ElementExecutionStatus.OutcomeUnknown, c2.Status);
        Assert.Equal("ERR_POST_DISPATCH_INTEGRITY_DEFECT", c2.ErrorCode);
        Assert.Contains("ERR_NOOP_TARGET_MISMATCH", c2.ErrorReason);
    }

    [Fact]
    public async Task ExecutePlanAsync_IntegrityDefectsWithRolledBackTrue_ReportsBatchRolledBack()
    {
        var plan = CreatePlan((CoordinationCategory.Column, "C1", "ledger-uid-100"), (CoordinationCategory.Column, "C2", null));
        var payload = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "NoOp", targetUniqueId = "mismatched-uid" },
            new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "Created", targetUniqueId = "uid-c2" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload, rolledBack: true));
        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        Assert.False(result.IsOutcomeUnknown);

        var c1 = result.ElementRecords.First(r => r.Mark == "C1");
        Assert.Equal(ElementExecutionStatus.Failed, c1.Status);
        Assert.Equal("ERR_NOOP_TARGET_MISMATCH", c1.ErrorCode);

        var c2 = result.ElementRecords.First(r => r.Mark == "C2");
        Assert.Equal(ElementExecutionStatus.Failed, c2.Status);
        Assert.Equal("ERR_BATCH_ROLLED_BACK", c2.ErrorCode);
    }

    [Fact]
    public async Task ExecutePlanAsync_MultipleDefectiveItems_PreservesSpecificDefectsAndMarksOthersOutcomeUnknown()
    {
        var plan = CreatePlan(
            (CoordinationCategory.Column, "C1", "uid-expected-1"),
            (CoordinationCategory.Column, "C2", null),
            (CoordinationCategory.Column, "C3", null));

        var payload = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "NoOp", targetUniqueId = "uid-wrong" },
            new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "NoOp", targetUniqueId = "" },
            new { sourceKey = "[Doc]#[Column]:C3", category = "Column", mark = "C3", status = "Created", targetUniqueId = "uid-c3" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload, rolledBack: false));
        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        Assert.True(result.IsOutcomeUnknown);

        var c1 = result.ElementRecords.First(r => r.Mark == "C1");
        Assert.Equal(ElementExecutionStatus.Failed, c1.Status);
        Assert.Equal("ERR_NOOP_TARGET_MISMATCH", c1.ErrorCode);

        var c2 = result.ElementRecords.First(r => r.Mark == "C2");
        Assert.Equal(ElementExecutionStatus.Failed, c2.Status);
        Assert.Equal("ERR_MISSING_TARGET_UNIQUE_ID", c2.ErrorCode);

        var c3 = result.ElementRecords.First(r => r.Mark == "C3");
        Assert.Equal(ElementExecutionStatus.OutcomeUnknown, c3.Status);
        Assert.Equal("ERR_POST_DISPATCH_INTEGRITY_DEFECT", c3.ErrorCode);
    }
}
