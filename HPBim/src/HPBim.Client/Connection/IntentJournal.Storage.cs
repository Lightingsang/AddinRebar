namespace HPBim.Client.Connection;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

public sealed partial class IntentJournal
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);

    private Task<CrossProcessFileLock> AcquireLockAsync(CancellationToken ct)
    {
        string lockPath = $"{_journalPath}.lock";
        return CrossProcessFileLock.AcquireAsync(lockPath, DefaultLockTimeoutMilliseconds, ct);
    }

    private async Task RewriteCompactedJournalCoreAsync(List<IntentJournalRecord> toKeep, CancellationToken ct)
    {
        string tempFile = $"{_journalPath}.tmp_{Guid.NewGuid():N}";
        try
        {
            await using (var stream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            await using (var writer = new StreamWriter(stream, Utf8NoBom))
            {
                foreach (var rec in toKeep)
                {
                    await writer.WriteLineAsync(JsonSerializer.Serialize(rec, JsonOptions)).ConfigureAwait(false);
                }
                await writer.FlushAsync(ct).ConfigureAwait(false);
                if (_flushToDiskImmediately)
                {
                    stream.Flush(flushToDisk: true);
                }
            }

            File.Move(tempFile, _journalPath, overwrite: true);

            _activeRecords.Clear();
            foreach (var rec in toKeep)
            {
                _activeRecords[rec.OperationId] = rec;
            }

            var fileInfo = new FileInfo(_journalPath);
            _lastKnownFileLength = fileInfo.Length;
            _lastKnownLastWriteTimeUtc = fileInfo.LastWriteTimeUtc;
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch (IOException)
                {
                    // Temporary file cleanup failure tolerance
                }
            }
        }
    }

    private async Task AppendLineWithFlushCoreAsync(string jsonLine, CancellationToken ct)
    {
        await using var stream = new FileStream(
            _journalPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, useAsync: true);

        await EnsureLineBoundaryAsync(stream, ct).ConfigureAwait(false);

        await using var writer = new StreamWriter(stream, Utf8NoBom, 4096, leaveOpen: true);
        await writer.WriteLineAsync(jsonLine).ConfigureAwait(false);
        await writer.FlushAsync(ct).ConfigureAwait(false);

        if (_flushToDiskImmediately)
        {
            stream.Flush(flushToDisk: true);
        }

        _lastKnownFileLength = stream.Length;
        _lastKnownLastWriteTimeUtc = File.GetLastWriteTimeUtc(_journalPath);
    }

    private static async Task EnsureLineBoundaryAsync(FileStream stream, CancellationToken ct)
    {
        if (stream.Length == 0)
        {
            return;
        }

        stream.Seek(-1, SeekOrigin.End);
        int lastByte = stream.ReadByte();
        if (lastByte != '\n' && lastByte != '\r')
        {
            stream.Seek(0, SeekOrigin.End);
            await stream.WriteAsync(NewlineBytes, 0, NewlineBytes.Length, ct).ConfigureAwait(false);
            await stream.FlushAsync(ct).ConfigureAwait(false);
        }
        else
        {
            stream.Seek(0, SeekOrigin.End);
        }
    }

    private async Task RefreshCacheFromDiskIfNeededAsync(CancellationToken ct)
    {
        if (!File.Exists(_journalPath))
        {
            if (_lastKnownFileLength != 0)
            {
                _activeRecords.Clear();
                _lastKnownFileLength = 0;
                _lastKnownLastWriteTimeUtc = DateTime.MinValue;
            }
            return;
        }

        var fileInfo = new FileInfo(_journalPath);
        long currentLength = fileInfo.Length;
        DateTime currentLastWrite = fileInfo.LastWriteTimeUtc;

        if (_lastKnownFileLength == currentLength && _lastKnownLastWriteTimeUtc == currentLastWrite)
        {
            return;
        }

        var latestMap = await ReadLatestRecordsMapCoreAsync(ct).ConfigureAwait(false);
        _activeRecords.Clear();
        foreach (var pair in latestMap)
        {
            _activeRecords[pair.Key] = pair.Value;
        }

        fileInfo.Refresh();
        _lastKnownFileLength = fileInfo.Length;
        _lastKnownLastWriteTimeUtc = fileInfo.LastWriteTimeUtc;
    }

    private async Task<Dictionary<Guid, IntentJournalRecord>> ReadLatestRecordsMapCoreAsync(CancellationToken ct)
    {
        var map = new Dictionary<Guid, IntentJournalRecord>();
        if (!File.Exists(_journalPath))
        {
            return map;
        }

        using var stream = new FileStream(_journalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, useAsync: true);
        using var reader = new StreamReader(stream, Utf8NoBom);

        string? line;
        while ((line = await reader.ReadLineAsync(ct).ConfigureAwait(false)) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var rec = JsonSerializer.Deserialize<IntentJournalRecord>(line, JsonOptions);
                if (rec != null)
                {
                    map[rec.OperationId] = rec;
                }
            }
            catch (JsonException)
            {
                // Corrupted or truncated journal line from abrupt process termination; skip gracefully to preserve undamaged records.
                continue;
            }
        }

        return map;
    }

    private static void EnsureDirectoryExists(string filePath)
    {
        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        opts.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return opts;
    }
}
