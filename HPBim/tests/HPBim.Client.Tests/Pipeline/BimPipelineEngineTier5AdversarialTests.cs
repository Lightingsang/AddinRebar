namespace HPBim.Client.Tests.Pipeline;

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Client.Pipeline;
using HPBim.Client.Tests.Fakes;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

public sealed class BimPipelineEngineTier5AdversarialTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _journalPath;

    public BimPipelineEngineTier5AdversarialTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "hpbim_t5_pipe_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _journalPath = Path.Combine(_tempDir, "intent_journal.jsonl");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
        }
        catch (IOException)
        {
            // Transient test lock cleanup tolerance
        }
    }

    private static BimExecutionPlan CreatePlan(bool dryRun, int itemCount = 1) =>
        new()
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "plan-t5-hash",
            Items = Enumerable.Range(1, itemCount).Select(i => new ElementPlanItem
            {
                SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, $"C{i}"),
                Category = CoordinationCategory.Column,
                Mark = $"C{i}",
                SequenceOrder = i
            }).ToList(),
            Options = new ExecutionOptions
            {
                OperationId = Guid.NewGuid(),
                DryRun = dryRun,
                TransactionLabel = "Tier5TestPlan"
            }
        };

    [Fact]
    public async Task ExecutePlanJournaled_PipeDisconnectDuringExecution_MarksOutcomeUnknownAndRecordsJournal()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient { ShouldDisconnectOnExecute = true };
        var engine = new BimPipelineEngine(fakePipe, intentJournal: journal);
        var plan = CreatePlan(dryRun: false);

        var result = await engine.ExecutePlanJournaledAsync(plan);

        Assert.False(result.Success);
        Assert.True(result.IsOutcomeUnknown);
        Assert.Contains("Execution outcome unknown", result.ErrorMessage);

        var recorded = await journal.GetIntentAsync(plan.Options.OperationId);
        Assert.NotNull(recorded);
        Assert.Equal(IntentStatus.OutcomeUnknown, recorded.Status);
    }

    [Fact]
    public async Task ExecutePlanJournaled_PipeTimeoutDuringExecution_MarksOutcomeUnknownAndRecordsJournal()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient { ShouldTimeoutOnExecute = true };
        var engine = new BimPipelineEngine(fakePipe, intentJournal: journal);
        var plan = CreatePlan(dryRun: false);

        var result = await engine.ExecutePlanJournaledAsync(plan);

        Assert.False(result.Success);
        Assert.True(result.IsOutcomeUnknown);
        Assert.Contains("Auto-reconciliation required", result.ErrorMessage);

        var recorded = await journal.GetIntentAsync(plan.Options.OperationId);
        Assert.NotNull(recorded);
        Assert.Equal(IntentStatus.OutcomeUnknown, recorded.Status);
    }

    [Fact]
    public async Task ExecutePlanJournaled_ApplyExecution_UnexpectedRollback_FailsWithExplicitMessage()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient
        {
            CustomExecuteHandler = req => new ExecuteResult
            {
                IsError = false,
                RolledBack = true // Unexpected rollback in Apply mode
            }
        };

        var engine = new BimPipelineEngine(fakePipe, intentJournal: journal);
        var plan = CreatePlan(dryRun: false);

        var result = await engine.ExecutePlanJournaledAsync(plan);

        Assert.False(result.Success);
        Assert.True(result.RolledBack);
        Assert.Contains("unexpectedly rolled back", result.ErrorMessage);

        var recorded = await journal.GetIntentAsync(plan.Options.OperationId);
        Assert.NotNull(recorded);
        Assert.Equal(IntentStatus.Failed, recorded.Status);
    }

    [Fact]
    public async Task ExecutePlanJournaled_DryRun_UnconfirmedRollback_FailsWithExplicitMessage()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient
        {
            CustomExecuteHandler = req => new ExecuteResult
            {
                IsError = false,
                RolledBack = false // Dry-run failed to rollback
            }
        };

        var engine = new BimPipelineEngine(fakePipe, intentJournal: journal);
        var plan = CreatePlan(dryRun: true);

        var result = await engine.ExecutePlanJournaledAsync(plan);

        Assert.False(result.Success);
        Assert.False(result.RolledBack);
        Assert.Contains("did not confirm transaction rollback", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecutePlanAsync_RevitWarnings_PropagatesToResultAndStageResults()
    {
        var fakePipe = new FakeRevitPipeClient
        {
            SimulatedWarnings = new[] { "Warning Alpha", "Warning Beta" }
        };

        var engine = new BimPipelineEngine(fakePipe);
        var plan = CreatePlan(dryRun: true);

        var result = await engine.ExecutePlanAsync(plan);

        Assert.True(result.Success);
        Assert.Equal(2, result.Warnings.Count);
        Assert.Contains("Warning Alpha", result.Warnings);
        Assert.Contains("Warning Beta", result.Warnings);
        Assert.All(result.StageResults, sr => Assert.Equal(2, sr.Warnings.Count));
    }

    [Fact]
    public async Task ExecutePlanAsync_ElementFailure_IdentifiesFailedElementsAndSetsOverallFailure()
    {
        var recordsPayload = new
        {
            records = new[]
            {
                new
                {
                    sourceKey = "Doc#Column:C1",
                    category = "Column",
                    mark = "C1",
                    status = "Failed",
                    errorCode = "ERR_SYMBOL_NOT_FOUND",
                    errorReason = "Concrete column 220x220 not found"
                }
            }
        };

        var fakePipe = new FakeRevitPipeClient
        {
            CustomExecuteHandler = req => new ExecuteResult
            {
                IsError = false,
                RolledBack = req.DryRun,
                Value = BridgeJson.ToElement(recordsPayload)
            }
        };

        var engine = new BimPipelineEngine(fakePipe);
        var plan = CreatePlan(dryRun: true);

        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        Assert.Contains("ERR_SYMBOL_NOT_FOUND", result.ErrorMessage);
        Assert.Contains("Concrete column 220x220 not found", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecutePlanJournaled_CalculatesTimeoutFromPolicy_WhenPlanTimeoutZero()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var policy = new TimeoutPolicy
        {
            QueueDeadline = TimeSpan.FromSeconds(20),
            BaseExecutionBudget = TimeSpan.FromSeconds(10),
            PerItemBudget = TimeSpan.FromMilliseconds(100)
        };

        var engine = new BimPipelineEngine(fakePipe, intentJournal: journal, timeoutPolicy: policy);
        var plan = CreatePlan(dryRun: false, itemCount: 10) with
        {
            Options = new ExecutionOptions { TimeoutSeconds = 0 }
        };

        await engine.ExecutePlanJournaledAsync(plan);

        Assert.Single(fakePipe.ExecutedRequests);
        var req = fakePipe.ExecutedRequests[0];
        // 20s + (10s + 10 * 100ms) = 20s + 11s = 31s
        Assert.Equal(31, req.TimeoutSeconds);
    }
}
