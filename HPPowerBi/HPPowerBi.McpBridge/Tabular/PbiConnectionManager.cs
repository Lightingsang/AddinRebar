using System;
using System.Threading;
using System.Threading.Tasks;
using HPPowerBi.McpBridge.Discovery;
using Microsoft.AnalysisServices.AdomdClient;
using Microsoft.AnalysisServices.Tabular;
using Serilog;

namespace HPPowerBi.McpBridge.Tabular;

/// <summary>
///     Manages active connections to Power BI Desktop's local Analysis Services engine via AMO-TOM
///     (Microsoft.AnalysisServices.Tabular.Server) and ADOMD.NET (AdomdConnection).
/// </summary>
public sealed class PbiConnectionManager : IDisposable
{
    private readonly object _sync = new();
    private Server? _server;
    private AdomdConnection? _adomd;
    private Database? _database;
    private Model? _model;
    private int? _port;
    private PbiInstanceInfo? _activeInstance;

    public bool IsConnected
    {
        get
        {
            lock (_sync)
            {
                return _server is { Connected: true } && _model != null;
            }
        }
    }

    public int? CurrentPort
    {
        get { lock (_sync) return _port; }
    }

    public Server? Server
    {
        get { lock (_sync) return _server; }
    }

    public Database? Database
    {
        get { lock (_sync) return _database; }
    }

    public Model? Model
    {
        get { lock (_sync) return _model; }
    }

    public AdomdConnection? Adomd
    {
        get { lock (_sync) return _adomd; }
    }

    public PbiInstanceInfo? ActiveInstance
    {
        get { lock (_sync) return _activeInstance; }
    }

    public event Action? StateChanged;

    /// <summary>
    ///     Connects to local Analysis Services at localhost:&lt;port&gt;.
    ///     Connects both AMO-TOM and ADOMD.NET.
    /// </summary>
    public async Task ConnectAsync(int port, string? databaseName = null, CancellationToken ct = default)
    {
        if (port is < 1024 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(port), port, "Port must be in valid TCP range [1024..65535].");

        Log.Information("Connecting to local Analysis Services at localhost:{Port}...", port);

        Server? newServer = null;
        AdomdConnection? newAdomd = null;

        try
        {
            // 1. Connect AMO-TOM
            await Task.Run(() =>
            {
                var s = new Server();
                s.Connect($"Data Source=localhost:{port};");
                newServer = s;
            }, ct).ConfigureAwait(false);

            if (newServer == null || newServer.Databases.Count == 0)
                throw new InvalidOperationException($"Connected to localhost:{port}, but no database was found.");

            var db = !string.IsNullOrWhiteSpace(databaseName)
                ? newServer.Databases.Find(databaseName) ?? throw new ArgumentException($"Database '{databaseName}' not found.", nameof(databaseName))
                : newServer.Databases[0];

            var model = db.Model ?? throw new InvalidOperationException($"Database '{db.Name}' has no active Tabular model.");

            // 2. Connect ADOMD.NET for DAX execution
            var adomdConnStr = $"Data Source=localhost:{port};Initial Catalog={db.Name};";
            newAdomd = new AdomdConnection(adomdConnStr);
            await Task.Run(() => newAdomd.Open(), ct).ConfigureAwait(false);

            lock (_sync)
            {
                DisconnectInternal();

                _server = newServer;
                _database = db;
                _model = model;
                _adomd = newAdomd;
                _port = port;
                _activeInstance = new PbiInstanceInfo(
                    ProcessId: 0,
                    WindowTitle: db.Name,
                    ReportName: db.Name,
                    Port: port,
                    DatabaseName: db.Name,
                    DiscoveredAt: DateTime.UtcNow);
            }

            Log.Information("Successfully connected to Power BI model '{Model}' (Compat {Compat}) on port {Port}",
                model.Name, db.CompatibilityLevel, port);

            StateChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to connect to Analysis Services at localhost:{Port}", port);
            try { newAdomd?.Dispose(); } catch { }
            try { newServer?.Dispose(); } catch { }
            throw;
        }
    }

    /// <summary>
    ///     Connects to a discovered Power BI Desktop instance.
    /// </summary>
    public async Task ConnectAsync(PbiInstanceInfo instance, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(instance);

        await ConnectAsync(instance.Port, instance.DatabaseName, ct).ConfigureAwait(false);

        lock (_sync)
        {
            _activeInstance = instance;
        }

        StateChanged?.Invoke();
    }

    /// <summary>
    ///     Disconnects and releases connections.
    /// </summary>
    public void Disconnect()
    {
        lock (_sync)
        {
            DisconnectInternal();
        }
        StateChanged?.Invoke();
    }

    private void DisconnectInternal()
    {
        try { _adomd?.Dispose(); } catch (Exception ex) { Log.Debug(ex, "Error disposing AdomdConnection"); }
        try { _server?.Disconnect(); } catch (Exception ex) { Log.Debug(ex, "Error disconnecting TOM Server"); }
        try { _server?.Dispose(); } catch (Exception ex) { Log.Debug(ex, "Error disposing TOM Server"); }

        _adomd = null;
        _server = null;
        _database = null;
        _model = null;
        _port = null;
        _activeInstance = null;
    }

    public void Dispose()
    {
        Disconnect();
    }
}
