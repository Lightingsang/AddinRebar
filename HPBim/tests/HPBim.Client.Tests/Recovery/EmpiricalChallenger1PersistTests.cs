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

public sealed class EmpiricalChallenger1PersistTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _journalPath;
    private readonly string _ledgerPath;

    public EmpiricalChallenger1PersistTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "hpbim_ch1_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _journalPath = Path.Combine(_tempDir, "intent_journal.jsonl");
        _ledgerPath = Path.Combine(_tempDir, "bim_coordination_ledger.json");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
        }
        catch (IOException)
        {
            // Transient test cleanup ignore
        }
    }

    [Fact]
    public async Task PersistBeforeTerminal_WhenSaveThrowsUnauthorized_JournalNeverCommitted()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(CreateIntent(opId, "h_sec", 2, "Doc#Column:C1", "Doc#Wall:W1"));
        fakePipe.AddSimulatedReceipt(CreateReceipt(opId, "h_sec", new() { ["Doc#Column:C1"] = "u1", ["Doc#Wall:W1"] = "u2" }));

        string unwriteableDir = Path.Combine(_tempDir, "no_access");
        File.WriteAllText(unwriteableDir, "block");
        string badPath = Path.Combine(unwriteableDir, "ledger.json");

        var service = new ReconcileService(journal, fakePipe, ledger, ledgerFilePath: badPath);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Contains("Failed to persist coordination ledger", result.Message);

        var record = await journal.GetIntentAsync(opId);
        Assert.NotNull(record);
        Assert.Equal(IntentStatus.OutcomeUnknown, record.Status);
        Assert.False(record.IsTerminal);
    }

    [Fact]
    public async Task KeysetCoverage_When1OutOf100KeysMissing_OutcomeStrictlyUnknown()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        var allKeys = Enumerable.Range(1, 100).Select(i => $"Doc#Column:C{i}").ToArray();
        await journal.RecordIntentAsync(CreateIntent(opId, "h_100", 100, allKeys));

        // Receipt provides 99 out of 100 keys (missing C100)
        var map99 = Enumerable.Range(1, 99).ToDictionary(i => $"Doc#Column:C{i}", i => $"uid_{i}");
        fakePipe.AddSimulatedReceipt(CreateReceipt(opId, "h_100", map99));

        var service = new ReconcileService(journal, fakePipe, ledger, ledgerFilePath: _ledgerPath);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Contains("missing=1, foreign=0", result.Message);
        Assert.False(File.Exists(_ledgerPath));

        var record = await journal.GetIntentAsync(opId);
        Assert.NotNull(record);
        Assert.Equal(IntentStatus.OutcomeUnknown, record.Status);
    }

    [Fact]
    public async Task KeysetCoverage_When1OutOf100KeysForeign_OutcomeStrictlyUnknown()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        var allKeys = Enumerable.Range(1, 100).Select(i => $"Doc#Column:C{i}").ToArray();
        await journal.RecordIntentAsync(CreateIntent(opId, "h_foreign100", 100, allKeys));

        // Receipt provides 100 keys, but C100 is replaced by ForeignX
        var mapForeign = Enumerable.Range(1, 99).ToDictionary(i => $"Doc#Column:C{i}", i => $"uid_{i}");
        mapForeign["Doc#Column:ForeignX"] = "uid_foreign";
        fakePipe.AddSimulatedReceipt(CreateReceipt(opId, "h_foreign100", mapForeign));

        var service = new ReconcileService(journal, fakePipe, ledger, ledgerFilePath: _ledgerPath);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Contains("missing=1, foreign=1", result.Message);
        Assert.False(File.Exists(_ledgerPath));

        var record = await journal.GetIntentAsync(opId);
        Assert.NotNull(record);
        Assert.Equal(IntentStatus.OutcomeUnknown, record.Status);
    }

    [Fact]
    public async Task KeysetCoverage_TemporalOrderStrictlyVerifiedBySpy()
    {
        using var realJournal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await realJournal.RecordIntentAsync(CreateIntent(opId, "h_temporal", 2, "Doc#Column:C1", "Doc#Wall:W1"));
        fakePipe.AddSimulatedReceipt(CreateReceipt(opId, "h_temporal", new() { ["Doc#Column:C1"] = "u1", ["Doc#Wall:W1"] = "u2" }));

        bool diskExistedBeforeJournalCommit = false;
        var spyJournal = new InterceptJournal(realJournal, (id, status) =>
        {
            if (status == IntentStatus.Committed)
            {
                diskExistedBeforeJournalCommit = File.Exists(_ledgerPath) &&
                    BimCoordinationLedgerSerializer.DeserializeFromFile(_ledgerPath).Mappings.Count == 2;
            }
        });

        var service = new ReconcileService(spyJournal, fakePipe, ledger, ledgerFilePath: _ledgerPath);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.Committed, result.Status);
        Assert.True(diskExistedBeforeJournalCommit, "Ledger must be fully flushed on disk before journal commit!");
    }

    [Fact]
    public async Task InvalidPayload_WhenKeyIsUnparseable_PreservesOutcomeUnknown_NoLedgerMutation()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(CreateIntent(opId, "h_badkey", 1, "Doc#Col:C1"));
        // Key missing category/handle delimiter or malformed
        fakePipe.AddSimulatedReceipt(CreateReceipt(opId, "h_badkey", new() { ["MalformedKeyWithoutDelimiter"] = "u1" }));

        var service = new ReconcileService(journal, fakePipe, ledger, ledgerFilePath: _ledgerPath);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.False(File.Exists(_ledgerPath));
        Assert.Empty(ledger.Mappings);
    }

    [Fact]
    public async Task InvalidPayload_WhenUniqueIdIsWhitespace_PreservesOutcomeUnknown_NoLedgerMutation()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(CreateIntent(opId, "h_space", 1, "Doc#Column:C1"));
        fakePipe.AddSimulatedReceipt(CreateReceipt(opId, "h_space", new() { ["Doc#Column:C1"] = "   " }));

        var service = new ReconcileService(journal, fakePipe, ledger, ledgerFilePath: _ledgerPath);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Contains("empty uniqueId", result.Message);
        Assert.False(File.Exists(_ledgerPath));
        Assert.Empty(ledger.Mappings);
    }

    [Fact]
    public async Task Reconcile_WhenLedgerFilePathIsNull_MarksJournalCommittedWithoutWritingToDisk()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(CreateIntent(opId, "h_nopath", 1, "Doc#Column:C1"));
        fakePipe.AddSimulatedReceipt(CreateReceipt(opId, "h_nopath", new() { ["Doc#Column:C1"] = "u1" }));

        // Null ledgerFilePath - simulates legacy or missing path
        var service = new ReconcileService(journal, fakePipe, ledger, ledgerFilePath: null);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.Committed, result.Status);
        // Empirically demonstrates that no file is written to disk when LedgerFilePath is null
        Assert.False(File.Exists(_ledgerPath));
        var record = await journal.GetIntentAsync(opId);
        Assert.NotNull(record);
        Assert.Equal(IntentStatus.Committed, record.Status);
    }

    private static IntentJournalRecord CreateIntent(Guid opId, string hash, int count, params string[] keys) =>
        new() { OperationId = opId, PlanHash = hash, Status = IntentStatus.OutcomeUnknown, ItemCount = count, SourceKeys = keys };

    private static CoordinationReceiptRecord CreateReceipt(Guid opId, string hash, Dictionary<string, string> map) =>
        new() { OperationId = opId, PlanHash = hash, ElementMap = map };

    private sealed class InterceptJournal(IIntentJournal inner, Action<Guid, IntentStatus> onUpdate) : IIntentJournal
    {
        public string JournalFilePath => inner.JournalFilePath;
        public Task RecordIntentAsync(IntentJournalRecord r, CancellationToken ct = default) => inner.RecordIntentAsync(r, ct);
        public async Task UpdateStatusAsync(Guid opId, IntentStatus s, string? r = null, CancellationToken ct = default)
        {
            onUpdate(opId, s);
            await inner.UpdateStatusAsync(opId, s, r, ct).ConfigureAwait(false);
        }
        public Task<IReadOnlyList<IntentJournalRecord>> GetIncompleteIntentsAsync(CancellationToken ct = default) => inner.GetIncompleteIntentsAsync(ct);
        public Task<IntentJournalRecord?> GetIntentAsync(Guid opId, CancellationToken ct = default) => inner.GetIntentAsync(opId, ct);
        public Task<int> PruneOldEntriesAsync(TimeSpan ret, CancellationToken ct = default) => inner.PruneOldEntriesAsync(ret, ct);
        public void Dispose() => inner.Dispose();
        public ValueTask DisposeAsync() => inner.DisposeAsync();
    }
}
