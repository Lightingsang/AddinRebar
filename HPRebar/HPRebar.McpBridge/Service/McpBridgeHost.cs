using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.ViewModel;
using Serilog;

namespace HPRebar.McpBridge.Service;

/// <summary>
///     The bridge's state machine: owns the listener, the settings and the last-run record, and is the one
///     object both the ribbon window and the executor talk to. Created once in Application.OnStartup and
///     kept in <see cref="Current"/> because the command that opens the window has no other way to find it.
/// </summary>
public sealed class McpBridgeHost : IMcpBridgeRunner, IDisposable
{
    private readonly IRevitExecutor _executor;
    private readonly BridgeSettings _settings;
    private readonly PipeListener _listener;
    private string? _statusMessage;

    public McpBridgeHost(IRevitExecutor executor, BridgeSettings settings, string revitVersion)
    {
        _executor = executor;
        _settings = settings;
        RevitVersion = revitVersion;
        PipeName = PipeNaming.For(int.Parse(revitVersion));

        _listener = new PipeListener(PipeName, new RequestDispatcher(executor, settings, revitVersion));
        _listener.StateChanged += OnListenerStateChanged;
        _listener.Faulted += OnListenerFaulted;
        _executor.StateChanged += OnListenerStateChanged;
        _executor.RunCompleted += RecordRun;
    }

    public static McpBridgeHost? Current { get; private set; }

    /// <summary>Called once from Application.OnStartup; a second call replaces (and disposes) the first.</summary>
    public static void Install(McpBridgeHost host)
    {
        Current?.Dispose();
        Current = host;
    }

    public string PipeName { get; }

    public string RevitVersion { get; }

    public string AuditDirectory => BridgeSettingsStore.AuditDirectory;

    public BridgeStatus Status { get; private set; } = BridgeStatus.Stopped;

    public string? StatusMessage => _statusMessage;

    public bool HasClient => _listener.HasClient;

    public bool ExecutionEnabled
    {
        get => _settings.ExecutionEnabled;
        set
        {
            if (_settings.ExecutionEnabled == value) return;

            _settings.ExecutionEnabled = value;
            Log.Information("MCP bridge code execution {State}", value ? "enabled" : "disabled");
            PublishStatus();
        }
    }

    public bool AutoStartListener
    {
        get => _settings.AutoStartListener;
        set
        {
            if (_settings.AutoStartListener == value) return;

            _settings.AutoStartListener = value;
            BridgeSettingsStore.Save(_settings);
            StateChanged?.Invoke();
        }
    }

    public LastRunInfo? LastRun { get; private set; }

    public int CompiledScriptCount => _executor.CompiledScriptCount;

    public event Action? StateChanged;

    public void Start()
    {
        _statusMessage = null;
        Log.Information("MCP bridge listener start requested on {Pipe}", PipeName);
        _listener.Start();
    }

    /// <summary>Never blocks the caller (the Revit UI thread); StateChanged reports when the pipe is really gone.</summary>
    public void Stop()
    {
        _ = _listener.StopAsync()
            .ContinueWith(t => Log.Warning(t.Exception, "MCP bridge listener stop failed"), TaskContinuationOptions.OnlyOnFaulted);
    }

    public void Restart()
    {
        _ = RestartCoreAsync();
    }

    private async Task RestartCoreAsync()
    {
        try
        {
            await _listener.StopAsync().ConfigureAwait(false);
            Start();
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "MCP bridge listener restart failed");
        }
    }

    /// <summary>The executor reports each finished script here so the window and the server both see it.</summary>
    public void RecordRun(LastRunInfo run)
    {
        LastRun = run;
        PublishStatus();
    }

    private void OnListenerStateChanged() => PublishStatus();

    private void OnListenerFaulted(string reason)
    {
        _statusMessage = reason;
        PublishStatus();
    }

    private void PublishStatus()
    {
        Status = ComputeStatus();

        _listener.CurrentWriter?.Post(JsonRpcEnvelope.Notification(JsonRpcMethods.StatusNotification,
            new StatusParams(_listener.IsListening, _settings.ExecutionEnabled, _executor.IsBusy, _executor.ActiveDocumentTitle, RevitVersion)));

        StateChanged?.Invoke();
    }

    private BridgeStatus ComputeStatus()
    {
        if (_statusMessage is not null && !_listener.IsListening) return BridgeStatus.Error;
        if (!_listener.IsListening) return BridgeStatus.Stopped;
        if (!_listener.HasClient) return BridgeStatus.Listening;

        return _executor.IsBusy ? BridgeStatus.Busy : BridgeStatus.Connected;
    }

    public void Dispose()
    {
        _listener.StateChanged -= OnListenerStateChanged;
        _listener.Faulted -= OnListenerFaulted;
        _executor.StateChanged -= OnListenerStateChanged;
        _executor.RunCompleted -= RecordRun;
        _listener.Dispose();

        if (ReferenceEquals(Current, this)) Current = null;
    }
}
