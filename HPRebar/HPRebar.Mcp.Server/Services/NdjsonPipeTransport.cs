using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using HPRebar.Mcp.Contracts.JsonRpc;
using Microsoft.Extensions.Logging;

namespace HPRebar.Mcp.Server.Services;

/// <summary>
///     One connection to the bridge's named pipe: newline-delimited JSON in both directions. Owns the
///     read loop and serializes writes so a progress notification arriving from Revit can never be
///     interleaved with a request the server is sending. Throws away nothing silently: a broken line
///     or an oversized message ends the connection and raises <see cref="Disconnected"/>.
/// </summary>
public sealed class NdjsonPipeTransport : IAsyncDisposable
{
    private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

    private readonly string _pipeName;
    private readonly int _maxMessageBytes;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
    private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();

    private NamedPipeClientStream? _pipe;
    private StreamWriter? _writer;
    private Task? _readLoop;
    private int _disconnectSignalled;
    private int _disposed;

    public NdjsonPipeTransport(string pipeName, int maxMessageBytes, ILogger logger)
    {
        _pipeName = pipeName;
        _maxMessageBytes = maxMessageBytes;
        _logger = logger;
    }

    public bool IsConnected => _pipe is { IsConnected: true } && _disconnectSignalled == 0;

    public event Action<JsonRpcEnvelope>? MessageReceived;

    /// <summary>Raised once per connection, from the read loop, with the failure that ended it (null on a clean close).</summary>
    public event Action<Exception?>? Disconnected;

    public async Task ConnectAsync(int timeoutMs, CancellationToken cancellationToken)
    {
        // CurrentUserOnly makes the client refuse a pipe not owned by the same Windows user, mirroring the
        // ACL the bridge puts on the server end.
        var pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

        try
        {
            await pipe.ConnectAsync(timeoutMs, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await pipe.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        _pipe = pipe;
        _writer = new StreamWriter(pipe, Utf8NoBom, 8192, leaveOpen: true) { AutoFlush = true, NewLine = "\n" };
        _readLoop = Task.Run(() => ReadLoopAsync(pipe, _lifetime.Token));
    }

    public async Task SendAsync(JsonRpcEnvelope envelope, CancellationToken cancellationToken)
    {
        var writer = _writer ?? throw new BridgeUnavailableException("Pipe is not connected.");
        var line = BridgeJson.Serialize(envelope);

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await writer.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException)
        {
            SignalDisconnect(exception);
            throw new BridgeUnavailableException("Pipe write failed; the bridge went away.", exception);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task ReadLoopAsync(NamedPipeClientStream pipe, CancellationToken cancellationToken)
    {
        Exception? failure = null;

        try
        {
            using var reader = new StreamReader(pipe, Utf8NoBom, detectEncodingFromByteOrderMarks: false, 65536, leaveOpen: true);

            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);

                if (line is null) break; // bridge closed its end

                if (line.Length == 0) continue;

                if (line.Length > _maxMessageBytes)
                {
                    failure = new InvalidDataException($"Bridge sent a {line.Length:N0}-byte message; limit is {_maxMessageBytes:N0}.");
                    break;
                }

                JsonRpcEnvelope? envelope;
                try
                {
                    envelope = BridgeJson.Deserialize<JsonRpcEnvelope>(line);
                }
                catch (JsonException exception)
                {
                    failure = new InvalidDataException("Bridge sent a line that is not valid JSON-RPC.", exception);
                    break;
                }

                if (envelope is not null) MessageReceived?.Invoke(envelope);
            }
        }
        catch (OperationCanceledException)
        {
            // disposing
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        SignalDisconnect(failure);
    }

    private void SignalDisconnect(Exception? failure)
    {
        if (Interlocked.Exchange(ref _disconnectSignalled, 1) != 0) return;

        if (failure is not null) _logger.LogWarning(failure, "Bridge pipe {Pipe} disconnected", _pipeName);
        else _logger.LogInformation("Bridge pipe {Pipe} closed", _pipeName);

        Disconnected?.Invoke(failure);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

        _lifetime.Cancel();

        if (_pipe is not null) await _pipe.DisposeAsync().ConfigureAwait(false);

        if (_readLoop is not null)
        {
            try { await _readLoop.ConfigureAwait(false); }
            catch { /* read loop already reported through Disconnected */ }
        }

        _writeLock.Dispose();
        _lifetime.Dispose();
    }
}
