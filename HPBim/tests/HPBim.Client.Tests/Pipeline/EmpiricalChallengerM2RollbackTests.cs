namespace HPBim.Client.Tests.Pipeline;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HPBim.Client.Pipeline;
using HPBim.Client.Tests.Fakes;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Geometry;
using HPBim.Core.Models;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

public sealed class EmpiricalChallengerM2RollbackTests
{
    private static BimExecutionPlan CreatePlan(bool dryRun) => new()
    {
        PlanId = Guid.NewGuid(),
        PlanHash = "adversarial-hash",
        Items = new[]
        {
            new ElementPlanItem
            {
                SourceKey = new CompositeSourceKey("TestDoc", CoordinationCategory.Column, "C1"),
                Category = CoordinationCategory.Column,
                Mark = "C1",
                SequenceOrder = 1,
                Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0) }
            }
        },
        Options = new ExecutionOptions { DryRun = dryRun, TransactionLabel = "AdversarialChallenge" }
    };

    private static BimModel CreateModel() => new()
    {
        ProjectName = "AdversarialModel",
        Levels = new[] { new LevelDef("TRET", 0.0, 3600, 1) },
        Grids = new[] { AxisGrid.CreateOrthogonal("X1", 0.0, GridDirection.X, 0, 10000) }
    };

    [Fact]
    public async Task Challenge_DryRun_WhenRolledBackFalse_MustFailEvenIfNoErrorsReported()
    {
        var fake = new FakeRevitPipeClient();
        fake.CustomExecuteHandler = req => new ExecuteResult { IsError = false, RolledBack = false, DurationMs = 50 };

        var engine = new BimPipelineEngine(fake);
        var result = await engine.ExecutePlanAsync(CreatePlan(dryRun: true));

        Assert.False(result.Success);
        Assert.False(result.RolledBack);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("did not confirm transaction rollback", result.ErrorMessage);
        Assert.All(result.StageResults, sr => Assert.False(sr.Success));
    }

    [Fact]
    public async Task Challenge_DryRun_WhenRolledBackFalse_WithNonZeroChanges_MustFail()
    {
        var fake = new FakeRevitPipeClient();
        fake.CustomExecuteHandler = req => new ExecuteResult
        {
            IsError = false,
            RolledBack = false,
            Changed = new ChangedCounts(10, 5, 0),
            DurationMs = 40
        };

        var engine = new BimPipelineEngine(fake);
        var result = await engine.ExecutePlanAsync(CreatePlan(dryRun: true));

        Assert.False(result.Success);
        Assert.False(result.RolledBack);
        Assert.Contains("did not confirm transaction rollback", result.ErrorMessage);
    }

    [Fact]
    public async Task Challenge_DryRun_WhenTimeoutOccurs_RollsBackAndReportsFailure()
    {
        var fake = new FakeRevitPipeClient();
        fake.CustomExecuteHandler = req => ExecuteResult.Failure("Script timed out after 30s", rolledBack: true, timedOut: true);

        var engine = new BimPipelineEngine(fake);
        var result = await engine.ExecutePlanAsync(CreatePlan(dryRun: true));

        Assert.False(result.Success);
        Assert.True(result.RolledBack);
        Assert.Contains("timed out", result.ErrorMessage);
    }

    [Fact]
    public async Task Challenge_Apply_WhenRolledBackTrue_MustFailWithExplicitError()
    {
        var fake = new FakeRevitPipeClient();
        fake.CustomExecuteHandler = req => new ExecuteResult { IsError = false, RolledBack = true, DurationMs = 30 };

        var engine = new BimPipelineEngine(fake);
        var result = await engine.ExecutePlanAsync(CreatePlan(dryRun: false));

        Assert.False(result.Success);
        Assert.True(result.RolledBack);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("unexpectedly rolled back", result.ErrorMessage);
        Assert.All(result.StageResults, sr => Assert.False(sr.Success));
    }

    [Fact]
    public async Task Challenge_Apply_WhenBridgeClaimsCreatedRecords_ButRolledBackTrue_MustFail()
    {
        var recordsPayload = new
        {
            records = new[]
            {
                new
                {
                    sourceKey = "TestDoc#Column:C1",
                    category = "Column",
                    mark = "C1",
                    status = "Created",
                    targetElementId = 1111L
                }
            }
        };

        var fake = new FakeRevitPipeClient();
        fake.CustomExecuteHandler = req => new ExecuteResult
        {
            IsError = false,
            RolledBack = true,
            Value = BridgeJson.ToElement(recordsPayload),
            DurationMs = 25
        };

        var engine = new BimPipelineEngine(fake);
        var result = await engine.ExecutePlanAsync(CreatePlan(dryRun: false));

        Assert.False(result.Success);
        Assert.Contains("unexpectedly rolled back", result.ErrorMessage);
    }

    [Fact]
    public async Task Challenge_SequentialStages_Apply_StopsAtFirstUnexpectedRollback()
    {
        var fake = new FakeRevitPipeClient();
        int invocationCount = 0;
        fake.CustomExecuteHandler = req =>
        {
            invocationCount++;
            return invocationCount == 1
                ? new ExecuteResult { IsError = false, RolledBack = false }
                : new ExecuteResult { IsError = false, RolledBack = true };
        };

        var engine = new BimPipelineEngine(fake);
        var options = new BimPipelineOptions
        {
            DryRun = false,
            AtomicDryRun = false,
            UseExecutionPlan = false,
            SelectedStages = new[] { 1, 2, 3 }
        };

        var result = await engine.ExecuteAsync(CreateModel(), options);

        Assert.False(result.Success);
        Assert.Equal(2, invocationCount);
        Assert.Equal(2, result.StageResults.Count);
        Assert.True(result.StageResults[0].Success);
        Assert.False(result.StageResults[1].Success);
        Assert.Contains("unexpectedly rolled back", result.ErrorMessage);
    }

    [Fact]
    public async Task Challenge_WarningPropagation_PreservesUnicodeAndMultipleWarnings()
    {
        var sampleWarnings = new[]
        {
            "Cảnh báo: Dầm D1 giao cắt với Cột C1 tại cao độ +3.600m.",
            "Warning: Duplicate mark \"W-101\" on Wall element.",
            "Special symbols: < > & \" ' \n \t \\ /"
        };

        var fake = new FakeRevitPipeClient();
        fake.CustomExecuteHandler = req => new ExecuteResult
        {
            IsError = false,
            RolledBack = true,
            Warnings = sampleWarnings,
            DurationMs = 20
        };

        var engine = new BimPipelineEngine(fake);
        var result = await engine.ExecutePlanAsync(CreatePlan(dryRun: true));

        Assert.True(result.Success);
        Assert.Equal(3, result.Warnings.Count);
        for (int i = 0; i < sampleWarnings.Length; i++)
        {
            Assert.Equal(sampleWarnings[i], result.Warnings[i]);
        }
        Assert.All(result.StageResults, sr => Assert.Equal(3, sr.Warnings.Count));
    }

    [Fact]
    public async Task Challenge_WarningPropagation_PreservesWarningsOnExecutionFailure()
    {
        var fake = new FakeRevitPipeClient();
        fake.CustomExecuteHandler = req => new ExecuteResult
        {
            IsError = true,
            Message = "Crash during creation",
            RolledBack = true,
            Warnings = new[] { "Warning collected before crash" },
            DurationMs = 15
        };

        var engine = new BimPipelineEngine(fake);
        var result = await engine.ExecutePlanAsync(CreatePlan(dryRun: true));

        Assert.False(result.Success);
        Assert.Single(result.Warnings);
        Assert.Equal("Warning collected before crash", result.Warnings[0]);
    }

    [Fact]
    public async Task Challenge_WarningPropagation_NullWarningsInExecuteResult_DoesNotThrow()
    {
        var fake = new FakeRevitPipeClient();
        fake.CustomExecuteHandler = req => new ExecuteResult
        {
            IsError = false,
            RolledBack = true,
            Warnings = null!,
            DurationMs = 10
        };

        var engine = new BimPipelineEngine(fake);
        var result = await engine.ExecutePlanAsync(CreatePlan(dryRun: true));

        Assert.True(result.Success);
        Assert.NotNull(result.Warnings);
        Assert.Empty(result.Warnings);
        Assert.All(result.StageResults, sr => Assert.NotNull(sr.Warnings));
    }

    [Fact]
    public async Task Challenge_DryRun_WithElementFailures_FailsEvenIfRolledBackTrue()
    {
        var recordsPayload = new
        {
            records = new[]
            {
                new
                {
                    sourceKey = "TestDoc#Column:C1",
                    category = "Column",
                    mark = "C1",
                    status = "Failed",
                    errorCode = "ERR_TEST",
                    errorReason = "Simulated element creation error"
                }
            }
        };

        var fake = new FakeRevitPipeClient();
        fake.CustomExecuteHandler = req => new ExecuteResult
        {
            IsError = false,
            RolledBack = true,
            Value = BridgeJson.ToElement(recordsPayload),
            DurationMs = 15
        };

        var engine = new BimPipelineEngine(fake);
        var result = await engine.ExecutePlanAsync(CreatePlan(dryRun: true));

        Assert.False(result.Success);
        Assert.True(result.RolledBack);
        Assert.Contains("Execution failed for 1 element(s)", result.ErrorMessage);
    }
}
