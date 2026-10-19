namespace HPBim.Client.Tests.Recovery;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Client.Recovery;
using HPBim.Client.Tests.Fakes;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using Xunit;

public sealed class ReconcilePersistBeforeTerminalTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _journalPath;
    private readonly string _ledgerPath;

    public ReconcilePersistBeforeTerminalTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "hpbim_pbt_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _journalPath = Path.Combine(_tempDir, "intent_journal.jsonl");
        _ledgerPath = Path.Combine(_tempDir, "bim_coordination_ledger.json");
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch (IOException) { /* Transient test teardown lock cleanup */ }
    }

    [Fact]
    public async Task Reconcile_WhenLedgerDiskSaveFails_PreservesOutcomeUnknown_JournalNotMarkedCommitted()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(CreateIntent(opId, "h1", 2, "Doc#Column:C1", "Doc#Wall:W1"));
        fakePipe.AddSimulatedReceipt(CreateReceipt(opId, "h1", new() { ["Doc#Column:C1"] = "u1", ["Doc#Wall:W1"] = "u2" }));

        string blockingFile = Path.Combine(_tempDir, "unwritable_dir");
        await File.WriteAllTextAsync(blockingFile, "not a dir");
        string unwritablePath = Path.Combine(blockingFile, "ledger.json");

        var service = new ReconcileService(journal, fakePipe, ledger, ledgerFilePath: unwritablePath);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Contains("Failed to persist coordination ledger to disk", result.Message);

        var record = await journal.GetIntentAsync(opId);
        Assert.NotNull(record);
        Assert.Equal(IntentStatus.OutcomeUnknown, record.Status);
        Assert.Contains("Failed to persist coordination ledger", record.ErrorReason);
    }

    [Fact]
    public async Task Reconcile_SuccessfulReconcile_SavesLedgerToDiskBeforeMarkingJournalCommitted()
    {
        using var realJournal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await realJournal.RecordIntentAsync(CreateIntent(opId, "h_order", 1, "Doc#Column:C1"));
        fakePipe.AddSimulatedReceipt(CreateReceipt(opId, "h_order", new() { ["Doc#Column:C1"] = "u1" }));

        var spyJournal = new JournalCommitOrderSpy(realJournal, _ledgerPath);
        var service = new ReconcileService(spyJournal, fakePipe, ledger, ledgerFilePath: _ledgerPath);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.Committed, result.Status);
        Assert.True(spyJournal.LedgerExistedWithMappingsBeforeCommit);

        var record = await realJournal.GetIntentAsync(opId);
        Assert.NotNull(record);
        Assert.Equal(IntentStatus.Committed, record.Status);

        var loaded = BimCoordinationLedgerSerializer.DeserializeFromFile(_ledgerPath);
        Assert.Single(loaded.Mappings);
        Assert.Equal("u1", loaded.Mappings.Values.First().TargetUniqueId);
    }

    [Fact]
    public async Task Reconcile_ExactKeysetCoverage_MissingSingleSourceKey_PreservesOutcomeUnknown()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(CreateIntent(opId, "h_miss", 2, "Doc#Column:C1", "Doc#Column:C2"));
        fakePipe.AddSimulatedReceipt(CreateReceipt(opId, "h_miss", new() { ["Doc#Column:C1"] = "u1" }));

        var service = new ReconcileService(journal, fakePipe, ledger, ledgerFilePath: _ledgerPath);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Contains("missing=1", result.Message);
        Assert.False(File.Exists(_ledgerPath));

        var record = await journal.GetIntentAsync(opId);
        Assert.NotNull(record);
        Assert.Equal(IntentStatus.OutcomeUnknown, record.Status);
    }

    [Fact]
    public async Task Reconcile_ExactKeysetCoverage_ForeignSourceKeyPresent_PreservesOutcomeUnknown()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(CreateIntent(opId, "h_for", 2, "Doc#Column:C1", "Doc#Column:C2"));
        fakePipe.AddSimulatedReceipt(CreateReceipt(opId, "h_for", new() { ["Doc#Column:C1"] = "u1", ["Doc#Wall:Foreign"] = "u2" }));

        var service = new ReconcileService(journal, fakePipe, ledger, ledgerFilePath: _ledgerPath);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Contains("foreign=1", result.Message);
        Assert.False(File.Exists(_ledgerPath));

        var record = await journal.GetIntentAsync(opId);
        Assert.NotNull(record);
        Assert.Equal(IntentStatus.OutcomeUnknown, record.Status);
    }

    [Fact]
    public async Task Reconcile_ExactKeysetCoverage_CompleteKeysetMatch_SuccessfullyPersistsAndCommits()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(CreateIntent(opId, "h_match", 3, "Doc#Column:C1", "Doc#Wall:W1", "Doc#Slab:S1"));
        fakePipe.AddSimulatedReceipt(CreateReceipt(opId, "h_match", new() { ["Doc#Column:C1"] = "u1", ["Doc#Wall:W1"] = "u2", ["Doc#Slab:S1"] = "u3" }));

        var service = new ReconcileService(journal, fakePipe, ledger, ledgerFilePath: _ledgerPath);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.Committed, result.Status);
        Assert.Equal(3, result.MappedElementCount);
        Assert.True(File.Exists(_ledgerPath));

        var record = await journal.GetIntentAsync(opId);
        Assert.NotNull(record);
        Assert.Equal(IntentStatus.Committed, record.Status);
    }

    [Fact]
    public async Task Reconcile_SimulateCrashThenRetry_RecoversAndPersistsSuccessfully()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(CreateIntent(opId, "h_retry", 1, "Doc#Column:C1"));
        fakePipe.AddSimulatedReceipt(CreateReceipt(opId, "h_retry", new() { ["Doc#Column:C1"] = "u1" }));

        string blockingFile = Path.Combine(_tempDir, "block_retry");
        await File.WriteAllTextAsync(blockingFile, "block");
        string badPath = Path.Combine(blockingFile, "ledger.json");

        var failingService = new ReconcileService(journal, fakePipe, ledger, ledgerFilePath: badPath);
        var failResult = await failingService.ReconcileIntentAsync(opId);
        Assert.Equal(ReconcileStatus.StillUnknown, failResult.Status);

        var midRecord = await journal.GetIntentAsync(opId);
        Assert.NotNull(midRecord);
        Assert.Equal(IntentStatus.OutcomeUnknown, midRecord.Status);

        var recoveryService = new ReconcileService(journal, fakePipe, ledger, ledgerFilePath: _ledgerPath);
        var successResult = await recoveryService.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.Committed, successResult.Status);
        Assert.True(File.Exists(_ledgerPath));
        var finalRecord = await journal.GetIntentAsync(opId);
        Assert.NotNull(finalRecord);
        Assert.Equal(IntentStatus.Committed, finalRecord.Status);
    }

    private static IntentJournalRecord CreateIntent(Guid opId, string hash, int count, params string[] keys) =>
        new() { OperationId = opId, PlanHash = hash, Status = IntentStatus.OutcomeUnknown, ItemCount = count, SourceKeys = keys };

    private static CoordinationReceiptRecord CreateReceipt(Guid opId, string hash, Dictionary<string, string> map) =>
        new() { OperationId = opId, PlanHash = hash, ElementMap = map };

    private sealed class JournalCommitOrderSpy(IIntentJournal inner, string ledgerFilePath) : IIntentJournal
    {
        public bool LedgerExistedWithMappingsBeforeCommit { get; private set; }

        public string JournalFilePath => inner.JournalFilePath;

        public Task RecordIntentAsync(IntentJournalRecord record, CancellationToken ct = default) =>
            inner.RecordIntentAsync(record, ct);

        public async Task UpdateStatusAsync(Guid opId, IntentStatus status, string? reason = null, CancellationToken ct = default)
        {
            if (status == IntentStatus.Committed && File.Exists(ledgerFilePath))
            {
                var ledger = BimCoordinationLedgerSerializer.DeserializeFromFile(ledgerFilePath);
                if (ledger.Mappings.Count > 0) LedgerExistedWithMappingsBeforeCommit = true;
            }
            await inner.UpdateStatusAsync(opId, status, reason, ct).ConfigureAwait(false);
        }

        public Task<IReadOnlyList<IntentJournalRecord>> GetIncompleteIntentsAsync(CancellationToken ct = default) =>
            inner.GetIncompleteIntentsAsync(ct);

        public Task<IntentJournalRecord?> GetIntentAsync(Guid opId, CancellationToken ct = default) =>
            inner.GetIntentAsync(opId, ct);

        public Task<int> PruneOldEntriesAsync(TimeSpan retentionPeriod, CancellationToken ct = default) =>
            inner.PruneOldEntriesAsync(retentionPeriod, ct);

        public void Dispose() => inner.Dispose();
        public ValueTask DisposeAsync() => inner.DisposeAsync();
    }
}
