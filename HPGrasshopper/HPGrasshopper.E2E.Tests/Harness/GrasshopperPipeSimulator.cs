namespace HPGrasshopper.E2E.Tests.Harness;

using System;
using System.Threading.Tasks;
using HPRebar.McpBridge.Core.Pipe;

/// <summary>
/// Headless Named Pipe integration simulator leveraging production PipeListener and RequestDispatcher.
/// </summary>
public sealed class GrasshopperPipeSimulator : IAsyncDisposable
{
    private readonly PipeListener _listener;
    private readonly GrasshopperE2EBridge _bridge;

    public string PipeName => _listener.PipeName;
    public GrasshopperE2EBridge Bridge => _bridge;

    public GrasshopperPipeSimulator(string? pipeName = null, GrasshopperE2EBridge? bridge = null)
    {
        _bridge = bridge ?? new GrasshopperE2EBridge();
        var name = pipeName ?? ("hpgrasshopper-test-" + Guid.NewGuid().ToString("N")[..8]);
        var dispatcher = new RequestDispatcher(_bridge, _bridge.Settings, "2026", "Grasshopper");
        _listener = new PipeListener(name, dispatcher);
    }

    public void Start() => _listener.Start();

    public async ValueTask DisposeAsync()
    {
        await _listener.StopAsync().ConfigureAwait(false);
        _listener.Dispose();
        _bridge.Dispose();
    }
}
