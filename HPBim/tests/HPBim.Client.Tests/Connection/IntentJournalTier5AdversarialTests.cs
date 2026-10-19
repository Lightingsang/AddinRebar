namespace HPBim.Client.Tests.Connection;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Core.Execution;
using Xunit;

public sealed class IntentJournalTier5AdversarialTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _journalPath;

    public IntentJournalTier5AdversarialTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "hpbim_t5_journal_" + Guid.NewGuid().ToString("N"));
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
            // Transient file lock cleanup tolerance
        }
    }

    [Fact]
    public async Task RapidStatusMutations_SameOperation_PersistsAndReadsLatestStateInCacheAndColdRestart()
    {
        using var journal = new IntentJournal(_journalPath);
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "hash-rapid",
            Status = IntentStatus.Pending,
            ItemCount = 5
        });

        await journal.UpdateStatusAsync(opId, IntentStatus.InFlight);
        await journal.UpdateStatusAsync(opId, IntentStatus.OutcomeUnknown, "Connection dropped");
        await journal.UpdateStatusAsync(opId, IntentStatus.InFlight);
        await journal.UpdateStatusAsync(opId, IntentStatus.Committed);

        var cached = await journal.GetIntentAsync(opId);
        Assert.NotNull(cached);
        Assert.Equal(IntentStatus.Committed, cached.Status);

        using var coldJournal = new IntentJournal(_journalPath);
        var diskLoaded = await coldJournal.GetIntentAsync(opId);
        Assert.NotNull(diskLoaded);
        Assert.Equal(IntentStatus.Committed, diskLoaded.Status);
    }

    [Fact]
    public async Task ConcurrentOperations_HighLoad_AppendAndReadSimultaneously_PreservesAllOperations()
    {
        using var journal = new IntentJournal(_journalPath);
        const int count = 25;
        var opIds = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToList();

        var tasks = opIds.Select(async (id, idx) =>
        {
            await journal.RecordIntentAsync(new IntentJournalRecord
            {
                OperationId = id,
                PlanHash = $"hash_{idx}",
                Status = IntentStatus.Pending,
                ItemCount = idx
            });
            await journal.UpdateStatusAsync(id, IntentStatus.InFlight);
            var current = await journal.GetIntentAsync(id);
            Assert.NotNull(current);
            await journal.UpdateStatusAsync(id, IntentStatus.Committed);
        });

        await Task.WhenAll(tasks);

        using var coldJournal = new IntentJournal(_journalPath);
        foreach (var id in opIds)
        {
            var rec = await coldJournal.GetIntentAsync(id);
            Assert.NotNull(rec);
            Assert.Equal(IntentStatus.Committed, rec.Status);
        }
    }

    [Fact]
    public async Task FileCorruption_BlankLinesAndMalformedRecords_GracefullyExtractsValidRecords()
    {
        var opValid1 = Guid.NewGuid();
        var opValid2 = Guid.NewGuid();

        var sb = new StringBuilder();
        sb.AppendLine(string.Empty);
        sb.AppendLine("   ");
        sb.AppendLine($"{{\"operationId\":\"{opValid1:D}\",\"status\":\"InFlight\",\"itemCount\":1}}");
        sb.AppendLine("{\"badJsonNoClose: 1234");
        sb.AppendLine("<<<RANDOM_OS_CORRUPTION_OR_GARBAGE>>>");
        sb.AppendLine(string.Empty);
        sb.AppendLine($"{{\"operationId\":\"{opValid2:D}\",\"status\":\"OutcomeUnknown\",\"itemCount\":2}}");

        await File.WriteAllTextAsync(_journalPath, sb.ToString());

        using var journal = new IntentJournal(_journalPath);
        var incomplete = await journal.GetIncompleteIntentsAsync();

        Assert.Equal(2, incomplete.Count);
        Assert.Contains(incomplete, r => r.OperationId == opValid1);
        Assert.Contains(incomplete, r => r.OperationId == opValid2);
    }

    [Fact]
    public async Task DisposedState_CallingAnyMethod_ThrowsObjectDisposedException()
    {
        var journal = new IntentJournal(_journalPath);
        journal.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            journal.RecordIntentAsync(new IntentJournalRecord()));
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            journal.UpdateStatusAsync(Guid.NewGuid(), IntentStatus.Committed));
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            journal.GetIncompleteIntentsAsync());
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            journal.GetIntentAsync(Guid.NewGuid()));
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            journal.PruneOldEntriesAsync(TimeSpan.FromDays(1)));

        journal.Dispose();
        await journal.DisposeAsync();
    }

    [Fact]
    public async Task PruneOldEntries_WhenFileDoesNotExist_ReturnsZero()
    {
        string nonExistentPath = Path.Combine(_tempDir, "missing", "journal.jsonl");
        using var journal = new IntentJournal(nonExistentPath);

        int pruned = await journal.PruneOldEntriesAsync(TimeSpan.FromDays(1));
        Assert.Equal(0, pruned);
    }

    [Fact]
    public async Task UpdateStatus_ForUnrecordedOperation_CreatesNewRecordAndAppendsToDisk()
    {
        using var journal = new IntentJournal(_journalPath);
        var unknownOpId = Guid.NewGuid();

        await journal.UpdateStatusAsync(unknownOpId, IntentStatus.Failed, "Direct failure entry");

        var rec = await journal.GetIntentAsync(unknownOpId);
        Assert.NotNull(rec);
        Assert.Equal(IntentStatus.Failed, rec.Status);
        Assert.Equal("Direct failure entry", rec.ErrorReason);

        using var cold = new IntentJournal(_journalPath);
        var coldRec = await cold.GetIntentAsync(unknownOpId);
        Assert.NotNull(coldRec);
        Assert.Equal(IntentStatus.Failed, coldRec.Status);
    }

    [Fact]
    public async Task RecordIntent_NullArgument_ThrowsArgumentNullException()
    {
        using var journal = new IntentJournal(_journalPath);
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            journal.RecordIntentAsync(null!));
    }
}
