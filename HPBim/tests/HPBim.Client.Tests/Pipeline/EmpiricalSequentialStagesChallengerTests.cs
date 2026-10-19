namespace HPBim.Client.Tests.Pipeline;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HPBim.Client.Pipeline;
using HPBim.Client.Tests.Fakes;
using HPBim.Core.Models;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

public sealed class EmpiricalSequentialStagesChallengerTests
{
    private static BimModel CreateModel() => new()
    {
        ProjectName = "ChallengerSequentialModel",
        Levels = new[] { new LevelDef("TRET", 0.0, 3600, 1) },
        Grids = new[] { AxisGrid.CreateOrthogonal("X1", 0.0, GridDirection.X, 0, 10000) }
    };

    private static Task<BimPipelineResult> RunSequentialAsync(
        FakeRevitPipeClient fake,
        bool dryRun,
        int[] stages,
        Action<StageResult>? onCompleted = null)
    {
        var engine = new BimPipelineEngine(fake);
        if (onCompleted != null) engine.StageCompleted += onCompleted;
        return engine.ExecuteAsync(CreateModel(), new BimPipelineOptions
        {
            DryRun = dryRun,
            AtomicDryRun = false,
            UseExecutionPlan = false,
            SelectedStages = stages
        });
    }

    [Fact]
    public async Task Challenge_SequentialStages_DryRun_MidStageRollbackUnconfirmed_HaltsImmediately()
    {
        var fake = new FakeRevitPipeClient();
        int callCount = 0;
        fake.CustomExecuteHandler = _ =>
        {
            callCount++;
            return callCount switch
            {
                1 => new ExecuteResult { IsError = false, RolledBack = true },
                2 => new ExecuteResult { IsError = false, RolledBack = true },
                3 => new ExecuteResult { IsError = false, RolledBack = false },
                _ => new ExecuteResult { IsError = false, RolledBack = true }
            };
        };

        var result = await RunSequentialAsync(fake, dryRun: true, stages: new[] { 1, 2, 3, 4 });

        Assert.False(result.Success);
        Assert.Equal(3, callCount);
        Assert.Equal(3, result.StageResults.Count);
        Assert.True(result.StageResults[0].Success);
        Assert.True(result.StageResults[1].Success);
        Assert.False(result.StageResults[2].Success);
        Assert.Contains("did not confirm transaction rollback", result.ErrorMessage);
        Assert.True(result.RolledBack);
    }

    [Fact]
    public async Task Challenge_SequentialStages_DryRun_FirstStageRollbackUnconfirmed_HaltsWithRolledBackFalse()
    {
        var fake = new FakeRevitPipeClient();
        fake.CustomExecuteHandler = _ => new ExecuteResult { IsError = false, RolledBack = false };

        var result = await RunSequentialAsync(fake, dryRun: true, stages: new[] { 1, 2 });

        Assert.False(result.Success);
        Assert.False(result.RolledBack);
        Assert.Single(result.StageResults);
        Assert.Contains("Stage 1 (Levels) failed", result.ErrorMessage);
        Assert.Contains("did not confirm transaction rollback", result.ErrorMessage);
    }

    [Fact]
    public async Task Challenge_SequentialStages_DryRun_AllStagesRollbackConfirmed_SucceedsWithRolledBackTrue()
    {
        var fake = new FakeRevitPipeClient();
        fake.CustomExecuteHandler = _ => new ExecuteResult { IsError = false, RolledBack = true };

        var result = await RunSequentialAsync(fake, dryRun: true, stages: new[] { 1, 2, 3 });

        Assert.True(result.Success);
        Assert.True(result.RolledBack);
        Assert.Equal(3, result.StageResults.Count);
        Assert.All(result.StageResults, sr => Assert.True(sr.Success && sr.RolledBack));
    }

    [Fact]
    public async Task Challenge_SequentialStages_Apply_MidStageUnexpectedRollback_HaltsImmediately()
    {
        var fake = new FakeRevitPipeClient();
        int callCount = 0;
        fake.CustomExecuteHandler = _ =>
        {
            callCount++;
            return callCount == 2
                ? new ExecuteResult { IsError = false, RolledBack = true }
                : new ExecuteResult { IsError = false, RolledBack = false };
        };

        var result = await RunSequentialAsync(fake, dryRun: false, stages: new[] { 1, 2, 3 });

        Assert.False(result.Success);
        Assert.Equal(2, callCount);
        Assert.Equal(2, result.StageResults.Count);
        Assert.True(result.StageResults[0].Success);
        Assert.False(result.StageResults[1].Success);
        Assert.True(result.RolledBack);
        Assert.Contains("unexpectedly rolled back", result.ErrorMessage);
    }

    [Fact]
    public async Task Challenge_SequentialStages_EarlyTermination_ExecutesNoDownstreamStagesOnScriptError()
    {
        var fake = new FakeRevitPipeClient();
        int callCount = 0;
        fake.CustomExecuteHandler = _ =>
        {
            callCount++;
            return callCount == 2
                ? ExecuteResult.Failure("Syntax error in grid generation")
                : new ExecuteResult { IsError = false, RolledBack = false };
        };

        var result = await RunSequentialAsync(fake, dryRun: false, stages: new[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        Assert.False(result.Success);
        Assert.Equal(2, callCount);
        Assert.Equal(2, result.StageResults.Count);
        Assert.Contains("Stage 2 (Grids) failed", result.ErrorMessage);
    }

    [Fact]
    public async Task Challenge_SequentialStages_WarningPropagation_AggregatesAndDeduplicates()
    {
        var fake = new FakeRevitPipeClient();
        int callCount = 0;
        fake.CustomExecuteHandler = _ =>
        {
            callCount++;
            return callCount switch
            {
                1 => new ExecuteResult { IsError = false, Warnings = new[] { "W1", "SharedWarning" } },
                2 => new ExecuteResult { IsError = false, Warnings = new[] { "SharedWarning", "W2" } },
                _ => new ExecuteResult { IsError = false, Warnings = new[] { "Cảnh báo Tiếng Việt: Dầm D1" } }
            };
        };

        var result = await RunSequentialAsync(fake, dryRun: false, stages: new[] { 1, 2, 3 });

        Assert.True(result.Success);
        Assert.Equal(4, result.Warnings.Count);
        Assert.Contains("W1", result.Warnings);
        Assert.Contains("SharedWarning", result.Warnings);
        Assert.Contains("W2", result.Warnings);
        Assert.Contains("Cảnh báo Tiếng Việt: Dầm D1", result.Warnings);
    }

    [Fact]
    public async Task Challenge_SequentialStages_WarningPropagation_PreservesWarningsOnFailure()
    {
        var fake = new FakeRevitPipeClient();
        int callCount = 0;
        fake.CustomExecuteHandler = _ =>
        {
            callCount++;
            return callCount == 1
                ? new ExecuteResult { IsError = false, Warnings = new[] { "Stage1Warning" } }
                : new ExecuteResult { IsError = true, Message = "Failure at stage 2", Warnings = new[] { "Stage2Warning" } };
        };

        var result = await RunSequentialAsync(fake, dryRun: false, stages: new[] { 1, 2, 3 });

        Assert.False(result.Success);
        Assert.Equal(2, result.Warnings.Count);
        Assert.Contains("Stage1Warning", result.Warnings);
        Assert.Contains("Stage2Warning", result.Warnings);
    }

    [Fact]
    public async Task Challenge_SequentialStages_NullWarnings_DoesNotThrowNre()
    {
        var fake = new FakeRevitPipeClient();
        fake.CustomExecuteHandler = _ => new ExecuteResult { IsError = false, Warnings = null! };

        var result = await RunSequentialAsync(fake, dryRun: false, stages: new[] { 1, 2 });

        Assert.True(result.Success);
        Assert.NotNull(result.Warnings);
        Assert.Empty(result.Warnings);
        Assert.All(result.StageResults, sr => Assert.Empty(sr.Warnings));
    }

    [Fact]
    public async Task Challenge_SequentialStages_StageCompletedEvent_FiresForExecutedStagesOnly()
    {
        var fake = new FakeRevitPipeClient();
        int callCount = 0;
        fake.CustomExecuteHandler = _ =>
        {
            callCount++;
            return callCount == 2
                ? ExecuteResult.Failure("Stage 2 failure")
                : new ExecuteResult { IsError = false };
        };

        var firedStages = new List<StageResult>();
        var result = await RunSequentialAsync(fake, dryRun: false, stages: new[] { 1, 2, 3 }, sr => firedStages.Add(sr));

        Assert.False(result.Success);
        Assert.Equal(2, firedStages.Count);
        Assert.True(firedStages[0].Success);
        Assert.False(firedStages[1].Success);
    }

    [Fact]
    public async Task Challenge_SequentialStages_FormatErrorMessage_IncludesDiagnosticsOnFailure()
    {
        var fake = new FakeRevitPipeClient();
        fake.CustomExecuteHandler = _ => new ExecuteResult
        {
            IsError = true,
            Message = "Compilation error",
            Diagnostics = new[] { new ScriptDiagnostic(42, 10, "CS0103", "The name 'Foo' does not exist") }
        };

        var result = await RunSequentialAsync(fake, dryRun: false, stages: new[] { 1 });

        Assert.False(result.Success);
        Assert.Contains("[L42:C10] CS0103: The name 'Foo' does not exist", result.ErrorMessage);
    }
}
