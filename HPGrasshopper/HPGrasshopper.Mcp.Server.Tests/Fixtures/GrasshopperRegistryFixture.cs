namespace HPGrasshopper.Mcp.Server.Tests.Fixtures;

using System;
using System.IO;
using HPGrasshopper.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.Mcp.Server.Services;
using HPRebar.Mcp.Server.Tests.Fakes;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

/// <summary>
///     Temp library, temp sqlite db, and named pipe listener with FakeRevitExecutor for headless Grasshopper tests.
/// </summary>
public sealed class GrasshopperRegistryFixture : IAsyncDisposable
{
    public readonly string Root = Path.Combine(Path.GetTempPath(), "hpgrasshopper-test-" + Guid.NewGuid().ToString("N"));
    public readonly FakeRevitExecutor Executor = new();
    public readonly BridgeSettings Settings = new() { ExecutionEnabled = true };
    public readonly RegistryOptions Options;
    public readonly ToolLibraryStore Store;
    public readonly ToolRegistryDb Db;
    public readonly ToolManager Manager;
    private readonly PipeListener _listener;
    private readonly RevitBridgeClient _client;

    public IRevitBridgeClient Bridge => _client;

    public GrasshopperRegistryFixture(Action<RegistryOptions>? configure = null)
    {
        Options = new RegistryOptions
        {
            LibraryPath = Path.Combine(Root, "lib"),
            DbPath = Path.Combine(Root, "registry.db"),
            WatchLibrary = false
        };
        configure?.Invoke(Options);

        var pipe = "hpgrasshopper-mcp-test-" + Guid.NewGuid().ToString("N");
        _listener = new PipeListener(pipe, new RequestDispatcher(Executor, Settings, "2026", "Grasshopper"));
        _listener.Start();

        _client = new RevitBridgeClient(
            Microsoft.Extensions.Options.Options.Create(new BridgeOptions { PipeName = pipe, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 }),
            NullLogger<RevitBridgeClient>.Instance,
            GrasshopperHostProfile.Instance);

        Store = new ToolLibraryStore(Options.LibraryPath, NullLogger<ToolLibraryStore>.Instance);
        Store.EnsureRoot();
        Db = new ToolRegistryDb(Options.DbPath, NullLogger<ToolRegistryDb>.Instance);
        Db.Initialize();
        Manager = new ToolManager(
            Microsoft.Extensions.Options.Options.Create(Options),
            Microsoft.Extensions.Options.Options.Create(new BridgeOptions()),
            Store,
            Db,
            _client,
            NullLogger<ToolManager>.Instance);
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync().ConfigureAwait(false);
        await _listener.StopAsync().ConfigureAwait(false);
        _listener.Dispose();
        Store.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        try
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
        catch (IOException)
        {
            // Allowed: file system locks on temp directory may delay deletion.
        }
    }
}
