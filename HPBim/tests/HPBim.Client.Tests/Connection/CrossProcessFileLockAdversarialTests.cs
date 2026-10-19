namespace HPBim.Client.Tests.Connection;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Core.Execution;
using Xunit;

public sealed class CrossProcessFileLockAdversarialTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _lockFilePath;
    private readonly string _journalPath;

    public CrossProcessFileLockAdversarialTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "hpbim_lock_adv_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _lockFilePath = Path.Combine(_tempDir, "test.lock");
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
            // Transient test teardown file lock cleanup skip
        }
    }

    [Fact]
    public async Task AcquireAsync_WhenLockHeldAndCancelled_ThrowsOperationCanceledExceptionPromptly()
    {
        await using var lock1 = await CrossProcessFileLock.AcquireAsync(_lockFilePath);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(80));
        var sw = Stopwatch.StartNew();

        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await CrossProcessFileLock.AcquireAsync(
                _lockFilePath,
                timeoutMilliseconds: 30000,
                cancellationToken: cts.Token);
        });

        sw.Stop();
        Assert.NotNull(ex);
        Assert.True(sw.ElapsedMilliseconds < 5000, $"Expected prompt cancellation, took {sw.ElapsedMilliseconds}ms");

        lock1.Dispose();

        await using var lock2 = await CrossProcessFileLock.AcquireAsync(_lockFilePath, timeoutMilliseconds: 2000);
        Assert.NotNull(lock2);
    }

    [Fact]
    public async Task AcquireAsync_WithPreCancelledToken_ThrowsOperationCanceledExceptionImmediately()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await CrossProcessFileLock.AcquireAsync(_lockFilePath, cancellationToken: cts.Token);
        });

        Assert.False(File.Exists(_lockFilePath));
    }

    [Fact]
    public async Task AcquireAsync_WhenLockHeldAndTimesOut_ThrowsTimeoutExceptionPromptly()
    {
        await using var lock1 = await CrossProcessFileLock.AcquireAsync(_lockFilePath);

        var sw = Stopwatch.StartNew();
        var ex = await Assert.ThrowsAsync<TimeoutException>(async () =>
        {
            await CrossProcessFileLock.AcquireAsync(_lockFilePath, timeoutMilliseconds: 150);
        });

        sw.Stop();
        Assert.NotNull(ex);
        Assert.InRange(sw.ElapsedMilliseconds, 100, 3000);
    }

    [Fact]
    public async Task AcquireAsync_ZeroByteVsExistingContentLockFile_BothSucceedAndPreserveContent()
    {
        // Scenario 1: File does not exist yet -> creates 0-byte file
        await using (var l1 = await CrossProcessFileLock.AcquireAsync(_lockFilePath))
        {
            Assert.True(File.Exists(_lockFilePath));
        }

        Assert.Equal(0, new FileInfo(_lockFilePath).Length);

        // Scenario 2: File exists with non-zero content
        byte[] payload = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x02, 0x03, 0x04 };
        await File.WriteAllBytesAsync(_lockFilePath, payload);

        await using (var l2 = await CrossProcessFileLock.AcquireAsync(_lockFilePath))
        {
            Assert.NotNull(l2);
            // FileShare.None guarantees external read attempts are rejected
            Assert.Throws<IOException>(() => File.ReadAllBytes(_lockFilePath));
        }

        byte[] readAfter = await File.ReadAllBytesAsync(_lockFilePath);
        Assert.Equal(payload, readAfter);
    }

    [Fact]
    public async Task AcquireAsync_RapidSequentialLockUnlock_1000Cycles_CompletesCleanly()
    {
        for (int i = 0; i < 1000; i++)
        {
            await using var fileLock = await CrossProcessFileLock.AcquireAsync(_lockFilePath, timeoutMilliseconds: 5000);
            Assert.NotNull(fileLock);
        }

        Assert.True(File.Exists(_lockFilePath));
    }

    [Fact]
    public async Task AcquireAsync_HighContentionConcurrentAccess_MaintainsStrictMutualExclusion()
    {
        int concurrentCount = 0;
        int maxObserved = 0;
        int totalCompleted = 0;
        int taskCount = 8;
        int iterationsPerTask = 25;

        var tasks = Enumerable.Range(0, taskCount).Select(async _ =>
        {
            for (int i = 0; i < iterationsPerTask; i++)
            {
                await using (await CrossProcessFileLock.AcquireAsync(_lockFilePath, timeoutMilliseconds: 30000))
                {
                    int current = Interlocked.Increment(ref concurrentCount);
                    int prevMax;
                    do
                    {
                        prevMax = maxObserved;
                        if (current <= prevMax)
                        {
                            break;
                        }
                    }
                    while (Interlocked.CompareExchange(ref maxObserved, current, prevMax) != prevMax);

                    Assert.Equal(1, current);
                    await Task.Yield();
                    Interlocked.Decrement(ref concurrentCount);
                }

                Interlocked.Increment(ref totalCompleted);
            }
        });

        await Task.WhenAll(tasks);

        Assert.Equal(taskCount * iterationsPerTask, totalCompleted);
        Assert.Equal(1, maxObserved);
        Assert.Equal(0, concurrentCount);
    }

    [Fact]
    public async Task Dispose_MultipleCalls_IsIdempotentAndReleasesImmediately()
    {
        var fileLock = await CrossProcessFileLock.AcquireAsync(_lockFilePath);
        fileLock.Dispose();
        fileLock.Dispose();
        await fileLock.DisposeAsync();

        await using var secondLock = await CrossProcessFileLock.AcquireAsync(_lockFilePath, timeoutMilliseconds: 1000);
        Assert.NotNull(secondLock);
    }

    [Fact]
    public async Task IntentJournal_CancellationTokenAbortDuringLockWait_ThrowsOperationCanceledException()
    {
        string journalLockPath = $"{_journalPath}.lock";
        await using var externalLock = await CrossProcessFileLock.AcquireAsync(journalLockPath);

        using var journal = new IntentJournal(_journalPath);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var record = new IntentJournalRecord
        {
            OperationId = Guid.NewGuid(),
            PlanHash = "test-hash",
            Status = IntentStatus.InFlight
        };

        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await journal.RecordIntentAsync(record, cts.Token);
        });

        Assert.NotNull(ex);

        externalLock.Dispose();

        await journal.RecordIntentAsync(record);
        var retrieved = await journal.GetIntentAsync(record.OperationId);
        Assert.NotNull(retrieved);
        Assert.Equal(IntentStatus.InFlight, retrieved.Status);
    }

    [Fact]
    public async Task IntentJournal_RapidSequentialRecordAndUpdate_AlternatingInstances_PreservesIntegrity()
    {
        using var journal1 = new IntentJournal(_journalPath);
        using var journal2 = new IntentJournal(_journalPath);

        int cycles = 50;
        var opIds = Enumerable.Range(0, cycles).Select(_ => Guid.NewGuid()).ToList();

        for (int i = 0; i < cycles; i++)
        {
            var id = opIds[i];
            await journal1.RecordIntentAsync(new IntentJournalRecord
            {
                OperationId = id,
                Status = IntentStatus.InFlight,
                PlanHash = $"cycle_{i}"
            });

            var rec2 = await journal2.GetIntentAsync(id);
            Assert.NotNull(rec2);
            Assert.Equal(IntentStatus.InFlight, rec2.Status);

            await journal2.UpdateStatusAsync(id, IntentStatus.Committed);

            var rec1 = await journal1.GetIntentAsync(id);
            Assert.NotNull(rec1);
            Assert.Equal(IntentStatus.Committed, rec1.Status);
        }

        using var coldJournal = new IntentJournal(_journalPath);
        var incomplete = await coldJournal.GetIncompleteIntentsAsync();
        Assert.Empty(incomplete);

        foreach (var id in opIds)
        {
            var rec = await coldJournal.GetIntentAsync(id);
            Assert.NotNull(rec);
            Assert.Equal(IntentStatus.Committed, rec.Status);
        }
    }
}
