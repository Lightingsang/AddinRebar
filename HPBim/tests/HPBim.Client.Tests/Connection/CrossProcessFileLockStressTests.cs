namespace HPBim.Client.Tests.Connection;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Core.Execution;
using Xunit;

public sealed class CrossProcessFileLockStressTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _lockPath;
    private readonly string _journalPath;

    public CrossProcessFileLockStressTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "hpbim_lock_stress_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _lockPath = Path.Combine(_tempDir, "stress.lock");
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
    public async Task AcquireAsync_ZeroTimeoutWhenLocked_ThrowsTimeoutExceptionImmediately()
    {
        await using var l1 = await CrossProcessFileLock.AcquireAsync(_lockPath);

        await Assert.ThrowsAsync<TimeoutException>(async () =>
        {
            await CrossProcessFileLock.AcquireAsync(_lockPath, timeoutMilliseconds: 0);
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AcquireAsync_InvalidPaths_ThrowsArgumentException(string? invalidPath)
    {
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await CrossProcessFileLock.AcquireAsync(invalidPath!);
        });
    }

    [Fact]
    public async Task AcquireAsync_NestedDirectoryAutoCreation_CreatesDirectoryHierarchy()
    {
        string deepLockPath = Path.Combine(_tempDir, "level1", "level2", "level3", "deep.lock");
        Assert.False(Directory.Exists(Path.GetDirectoryName(deepLockPath)));

        await using (var l = await CrossProcessFileLock.AcquireAsync(deepLockPath))
        {
            Assert.NotNull(l);
            Assert.True(File.Exists(deepLockPath));
        }

        Assert.True(Directory.Exists(Path.GetDirectoryName(deepLockPath)));
    }

    [Fact]
    public async Task MultiThreadedSharedFileProtection_16WorkersWriting1600TotalLines_ZeroDataCorruption()
    {
        string sharedDataFile = Path.Combine(_tempDir, "shared_data.txt");
        int workerCount = 16;
        int linesPerWorker = 100;
        var expectedLines = new HashSet<string>();

        var workers = Enumerable.Range(0, workerCount).Select(async workerId =>
        {
            for (int i = 0; i < linesPerWorker; i++)
            {
                string line = $"{workerId}_{i}_{Guid.NewGuid():N}";
                lock (expectedLines)
                {
                    expectedLines.Add(line);
                }

                await using (await CrossProcessFileLock.AcquireAsync(_lockPath, timeoutMilliseconds: 30000))
                {
                    await using var stream = new FileStream(
                        sharedDataFile, FileMode.Append, FileAccess.Write, FileShare.None, 4096, useAsync: true);
                    await using var writer = new StreamWriter(stream);
                    await writer.WriteLineAsync(line);
                    await writer.FlushAsync();
                }
            }
        });

        await Task.WhenAll(workers);

        string[] writtenLines = await File.ReadAllLinesAsync(sharedDataFile);
        Assert.Equal(workerCount * linesPerWorker, writtenLines.Length);
        Assert.Equal(expectedLines.Count, writtenLines.Length);
        Assert.True(expectedLines.SetEquals(writtenLines));
    }

    [Fact]
    public async Task IntentJournal_ZeroByteJournalFile_HandlesGracefully()
    {
        await File.WriteAllBytesAsync(_journalPath, Array.Empty<byte>());
        Assert.True(File.Exists(_journalPath));
        Assert.Equal(0, new FileInfo(_journalPath).Length);

        using var journal = new IntentJournal(_journalPath);
        var incomplete = await journal.GetIncompleteIntentsAsync();
        Assert.Empty(incomplete);

        var opId = Guid.NewGuid();
        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            Status = IntentStatus.InFlight,
            PlanHash = "hash-zero-byte"
        });

        var retrieved = await journal.GetIntentAsync(opId);
        Assert.NotNull(retrieved);
        Assert.Equal(IntentStatus.InFlight, retrieved.Status);
    }
}
