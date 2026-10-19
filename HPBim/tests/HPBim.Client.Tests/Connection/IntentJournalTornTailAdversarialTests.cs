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
/// Adversarial stress tests for Requirement R2: Journal Torn-tail Resilience &amp; Auto-Recovery.
/// </summary>
public sealed class IntentJournalTornTailAdversarialTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _journalPath;

    public IntentJournalTornTailAdversarialTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "hpbim_torntail_" + Guid.NewGuid().ToString("N"));
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
    public async Task CornerCase1_EmptyAndZeroByteFile_AppendsCleanlyAndRecoversOnColdRestart()
    {
        await File.WriteAllBytesAsync(_journalPath, Array.Empty<byte>());
        Assert.True(File.Exists(_journalPath));
        Assert.Equal(0, new FileInfo(_journalPath).Length);

        using (var journal = new IntentJournal(_journalPath))
        {
            var incomplete = await journal.GetIncompleteIntentsAsync();
            Assert.Empty(incomplete);

            var opId = Guid.NewGuid();
            await journal.RecordIntentAsync(CreateRecord(opId, "plan-zero", IntentStatus.Pending, 4));
            var rec = await journal.GetIntentAsync(opId);
            Assert.NotNull(rec);
            Assert.Equal(IntentStatus.Pending, rec.Status);
        }

        using (var cold = new IntentJournal(_journalPath))
        {
            var incomplete = await cold.GetIncompleteIntentsAsync();
            Assert.Single(incomplete);
            Assert.Equal("plan-zero", incomplete[0].PlanHash);
        }
    }

    [Theory]
    [InlineData(new byte[] { 0x7B })] // '{'
    [InlineData(new byte[] { 0x0A })] // '\n'
    [InlineData(new byte[] { 0x0D })] // '\r'
    [InlineData(new byte[] { 0x20 })] // ' '
    [InlineData(new byte[] { 0x00 })] // '\0'
    [InlineData(new byte[] { 0xFF })] // Invalid UTF-8 byte
    [InlineData(new byte[] { 0xEF, 0xBB, 0xBF })] // UTF-8 BOM
    public async Task CornerCase1_SingleByteAndMalformedHeads_AutoRecoversAndAppendsValidRecord(byte[] initialBytes)
    {
        await File.WriteAllBytesAsync(_journalPath, initialBytes);
        var opId = Guid.NewGuid();

        using (var journal = new IntentJournal(_journalPath))
        {
            await journal.RecordIntentAsync(CreateRecord(opId, "head-test", IntentStatus.Pending, 1));
            await journal.UpdateStatusAsync(opId, IntentStatus.InFlight);
        }

        using (var cold = new IntentJournal(_journalPath))
        {
            var recovered = await cold.GetIntentAsync(opId);
            Assert.NotNull(recovered);
            Assert.Equal(IntentStatus.InFlight, recovered.Status);
            Assert.Equal("head-test", recovered.PlanHash);
        }
    }

    [Fact]
    public async Task CornerCase2_TruncatedJsonWithoutNewline_ImmediateAppendSeparatesLines()
    {
        var op1 = Guid.NewGuid();
        var sb = new StringBuilder();
        sb.AppendLine($"{{\"operationId\":\"{op1:D}\",\"status\":\"Committed\",\"planHash\":\"h1\"}}");
        sb.Append("{\"operationId\":\"torn-no-newline-crash"); // Unfinished line, no \n

        await File.WriteAllTextAsync(_journalPath, sb.ToString());

        var op2 = Guid.NewGuid();
        using (var journal = new IntentJournal(_journalPath))
        {
            await journal.RecordIntentAsync(CreateRecord(op2, "h2", IntentStatus.Pending, 10));
            await journal.UpdateStatusAsync(op2, IntentStatus.InFlight);
        }

        string[] lines = await File.ReadAllLinesAsync(_journalPath);
        Assert.Equal(4, lines.Length);
        Assert.StartsWith("{\"operationId\":\"torn-no-newline-crash", lines[1]);

        using (var cold = new IntentJournal(_journalPath))
        {
            var rec1 = await cold.GetIntentAsync(op1);
            var rec2 = await cold.GetIntentAsync(op2);
            Assert.NotNull(rec1);
            Assert.NotNull(rec2);
            Assert.Equal(IntentStatus.Committed, rec1.Status);
            Assert.Equal(IntentStatus.InFlight, rec2.Status);
        }
    }

    [Fact]
    public async Task CornerCase2_TornTailWithoutNewline_UpdateStatusOnCacheMiss_PreservesMetadata()
    {
        var opClean = Guid.NewGuid();
        var sb = new StringBuilder();
        sb.AppendLine($"{{\"operationId\":\"{opClean:D}\",\"status\":\"InFlight\",\"planHash\":\"hash-meta\",\"itemCount\":88}}");
        sb.Append("{\"operationId\":\"crashed-process-tail"); // Torn tail without newline

        await File.WriteAllTextAsync(_journalPath, sb.ToString());

        // New cold process without in-memory cache directly updates status of clean record
        using (var freshJournal = new IntentJournal(_journalPath))
        {
            await freshJournal.UpdateStatusAsync(opClean, IntentStatus.Committed);
        }

        using (var cold = new IntentJournal(_journalPath))
        {
            var rec = await cold.GetIntentAsync(opClean);
            Assert.NotNull(rec);
            Assert.Equal(IntentStatus.Committed, rec.Status);
            Assert.Equal("hash-meta", rec.PlanHash);
            Assert.Equal(88, rec.ItemCount);
        }
    }

    [Fact]
    public async Task CornerCase3_MultipleCrashedProcesses_AlternatingTornTails_AllSurviveAcrossRestarts()
    {
        var op1 = Guid.NewGuid();
        var op2 = Guid.NewGuid();
        var op3 = Guid.NewGuid();

        // Process 1: Writes Op1
        using (var p1 = new IntentJournal(_journalPath))
            await p1.RecordIntentAsync(CreateRecord(op1, "p1", IntentStatus.Committed, 1));

        // Crash 1: Process dies mid-write
        await File.AppendAllTextAsync(_journalPath, "{\"operationId\":\"crash1-torn");

        // Process 2: Recovers Op1, writes Op2
        using (var p2 = new IntentJournal(_journalPath))
        {
            Assert.NotNull(await p2.GetIntentAsync(op1));
            await p2.RecordIntentAsync(CreateRecord(op2, "p2", IntentStatus.InFlight, 2));
        }

        // Crash 2: Another process dies mid-write with partial UTF-8
        await File.AppendAllTextAsync(_journalPath, "{\"broken\":\"unicode-\u00E1\u00E9");

        // Process 3: Recovers Op1 and Op2, writes Op3
        using (var p3 = new IntentJournal(_journalPath))
        {
            Assert.NotNull(await p3.GetIntentAsync(op1));
            Assert.NotNull(await p3.GetIntentAsync(op2));
            await p3.RecordIntentAsync(CreateRecord(op3, "p3", IntentStatus.Pending, 3));
        }

        // Cold restart verification
        using (var cold = new IntentJournal(_journalPath))
        {
            var r1 = await cold.GetIntentAsync(op1);
            var r2 = await cold.GetIntentAsync(op2);
            var r3 = await cold.GetIntentAsync(op3);
            Assert.NotNull(r1);
            Assert.NotNull(r2);
            Assert.NotNull(r3);
            Assert.Equal(IntentStatus.Committed, r1.Status);
            Assert.Equal(IntentStatus.InFlight, r2.Status);
            Assert.Equal(IntentStatus.Pending, r3.Status);
        }
    }

    [Fact]
    public async Task CornerCase4_HighConcurrency_ThreadSafetyUnderIoLock_ZeroLostLines()
    {
        using var journal = new IntentJournal(_journalPath);
        const int opCount = 40;
        var opIds = Enumerable.Range(0, opCount).Select(_ => Guid.NewGuid()).ToList();

        var tasks = opIds.Select(async (id, idx) =>
        {
            await journal.RecordIntentAsync(CreateRecord(id, $"conc_{idx}", IntentStatus.Pending, idx));
            await journal.UpdateStatusAsync(id, IntentStatus.InFlight);

            var check = await journal.GetIntentAsync(id);
            Assert.NotNull(check);

            await journal.UpdateStatusAsync(id, IntentStatus.Committed);
        });

        await Task.WhenAll(tasks);

        string[] lines = await File.ReadAllLinesAsync(_journalPath);
        Assert.Equal(opCount * 3, lines.Length);

        using var cold = new IntentJournal(_journalPath);
        var incomplete = await cold.GetIncompleteIntentsAsync();
        Assert.Empty(incomplete);

        foreach (var id in opIds)
        {
            var rec = await cold.GetIntentAsync(id);
            Assert.NotNull(rec);
            Assert.Equal(IntentStatus.Committed, rec.Status);
        }
    }

    [Fact]
    public async Task CornerCase4_ConcurrentReadersAndWriters_UnderIoLock_MaintainsConsistency()
    {
        using var journal = new IntentJournal(_journalPath);
        const int count = 20;
        var opIds = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToList();

        var writeTasks = opIds.Select(async (id, idx) =>
        {
            await journal.RecordIntentAsync(CreateRecord(id, $"mix_{idx}", IntentStatus.Pending, idx));
            await Task.Yield();
            await journal.UpdateStatusAsync(id, IntentStatus.InFlight);
        });

        var readTasks = Enumerable.Range(0, 10).Select(async _ =>
        {
            for (int i = 0; i < 5; i++)
            {
                await Task.Yield();
                var list = await journal.GetIncompleteIntentsAsync();
                Assert.NotNull(list);
            }
        });

        await Task.WhenAll(writeTasks.Concat(readTasks));

        using var cold = new IntentJournal(_journalPath);
        var incomplete = await cold.GetIncompleteIntentsAsync();
        Assert.Equal(count, incomplete.Count);
    }

    private static IntentJournalRecord CreateRecord(Guid id, string hash, IntentStatus status, int items) =>
        new()
        {
            OperationId = id,
            PlanHash = hash,
            Status = status,
            ItemCount = items,
            TargetDoc = new DocumentIdentitySnapshot { Title = "Model.rvt" }
        };
}
