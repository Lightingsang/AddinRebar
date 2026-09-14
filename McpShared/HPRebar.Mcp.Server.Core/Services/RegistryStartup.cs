using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HPRebar.Mcp.Server.Services;

/// <summary>
///     Brings the registry up with the process: folders, database, seeds, first load, MCP registration,
///     then the folder watcher that keeps all of it live. Registered before the MCP transport so the
///     published tools are in the collection by the time the first <c>tools/list</c> arrives.
/// </summary>
public sealed class RegistryStartup : IHostedService
{
    private readonly RegistryOptions _options;
    private readonly ToolLibraryStore _store;
    private readonly ToolRegistryDb _db;
    private readonly ToolManager _manager;
    private readonly DynamicToolRegistrar _registrar;
    private readonly ILogger<RegistryStartup> _logger;
    private readonly IHostProfile _profile;
    private int _reloading;

    public RegistryStartup(IOptions<RegistryOptions> options, ToolLibraryStore store, ToolRegistryDb db, ToolManager manager,
        DynamicToolRegistrar registrar, ILogger<RegistryStartup> logger, IHostProfile? profile = null)
    {
        _options = options.Value;
        _store = store;
        _db = db;
        _manager = manager;
        _registrar = registrar;
        _logger = logger;
        _profile = profile ?? HostProfile.Revit;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _store.EnsureRoot();
            _db.Initialize();
            // Seeds are embedded in the host exe, not in this engine assembly.
            if (_options.InstallSeeds)
            {
                if (SeedInstaller.ListSeeds(_profile.HostAssembly).Count == 0)
                    _logger.LogWarning("No seed tools embedded in {Assembly}; the library starts empty", _profile.HostAssembly.GetName().Name);
                SeedInstaller.Install(_store, _logger, _profile.HostAssembly);
            }

            _manager.Changed += () => _registrar.Sync();
            await _manager.LoadAllAsync(cancellationToken).ConfigureAwait(false);

            if (_options.WatchLibrary)
            {
                _store.Changed += OnLibraryChanged;
                _store.StartWatching();
            }

            _logger.LogInformation("Registry ready: {Tools} tools, {Registered} published as MCP tools, FTS5={Fts}, library={Root}",
                _manager.Tools.Count, _registrar.RegisteredNames.Count, _db.HasFullTextSearch, _store.Root);
        }
        catch (Exception exception)
        {
            // The core tools must keep working even when the registry cannot: log and carry on without it.
            _logger.LogError(exception, "Tool registry failed to start; search_tools/run_tool will report the failure");
        }
    }

    private async void OnLibraryChanged()
    {
        if (Interlocked.Exchange(ref _reloading, 1) == 1) return;
        try
        {
            await _manager.LoadAllAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Library reload after a file change failed");
        }
        finally
        {
            Volatile.Write(ref _reloading, 0);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _store.Changed -= OnLibraryChanged;
        _store.Dispose();
        return Task.CompletedTask;
    }
}
