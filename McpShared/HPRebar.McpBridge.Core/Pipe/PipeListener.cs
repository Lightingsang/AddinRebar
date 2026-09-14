using System.IO;
using System.IO.Pipes;
using System.Text;
using Serilog;

namespace HPRebar.McpBridge.Core.Pipe;

/// <summary>
///     Owns the named pipe inside Revit. One server at a time (maxNumberOfServerInstances = 1) so a
///     second Revit of the same version fails fast instead of silently sharing a name. Lines are
///     dispatched fire-and-forget: a `revit.cancel` must be able to overtake the `revit.execute` that is
///     still waiting on the Revit thread.
/// </summary>
public sealed class PipeListener : IDisposable
{
    private const int BufferSize = 65536;
    private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

    private readonly RequestDispatcher _dispatcher;
    private CancellationTokenSource? _cts;
    private Task? _acceptLoop;

    public PipeListener(string pipeName, RequestDispatcher dispatcher)
    {
        PipeName = pipeName;
        _dispatcher = dispatcher;
    }

    public string PipeName { get; }

    public bool IsListening => _cts is { IsCancellationRequested: false };

    public bool HasClient => CurrentWriter is { IsFaulted: false };

    /// <summary>Outbound side of the live connection, for status notifications; null when nobody is connected.</summary>
    public NdjsonPipeWriter? CurrentWriter { get; private set; }

    /// <summary>Listening / client connected / client gone. Raised on a thread-pool thread.</summary>
    public event Action? StateChanged;

    /// <summary>The listener died and will not come back without a restart; the argument is the reason.</summary>
    public event Action<string>? Faulted;

    public void Start()
    {
        if (IsListening) return;

        _cts = new CancellationTokenSource();
        _acceptLoop = Task.Run(() => AcceptLoopAsync(_cts.Token));
        StateChanged?.Invoke();
    }

    public async Task StopAsync()
    {
        var cts = _cts;
        if (cts is null) return;

        _cts = null;
        cts.Cancel();

        if (_acceptLoop is not null)
        {
            try { await _acceptLoop.ConfigureAwait(false); }
            catch { /* loop reports its own failures */ }
        }

        cts.Dispose();
        StateChanged?.Invoke();
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            NamedPipeServerStream stream;
            try
            {
                // CurrentUserOnly = ACL for the current Windows user only; no other account can connect.
                stream = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly, BufferSize, BufferSize);
            }
            catch (Exception exception)
            {
                // IOException "All pipe instances are busy" = another instance of this host version already owns the
                // name; anything else is a platform/ACL problem. Either way the loop cannot continue, and a
                // silent death here would look exactly like "host not running" from the server's side.
                Log.Error(exception, "MCP bridge could not create pipe {Pipe}", PipeName);
                _cts?.Cancel(); // IsListening must read false: nothing will accept until Start() is called again
                Faulted?.Invoke(exception is IOException
                    ? $"Pipe {PipeName} is already in use — another {_dispatcher.HostName} {_dispatcher.HostVersion} instance is serving MCP."
                    : $"Pipe {PipeName} could not be created: {exception.GetType().Name}: {exception.Message}");
                return;
            }

            Log.Information("MCP bridge listening on pipe {Pipe}", PipeName);

            try
            {
                await stream.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                await ServeClientAsync(stream, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // stopping
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "MCP bridge connection ended with an error");
            }
            finally
            {
                CurrentWriter = null; // the writer itself is disposed by ServeClientAsync's using
                stream.Dispose();
                StateChanged?.Invoke();
            }
        }
    }

    private async Task ServeClientAsync(NamedPipeServerStream stream, CancellationToken cancellationToken)
    {
        Log.Information("MCP server connected on {Pipe}", PipeName);

        using var writer = new NdjsonPipeWriter(stream);
        CurrentWriter = writer;
        StateChanged?.Invoke();

        using var reader = new StreamReader(stream, Utf8NoBom, detectEncodingFromByteOrderMarks: false, BufferSize, leaveOpen: true);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);

            if (line is null) break; // server closed its end

            if (line.Length == 0) continue;

            _ = _dispatcher.HandleLineAsync(line, writer, cancellationToken)
                .ContinueWith(t => Log.Error(t.Exception, "MCP bridge dispatcher crashed"), TaskContinuationOptions.OnlyOnFaulted);
        }

        Log.Information("MCP server disconnected from {Pipe}", PipeName);
    }

    public void Dispose()
    {
        _cts?.Cancel();
    }
}
