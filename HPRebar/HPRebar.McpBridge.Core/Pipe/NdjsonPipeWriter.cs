using System.IO;
using System.IO.Pipes;
using System.Text;
using HPRebar.Mcp.Contracts.JsonRpc;
using Serilog;

namespace HPRebar.McpBridge.Core.Pipe;

/// <summary>
///     The bridge's outbound half of one pipe connection. One lock, one line per write: a progress
///     notification raised from the Revit thread can never be spliced into a response being written from
///     the pipe thread. Write failures mean the server went away; they are logged, not thrown, because
///     the read loop reports the disconnect on its own.
/// </summary>
public sealed class NdjsonPipeWriter : IDisposable
{
    private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

    private readonly StreamWriter _writer;
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
    private bool _faulted;

    public NdjsonPipeWriter(NamedPipeServerStream stream)
    {
        _writer = new StreamWriter(stream, Utf8NoBom, 8192, leaveOpen: true) { AutoFlush = true, NewLine = "\n" };
    }

    public bool IsFaulted => _faulted;

    public async Task WriteAsync(JsonRpcEnvelope envelope, CancellationToken cancellationToken)
    {
        if (_faulted) return;

        var line = BridgeJson.Serialize(envelope);

        try
        {
            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            return; // connection already torn down while this reply was being produced
        }

        try
        {
            await _writer.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException)
        {
            _faulted = true;
            Log.Debug(exception, "MCP bridge pipe write failed; server disconnected");
        }
        finally
        {
            try { _lock.Release(); } catch (ObjectDisposedException) { /* disposed under us */ }
        }
    }

    /// <summary>Fire-and-forget for notifications raised where nobody can await (Revit thread callbacks).</summary>
    public void Post(JsonRpcEnvelope envelope)
    {
        _ = WriteAsync(envelope, CancellationToken.None)
            .ContinueWith(t => Log.Debug(t.Exception, "MCP bridge notification dropped"), TaskContinuationOptions.OnlyOnFaulted);
    }

    public void Dispose()
    {
        _faulted = true;
        _writer.Dispose();
        _lock.Dispose();
    }
}
