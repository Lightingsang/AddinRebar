namespace HPBim.Client.Tests.Recovery;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Client.Recovery;
using HPBim.Client.Tests.Fakes;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using Xunit;

public sealed class EmpiricalChallenger2ReconcileBoundaryTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _journalPath;
    private readonly string _ledgerPath;

    public EmpiricalChallenger2ReconcileBoundaryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "hpbim_ch2_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _journalPath = Path.Combine(_tempDir, "intent_journal.jsonl");
        _ledgerPath = Path.Combine(_tempDir, "bim_coordination_ledger.json");
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch (IOException) { /* Cleanup lock ignore */ }
    }

    [Fact]
    public async Task Reconcile_ReceiptWithExtraForeignKeys_PreservesOutcomeUnknown_NoLedgerMutation_NoCommit()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(CreateIntent(opId, "h_extra", 2, "Doc#Column:C1", "Doc#Wall:W1"));
        var mapWithExtra = new Dictionary<string, string>
        {
            ["Doc#Column:C1"] = "uid-c1",
            ["Doc#Wall:W1"] = "uid-w1",
            ["Doc#Column:ForeignExtra"] = "uid-extra"
        };
        fakePipe.AddSimulatedReceipt(CreateReceipt(opId, "h_extra", mapWithExtra));

        var service = new ReconcileService(journal, fakePipe, ledger, ledgerFilePath: _ledgerPath);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Contains("foreign=1", result.Message);
        Assert.Contains("expected 2 keys, but receipt contained 3 entries", result.Message);
        Assert.False(File.Exists(_ledgerPath));
        Assert.Empty(ledger.Mappings);

        var record = await journal.GetIntentAsync(opId);
        Assert.NotNull(record);
        Assert.Equal(IntentStatus.OutcomeUnknown, record.Status);
        Assert.Contains("foreign=1", record.ErrorReason);
    }

    [Fact]
    public async Task Reconcile_ReceiptWithOnlyForeignKeys_PreservesOutcomeUnknown_NoLedgerMutation()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(CreateIntent(opId, "h_foreign_only", 2, "Doc#Column:C1", "Doc#Wall:W1"));
        var mapForeignOnly = new Dictionary<string, string>
        {
            ["Doc#Column:ForeignA"] = "uid-fa",
            ["Doc#Wall:ForeignB"] = "uid-fb"
        };
        fakePipe.AddSimulatedReceipt(CreateReceipt(opId, "h_foreign_only", mapForeignOnly));

        var service = new ReconcileService(journal, fakePipe, ledger, ledgerFilePath: _ledgerPath);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Contains("missing=2, foreign=2", result.Message);
        Assert.False(File.Exists(_ledgerPath));
        Assert.Empty(ledger.Mappings);

        var record = await journal.GetIntentAsync(opId);
        Assert.NotNull(record);
        Assert.Equal(IntentStatus.OutcomeUnknown, record.Status);
    }

    [Fact]
    public async Task Reconcile_EmptyReceiptMap_WithNonEmptyIntent_PreservesOutcomeUnknown()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(CreateIntent(opId, "h_empty_map", 3, "Doc#Column:C1", "Doc#Wall:W1", "Doc#Slab:S1"));
        fakePipe.AddSimulatedReceipt(CreateReceipt(opId, "h_empty_map", new Dictionary<string, string>()));

        var service = new ReconcileService(journal, fakePipe, ledger, ledgerFilePath: _ledgerPath);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Contains("missing=3, foreign=0", result.Message);
        Assert.False(File.Exists(_ledgerPath));
        Assert.Empty(ledger.Mappings);

        var record = await journal.GetIntentAsync(opId);
        Assert.NotNull(record);
        Assert.Equal(IntentStatus.OutcomeUnknown, record.Status);
    }

    [Fact]
    public async Task Reconcile_EmptyReceiptMap_LegacyIntentWithItemCount_PreservesOutcomeUnknown()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        var legacyIntent = new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "h_legacy_empty",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 2,
            SourceKeys = Array.Empty<string>()
        };
        await journal.RecordIntentAsync(legacyIntent);
        fakePipe.AddSimulatedReceipt(CreateReceipt(opId, "h_legacy_empty", new Dictionary<string, string>()));

        var service = new ReconcileService(journal, fakePipe, ledger, ledgerFilePath: _ledgerPath);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Contains("Receipt completeness mismatch: expected ItemCount=2, but receipt contained 0 entries", result.Message);
        Assert.False(File.Exists(_ledgerPath));
        Assert.Empty(ledger.Mappings);

        var record = await journal.GetIntentAsync(opId);
        Assert.NotNull(record);
        Assert.Equal(IntentStatus.OutcomeUnknown, record.Status);
    }

    [Fact]
    public async Task Reconcile_SequentialMultipleFailures_FollowedBySuccessfulRecovery()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(CreateIntent(opId, "h_seq", 2, "Doc#Column:C1", "Doc#Wall:W1"));

        // Attempt 1: Receipt absent in Extensible Storage
        var service1 = new ReconcileService(journal, fakePipe, ledger, ledgerFilePath: _ledgerPath);
        var res1 = await service1.ReconcileIntentAsync(opId);
        Assert.Equal(ReconcileStatus.StillUnknown, res1.Status);
        Assert.Contains("Receipt absent", res1.Message);

        // Attempt 2: Empty receipt map
        fakePipe.AddSimulatedReceipt(CreateReceipt(opId, "h_seq", new Dictionary<string, string>()));
        var res2 = await service1.ReconcileIntentAsync(opId);
        Assert.Equal(ReconcileStatus.StillUnknown, res2.Status);
        Assert.Contains("missing=2, foreign=0", res2.Message);

        // Attempt 3: Receipt with foreign keys
        fakePipe.AddSimulatedReceipt(CreateReceipt(opId, "h_seq", new() { ["Doc#Column:C1"] = "u1", ["Doc#Column:Foreign"] = "u2" }));
        var res3 = await service1.ReconcileIntentAsync(opId);
        Assert.Equal(ReconcileStatus.StillUnknown, res3.Status);
        Assert.Contains("foreign=1", res3.Message);

        // Attempt 4: Exact keys, but disk write failure
        fakePipe.AddSimulatedReceipt(CreateReceipt(opId, "h_seq", new() { ["Doc#Column:C1"] = "u1", ["Doc#Wall:W1"] = "u2" }));
        string blockingFile = Path.Combine(_tempDir, "block_seq_dir");
        await File.WriteAllTextAsync(blockingFile, "block");
        string badLedgerPath = Path.Combine(blockingFile, "ledger.json");
        var serviceBadDisk = new ReconcileService(journal, fakePipe, ledger, ledgerFilePath: badLedgerPath);
        var res4 = await serviceBadDisk.ReconcileIntentAsync(opId);
        Assert.Equal(ReconcileStatus.StillUnknown, res4.Status);
        Assert.Contains("Failed to persist coordination ledger", res4.Message);

        var midRecord = await journal.GetIntentAsync(opId);
        Assert.NotNull(midRecord);
        Assert.Equal(IntentStatus.OutcomeUnknown, midRecord.Status);

        // Attempt 5: Recovery with valid disk path
        var recoveryService = new ReconcileService(journal, fakePipe, ledger, ledgerFilePath: _ledgerPath);
        var res5 = await recoveryService.ReconcileIntentAsync(opId);
        Assert.Equal(ReconcileStatus.Committed, res5.Status);
        Assert.Equal(2, res5.MappedElementCount);
        Assert.True(File.Exists(_ledgerPath));

        var finalRecord = await journal.GetIntentAsync(opId);
        Assert.NotNull(finalRecord);
        Assert.Equal(IntentStatus.Committed, finalRecord.Status);
        Assert.Null(finalRecord.ErrorReason);

        var persisted = BimCoordinationLedgerSerializer.DeserializeFromFile(_ledgerPath);
        Assert.Equal(2, persisted.Mappings.Count);
    }

    [Fact]
    public async Task Reconcile_SequentialFailures_WithColdProcessRestart_RecoversCleanly()
    {
        var opId = Guid.NewGuid();

        // Process 1: Record intent and attempt reconcile with failing disk
        using (var j1 = new IntentJournal(_journalPath))
        {
            var p1 = new FakeRevitPipeClient();
            var l1 = new BimCoordinationLedgerService();
            await j1.RecordIntentAsync(CreateIntent(opId, "h_cold", 1, "Doc#Column:C1"));
            p1.AddSimulatedReceipt(CreateReceipt(opId, "h_cold", new() { ["Doc#Column:C1"] = "u1" }));

            string blocking = Path.Combine(_tempDir, "block_cold");
            await File.WriteAllTextAsync(blocking, "block");
            var s1 = new ReconcileService(j1, p1, l1, ledgerFilePath: Path.Combine(blocking, "l.json"));
            var r1 = await s1.ReconcileIntentAsync(opId);
            Assert.Equal(ReconcileStatus.StillUnknown, r1.Status);
        }

        // Process 2 (Cold restart): New journal and ledger instances
        using (var j2 = new IntentJournal(_journalPath))
        {
            var p2 = new FakeRevitPipeClient();
            var l2 = new BimCoordinationLedgerService();
            p2.AddSimulatedReceipt(CreateReceipt(opId, "h_cold", new() { ["Doc#Column:C1"] = "u1" }));

            var s2 = new ReconcileService(j2, p2, l2, ledgerFilePath: _ledgerPath);
            var r2 = await s2.ReconcileIntentAsync(opId);

            Assert.Equal(ReconcileStatus.Committed, r2.Status);
            Assert.True(File.Exists(_ledgerPath));

            var rec2 = await j2.GetIntentAsync(opId);
            Assert.NotNull(rec2);
            Assert.Equal(IntentStatus.Committed, rec2.Status);
        }
    }

    private static IntentJournalRecord CreateIntent(Guid opId, string hash, int count, params string[] keys) =>
        new() { OperationId = opId, PlanHash = hash, Status = IntentStatus.OutcomeUnknown, ItemCount = count, SourceKeys = keys };

    private static CoordinationReceiptRecord CreateReceipt(Guid opId, string hash, Dictionary<string, string> map) =>
        new() { OperationId = opId, PlanHash = hash, ElementMap = map };
}
