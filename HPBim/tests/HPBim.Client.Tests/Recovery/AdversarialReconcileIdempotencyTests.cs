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

public sealed class AdversarialReconcileIdempotencyTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "hpbim_adv_test_" + Guid.NewGuid().ToString("N"));
    private readonly string _journalPath;

    public AdversarialReconcileIdempotencyTests()
    {
        Directory.CreateDirectory(_tempDir);
        _journalPath = Path.Combine(_tempDir, "intent_journal.jsonl");
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch (IOException) { /* Transient lock cleanup tolerance */ }
    }

    [Fact]
    public async Task ScenarioA_Committed_ReceiptInRevit_UpdatesLedgerAndMarksCommitted()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();
        string planHash = "plan-scenario-a";

        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = opId, PlanHash = planHash, Status = IntentStatus.OutcomeUnknown, ItemCount = 3 });
        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId, PlanHash = planHash, CommittedAtUtc = DateTime.UtcNow.ToString("o"),
            ElementMap = new Dictionary<string, string> { ["Doc#Column:C1"] = "uid-c1", ["Doc#Column:C2"] = "uid-c2", ["Doc#Beam:B1"] = "uid-b1" }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.Committed, result.Status);
        Assert.Equal(3, result.MappedElementCount);
        Assert.Equal(3, ledger.Mappings.Count);
        var keyC1 = CompositeSourceKey.Create("Doc", CoordinationCategory.Column, "C1");
        Assert.True(ledger.TryGetTarget(keyC1, out var uidC1));
        Assert.Equal("uid-c1", uidC1);
        var updatedIntent = await journal.GetIntentAsync(opId);
        Assert.NotNull(updatedIntent);
        Assert.Equal(IntentStatus.Committed, updatedIntent.Status);
    }

    [Fact]
    public async Task ScenarioB_RolledBack_ReceiptAbsentInRevit_MarksFailedAndLedgerUntouched()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = opId, PlanHash = "plan-scenario-b", Status = IntentStatus.OutcomeUnknown, ItemCount = 2 });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Equal(0, result.MappedElementCount);
        Assert.Empty(ledger.Mappings);
        var updatedIntent = await journal.GetIntentAsync(opId);
        Assert.NotNull(updatedIntent);
        Assert.Equal(IntentStatus.OutcomeUnknown, updatedIntent.Status);
        Assert.Contains("Receipt absent", updatedIntent.ErrorReason);
    }

    [Fact]
    public async Task ScenarioC_StillUnknown_PipeSeveredDuringReconciliation_IntentUnchanged()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient { ShouldDisconnectOnExecute = true };
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = opId, PlanHash = "plan-scenario-c", Status = IntentStatus.OutcomeUnknown, ItemCount = 2 });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Empty(ledger.Mappings);
        var unchangedIntent = await journal.GetIntentAsync(opId);
        Assert.NotNull(unchangedIntent);
        Assert.Equal(IntentStatus.OutcomeUnknown, unchangedIntent.Status);
    }

    [Fact]
    public async Task ScenarioD_MixedBatch_ReconcilesMultipleIntentsAccurately()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opCommitted = Guid.NewGuid();
        var opRolledBack = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = opCommitted, PlanHash = "p-comm", Status = IntentStatus.OutcomeUnknown, ItemCount = 1 });
        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = opRolledBack, PlanHash = "p-roll", Status = IntentStatus.OutcomeUnknown });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opCommitted, PlanHash = "p-comm",
            ElementMap = new Dictionary<string, string> { ["Doc#Column:C1"] = "uid-100" }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var results = await service.ReconcileAllUnknownIntentsAsync();

        Assert.Equal(2, results.Count);
        Assert.Equal(ReconcileStatus.Committed, results[0].Status);
        Assert.Equal(ReconcileStatus.StillUnknown, results[1].Status);
        Assert.Single(ledger.Mappings);
    }

    [Fact]
    public async Task ScenarioE_CorruptElementMapInReceipt_GracefullyRecoversWithoutCrashing()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = opId, PlanHash = "p-corrupt", Status = IntentStatus.OutcomeUnknown, ItemCount = 3 });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId, PlanHash = "p-corrupt",
            ElementMap = new Dictionary<string, string>
            {
                ["Doc#Column:C1"] = "uid-valid-1",
                ["malformed-invalid-source-key"] = "uid-ignored",
                ["Doc#Beam:B1"] = "uid-valid-2"
            }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        // Corrupt or unparseable source key in receipt must preserve OutcomeUnknown and refuse Committed
        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Empty(ledger.Mappings);
    }

    [Fact]
    public async Task EndToEnd_ZeroDuplication_SeveredConnection_Reconciled_RerunPlanProducesZeroDuplicates()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var engine = new BimPipelineEngine(fakePipe, intentJournal: journal) { LedgerService = ledger };

        var plan = BuildTestPlan(6);
        fakePipe.ShouldTimeoutOnExecute = true;

        var run1 = await engine.ExecutePlanJournaledAsync(plan, ledger);
        Assert.False(run1.Success);
        Assert.True(run1.IsOutcomeUnknown);

        fakePipe.ShouldTimeoutOnExecute = false;
        var elementMap = plan.Items.ToDictionary(it => it.SourceKey.ToString(), it => $"revit-uid-{it.Mark}");
        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = plan.Options.OperationId, PlanHash = plan.PlanHash,
            TargetDocId = plan.TargetDoc?.Title ?? string.Empty,
            CommittedAtUtc = DateTime.UtcNow.ToString("o"), ElementMap = elementMap
        });

        var reconcileService = new ReconcileService(journal, fakePipe, ledger);
        var recRes = await reconcileService.ReconcileAllUnknownIntentsAsync();
        Assert.Single(recRes);
        Assert.Equal(ReconcileStatus.Committed, recRes[0].Status);

        ConfigureFakePipeSimulator(fakePipe, elementMap);

        var run2 = await engine.ExecutePlanJournaledAsync(plan, ledger);
        Assert.True(run2.Success);
        Assert.Equal(0, run2.CreatedCount);
        Assert.Equal(6, run2.NoOpCount);
        Assert.Equal(0, run2.FailedCount);

        var run3 = await engine.ExecutePlanJournaledAsync(plan, ledger);
        Assert.True(run3.Success);
        Assert.Equal(0, run3.CreatedCount);
        Assert.Equal(6, run3.NoOpCount);
    }

    private static void ConfigureFakePipeSimulator(FakeRevitPipeClient fakePipe, Dictionary<string, string> knownMap)
    {
        fakePipe.CustomExecuteHandler = req =>
        {
            var itemsArray = req.Args?.GetProperty("items").EnumerateArray().ToList() ?? new List<JsonElement>();
            var recs = itemsArray.Select(it =>
            {
                string mk = it.GetProperty("mark").GetString() ?? string.Empty;
                string cat = it.GetProperty("category").GetString() ?? "Column";
                string targetUid = it.TryGetProperty("targetUniqueId", out var tup) ? tup.GetString() ?? string.Empty : string.Empty;
                string sk = $"[Doc]#[Column]:{mk}";
                bool isNoOp = !string.IsNullOrEmpty(targetUid) && knownMap.ContainsKey(sk) && knownMap[sk] == targetUid;

                return new Dictionary<string, object>
                {
                    ["sourceKey"] = sk, ["category"] = cat, ["mark"] = mk,
                    ["status"] = isNoOp ? "NoOp" : "Created",
                    ["targetUniqueId"] = isNoOp ? targetUid : $"uid-new-{mk}"
                };
            }).ToList();

            var payload = new
            {
                planId = Guid.NewGuid().ToString("D"), total = recs.Count,
                created = recs.Count(r => (string)r["status"] == "Created"), updated = 0,
                noOp = recs.Count(r => (string)r["status"] == "NoOp"), skipped = 0, failed = 0,
                records = recs
            };

            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
            using var doc = JsonDocument.Parse(jsonBytes);
            return new ExecuteResult { IsError = false, Value = doc.RootElement.Clone(), DurationMs = 8 };
        };
    }

    private static BimExecutionPlan BuildTestPlan(int count)
    {
        var items = Enumerable.Range(1, count).Select(i => new ElementPlanItem
        {
            SourceKey = CompositeSourceKey.Create("Doc", CoordinationCategory.Column, $"C{i}"),
            Category = CoordinationCategory.Column, Mark = $"C{i}"
        }).ToList();

        return new BimExecutionPlan
        {
            SourceModelName = "AdversarialModel",
            PlanHash = $"plan_hash_{Guid.NewGuid():N}",
            TargetDoc = new DocumentIdentitySnapshot { Title = "Doc.rvt" },
            Options = new ExecutionOptions { OperationId = Guid.NewGuid(), DryRun = false },
            Items = items
        };
    }
}
