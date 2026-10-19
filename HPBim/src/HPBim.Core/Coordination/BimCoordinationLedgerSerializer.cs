namespace HPBim.Core.Coordination;

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// JSON serializer for BimCoordinationLedger using System.Text.Json.
/// Supports both synchronous and asynchronous operations.
/// </summary>
public static class BimCoordinationLedgerSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = CreateOptions(true);
    private static readonly JsonSerializerOptions CompactOptions = CreateOptions(false);

    public static JsonSerializerOptions CreateOptions(bool indented)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = indented,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new CompositeSourceKeyJsonConverter());
        options.Converters.Add(new CoordinationCategoryJsonConverter());
        options.Converters.Add(new RfiStatusJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    public static string Serialize(BimCoordinationLedger ledger, bool indented = true)
    {
        if (ledger == null)
        {
            throw new ArgumentNullException(nameof(ledger));
        }

        return JsonSerializer.Serialize(ledger, indented ? DefaultOptions : CompactOptions);
    }

    public static BimCoordinationLedger Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON content cannot be null or whitespace.", nameof(json));
        }

        var ledger = JsonSerializer.Deserialize<BimCoordinationLedger>(json, DefaultOptions);
        return ledger ?? throw new JsonException("Deserialization returned null BimCoordinationLedger.");
    }

    public static void SerializeToFile(BimCoordinationLedger ledger, string filePath, bool indented = true)
    {
        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string lockPath = $"{filePath}.lock";
        using var lockStream = AcquireFileLock(lockPath, 5000);

        string tempPath = $"{filePath}.tmp_{Guid.NewGuid():N}";
        try
        {
            string json = Serialize(ledger, indented);
            using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(json);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }

            File.Move(tempPath, filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); }
                catch (IOException) { /* File might be locked or already cleaned */ }
            }
        }
    }

    public static BimCoordinationLedger DeserializeFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Ledger JSON file not found at: {filePath}", filePath);
        }

        string json = ReadFileWithRetry(filePath, 3000);
        return Deserialize(json);
    }

    public static async Task SerializeToFileAsync(
        BimCoordinationLedger ledger, string filePath, bool indented = true, CancellationToken ct = default)
    {
        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string lockPath = $"{filePath}.lock";
        using var lockStream = await AcquireFileLockAsync(lockPath, 5000, ct).ConfigureAwait(false);

        string tempPath = $"{filePath}.tmp_{Guid.NewGuid():N}";
        try
        {
            string json = Serialize(ledger, indented);
            using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            using (var writer = new StreamWriter(stream))
            {
                await writer.WriteAsync(json).ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            File.Move(tempPath, filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); }
                catch (IOException) { /* File might be locked or already cleaned */ }
            }
        }
    }

    public static async Task<BimCoordinationLedger> DeserializeFromFileAsync(
        string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Ledger JSON file not found at: {filePath}", filePath);
        }

        string json = await ReadFileWithRetryAsync(filePath, 3000, ct).ConfigureAwait(false);
        return Deserialize(json);
    }

    private static FileStream AcquireFileLock(string lockFilePath, int timeoutMs)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (true)
        {
            try
            {
                return new FileStream(lockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
            }
            catch (IOException)
            {
                if (sw.ElapsedMilliseconds >= timeoutMs)
                {
                    throw new TimeoutException($"Failed to acquire ledger file lock on '{lockFilePath}' within {timeoutMs}ms.");
                }
                Thread.Sleep(20);
            }
        }
    }

    private static async Task<FileStream> AcquireFileLockAsync(string lockFilePath, int timeoutMs, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                return new FileStream(lockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
            }
            catch (IOException)
            {
                if (sw.ElapsedMilliseconds >= timeoutMs)
                {
                    throw new TimeoutException($"Failed to acquire ledger file lock on '{lockFilePath}' within {timeoutMs}ms.");
                }
                await Task.Delay(20, ct).ConfigureAwait(false);
            }
        }
    }

    private static string ReadFileWithRetry(string filePath, int timeoutMs)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (true)
        {
            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            catch (IOException)
            {
                if (sw.ElapsedMilliseconds >= timeoutMs) throw;
                Thread.Sleep(20);
            }
        }
    }

    private static async Task<string> ReadFileWithRetryAsync(string filePath, int timeoutMs, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, useAsync: true);
                using var reader = new StreamReader(stream);
                return await reader.ReadToEndAsync().ConfigureAwait(false);
            }
            catch (IOException)
            {
                if (sw.ElapsedMilliseconds >= timeoutMs) throw;
                await Task.Delay(20, ct).ConfigureAwait(false);
            }
        }
    }
}
