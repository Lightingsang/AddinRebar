namespace HPBim.Client.Connection;

using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;

/// <summary>
///     Named Pipe client communicating with HPRebar.McpBridge using JSON-RPC 2.0 and NDJSON framing.
/// </summary>
public sealed class RevitPipeClient : IRevitPipeClient
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);

    private readonly RevitConnectionOptions _options;
    private readonly ConcurrentDictionary<long, TaskCompletionSource<JsonRpcEnvelope>> _pendingRequests = new();
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly SemaphoreSlim _connectLock = new(1, 1);

    private NamedPipeClientStream? _pipe;
    private StreamWriter? _writer;
    private PipeMessageReader? _reader;
    private long _nextRequestId;
    private int _disposed;

    public RevitPipeClient(RevitConnectionOptions? options = null)
    {
        _options = options ?? new RevitConnectionOptions();
        _options.Validate();
    }

    public bool IsConnected => _pipe is { IsConnected: true } && _disposed == 0;
    public string PipeName => _options.PipeName;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        try { await _connectLock.WaitAsync(cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException ex)
        {
            throw new RevitPipeException($"Connection to named pipe '{_options.PipeName}' was cancelled.", ex);
        }

        try
        {
            ThrowIfDisposed();
            if (IsConnected) return;

            var pipe = new NamedPipeClientStream(".", _options.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            try
            {
                using var timeoutCts = new CancellationTokenSource(_options.ConnectTimeoutMs);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                await pipe.ConnectAsync(linkedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                await pipe.DisposeAsync().ConfigureAwait(false);
                throw new RevitPipeException($"Connection to pipe '{_options.PipeName}' timed out after {_options.ConnectTimeoutMs}ms.", isTimeout: true);
            }
            catch (Exception ex) when (ex is not RevitPipeException)
            {
                await pipe.DisposeAsync().ConfigureAwait(false);
                throw new RevitPipeException($"Failed to connect to named pipe '{_options.PipeName}': {ex.Message}", ex);
            }

            _pipe = pipe;
            _writer = new StreamWriter(pipe, Utf8NoBom, 8192, leaveOpen: true) { AutoFlush = true, NewLine = "\n" };
            _reader = new PipeMessageReader(pipe, _options.MaxMessageBytes, OnEnvelopeReceived, CancelPendingRequests);
            _reader.Start();
        }
        finally { _connectLock.Release(); }
    }

    public async Task DisconnectAsync()
    {
        await _connectLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_pipe == null) return;
            if (_reader != null) { await _reader.StopAsync().ConfigureAwait(false); _reader = null; }
            CancelPendingRequests(new RevitPipeException("Pipe client disconnected."));
            if (_writer != null) { try { await _writer.DisposeAsync().ConfigureAwait(false); } catch { /* Ignore */ } _writer = null; }
            if (_pipe != null) { try { await _pipe.DisposeAsync().ConfigureAwait(false); } catch { /* Ignore */ } _pipe = null; }
        }
        finally { _connectLock.Release(); }
    }

    public async Task<ExecuteResult> ExecuteAsync(ExecuteRequest request, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        var timeout = TimeSpan.FromSeconds(request.TimeoutSeconds > 0 ? request.TimeoutSeconds : _options.DefaultExecuteTimeoutSeconds);
        var envelope = await SendRequestAsync(JsonRpcMethods.Execute, request, timeout, cancellationToken).ConfigureAwait(false);
        if (envelope.Error != null) return ExecuteResult.Failure($"Revit RPC error {envelope.Error.Code}: {envelope.Error.Message}");
        return envelope.ResultAs<ExecuteResult>() ?? ExecuteResult.Failure("Revit bridge returned an empty execution result.");
    }

    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected) return false;
        try
        {
            var env = await SendRequestAsync(JsonRpcMethods.Ping, new { }, TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
            return env.Error == null;
        }
        catch { return false; }
    }

    public async Task<JsonElement?> GetContextAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        var env = await SendRequestAsync(JsonRpcMethods.Context, new { }, TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
        if (env.Error != null) throw new RevitPipeException($"Context query failed: {env.Error.Message}", env.Error.Code);
        return env.Result;
    }

    public async Task<bool> CancelAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        try
        {
            var env = await SendRequestAsync(JsonRpcMethods.Cancel, new { }, TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
            return env.Error == null;
        }
        catch { return false; }
    }

    private async Task<JsonRpcEnvelope> SendRequestAsync(string method, object? parameters, TimeSpan timeout, CancellationToken cancellationToken)
    {
        long id = Interlocked.Increment(ref _nextRequestId);
        var envelope = JsonRpcEnvelope.Request(id, method, parameters);
        var tcs = new TaskCompletionSource<JsonRpcEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingRequests[id] = tcs;

        try
        {
            await SendLineAsync(envelope, cancellationToken).ConfigureAwait(false);
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            var registration = linkedCts.Token.Register(() =>
            {
                if (cancellationToken.IsCancellationRequested) tcs.TrySetCanceled(cancellationToken);
                else tcs.TrySetException(new RevitPipeException($"Request {id} ({method}) timed out after {timeout.TotalSeconds}s.", isTimeout: true));
            });

            await using (registration.ConfigureAwait(false)) return await tcs.Task.ConfigureAwait(false);
        }
        finally { _pendingRequests.TryRemove(id, out _); }
    }

    private async Task SendLineAsync(JsonRpcEnvelope envelope, CancellationToken cancellationToken)
    {
        var line = BridgeJson.Serialize(envelope);
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var writer = _writer ?? throw new RevitPipeException("Pipe writer is not available.");
            await writer.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {
            CancelPendingRequests(new RevitPipeException("Pipe closed unexpectedly during write.", ex));
            throw new RevitPipeException("Failed to write to named pipe.", ex);
        }
        finally { _writeLock.Release(); }
    }

    private void OnEnvelopeReceived(JsonRpcEnvelope envelope)
    {
        if (envelope.Id is { } id && _pendingRequests.TryGetValue(id, out var tcs)) tcs.TrySetResult(envelope);
    }

    private void CancelPendingRequests(Exception exception)
    {
        foreach (var entry in _pendingRequests)
        {
            if (_pendingRequests.TryRemove(entry.Key, out var tcs)) tcs.TrySetException(exception);
        }
    }

    private void EnsureConnected()
    {
        ThrowIfDisposed();
        if (!IsConnected) throw new RevitPipeException($"Client is not connected to pipe '{_options.PipeName}'.");
    }

    private void ThrowIfDisposed()
    {
        if (_disposed != 0) throw new ObjectDisposedException(nameof(RevitPipeClient));
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        DisconnectAsync().GetAwaiter().GetResult();
        _connectLock.Dispose(); _writeLock.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        await DisconnectAsync().ConfigureAwait(false);
        _connectLock.Dispose(); _writeLock.Dispose();
    }
}
