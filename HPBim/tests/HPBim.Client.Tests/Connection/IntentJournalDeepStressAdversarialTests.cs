namespace HPBim.Client.Tests.Connection;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Core.Execution;
using Xunit;

/// <summary>
/// Deep adversarial stress tests for IntentJournal torn-tail recovery, compaction with corruption, and process restart resilience.
/// </summary>
public sealed class IntentJournalDeepStressAdversarialTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _journalPath;

    public IntentJournalDeepStressAdversarialTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "hpbim_deepstress_" + Guid.NewGuid().ToString("N"));
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
            // Transient test teardown locking tolerance
        }
    }

    [Fact]
    public async Task Compaction_WithTornTailsAndExpiredRecords_PurgesTornLinesAndPreservesIncomplete()
    {
        var opExpired = Guid.NewGuid();
        var opActive = Guid.NewGuid();
        var oldTime = DateTime.UtcNow.AddDays(-20);

        var sb = new StringBuilder();
        sb.AppendLine($"{{\"operationId\":\"{opExpired:D}\",\"status\":\"Committed\",\"timestampUtc\":\"{oldTime:O}\"}}");
        sb.AppendLine("{\"operationId\":\"middle-torn-line-no-close");
        sb.AppendLine($"{{\"operationId\":\"{opActive:D}\",\"status\":\"InFlight\",\"itemCount\":5}}");
        sb.Append("{\"operationId\":\"trailing-torn-tail-no-newline");

        await File.WriteAllTextAsync(_journalPath, sb.ToString());

        using (var journal = new IntentJournal(_journalPath))
        {
            int pruned = await journal.PruneOldEntriesAsync(TimeSpan.FromDays(7));
            Assert.Equal(1, pruned); // opExpired pruned

            var active = await journal.GetIntentAsync(opActive);
            Assert.NotNull(active);
            Assert.Equal(IntentStatus.InFlight, active.Status);
        }

        // Compaction should result in valid JSON lines only
        string[] compactedLines = await File.ReadAllLinesAsync(_journalPath);
        Assert.Single(compactedLines);
        Assert.Contains(opActive.ToString("D"), compactedLines[0]);
    }

    [Fact]
    public async Task ConsecutiveProcessCrashes_WithoutValidRecords_RecoversCleanRecord()
    {
        // 3 consecutive crashes leaving torn fragments without newlines
        await File.AppendAllTextAsync(_journalPath, "{\"crash1\":\"raw-bytes");
        using (var p1 = new IntentJournal(_journalPath))
        {
            // P1 opened, appends newline via boundary check, but crashes during write
        }
        await File.AppendAllTextAsync(_journalPath, "{\"crash2\":\"more-garbage");
        using (var p2 = new IntentJournal(_journalPath))
        {
            // P2 crashes during write
        }
        await File.AppendAllTextAsync(_journalPath, "{\"crash3\":\"final-cut");

        var cleanOp = Guid.NewGuid();
        using (var p3 = new IntentJournal(_journalPath))
        {
            await p3.RecordIntentAsync(new IntentJournalRecord
            {
                OperationId = cleanOp,
                Status = IntentStatus.Pending,
                PlanHash = "resilient-hash"
            });
        }

        using (var cold = new IntentJournal(_journalPath))
        {
            var recovered = await cold.GetIntentAsync(cleanOp);
            Assert.NotNull(recovered);
            Assert.Equal(IntentStatus.Pending, recovered.Status);
            Assert.Equal("resilient-hash", recovered.PlanHash);
        }
    }

    [Fact]
    public async Task LargeTornTail_64KBytesGarbageWithoutNewline_GracefullySkippedOnColdRestart()
    {
        var opClean = Guid.NewGuid();
        var sb = new StringBuilder();
        sb.AppendLine($"{{\"operationId\":\"{opClean:D}\",\"status\":\"Committed\"}}");
        sb.Append("{\"garbage\":\"");
        sb.Append(new string('X', 65536)); // 64KB giant string without closing or newline

        await File.WriteAllTextAsync(_journalPath, sb.ToString());

        var opNew = Guid.NewGuid();
        using (var journal = new IntentJournal(_journalPath))
        {
            await journal.RecordIntentAsync(new IntentJournalRecord
            {
                OperationId = opNew,
                Status = IntentStatus.InFlight,
                ItemCount = 99
            });
        }

        using (var cold = new IntentJournal(_journalPath))
        {
            var rClean = await cold.GetIntentAsync(opClean);
            var rNew = await cold.GetIntentAsync(opNew);
            Assert.NotNull(rClean);
            Assert.NotNull(rNew);
            Assert.Equal(IntentStatus.Committed, rClean.Status);
            Assert.Equal(IntentStatus.InFlight, rNew.Status);
        }
    }

    [Fact]
    public async Task EnsureLineBoundary_WithDoubleCrLf_DoesNotAddExtraBlankLines()
    {
        var op = Guid.NewGuid();
        var content = $"{{\"operationId\":\"{op:D}\",\"status\":\"Committed\"}}\r\n\r\n";
        await File.WriteAllTextAsync(_journalPath, content);

        var opNext = Guid.NewGuid();
        using (var journal = new IntentJournal(_journalPath))
        {
            await journal.RecordIntentAsync(new IntentJournalRecord
            {
                OperationId = opNext,
                Status = IntentStatus.Pending
            });
        }

        using (var cold = new IntentJournal(_journalPath))
        {
            var incomplete = await cold.GetIncompleteIntentsAsync();
            Assert.Single(incomplete);
            Assert.Equal(opNext, incomplete[0].OperationId);
        }
    }

    [Fact]
    public async Task RapidColdRestarts_UnderHeavyParallelOperations_ZeroDataLoss()
    {
        const int totalOps = 30;
        var opIds = Enumerable.Range(0, totalOps).Select(_ => Guid.NewGuid()).ToList();

        // Sequential batches of cold-start writers
        foreach (var chunk in opIds.Chunk(10))
        {
            using var journal = new IntentJournal(_journalPath);
            var tasks = chunk.Select(async id =>
            {
                await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = id, Status = IntentStatus.Pending });
                await journal.UpdateStatusAsync(id, IntentStatus.InFlight);
                await journal.UpdateStatusAsync(id, IntentStatus.Committed);
            });
            await Task.WhenAll(tasks);
        }

        // Final cold restart verification
        using var finalJournal = new IntentJournal(_journalPath);
        var incomplete = await finalJournal.GetIncompleteIntentsAsync();
        Assert.Empty(incomplete);

        foreach (var id in opIds)
        {
            var rec = await finalJournal.GetIntentAsync(id);
            Assert.NotNull(rec);
            Assert.Equal(IntentStatus.Committed, rec.Status);
        }
    }
}
