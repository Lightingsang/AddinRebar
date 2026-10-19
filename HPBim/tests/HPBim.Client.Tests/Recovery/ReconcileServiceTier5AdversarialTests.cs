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

public sealed class ReconcileServiceTier5AdversarialTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _journalPath;

    public ReconcileServiceTier5AdversarialTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "hpbim_t5_rec_" + Guid.NewGuid().ToString("N"));
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
            // Transient lock cleanup tolerance
        }
    }

    [Fact]
    public async Task ReconcileAllUnknownIntents_InterleavedStatuses_ReconcilesEachAccurately()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();

        var opCommitted = Guid.NewGuid();
        var opRolledBack = Guid.NewGuid();
        var opMixedKeys = Guid.NewGuid();
        await RecordInitialIntent(journal, opCommitted, "p-com", itemCount: 1);
        await RecordInitialIntent(journal, opRolledBack, "p-rol", itemCount: 1);
        await RecordInitialIntent(journal, opMixedKeys, "p-mix", itemCount: 2);

        AddReceipt(fakePipe, opCommitted, "p-com", CreateReceiptElementMap(("Doc#Column:C1", "uid-c1")));
        AddReceipt(fakePipe, opMixedKeys, "p-mix", CreateReceiptElementMap(
            ("Doc#Beam:B1", "uid-b1"), ("invalid-key-no-category", "uid-ignored")));

        var service = new ReconcileService(journal, fakePipe, ledger);
        var results = await service.ReconcileAllUnknownIntentsAsync();

        Assert.Equal(3, results.Count);
        Assert.Equal(ReconcileStatus.Committed, results[0].Status);
        Assert.Equal(ReconcileStatus.StillUnknown, results[1].Status);
        // Invalid composite source key in receipt must preserve OutcomeUnknown and refuse Committed
        Assert.Equal(ReconcileStatus.StillUnknown, results[2].Status);

        Assert.Single(ledger.Mappings);
        AssertLedgerTarget(ledger, CoordinationCategory.Column, "C1", "uid-c1");

        await AssertJournalStatus(journal, opCommitted, IntentStatus.Committed);
        await AssertJournalStatus(journal, opRolledBack, IntentStatus.OutcomeUnknown);
        await AssertJournalStatus(journal, opMixedKeys, IntentStatus.OutcomeUnknown);
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
    public async Task ReconcileIntent_MissingInJournal_ReturnsStillUnknownWithExplanation()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var missingOpId = Guid.NewGuid();

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(missingOpId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Equal(missingOpId, result.OperationId);
        Assert.Contains("Intent not found in journal", result.Message);
    }

    [Fact]
    public async Task ReconcileIntent_NetworkSeveredDuringQuery_ReturnsStillUnknownAndPreservesIntentState()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient { ShouldDisconnectOnExecute = true };
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = opId, Status = IntentStatus.OutcomeUnknown });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Contains("Pipe communication error", result.Message);

        var record = await journal.GetIntentAsync(opId);
        Assert.NotNull(record);
        Assert.Equal(IntentStatus.OutcomeUnknown, record.Status);
    }

    [Fact]
    public async Task ReconcileIntent_PipeTimeoutDuringQuery_ReturnsStillUnknownAndPreservesIntentState()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient { ShouldTimeoutOnExecute = true };
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = opId, Status = IntentStatus.OutcomeUnknown });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Contains("Pipe communication error", result.Message);

        var record = await journal.GetIntentAsync(opId);
        Assert.NotNull(record);
        Assert.Equal(IntentStatus.OutcomeUnknown, record.Status);
    }

    [Fact]
    public async Task AutoConnect_WhenPipeDisconnected_ConnectsAutomatically()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient { IsConnected = false };
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = opId, Status = IntentStatus.OutcomeUnknown });

        var service = new ReconcileService(journal, fakePipe, ledger);
        await service.ReconcileIntentAsync(opId);

        Assert.True(fakePipe.IsConnected);
    }

    private static async Task RecordInitialIntent(IntentJournal journal, Guid opId, string planHash, int itemCount = 0)
    {
        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = planHash,
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = itemCount
        });
    }

    private static Dictionary<string, string> CreateReceiptElementMap(params (string Key, string Value)[] entries)
    {
        var map = new Dictionary<string, string>(entries.Length);
        foreach (var (k, v) in entries)
        {
            map[k] = v;
        }
        return map;
    }

    private static void AddReceipt(FakeRevitPipeClient pipe, Guid opId, string planHash, Dictionary<string, string> map)
    {
        pipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = planHash,
            ElementMap = map
        });
    }

    private static void AssertLedgerTarget(BimCoordinationLedgerService ledger, CoordinationCategory category, string handle, string expectedUid)
    {
        Assert.True(ledger.TryGetTarget(CompositeSourceKey.Create("Doc", category, handle), out var uid));
        Assert.Equal(expectedUid, uid);
    }

    private static async Task AssertJournalStatus(IntentJournal journal, Guid opId, IntentStatus expectedStatus)
    {
        var record = await journal.GetIntentAsync(opId);
        Assert.NotNull(record);
        Assert.Equal(expectedStatus, record.Status);
    }
}
