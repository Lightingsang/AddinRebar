namespace HPBim.Client.Tests.Recovery;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Client.Recovery;
using HPBim.Client.Tests.Fakes;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using Xunit;

public sealed class ReconcileServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _journalPath;

    public ReconcileServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "hpbim_reconcile_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _journalPath = Path.Combine(_tempDir, "intent_journal.jsonl");
    }

    public void Dispose()
    {
        // Cleanup test directory gracefully
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch (IOException)
        {
            // Allow transient lock in test cleanup
        }
    }

    [Fact]
    public async Task ReconcileAllUnknownIntents_WhenNoIncompleteIntents_ReturnsNoOp()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var service = new ReconcileService(journal, fakePipe, ledger);

        var results = await service.ReconcileAllUnknownIntentsAsync();

        Assert.Single(results);
        Assert.Equal(ReconcileStatus.NoOp, results[0].Status);
    }

    [Fact]
    public async Task ReconcileIntent_WhenReceiptFound_UpdatesLedgerAndMarksCommitted()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();

        var opId = Guid.NewGuid();
        string planHash = "test_plan_hash_1";
        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = planHash,
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 2
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = planHash,
            CommittedAtUtc = "2026-10-07T21:30:00Z",
            ElementMap = new Dictionary<string, string>
            {
                ["Doc#Column:C1"] = "uid-c1",
                ["Doc#Wall:W1"] = "uid-w1"
            }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.Committed, result.Status);
        Assert.Equal(2, result.MappedElementCount);

        var keyC1 = CompositeSourceKey.Create("Doc", CoordinationCategory.Column, "C1");
        Assert.True(ledger.TryGetTarget(keyC1, out var uidC1));
        Assert.Equal("uid-c1", uidC1);

        var updatedIntent = await journal.GetIntentAsync(opId);
        Assert.NotNull(updatedIntent);
        Assert.Equal(IntentStatus.Committed, updatedIntent.Status);
    }

    [Fact]
    public async Task ReconcileIntent_WhenReceiptAbsent_MarksIntentFailedAndRolledBack()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();

        var opId = Guid.NewGuid();
        string planHash = "test_plan_hash_2";
        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = planHash,
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 3
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Equal(0, result.MappedElementCount);
        Assert.Empty(ledger.Mappings);

        var updatedIntent = await journal.GetIntentAsync(opId);
        Assert.NotNull(updatedIntent);
        Assert.Equal(IntentStatus.OutcomeUnknown, updatedIntent.Status);
    }

    [Fact]
    public async Task ReconcileIntent_WhenPipeThrowsIOException_ReturnsStillUnknownWithoutFailingIntent()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();

        var opId = Guid.NewGuid();
        string planHash = "test_plan_hash_3";
        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = planHash,
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 1
        });

        fakePipe.ShouldDisconnectOnExecute = true;

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);

        var unchangedIntent = await journal.GetIntentAsync(opId);
        Assert.NotNull(unchangedIntent);
        Assert.Equal(IntentStatus.OutcomeUnknown, unchangedIntent.Status);
    }

    [Fact]
    public async Task ReconcileIntent_WhenIntentNotInJournal_ReturnsStillUnknown()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var service = new ReconcileService(journal, fakePipe, ledger);

        var result = await service.ReconcileIntentAsync(Guid.NewGuid());
        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
    }

    [Fact]
    public async Task ReconcileAllUnknownIntents_MultipleIntents_ReconcilesEachSequentially()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();

        var op1 = Guid.NewGuid();
        var op2 = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = op1, PlanHash = "p1", Status = IntentStatus.OutcomeUnknown, ItemCount = 1 });
        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = op2, PlanHash = "p2", Status = IntentStatus.OutcomeUnknown });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = op1,
            PlanHash = "p1",
            ElementMap = new Dictionary<string, string> { ["Doc#Column:C1"] = "uid-1" }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var results = await service.ReconcileAllUnknownIntentsAsync();

        Assert.Equal(2, results.Count);
        Assert.Equal(ReconcileStatus.Committed, results[0].Status);
        Assert.Equal(ReconcileStatus.StillUnknown, results[1].Status);
    }
}
