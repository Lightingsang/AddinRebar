namespace HPBim.Client.Tests.Pipeline;

using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HPBim.Client.Pipeline;
using HPBim.Client.Tests.Fakes;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Geometry;
using HPBim.Core.Models;
using HPBim.Core.Tests.Fixtures;
using HPBim.Core.Validation;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

public sealed class SingleExecutionPlanTests
{
    [Fact]
    public void ExecutionPlanBuilder_NhaPho5x18m_MapsAllCategoriesWithZeroFallback()
    {
        var model = NhaPho5x18mFixture.CreateModel();
        var plan = ExecutionPlanBuilder.Build(model, options: new ExecutionOptions { CreatePlanViews = true });

        Assert.NotNull(plan);
        Assert.NotEmpty(plan.Items);
        Assert.False(string.IsNullOrWhiteSpace(plan.PlanHash));

        // Verify topological sequencing
        for (int i = 0; i < plan.Items.Count; i++)
        {
            Assert.Equal(i + 1, plan.Items[i].SequenceOrder);
        }

        // Verify every element category is represented
        var categories = plan.Items.Select(x => x.Category).Distinct().ToHashSet();
        Assert.Contains(CoordinationCategory.Level, categories);
        Assert.Contains(CoordinationCategory.Grid, categories);
        Assert.Contains(CoordinationCategory.Column, categories);
        Assert.Contains(CoordinationCategory.Wall, categories);
        Assert.Contains(CoordinationCategory.Beam, categories);
        Assert.Contains(CoordinationCategory.Slab, categories);
        Assert.Contains(CoordinationCategory.Room, categories);
        Assert.Contains(CoordinationCategory.MepFixture, categories);
        Assert.Contains(CoordinationCategory.PipeRun, categories);
        Assert.Contains(CoordinationCategory.Rebar, categories);

        // Verify level associated views requested
        var levelItems = plan.Items.Where(x => x.Category == CoordinationCategory.Level).ToList();
        Assert.All(levelItems, item => Assert.True(item.CreateAssociatedView));
    }

    [Fact]
    public void ExecutionPlanBuilder_MissingLevelReference_ThrowsSchemaValidationException()
    {
        var model = new BimModel
        {
            ProjectName = "BrokenModel",
            Levels = new[] { new LevelDef("TRET", 0.0, 3600, 1) },
            Columns = new[]
            {
                new ColumnDef
                {
                    Id = "C1",
                    BaseLevel = "NON_EXISTENT_LEVEL",
                    WidthMm = 220,
                    DepthMm = 220
                }
            }
        };

        var ex = Assert.Throws<SchemaValidationException>(() => ExecutionPlanBuilder.Build(model));
        Assert.Contains(ex.ValidationErrors, err => err.Contains("ERR_BROKEN_REFERENCE"));
    }

    [Fact]
    public async Task BimPipelineEngine_ExecutePlanAsync_PreviewAndApplyUseIdenticalScriptAndArgs()
    {
        var fakeClient = new FakeRevitPipeClient();
        var engine = new BimPipelineEngine(fakeClient);
        var model = NhaPho5x18mFixture.CreateModel();

        var fixedOperationId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var basePlan = ExecutionPlanBuilder.Build(model, options: new ExecutionOptions
        {
            TimeoutSeconds = 120,
            CreatePlanViews = true,
            OperationId = fixedOperationId
        });

        var previewPlan = basePlan with { Options = basePlan.Options with { DryRun = true } };
        var applyPlan = basePlan with { Options = basePlan.Options with { DryRun = false } };

        // 1. Run Preview
        var previewResult = await engine.ExecutePlanAsync(previewPlan);
        Assert.True(previewResult.Success);
        Assert.True(previewResult.RolledBack);

        // 2. Run Apply
        var applyResult = await engine.ExecutePlanAsync(applyPlan);
        Assert.True(applyResult.Success);
        Assert.False(applyResult.RolledBack);

        // 3. Verify exactly 2 requests sent
        Assert.Equal(2, fakeClient.ExecutedRequests.Count);
        var previewReq = fakeClient.ExecutedRequests[0];
        var applyReq = fakeClient.ExecutedRequests[1];

        // 4. Verify identical script and IR items, differing only in DryRun flag
        Assert.Equal(previewReq.Code, applyReq.Code);
        Assert.Equal(previewPlan.PlanHash, applyPlan.PlanHash);
        Assert.Equal(previewPlan.Items.Count, applyPlan.Items.Count);
        Assert.True(previewReq.DryRun);
        Assert.False(applyReq.DryRun);
    }

    [Fact]
    public async Task BimPipelineEngine_ExecutePlanAsync_ParsesGranularElementRecords()
    {
        var fakeResponseJson = """
        {
            "records": [
                {
                    "sourceKey": "Doc#Level:TRET",
                    "mark": "TRET",
                    "category": "Level",
                    "status": "Created",
                    "targetElementId": 1001,
                    "targetUniqueId": "guid-lvl-1"
                },
                {
                    "sourceKey": "Doc#Column:C1",
                    "mark": "C1",
                    "category": "Column",
                    "status": "Failed",
                    "errorCode": "ERR_SYMBOL_MISSING",
                    "errorReason": "Column family symbol 'Concrete 220x220' not found."
                }
            ]
        }
        """;

        var fakeClient = new FakeRevitPipeClient
        {
            CustomExecuteHandler = req => new ExecuteResult
            {
                IsError = false,
                DurationMs = 45,
                RolledBack = req.DryRun,
                Value = JsonDocument.Parse(fakeResponseJson).RootElement
            }
        };
        var engine = new BimPipelineEngine(fakeClient);

        var item1 = new ElementPlanItem
        {
            SourceKey = CompositeSourceKey.Parse("Doc#Level:TRET"),
            Category = CoordinationCategory.Level,
            Mark = "TRET",
            SequenceOrder = 1
        };
        var item2 = new ElementPlanItem
        {
            SourceKey = CompositeSourceKey.Parse("Doc#Column:C1"),
            Category = CoordinationCategory.Column,
            Mark = "C1",
            SequenceOrder = 2
        };
        var plan = new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "hash-123",
            Items = new[] { item1, item2 },
            Options = new ExecutionOptions { DryRun = true }
        };

        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        Assert.NotNull(result.ElementRecords);
        Assert.Equal(2, result.ElementRecords.Count);

        var lvlRecord = result.ElementRecords[0];
        Assert.Equal(ElementExecutionStatus.Created, lvlRecord.Status);
        Assert.Equal(1001L, lvlRecord.TargetElementId);
        Assert.Equal("guid-lvl-1", lvlRecord.TargetUniqueId);

        var colRecord = result.ElementRecords[1];
        Assert.Equal(ElementExecutionStatus.Failed, colRecord.Status);
        Assert.Equal("ERR_SYMBOL_MISSING", colRecord.ErrorCode);
        Assert.Contains("not found", colRecord.ErrorReason);
    }
}
