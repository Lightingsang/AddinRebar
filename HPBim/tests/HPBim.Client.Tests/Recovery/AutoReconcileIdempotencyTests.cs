namespace HPBim.Client.Tests.Recovery;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Client.Pipeline;
using HPBim.Client.Recovery;
using HPBim.Client.Tests.Fakes;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

public sealed class AutoReconcileIdempotencyTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _journalPath;

    public AutoReconcileIdempotencyTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "hpbim_idempotency_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _journalPath = Path.Combine(_tempDir, "intent_journal.jsonl");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch (IOException)
        {
            // Allow transient file lock cleanup skip
        }
    }

    [Fact]
    public async Task EndToEnd_Timeout_AutoReconcile_RerunPlan_CreatesZeroDuplicates()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var engine = new BimPipelineEngine(fakePipe, intentJournal: journal, timeoutPolicy: new TimeoutPolicy()) { LedgerService = ledger };

        var plan = BuildTestPlan(4);
        await RunPhase1_TimeoutExecutionAsync(engine, plan, ledger, fakePipe, journal);

        var elementMap = SetupPhase2_RevitCommittedReceipt(fakePipe, plan);
        await RunPhase2_AutoReconciliationAsync(journal, fakePipe, ledger, plan);

        await RunPhase3_RerunPlanAndVerifyZeroDuplicatesAsync(engine, fakePipe, plan, ledger, elementMap);
    }

    private static async Task RunPhase1_TimeoutExecutionAsync(
        BimPipelineEngine engine, BimExecutionPlan plan, BimCoordinationLedgerService ledger,
        FakeRevitPipeClient fakePipe, IntentJournal journal)
    {
        fakePipe.ShouldTimeoutOnExecute = true;
        var run1 = await engine.ExecutePlanJournaledAsync(plan, ledger);

        Assert.False(run1.Success);
        Assert.True(run1.IsOutcomeUnknown);
        Assert.Equal(plan.Options.OperationId, run1.OperationId);

        var intent = await journal.GetIntentAsync(plan.Options.OperationId);
        Assert.NotNull(intent);
        Assert.Equal(IntentStatus.OutcomeUnknown, intent.Status);
    }

    private static Dictionary<string, string> SetupPhase2_RevitCommittedReceipt(FakeRevitPipeClient fakePipe, BimExecutionPlan plan)
    {
        fakePipe.ShouldTimeoutOnExecute = false;
        var map = plan.Items.Select((it, i) => new { it.SourceKey, Uid = $"revit-unique-id-{i}" })
            .ToDictionary(x => x.SourceKey.ToString(), x => x.Uid);

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = plan.Options.OperationId,
            PlanHash = plan.PlanHash,
            TargetDocId = plan.TargetDoc?.Title ?? string.Empty,
            CommittedAtUtc = DateTime.UtcNow.ToString("o"),
            ElementMap = map
        });
        return map;
    }

    private static async Task RunPhase2_AutoReconciliationAsync(
        IntentJournal journal, FakeRevitPipeClient fakePipe, BimCoordinationLedgerService ledger, BimExecutionPlan plan)
    {
        var reconcileService = new ReconcileService(journal, fakePipe, ledger);
        var reconcileResults = await reconcileService.ReconcileAllUnknownIntentsAsync();

        Assert.Single(reconcileResults);
        Assert.Equal(ReconcileStatus.Committed, reconcileResults[0].Status);
        Assert.Equal(plan.Items.Count, reconcileResults[0].MappedElementCount);

        var intent = await journal.GetIntentAsync(plan.Options.OperationId);
        Assert.NotNull(intent);
        Assert.Equal(IntentStatus.Committed, intent.Status);

        foreach (var item in plan.Items)
        {
            Assert.True(ledger.TryGetTarget(item.SourceKey, out var uid));
            Assert.False(string.IsNullOrEmpty(uid));
        }
    }

    private static async Task RunPhase3_RerunPlanAndVerifyZeroDuplicatesAsync(
        BimPipelineEngine engine, FakeRevitPipeClient fakePipe, BimExecutionPlan plan,
        BimCoordinationLedgerService ledger, Dictionary<string, string> elementMap)
    {
        ConfigureFakePipeForNoOpRerun(fakePipe, plan, elementMap);
        var run2 = await engine.ExecutePlanJournaledAsync(plan, ledger);

        Assert.True(run2.Success);
        Assert.False(run2.IsOutcomeUnknown);
        Assert.Equal(0, run2.CreatedCount);
        Assert.Equal(plan.Items.Count, run2.NoOpCount);
        Assert.Equal(0, run2.FailedCount);
    }

    private static void ConfigureFakePipeForNoOpRerun(FakeRevitPipeClient fakePipe, BimExecutionPlan plan, Dictionary<string, string> map)
    {
        fakePipe.CustomExecuteHandler = req =>
        {
            var noOpRecords = plan.Items.Select(it => new Dictionary<string, object>
            {
                ["sourceKey"] = it.SourceKey.ToString(),
                ["category"] = it.Category.ToString(),
                ["mark"] = it.Mark,
                ["status"] = "NoOp",
                ["targetUniqueId"] = map[it.SourceKey.ToString()]
            }).ToList();

            var payload = new
            {
                planId = plan.Options.OperationId.ToString("D"),
                total = plan.Items.Count,
                created = 0,
                updated = 0,
                noOp = plan.Items.Count,
                skipped = 0,
                failed = 0,
                records = noOpRecords
            };

            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
            using var doc = JsonDocument.Parse(jsonBytes);
            return new ExecuteResult { IsError = false, Changed = new ChangedCounts(0, 0, 0), Value = doc.RootElement.Clone(), DurationMs = 10 };
        };
    }

    [Fact]
    public async Task EndToEnd_Disconnect_ReceiptAbsent_ReconcileRollsBack_AllowsCleanRerun()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var engine = new BimPipelineEngine(fakePipe, intentJournal: journal) { LedgerService = ledger };

        var plan = BuildTestPlan(2);
        var opId = plan.Options.OperationId;

        fakePipe.ShouldDisconnectOnExecute = true;
        var run1Result = await engine.ExecutePlanJournaledAsync(plan, ledger);

        Assert.False(run1Result.Success);
        Assert.True(run1Result.IsOutcomeUnknown);

        fakePipe.ShouldDisconnectOnExecute = false;
        fakePipe.IsConnected = true;

        var reconcileService = new ReconcileService(journal, fakePipe, ledger);
        var reconcileResults = await reconcileService.ReconcileAllUnknownIntentsAsync();

        Assert.Single(reconcileResults);
        Assert.Equal(ReconcileStatus.StillUnknown, reconcileResults[0].Status);
        Assert.Empty(ledger.Mappings);

        var intentAfterReconcile = await journal.GetIntentAsync(opId);
        Assert.NotNull(intentAfterReconcile);
        Assert.Equal(IntentStatus.OutcomeUnknown, intentAfterReconcile.Status);

        fakePipe.CustomExecuteHandler = null;
        var run2Result = await engine.ExecutePlanJournaledAsync(plan, ledger);
        Assert.True(run2Result.Success);
    }

    [Fact]
    public async Task DryRun_DoesNotRecordInJournal_AndRollsBackCleanly()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var engine = new BimPipelineEngine(fakePipe, intentJournal: journal);

        var plan = BuildTestPlan(3) with
        {
            Options = new ExecutionOptions { DryRun = true }
        };

        var result = await engine.ExecutePlanJournaledAsync(plan);

        Assert.True(result.Success);
        Assert.True(result.RolledBack);

        var incomplete = await journal.GetIncompleteIntentsAsync();
        Assert.Empty(incomplete);
    }

    private static BimExecutionPlan BuildTestPlan(int itemCount)
    {
        var items = Enumerable.Range(1, itemCount).Select(i => new ElementPlanItem
        {
            SourceKey = CompositeSourceKey.Create("TestDoc", CoordinationCategory.Column, $"C{i}"),
            Category = CoordinationCategory.Column,
            Mark = $"C{i}"
        }).ToList();

        return new BimExecutionPlan
        {
            SourceModelName = "TestModel",
            PlanHash = $"hash_plan_{Guid.NewGuid():N}",
            TargetDoc = new DocumentIdentitySnapshot { Title = "TestDoc.rvt" },
            Options = new ExecutionOptions
            {
                OperationId = Guid.NewGuid(),
                DryRun = false,
                TransactionLabel = "Test Plan Execution"
            },
            Items = items
        };
    }
}
