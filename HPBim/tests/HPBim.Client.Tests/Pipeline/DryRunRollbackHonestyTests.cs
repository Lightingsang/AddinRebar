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
using HPBim.Core.Geometry;
using HPBim.Core.Models;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

public sealed class DryRunRollbackHonestyTests
{
    private static BimExecutionPlan CreateSamplePlan(bool dryRun)
    {
        var item = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("TestDoc", CoordinationCategory.Column, "C1"),
            Category = CoordinationCategory.Column,
            Mark = "C1",
            SequenceOrder = 1,
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0) }
        };

        return new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "sample-hash",
            Items = new[] { item },
            Options = new ExecutionOptions
            {
                DryRun = dryRun,
                TransactionLabel = "SamplePlanExecution"
            }
        };
    }

    private static BimModel CreateSimpleModel() => new()
    {
        ProjectName = "SampleProject",
        Levels = new[] { new LevelDef("TRET", 0.0, 3600, 1) },
        Grids = new[] { AxisGrid.CreateOrthogonal("X1", 0.0, GridDirection.X, 0, 10000) }
    };

    [Fact]
    public async Task ExecutePlanAsync_DryRun_WhenRollbackConfirmed_SucceedsAndReportsRolledBackTrue()
    {
        var fake = new FakeRevitPipeClient();
        fake.CustomExecuteHandler = req => new ExecuteResult
        {
            IsError = false,
            RolledBack = true,
            Changed = new ChangedCounts(1, 0, 0),
            DurationMs = 20
        };

        var engine = new BimPipelineEngine(fake);
        var plan = CreateSamplePlan(dryRun: true);

        var result = await engine.ExecutePlanAsync(plan);

        Assert.True(result.Success);
        Assert.True(result.RolledBack);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ExecutePlanAsync_DryRun_WhenRollbackUnconfirmed_FailsWithExplicitErrorMessage()
    {
        var fake = new FakeRevitPipeClient();
        fake.CustomExecuteHandler = req => new ExecuteResult
        {
            IsError = false,
            RolledBack = false, // Unconfirmed rollback simulation defect
            Changed = new ChangedCounts(1, 0, 0),
            DurationMs = 20
        };

        var engine = new BimPipelineEngine(fake);
        var plan = CreateSamplePlan(dryRun: true);

        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        Assert.False(result.RolledBack);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("did not confirm transaction rollback", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecutePlanAsync_Apply_WhenRolledBackUnexpectedly_FailsWithExplicitErrorMessage()
    {
        var fake = new FakeRevitPipeClient();
        fake.CustomExecuteHandler = req => new ExecuteResult
        {
            IsError = false,
            RolledBack = true, // Unexpected rollback in Apply mode
            Changed = new ChangedCounts(0, 0, 0),
            DurationMs = 20
        };

        var engine = new BimPipelineEngine(fake);
        var plan = CreateSamplePlan(dryRun: false);

        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("unexpectedly rolled back", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecutePlanAsync_WhenRevitReturnsWarnings_PropagatesWarningsToResultAndStages()
    {
        var fake = new FakeRevitPipeClient();
        fake.CustomExecuteHandler = req => new ExecuteResult
        {
            IsError = false,
            RolledBack = true,
            Warnings = new[] { "Wall overlap detected", "Join geometry warning" },
            DurationMs = 30
        };

        var engine = new BimPipelineEngine(fake);
        var plan = CreateSamplePlan(dryRun: true);

        var result = await engine.ExecutePlanAsync(plan);

        Assert.True(result.Success);
        Assert.Equal(2, result.Warnings.Count);
        Assert.Contains("Wall overlap detected", result.Warnings);
        Assert.Contains("Join geometry warning", result.Warnings);
        Assert.All(result.StageResults, sr => Assert.Equal(2, sr.Warnings.Count));
    }

    [Fact]
    public async Task ExecuteAsync_AtomicDryRun_WhenRollbackUnconfirmed_Fails()
    {
        var fake = new FakeRevitPipeClient();
        fake.CustomExecuteHandler = req => new ExecuteResult
        {
            IsError = false,
            RolledBack = false, // Rollback not confirmed
            DurationMs = 25
        };

        var engine = new BimPipelineEngine(fake);
        var options = new BimPipelineOptions
        {
            DryRun = true,
            AtomicDryRun = true,
            UseExecutionPlan = false
        };

        var result = await engine.ExecuteAsync(CreateSimpleModel(), options);

        Assert.False(result.Success);
        Assert.False(result.RolledBack);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("did not confirm transaction rollback", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_SequentialStages_DryRun_WhenRollbackUnconfirmed_FailsAndHalts()
    {
        var fake = new FakeRevitPipeClient();
        fake.CustomExecuteHandler = req => new ExecuteResult
        {
            IsError = false,
            RolledBack = false, // Failure to rollback
            DurationMs = 20
        };

        var engine = new BimPipelineEngine(fake);
        var options = new BimPipelineOptions
        {
            DryRun = true,
            AtomicDryRun = false,
            UseExecutionPlan = false,
            SelectedStages = new[] { 1 }
        };

        var result = await engine.ExecuteAsync(CreateSimpleModel(), options);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("did not confirm transaction rollback", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_SequentialStages_CapturesWarningsAcrossStages()
    {
        var fake = new FakeRevitPipeClient();
        int callCount = 0;
        fake.CustomExecuteHandler = req =>
        {
            callCount++;
            return new ExecuteResult
            {
                IsError = false,
                RolledBack = false,
                Warnings = new[] { $"StageWarning_{callCount}" },
                DurationMs = 15
            };
        };

        var engine = new BimPipelineEngine(fake);
        var options = new BimPipelineOptions
        {
            DryRun = false,
            AtomicDryRun = false,
            UseExecutionPlan = false,
            SelectedStages = new[] { 1, 2 }
        };

        var result = await engine.ExecuteAsync(CreateSimpleModel(), options);

        Assert.True(result.Success);
        Assert.Contains("StageWarning_1", result.Warnings);
        Assert.Contains("StageWarning_2", result.Warnings);
    }

    [Fact]
    public async Task ResolvePlanExecutionOutcome_PreservesDiagnosticsWhenFailed()
    {
        var fake = new FakeRevitPipeClient();
        fake.CustomExecuteHandler = req => new ExecuteResult
        {
            IsError = true,
            Message = "Compilation failed",
            Diagnostics = new[] { new ScriptDiagnostic(12, 4, "CS0103", "The name 'foo' does not exist") },
            DurationMs = 10
        };

        var engine = new BimPipelineEngine(fake);
        var plan = CreateSamplePlan(dryRun: true);

        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("CS0103", result.ErrorMessage);
    }

    [Fact]
    public async Task ParseSingleElementRecord_DeserializesElementLevelWarnings()
    {
        var fake = new FakeRevitPipeClient();
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
                    targetElementId = 98765L,
                    targetUniqueId = "revit-guid-123",
                    warnings = new[] { "Warning 1", "Warning 2" }
                }
            }
        };

        fake.CustomExecuteHandler = req => new ExecuteResult
        {
            IsError = false,
            RolledBack = true,
            Value = BridgeJson.ToElement(recordsPayload),
            DurationMs = 10
        };

        var engine = new BimPipelineEngine(fake);
        var plan = CreateSamplePlan(dryRun: true);

        var result = await engine.ExecutePlanAsync(plan);

        Assert.True(result.Success);
        Assert.Single(result.ElementRecords);
        var record = result.ElementRecords[0];
        Assert.Equal(2, record.Warnings.Count);
        Assert.Contains("Warning 1", record.Warnings);
        Assert.Contains("Warning 2", record.Warnings);
    }
}
