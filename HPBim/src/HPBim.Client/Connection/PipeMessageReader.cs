namespace HPBim.Client.Connection;

using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPRebar.Mcp.Contracts.JsonRpc;

/// <summary>
///     Handles background line-by-line reading from the Named Pipe stream and dispatches deserialized JSON-RPC envelopes.
/// </summary>
internal sealed class PipeMessageReader : IDisposable, IAsyncDisposable
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);

    private readonly NamedPipeClientStream _stream;
    private readonly int _maxMessageBytes;
    private readonly Action<JsonRpcEnvelope> _onMessageReceived;
    private readonly Action<Exception> _onClosed;
    private readonly CancellationTokenSource _cts = new();

    private Task? _readTask;
    private int _disposed;

    public PipeMessageReader(
        NamedPipeClientStream stream,
        int maxMessageBytes,
        Action<JsonRpcEnvelope> onMessageReceived,
        Action<Exception> onClosed)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _maxMessageBytes = maxMessageBytes;
        _onMessageReceived = onMessageReceived ?? throw new ArgumentNullException(nameof(onMessageReceived));
        _onClosed = onClosed ?? throw new ArgumentNullException(nameof(onClosed));
    }

    public void Start()
    {
        _readTask = Task.Run(() => ReadLoopAsync(_cts.Token));
    }

    public async Task StopAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

        try { _cts.Cancel(); }
        catch (ObjectDisposedException) { /* CTS already disposed */ }

        if (_readTask != null)
        {
            try { await _readTask.ConfigureAwait(false); }
            catch (Exception) { /* Read task cancellation or exit */ }
            _readTask = null;
        }
        _cts.Dispose();
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        using var reader = new StreamReader(_stream, Utf8NoBom, false, 65536, leaveOpen: true);
        Exception? closeReason = null;

        while (!ct.IsCancellationRequested)
        {
            string? line;
            try
            {
                line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is OperationCanceledException or IOException or ObjectDisposedException)
            {
                closeReason = ex;
                break;
            }

            if (line == null) break;
            if (line.Length == 0 || line.Length > _maxMessageBytes) continue;

            ProcessLine(line);
        }

        _onClosed(closeReason ?? new RevitPipeException("Revit bridge pipe connection closed."));
    }

    private void ProcessLine(string line)
    {
        try
        {
            var envelope = BridgeJson.Deserialize<JsonRpcEnvelope>(line);
            if (envelope != null)
            {
                _onMessageReceived(envelope);
            }
        }
        catch (JsonException)
        {
            // Ignore malformed JSON-RPC frames gracefully without crashing the read loop.
        }
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }
}
