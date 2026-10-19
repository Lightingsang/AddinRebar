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
    [Fact]
    public async Task ExecutePlanAsync_RequiredItemSkippedWithoutDefinedReason_TreatsAsIntegrityDefectAndRollsBack()
    {
        var plan = CreatePlan((CoordinationCategory.Column, "C1"), (CoordinationCategory.Column, "C2"));
        var payload = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Skipped", errorReason = "Arbitrary unapproved skip reason", targetUniqueId = "" },
            new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "Created", targetUniqueId = "uid-2" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload));
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

    [Fact]
    public async Task ExecutePlanAsync_PlanItemSkippedWithValidDefinedReason_PreservesSkippedStatus()
    {
        var plan = CreatePlan((CoordinationCategory.Column, "C1"), (CoordinationCategory.Column, "C2"));
        var payload = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Skipped", errorCode = "SKIP_FROZEN_ZONE", targetUniqueId = "" },
            new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "Created", targetUniqueId = "uid-2" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload));
        var result = await engine.ExecutePlanAsync(plan);

        Assert.True(result.Success);
        var c1 = result.ElementRecords.First(r => r.Mark == "C1");
        Assert.Equal(ElementExecutionStatus.Skipped, c1.Status);

        var c2 = result.ElementRecords.First(r => r.Mark == "C2");
        Assert.Equal(ElementExecutionStatus.Created, c2.Status);
    }

    [Fact]
    public async Task ExecutePlanAsync_SkippedWithSubstringReasonInError_RejectedAsIntegrityDefect()
    {
        var plan = CreatePlan((CoordinationCategory.Column, "C1"));
        var payload = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Skipped", errorCode = "ERR_UNHANDLED_EXCEPTION", errorReason = "Error when evaluating SKIP_FROZEN_ZONE precondition", targetUniqueId = "" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload));
        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        var c1 = result.ElementRecords[0];
        Assert.Equal(ElementExecutionStatus.Failed, c1.Status);
        Assert.Equal("ERR_INTEGRITY_DEFECT_SKIPPED", c1.ErrorCode);
    }

    [Fact]
    public async Task ExecutePlanAsync_PlanItemWithSkipAllowedParameter_PreservesSkippedStatus()
    {
        var plan = CreatePlan((CoordinationCategory.Column, "C1"));
        var itemWithSkipAllowed = plan.Items[0] with
        {
            Parameters = new Dictionary<string, object> { ["SkipAllowed"] = true }
        };
        plan = plan with { Items = new[] { itemWithSkipAllowed } };

        var payload = new[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Skipped", errorReason = "User custom reason", targetUniqueId = "" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload));
        var result = await engine.ExecutePlanAsync(plan);

        Assert.True(result.Success);
        Assert.Equal(ElementExecutionStatus.Skipped, result.ElementRecords[0].Status);
    }

    [Fact]
    public async Task ExecutePlanAsync_IntegrityDefectInOneItem_ExecutesAllOrNothingBatchRollback()
    {
        var plan = CreatePlan(
            (CoordinationCategory.Column, "C1"),
            (CoordinationCategory.Column, "C2"),
            (CoordinationCategory.Column, "C3"));

        var payload = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-1" },
            new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "Created", targetUniqueId = "" },
            new { sourceKey = "[Doc]#[Column]:C3", category = "Column", mark = "C3", status = "Created", targetUniqueId = "uid-3" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload));
        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        Assert.True(result.IsOutcomeUnknown);
        Assert.Equal(3, result.ElementRecords.Count);

        var c2 = result.ElementRecords.First(r => r.Mark == "C2");
        Assert.Equal(ElementExecutionStatus.Failed, c2.Status);
        Assert.Equal("ERR_MISSING_TARGET_UNIQUE_ID", c2.ErrorCode);

        var c1 = result.ElementRecords.First(r => r.Mark == "C1");
        Assert.Equal(ElementExecutionStatus.OutcomeUnknown, c1.Status);
        Assert.Equal("ERR_POST_DISPATCH_INTEGRITY_DEFECT", c1.ErrorCode);
        Assert.Contains("ERR_MISSING_TARGET_UNIQUE_ID", c1.ErrorReason);

        var c3 = result.ElementRecords.First(r => r.Mark == "C3");
        Assert.Equal(ElementExecutionStatus.OutcomeUnknown, c3.Status);
        Assert.Equal("ERR_POST_DISPATCH_INTEGRITY_DEFECT", c3.ErrorCode);
        Assert.Contains("ERR_MISSING_TARGET_UNIQUE_ID", c3.ErrorReason);
    }

    [Fact]
    public async Task ExecutePlanAsync_IntegrityDefectWithHostRolledBack_ReportsBatchRolledBack()
    {
        var plan = CreatePlan(
            (CoordinationCategory.Column, "C1"),
            (CoordinationCategory.Column, "C2"));

        var payload = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-1" },
            new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "Created", targetUniqueId = "" }
        };

        var fakeClient = new FakeRevitPipeClient
        {
            CustomExecuteHandler = _ => new ExecuteResult
            {
                IsError = false,
                RolledBack = true,
                Value = JsonDocument.Parse(JsonSerializer.Serialize(new { records = payload })).RootElement
            }
        };

        var engine = new BimPipelineEngine(fakeClient);
        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        var c2 = result.ElementRecords.First(r => r.Mark == "C2");
        Assert.Equal(ElementExecutionStatus.Failed, c2.Status);
        Assert.Equal("ERR_MISSING_TARGET_UNIQUE_ID", c2.ErrorCode);

        var c1 = result.ElementRecords.First(r => r.Mark == "C1");
        Assert.Equal(ElementExecutionStatus.Failed, c1.Status);
        Assert.Equal("ERR_BATCH_ROLLED_BACK", c1.ErrorCode);
    }

    [Fact]
    public async Task ExecutePlanAsync_NoOpWithMissingTargetUniqueId_RejectedWithMissingTargetUniqueId()
    {
        var plan = CreatePlan((CoordinationCategory.Column, "C1"));
        var payload = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "NoOp", targetUniqueId = "" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload));
        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        var c1 = result.ElementRecords[0];
        Assert.Equal(ElementExecutionStatus.Failed, c1.Status);
        Assert.Equal("ERR_MISSING_TARGET_UNIQUE_ID", c1.ErrorCode);
    }

    [Fact]
    public async Task ExecutePlanAsync_NoOpWithTargetUniqueIdMismatch_RejectedWithNoOpTargetMismatch()
    {
        var plan = CreatePlan((CoordinationCategory.Column, "C1"));
        var itemWithTarget = plan.Items[0] with { TargetUniqueId = "expected-ledger-uid-1" };
        plan = plan with { Items = new[] { itemWithTarget } };

        var payload = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "NoOp", targetUniqueId = "different-revit-uid-2" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload));
        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        var c1 = result.ElementRecords[0];
        Assert.Equal(ElementExecutionStatus.Failed, c1.Status);
        Assert.Equal("ERR_NOOP_TARGET_MISMATCH", c1.ErrorCode);
    }

    [Fact]
    public async Task ExecutePlanAsync_NoOpWithMatchingTargetUniqueId_AcceptedAsNoOp()
    {
        var plan = CreatePlan((CoordinationCategory.Column, "C1"));
        var itemWithTarget = plan.Items[0] with { TargetUniqueId = "matched-ledger-uid-1" };
        plan = plan with { Items = new[] { itemWithTarget } };

        var payload = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "NoOp", targetUniqueId = "matched-ledger-uid-1" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload));
        var result = await engine.ExecutePlanAsync(plan);

        Assert.True(result.Success);
        var c1 = result.ElementRecords[0];
        Assert.Equal(ElementExecutionStatus.NoOp, c1.Status);
        Assert.Equal("matched-ledger-uid-1", c1.TargetUniqueId);
    }
}
