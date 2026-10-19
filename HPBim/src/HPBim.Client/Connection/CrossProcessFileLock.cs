namespace HPBim.Client.Connection;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Cross-process file lock using an exclusive FileStream on a companion .lock file.
/// Compatible with netstandard2.0 and modern .NET runtimes.
/// </summary>
public sealed class CrossProcessFileLock : IDisposable, IAsyncDisposable
{
    private readonly FileStream _lockStream;
    private int _disposed;

    private CrossProcessFileLock(FileStream lockStream)
    {
        _lockStream = lockStream;
    }

    public static async Task<CrossProcessFileLock> AcquireAsync(
        string lockFilePath,
        int timeoutMilliseconds = 60000,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lockFilePath))
        {
            throw new ArgumentException("Lock file path cannot be null or whitespace.", nameof(lockFilePath));
        }

        EnsureDirectoryExists(lockFilePath);
        var stopwatch = Stopwatch.StartNew();
        int spinCount = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var stream = new FileStream(
                    lockFilePath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    1,
                    FileOptions.None);

                return new CrossProcessFileLock(stream);
            }
            catch (IOException)
            {
                if (stopwatch.ElapsedMilliseconds >= timeoutMilliseconds)
                {
                    throw new TimeoutException(
                        $"Failed to acquire cross-process file lock '{lockFilePath}' within {timeoutMilliseconds}ms.");
                }

                spinCount++;
                await BackoffAsync(spinCount, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static void EnsureDirectoryExists(string lockFilePath)
    {
        string? directory = Path.GetDirectoryName(lockFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static async Task BackoffAsync(int spinCount, CancellationToken cancellationToken)
    {
        if (spinCount <= 3)
        {
            await Task.Yield();
        }
        else
        {
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _lockStream.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }
}
