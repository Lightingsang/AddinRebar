namespace HPBim.Client.Connection;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Thread-safe, cross-process append-only JSONL write-ahead journal with hardware disk flushing.
/// </summary>
public sealed partial class IntentJournal : IIntentJournal
{
    private const int DefaultLockTimeoutMilliseconds = 60000;
    private static readonly byte[] NewlineBytes = new byte[] { (byte)'\n' };
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private readonly string _journalPath;
    private readonly bool _flushToDiskImmediately;
    private readonly ConcurrentDictionary<Guid, IntentJournalRecord> _activeRecords = new();
    private long _lastKnownFileLength = -1;
    private DateTime _lastKnownLastWriteTimeUtc = DateTime.MinValue;
    private int _disposed;

    public string JournalFilePath => _journalPath;

    public IntentJournal(string? journalPath = null, bool flushToDiskImmediately = true)
    {
        _journalPath = string.IsNullOrWhiteSpace(journalPath)
            ? Path.Combine(Environment.CurrentDirectory, ".hpbim_journal", "intent_journal.jsonl")
            : journalPath;
        _flushToDiskImmediately = flushToDiskImmediately;
        EnsureDirectoryExists(_journalPath);
    }

    public async Task RecordIntentAsync(IntentJournalRecord record, CancellationToken cancellationToken = default)
    {
        if (record == null)
        {
            throw new ArgumentNullException(nameof(record));
        }
        ThrowIfDisposed();

        await using var fileLock = await AcquireLockAsync(cancellationToken).ConfigureAwait(false);
        await RefreshCacheFromDiskIfNeededAsync(cancellationToken).ConfigureAwait(false);

        _activeRecords[record.OperationId] = record;
        string jsonLine = JsonSerializer.Serialize(record, JsonOptions);
        await AppendLineWithFlushCoreAsync(jsonLine, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateStatusAsync(
        Guid operationId,
        IntentStatus status,
        string? errorReason = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await using var fileLock = await AcquireLockAsync(cancellationToken).ConfigureAwait(false);
        await RefreshCacheFromDiskIfNeededAsync(cancellationToken).ConfigureAwait(false);

        if (!_activeRecords.TryGetValue(operationId, out var existing))
        {
            var diskRecords = await ReadLatestRecordsMapCoreAsync(cancellationToken).ConfigureAwait(false);
            if (diskRecords.TryGetValue(operationId, out var diskRecord))
            {
                existing = diskRecord;
            }
        }

        var updated = existing != null
            ? existing with { Status = status, TimestampUtc = DateTime.UtcNow, ErrorReason = errorReason }
            : new IntentJournalRecord
            {
                OperationId = operationId,
                Status = status,
                TimestampUtc = DateTime.UtcNow,
                ErrorReason = errorReason
            };

        _activeRecords[operationId] = updated;
        string jsonLine = JsonSerializer.Serialize(updated, JsonOptions);
        await AppendLineWithFlushCoreAsync(jsonLine, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<IntentJournalRecord>> GetIncompleteIntentsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await using var fileLock = await AcquireLockAsync(cancellationToken).ConfigureAwait(false);
        await RefreshCacheFromDiskIfNeededAsync(cancellationToken).ConfigureAwait(false);

        return _activeRecords.Values
            .Where(r => r.IsIncomplete)
            .OrderBy(r => r.TimestampUtc)
            .ToList();
    }

    public async Task<IntentJournalRecord?> GetIntentAsync(Guid operationId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await using var fileLock = await AcquireLockAsync(cancellationToken).ConfigureAwait(false);
        await RefreshCacheFromDiskIfNeededAsync(cancellationToken).ConfigureAwait(false);

        return _activeRecords.TryGetValue(operationId, out var record) ? record : null;
    }

    public async Task<int> PruneOldEntriesAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await using var fileLock = await AcquireLockAsync(cancellationToken).ConfigureAwait(false);
        if (!File.Exists(_journalPath))
        {
            return 0;
        }

        await RefreshCacheFromDiskIfNeededAsync(cancellationToken).ConfigureAwait(false);
        var cutoff = DateTime.UtcNow - olderThan;

        var toKeep = _activeRecords.Values
            .Where(r => r.IsIncomplete || r.TimestampUtc > cutoff)
            .OrderBy(r => r.TimestampUtc)
            .ToList();

        int prunedCount = _activeRecords.Count - toKeep.Count;
        if (prunedCount <= 0)
        {
            return 0;
        }

        await RewriteCompactedJournalCoreAsync(toKeep, cancellationToken).ConfigureAwait(false);
        return prunedCount;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed != 0)
        {
            throw new ObjectDisposedException(nameof(IntentJournal));
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }
}
