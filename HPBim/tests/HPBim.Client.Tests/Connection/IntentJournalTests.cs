namespace HPBim.Client.Tests.Connection;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Core.Execution;
using Xunit;

public sealed class IntentJournalTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _journalPath;

    public IntentJournalTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "hpbim_journal_test_" + Guid.NewGuid().ToString("N"));
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
            // Allow transient file locking cleanup skip in test teardown
        }
    }

    [Fact]
    public async Task RecordIntentAsync_WritesRecordToJsonlFile_WithValidJson()
    {
        using var journal = new IntentJournal(_journalPath);
        var opId = Guid.NewGuid();
        var record = new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "hash123",
            Status = IntentStatus.InFlight,
            ItemCount = 5,
            TargetDoc = new DocumentIdentitySnapshot { Title = "Project1.rvt" }
        };

        await journal.RecordIntentAsync(record);

        Assert.True(File.Exists(_journalPath));
        string[] lines = await File.ReadAllLinesAsync(_journalPath);
        Assert.Single(lines);
        Assert.Contains(opId.ToString("D"), lines[0]);
        Assert.Contains("InFlight", lines[0]);

        var retrieved = await journal.GetIntentAsync(opId);
        Assert.NotNull(retrieved);
        Assert.Equal(opId, retrieved.OperationId);
        Assert.Equal(IntentStatus.InFlight, retrieved.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_AppendsNewStateLine_ReflectsLatestStatus()
    {
        using var journal = new IntentJournal(_journalPath);
        var opId = Guid.NewGuid();
        var record = new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "hash456",
            Status = IntentStatus.InFlight,
            ItemCount = 10
        };

        await journal.RecordIntentAsync(record);
        await journal.UpdateStatusAsync(opId, IntentStatus.Committed);

        string[] lines = await File.ReadAllLinesAsync(_journalPath);
        Assert.Equal(2, lines.Length);

        var retrieved = await journal.GetIntentAsync(opId);
        Assert.NotNull(retrieved);
        Assert.Equal(IntentStatus.Committed, retrieved.Status);
        Assert.True(retrieved.IsTerminal);
    }

    [Fact]
    public async Task GetIncompleteIntentsAsync_FiltersPendingInFlightOutcomeUnknown_IgnoresCommittedAndFailed()
    {
        using var journal = new IntentJournal(_journalPath);
        var op1 = Guid.NewGuid();
        var op2 = Guid.NewGuid();
        var op3 = Guid.NewGuid();
        var op4 = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = op1, Status = IntentStatus.InFlight, TimestampUtc = DateTime.UtcNow.AddMinutes(-4) });
        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = op2, Status = IntentStatus.Committed, TimestampUtc = DateTime.UtcNow.AddMinutes(-3) });
        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = op3, Status = IntentStatus.Failed, TimestampUtc = DateTime.UtcNow.AddMinutes(-2) });
        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = op4, Status = IntentStatus.OutcomeUnknown, TimestampUtc = DateTime.UtcNow.AddMinutes(-1) });

        var incomplete = await journal.GetIncompleteIntentsAsync();
        Assert.Equal(2, incomplete.Count);
        Assert.Contains(incomplete, i => i.OperationId == op1 && i.Status == IntentStatus.InFlight);
        Assert.Contains(incomplete, i => i.OperationId == op4 && i.Status == IntentStatus.OutcomeUnknown);
    }

    [Fact]
    public async Task CorruptedJournalLines_GracefullySkipped_PreservesValidRecords()
    {
        var opValid1 = Guid.NewGuid();
        var opValid2 = Guid.NewGuid();

        var sb = new StringBuilder();
        sb.AppendLine($"{{\"operationId\":\"{opValid1:D}\",\"status\":\"InFlight\",\"itemCount\":3}}");
        sb.AppendLine("{ \"operationId\": truncated json...");
        sb.AppendLine("CORRUPT_NOT_JSON_DATA");
        sb.AppendLine(string.Empty);
        sb.AppendLine($"{{\"operationId\":\"{opValid2:D}\",\"status\":\"OutcomeUnknown\",\"itemCount\":7}}");

        await File.WriteAllTextAsync(_journalPath, sb.ToString());

        using var journal = new IntentJournal(_journalPath);
        var incomplete = await journal.GetIncompleteIntentsAsync();

        Assert.Equal(2, incomplete.Count);
        Assert.Contains(incomplete, r => r.OperationId == opValid1);
        Assert.Contains(incomplete, r => r.OperationId == opValid2);
    }

    [Fact]
    public async Task PruneOldEntriesAsync_RemovesExpiredTerminalRecords_PreservesIncompleteRecords()
    {
        using var journal = new IntentJournal(_journalPath);
        var opOldCommitted = Guid.NewGuid();
        var opNewCommitted = Guid.NewGuid();
        var opOldInFlight = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = opOldCommitted, Status = IntentStatus.Committed, TimestampUtc = DateTime.UtcNow.AddDays(-10) });
        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = opNewCommitted, Status = IntentStatus.Committed, TimestampUtc = DateTime.UtcNow.AddDays(-1) });
        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = opOldInFlight, Status = IntentStatus.InFlight, TimestampUtc = DateTime.UtcNow.AddDays(-10) });

        int pruned = await journal.PruneOldEntriesAsync(TimeSpan.FromDays(7));
        Assert.Equal(1, pruned);

        Assert.Null(await journal.GetIntentAsync(opOldCommitted));
        Assert.NotNull(await journal.GetIntentAsync(opNewCommitted));
        Assert.NotNull(await journal.GetIntentAsync(opOldInFlight));
    }

    [Fact]
    public async Task ConcurrentAppends_ThreadSafe_NoLineInterleaving()
    {
        using var journal = new IntentJournal(_journalPath);
        int taskCount = 20;

        var tasks = Enumerable.Range(0, taskCount).Select(i =>
        {
            var rec = new IntentJournalRecord
            {
                OperationId = Guid.NewGuid(),
                Status = IntentStatus.InFlight,
                ItemCount = i
            };
            return journal.RecordIntentAsync(rec);
        });

        await Task.WhenAll(tasks);

        string[] lines = await File.ReadAllLinesAsync(_journalPath);
        Assert.Equal(taskCount, lines.Length);
        var incomplete = await journal.GetIncompleteIntentsAsync();
        Assert.Equal(taskCount, incomplete.Count);
    }

    [Fact]
    public async Task Dispose_DisallowsFurtherOperations()
    {
        var journal = new IntentJournal(_journalPath);
        journal.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            journal.RecordIntentAsync(new IntentJournalRecord()));
    }

    [Fact]
    public async Task UpdateStatusAsync_OnCacheMissAcrossProcessRestart_ReloadsFromDiskAndPreservesAllMetadata()
    {
        var opId = Guid.NewGuid();
        var vGuid = Guid.NewGuid();
        using (var journal1 = new IntentJournal(_journalPath))
        {
            var initial = new IntentJournalRecord
            {
                OperationId = opId,
                PlanHash = "hash-persist-123",
                ItemCount = 7,
                Status = IntentStatus.InFlight,
                TargetDoc = new DocumentIdentitySnapshot { Title = "Doc1.rvt", VersionGuid = vGuid }
            };
            await journal1.RecordIntentAsync(initial);
        }

        // Simulate process restart: brand new journal instance with empty in-memory cache
        using (var journal2 = new IntentJournal(_journalPath))
        {
            await journal2.UpdateStatusAsync(opId, IntentStatus.OutcomeUnknown, "Connection severed");

            var updated = await journal2.GetIntentAsync(opId);
            Assert.NotNull(updated);
            Assert.Equal(IntentStatus.OutcomeUnknown, updated.Status);
            Assert.Equal("hash-persist-123", updated.PlanHash);
            Assert.Equal(7, updated.ItemCount);
            Assert.Equal("Doc1.rvt", updated.TargetDoc.Title);
            Assert.Equal(vGuid, updated.TargetDoc.VersionGuid);
            Assert.Equal("Doc1.rvt", updated.TargetDocId);
            Assert.Equal("Connection severed", updated.ErrorReason);
        }
    }
}
