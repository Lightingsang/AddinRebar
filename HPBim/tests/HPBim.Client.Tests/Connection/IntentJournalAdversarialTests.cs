namespace HPBim.Client.Tests.Connection;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Core.Execution;
using Xunit;

public sealed class AdversarialIntentJournalTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _journalPath;

    public AdversarialIntentJournalTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "hpbim_adv_journal_" + Guid.NewGuid().ToString("N"));
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
            // Transient test cleanup locking skip
        }
    }

    [Fact]
    public async Task HighConcurrency_50ParallelOperations_ZeroLostLinesAndColdRestartIntegrity()
    {
        using var journal = new IntentJournal(_journalPath);
        int opCount = 50;
        var opIds = Enumerable.Range(0, opCount).Select(_ => Guid.NewGuid()).ToList();

        // 50 parallel operations: RecordIntent -> InFlight -> Committed (150 writes total)
        var tasks = opIds.Select(async (opId, idx) =>
        {
            var record = new IntentJournalRecord
            {
                OperationId = opId,
                PlanHash = $"hash_{idx}",
                Status = IntentStatus.Pending,
                ItemCount = idx + 1,
                TargetDoc = new DocumentIdentitySnapshot { Title = $"Doc_{idx}.rvt" }
            };
            await journal.RecordIntentAsync(record);
            await journal.UpdateStatusAsync(opId, IntentStatus.InFlight);
            await journal.UpdateStatusAsync(opId, IntentStatus.Committed);
        });

        await Task.WhenAll(tasks);

        string[] lines = await File.ReadAllLinesAsync(_journalPath);
        Assert.Equal(opCount * 3, lines.Length);

        var incomplete = await journal.GetIncompleteIntentsAsync();
        Assert.Empty(incomplete);

        foreach (var opId in opIds)
        {
            var rec = await journal.GetIntentAsync(opId);
            Assert.NotNull(rec);
            Assert.Equal(IntentStatus.Committed, rec.Status);
        }

        // Cold restart verification: load new instance from disk without in-memory cache
        using var coldJournal = new IntentJournal(_journalPath);
        var coldIncomplete = await coldJournal.GetIncompleteIntentsAsync();
        Assert.Empty(coldIncomplete);

        foreach (var opId in opIds)
        {
            var rec = await coldJournal.GetIntentAsync(opId);
            Assert.NotNull(rec);
            Assert.Equal(IntentStatus.Committed, rec.Status);
        }
    }

    [Fact]
    public async Task Concurrency_MixedAppendsAndCompaction_CompletesSafely()
    {
        using var journal = new IntentJournal(_journalPath);
        var opIds = Enumerable.Range(0, 30).Select(_ => Guid.NewGuid()).ToList();

        var appendTasks = opIds.Select(async (opId, idx) =>
        {
            await journal.RecordIntentAsync(new IntentJournalRecord
            {
                OperationId = opId,
                Status = IntentStatus.Committed,
                TimestampUtc = DateTime.UtcNow.AddDays(-10)
            });
            await journal.UpdateStatusAsync(opId, IntentStatus.Committed);
        });

        var pruneTask = Task.Run(async () =>
        {
            await Task.Yield();
            return await journal.PruneOldEntriesAsync(TimeSpan.FromDays(5));
        });

        await Task.WhenAll(appendTasks);
        await pruneTask;

        // Verify journal remains readable and intact
        var remaining = await journal.GetIncompleteIntentsAsync();
        Assert.NotNull(remaining);
    }

    [Fact]
    public async Task Recovery_CorruptedAndTruncatedLines_GracefullyContinues()
    {
        var opValid1 = Guid.NewGuid();
        var opValid2 = Guid.NewGuid();
        var opValid3 = Guid.NewGuid();

        var sb = new StringBuilder();
        sb.AppendLine($"{{\"operationId\":\"{opValid1:D}\",\"status\":\"InFlight\",\"itemCount\":3}}");
        sb.AppendLine("{\"operationId\":\"bad-truncated-json-no-close");
        sb.AppendLine("<<<FATAL OS PANIC OR HARDWARE NOISE>>>");
        sb.AppendLine(string.Empty);
        sb.AppendLine($"{{\"operationId\":\"{opValid2:D}\",\"status\":\"OutcomeUnknown\",\"itemCount\":7}}");
        sb.AppendLine($"{{\"operationId\":\"{opValid3:D}\",\"status\":\"Committed\",\"itemCount\":12}}");

        await File.WriteAllTextAsync(_journalPath, sb.ToString());

        using var journal = new IntentJournal(_journalPath);
        var incomplete = await journal.GetIncompleteIntentsAsync();

        Assert.Equal(2, incomplete.Count);
        Assert.Contains(incomplete, r => r.OperationId == opValid1);
        Assert.Contains(incomplete, r => r.OperationId == opValid2);

        var committed = await journal.GetIntentAsync(opValid3);
        Assert.NotNull(committed);
        Assert.Equal(IntentStatus.Committed, committed.Status);
    }

    [Fact]
    public async Task Compaction_StrictlyPreservesAllIncompleteIntents_RegardlessOfAge()
    {
        using var journal = new IntentJournal(_journalPath);
        var opOldPending = Guid.NewGuid();
        var opOldInFlight = Guid.NewGuid();
        var opOldUnknown = Guid.NewGuid();
        var opOldCommitted = Guid.NewGuid();
        var opOldFailed = Guid.NewGuid();
        var opNewCommitted = Guid.NewGuid();
        var opNewFailed = Guid.NewGuid();

        var oldTime = DateTime.UtcNow.AddDays(-30);
        var recentTime = DateTime.UtcNow.AddHours(-1);

        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = opOldPending, Status = IntentStatus.Pending, TimestampUtc = oldTime });
        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = opOldInFlight, Status = IntentStatus.InFlight, TimestampUtc = oldTime });
        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = opOldUnknown, Status = IntentStatus.OutcomeUnknown, TimestampUtc = oldTime });
        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = opOldCommitted, Status = IntentStatus.Committed, TimestampUtc = oldTime });
        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = opOldFailed, Status = IntentStatus.Failed, TimestampUtc = oldTime });
        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = opNewCommitted, Status = IntentStatus.Committed, TimestampUtc = recentTime });
        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = opNewFailed, Status = IntentStatus.Failed, TimestampUtc = recentTime });

        int pruned = await journal.PruneOldEntriesAsync(TimeSpan.FromDays(7));
        Assert.Equal(2, pruned); // Exactly opOldCommitted and opOldFailed pruned

        // All incomplete entries preserved despite being 30 days old
        Assert.NotNull(await journal.GetIntentAsync(opOldPending));
        Assert.NotNull(await journal.GetIntentAsync(opOldInFlight));
        Assert.NotNull(await journal.GetIntentAsync(opOldUnknown));

        // Recent terminal entries preserved
        Assert.NotNull(await journal.GetIntentAsync(opNewCommitted));
        Assert.NotNull(await journal.GetIntentAsync(opNewFailed));

        // Old terminal entries removed
        Assert.Null(await journal.GetIntentAsync(opOldCommitted));
        Assert.Null(await journal.GetIntentAsync(opOldFailed));
    }

    [Fact]
    public async Task Compaction_CollapsesMultiStateHistory_DownToLatestState()
    {
        using var journal = new IntentJournal(_journalPath);
        var op1 = Guid.NewGuid();
        var op2 = Guid.NewGuid();
        var oldTime = DateTime.UtcNow.AddDays(-10);

        // Op1 evolves to Committed (old)
        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = op1, Status = IntentStatus.InFlight, TimestampUtc = oldTime });
        await journal.UpdateStatusAsync(op1, IntentStatus.Committed);

        // Update Op1 timestamp back to old to simulate old committed
        var op1Rec = (await journal.GetIntentAsync(op1))! with { TimestampUtc = oldTime };
        await File.WriteAllLinesAsync(_journalPath, new[]
        {
            $"{{\"operationId\":\"{op1:D}\",\"status\":\"InFlight\",\"timestampUtc\":\"{oldTime:O}\"}}",
            $"{{\"operationId\":\"{op1:D}\",\"status\":\"Committed\",\"timestampUtc\":\"{oldTime:O}\"}}",
            $"{{\"operationId\":\"{op2:D}\",\"status\":\"InFlight\",\"timestampUtc\":\"{oldTime:O}\"}}",
            $"{{\"operationId\":\"{op2:D}\",\"status\":\"OutcomeUnknown\",\"timestampUtc\":\"{oldTime:O}\"}}"
        });

        using var freshJournal = new IntentJournal(_journalPath);
        int pruned = await freshJournal.PruneOldEntriesAsync(TimeSpan.FromDays(5));
        Assert.Equal(1, pruned); // Op1 pruned

        var op2Rec = await freshJournal.GetIntentAsync(op2);
        Assert.NotNull(op2Rec);
        Assert.Equal(IntentStatus.OutcomeUnknown, op2Rec.Status);

        string[] postPruneLines = await File.ReadAllLinesAsync(_journalPath);
        Assert.Single(postPruneLines);
        Assert.Contains(op2.ToString("D"), postPruneLines[0]);
    }

    [Fact]
    public async Task TrailingTruncatedLineWithoutNewline_InvestigatesAppendBehavior()
    {
        var op1 = Guid.NewGuid();
        var sb = new StringBuilder();
        sb.AppendLine($"{{\"operationId\":\"{op1:D}\",\"status\":\"Committed\"}}");
        sb.Append("{\"operationId\":\"partial-no-newline"); // Intentionally no newline

        await File.WriteAllTextAsync(_journalPath, sb.ToString());

        var op2 = Guid.NewGuid();
        using (var journal = new IntentJournal(_journalPath))
        {
            await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = op2, Status = IntentStatus.InFlight });
        }

        string[] lines = await File.ReadAllLinesAsync(_journalPath);
        Assert.Equal(3, lines.Length);
        Assert.Equal("{\"operationId\":\"partial-no-newline", lines[1]);
        Assert.Contains(op2.ToString("D"), lines[2]);

        using (var coldJournal = new IntentJournal(_journalPath))
        {
            var op2Cold = await coldJournal.GetIntentAsync(op2);
            Assert.NotNull(op2Cold);
            Assert.Equal(IntentStatus.InFlight, op2Cold.Status);
        }
    }

    [Fact]
    public async Task TornTailAutoRecovery_PreservesPreviousCleanRecordsAndAppendedRecordOnColdRestart()
    {
        var opClean = Guid.NewGuid();
        var sb = new StringBuilder();
        sb.AppendLine($"{{\"operationId\":\"{opClean:D}\",\"status\":\"Committed\",\"planHash\":\"hash-clean\"}}");
        sb.Append("{\"operationId\":\"truncated-torn-tail-without-newline");

        await File.WriteAllTextAsync(_journalPath, sb.ToString());

        var opNew = Guid.NewGuid();
        using (var journal = new IntentJournal(_journalPath))
        {
            await journal.RecordIntentAsync(new IntentJournalRecord
            {
                OperationId = opNew,
                Status = IntentStatus.Pending,
                PlanHash = "hash-new"
            });
        }

        using (var coldJournal = new IntentJournal(_journalPath))
        {
            var cleanRec = await coldJournal.GetIntentAsync(opClean);
            Assert.NotNull(cleanRec);
            Assert.Equal(IntentStatus.Committed, cleanRec.Status);
            Assert.Equal("hash-clean", cleanRec.PlanHash);

            var newRec = await coldJournal.GetIntentAsync(opNew);
            Assert.NotNull(newRec);
            Assert.Equal(IntentStatus.Pending, newRec.Status);
            Assert.Equal("hash-new", newRec.PlanHash);
        }
    }
}
