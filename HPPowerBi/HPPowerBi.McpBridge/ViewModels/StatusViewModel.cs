using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPPowerBi.McpBridge.Cloud;
using HPPowerBi.McpBridge.Discovery;
using HPPowerBi.McpBridge.ExternalTools;
using HPPowerBi.McpBridge.Safety;
using HPPowerBi.McpBridge.Tabular;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;
using Serilog;

namespace HPPowerBi.McpBridge.ViewModels;

/// <summary>
///     View model for the Power BI MCP Bridge status and management window.
///     Coordinates connection state, running PBIDesktop instances, model statistics,
///     safety opt-in gates (DAX execution and TMSL mutation), cloud configuration,
///     and named pipe listener monitoring.
/// </summary>
public sealed partial class StatusViewModel : ObservableObject
{
    private readonly PbiConnectionManager _connectionManager;
    private readonly PbiSafetyGuard _guard;
    private readonly PbiSnapshotManager _snapshotManager;
    private readonly McpBridgeHost? _host;
    private readonly PowerBiCloudClient? _cloudClient;
    private readonly Action<Action> _onUiThread;

    #region Observable Properties

    // Connection state
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    private bool _isConnected;

    [ObservableProperty] private string _connectionStatusText = "Disconnected — select an instance and click Connect";
    [ObservableProperty] private string? _activeReportName;
    [ObservableProperty] private int? _attachedPid;
    [ObservableProperty] private int _localPort;
    [ObservableProperty] private string? _databaseName;

    // Model stats
    [ObservableProperty] private int _tableCount;
    [ObservableProperty] private int _measureCount;
    [ObservableProperty] private int _relationshipCount;

    // Safety controls
    [ObservableProperty] private bool _isExecutionEnabled;
    [ObservableProperty] private bool _isMutationEnabled;
    [ObservableProperty] private bool _canEnableMutation;

    // Cloud status
    [ObservableProperty] private bool _isCloudConfigured;
    [ObservableProperty] private string _cloudStatusText = "Not configured";

    // Pipe listener status
    [ObservableProperty] private string _pipeStatusText = "Listening on hppowerbi-mcp-2026";
    [ObservableProperty] private bool _isListening;

    // Instance selector
    [ObservableProperty] private ObservableCollection<PbiInstanceInfo> _availableInstances = new();
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    private PbiInstanceInfo? _selectedInstance;

    // General status & paths
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private string _snapshotDirectory = string.Empty;
    [ObservableProperty] private string _logDirectory = string.Empty;

    #endregion

    public StatusViewModel(
        PbiConnectionManager connectionManager,
        PbiSafetyGuard guard,
        PbiSnapshotManager snapshotManager,
        McpBridgeHost? host = null,
        PowerBiCloudClient? cloudClient = null,
        string? logDirectory = null,
        Action<Action>? onUiThread = null)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _guard = guard ?? throw new ArgumentNullException(nameof(guard));
        _snapshotManager = snapshotManager ?? throw new ArgumentNullException(nameof(snapshotManager));
        _host = host;
        _cloudClient = cloudClient;
        _onUiThread = onUiThread ?? (action => action());

        SnapshotDirectory = _snapshotManager.SnapshotDirectory;
        LogDirectory = logDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HPPowerBi", "McpBridge", "logs");

        // Sync initial safety values
        IsExecutionEnabled = _guard.IsExecutionEnabled;
        IsMutationEnabled = _guard.IsMutationEnabled;
        CanEnableMutation = IsExecutionEnabled;

        // Subscribe to engine state changes
        _connectionManager.StateChanged += OnConnectionStateChanged;
        _guard.StateChanged += OnGuardStateChanged;

        if (_host != null)
        {
            _host.StateChanged += OnHostStateChanged;
            UpdateHostStatus();
        }

        CheckCloudStatus();
        UpdateFromConnection();
    }

    private bool CanConnect => SelectedInstance != null && !IsConnected;
    private bool CanDisconnect => IsConnected;

    #region Commands

    [RelayCommand]
    public void RefreshInstances()
    {
        try
        {
            var instances = PbiProcessDetector.DetectInstances();
            AvailableInstances.Clear();
            foreach (var instance in instances)
            {
                AvailableInstances.Add(instance);
            }

            if (SelectedInstance == null || !AvailableInstances.Contains(SelectedInstance))
            {
                SelectedInstance = AvailableInstances.FirstOrDefault();
            }

            if (AvailableInstances.Count == 0 && !IsConnected)
            {
                ConnectionStatusText = "No running Power BI Desktop instances found. Open a report and click Refresh.";
            }

            StatusMessage = $"Found {AvailableInstances.Count} running Power BI Desktop instance(s).";
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error refreshing Power BI Desktop instances");
            StatusMessage = $"Error finding instances: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanConnect))]
    public async Task ConnectAsync()
    {
        if (SelectedInstance == null) return;

        try
        {
            ConnectionStatusText = $"Connecting to {SelectedInstance.ReportName} on port {SelectedInstance.Port}...";
            await _connectionManager.ConnectAsync(SelectedInstance).ConfigureAwait(false);
            _onUiThread(() =>
            {
                UpdateFromConnection();
                StatusMessage = $"Connected to '{SelectedInstance.ReportName}' on port {SelectedInstance.Port}.";
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to connect to Power BI Desktop instance on port {Port}", SelectedInstance.Port);
            _onUiThread(() =>
            {
                ConnectionStatusText = $"Connection failed: {ex.Message}";
                StatusMessage = $"Failed to connect: {ex.Message}";
            });
        }
    }

    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    public void Disconnect()
    {
        _connectionManager.Disconnect();
        UpdateFromConnection();
        StatusMessage = "Disconnected from Power BI Desktop.";
    }

    [RelayCommand]
    public void RegisterExternalTool()
    {
        try
        {
            var result = ExternalToolsRegistrar.Register();
            if (result.Success)
            {
                StatusMessage = $"Successfully registered External Tool in Power BI Desktop at:\n{result.FilePath}";
            }
            else
            {
                StatusMessage = $"Failed to register External Tool: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to register external tool definition");
            StatusMessage = $"Error registering external tool: {ex.Message}";
        }
    }

    [RelayCommand]
    public void RestoreSnapshot()
    {
        try
        {
            if (!Directory.Exists(SnapshotDirectory))
            {
                StatusMessage = "No snapshots directory exists.";
                return;
            }

            var files = Directory.GetFiles(SnapshotDirectory, "Snapshot_*.json")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTimeUtc)
                .ToList();

            if (files.Count == 0)
            {
                StatusMessage = "No snapshot files found in snapshots directory.";
                return;
            }

            var latest = files[0];
            var db = _snapshotManager.RestoreSnapshot(latest.FullName);
            StatusMessage = $"Successfully restored snapshot '{latest.Name}' (Database '{db.Name}').";
            UpdateFromConnection();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error restoring snapshot");
            StatusMessage = $"Snapshot restore failed: {ex.Message}";
        }
    }

    [RelayCommand]
    public void ToggleListener()
    {
        if (_host == null) return;

        if (_host.Status == BridgeStatus.Stopped)
        {
            _host.Start();
        }
        else
        {
            _host.Stop();
        }
    }

    [RelayCommand]
    public void OpenSnapshotFolder()
    {
        try
        {
            Directory.CreateDirectory(SnapshotDirectory);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{SnapshotDirectory}\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not open snapshot folder: {ex.Message}";
        }
    }

    [RelayCommand]
    public void OpenLogFolder()
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{LogDirectory}\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not open log folder: {ex.Message}";
        }
    }

    #endregion

    #region Property Change Handlers

    partial void OnIsExecutionEnabledChanged(bool value)
    {
        if (_guard.IsExecutionEnabled != value)
        {
            _guard.IsExecutionEnabled = value;
        }

        if (_host != null && _host.ExecutionEnabled != value)
        {
            _host.ExecutionEnabled = value;
        }

        CanEnableMutation = value;
        if (!value && IsMutationEnabled)
        {
            IsMutationEnabled = false;
        }
    }

    partial void OnIsMutationEnabledChanged(bool value)
    {
        if (value && !IsExecutionEnabled)
        {
            IsMutationEnabled = false;
            return;
        }

        if (_guard.IsMutationEnabled != value)
        {
            _guard.IsMutationEnabled = value;
        }
    }

    #endregion

    #region State Synchronizers

    private void OnConnectionStateChanged() => _onUiThread(UpdateFromConnection);

    private void OnGuardStateChanged()
    {
        _onUiThread(() =>
        {
            if (IsExecutionEnabled != _guard.IsExecutionEnabled)
                IsExecutionEnabled = _guard.IsExecutionEnabled;
            if (IsMutationEnabled != _guard.IsMutationEnabled)
                IsMutationEnabled = _guard.IsMutationEnabled;
        });
    }

    private void OnHostStateChanged() => _onUiThread(UpdateHostStatus);

    private void UpdateFromConnection()
    {
        IsConnected = _connectionManager.IsConnected;
        if (IsConnected)
        {
            var active = _connectionManager.ActiveInstance;
            ActiveReportName = active?.ReportName ?? _connectionManager.Database?.Name ?? "Model";
            AttachedPid = active?.ProcessId;
            LocalPort = _connectionManager.CurrentPort ?? 0;
            DatabaseName = _connectionManager.Database?.Name;

            var model = _connectionManager.Model;
            TableCount = model?.Tables.Count ?? 0;
            MeasureCount = model != null ? model.Tables.Sum(t => t.Measures.Count) : 0;
            RelationshipCount = model?.Relationships.Count ?? 0;

            ConnectionStatusText = $"Connected to {ActiveReportName} (Port {LocalPort})";
        }
        else
        {
            ActiveReportName = null;
            AttachedPid = null;
            LocalPort = 0;
            DatabaseName = null;
            TableCount = 0;
            MeasureCount = 0;
            RelationshipCount = 0;
            ConnectionStatusText = "Disconnected";
        }

        ConnectCommand.NotifyCanExecuteChanged();
        DisconnectCommand.NotifyCanExecuteChanged();
    }

    private void UpdateHostStatus()
    {
        if (_host == null) return;

        IsListening = _host.Status is BridgeStatus.Listening or BridgeStatus.Connected or BridgeStatus.Busy;
        PipeStatusText = _host.Status switch
        {
            BridgeStatus.Listening => $"Listening on {_host.PipeName}",
            BridgeStatus.Connected => $"Connected on {_host.PipeName}",
            BridgeStatus.Busy => $"Busy on {_host.PipeName}",
            BridgeStatus.Error => $"Pipe error: {_host.StatusMessage ?? "Unknown"}",
            _ => $"Pipe {_host.PipeName} stopped"
        };
    }

    private void CheckCloudStatus()
    {
        var tenant = Environment.GetEnvironmentVariable("POWERBI_TENANT_ID") ?? _cloudClient?.TenantId;
        var client = Environment.GetEnvironmentVariable("POWERBI_CLIENT_ID") ?? _cloudClient?.ClientId;
        var secret = Environment.GetEnvironmentVariable("POWERBI_CLIENT_SECRET") ?? _cloudClient?.ClientSecret;

        IsCloudConfigured = !string.IsNullOrWhiteSpace(tenant) && !string.IsNullOrWhiteSpace(client) && !string.IsNullOrWhiteSpace(secret);
        CloudStatusText = IsCloudConfigured
            ? "Configured (Service Principal / MSAL active)"
            : "Not configured (set POWERBI_TENANT_ID & POWERBI_CLIENT_ID)";
    }

    #endregion
}
