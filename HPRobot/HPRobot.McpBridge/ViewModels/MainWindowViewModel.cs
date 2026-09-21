using System;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPRobot.McpBridge.Com;
using HPRobot.McpBridge.Host;
using HPRobot.McpBridge.Safety;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.ViewModel;
using Serilog;

namespace HPRobot.McpBridge.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly RobotBridgeExecutor _executor;
    private readonly RobotAttachment _attachment;
    private readonly RobotSafetyGuard _guard;
    private readonly Action<Action> _onUiThread;

    [ObservableProperty] private bool _isAttached;
    [ObservableProperty] private int? _attachedPid;
    [ObservableProperty] private string? _robotVersion;
    [ObservableProperty] private string? _programVersion;
    [ObservableProperty] private string? _activeModelPath;
    [ObservableProperty] private string? _activeModelFileName;
    [ObservableProperty] private string? _structureType;
    [ObservableProperty] private bool _isCalculated;
    [ObservableProperty] private int _nodeCount;
    [ObservableProperty] private int _barCount;
    [ObservableProperty] private int _panelCount;
    [ObservableProperty] private int _loadCaseCount;
    [ObservableProperty] private string _attachStatusText = "Disconnected";
    [ObservableProperty] private string _calculatedStatusText = "Not calculated";
    [ObservableProperty] private bool _canEnableHeavyOperations;

    public McpBridgeStatusViewModel Core { get; }

    public MainWindowViewModel(
        IMcpBridgeRunner runner,
        RobotBridgeExecutor executor,
        Action<Action> onUiThread,
        Action<string>? copyToClipboard = null)
    {
        _executor = executor;
        _attachment = executor.Attachment;
        _guard = executor.Guard;
        _onUiThread = onUiThread ?? throw new ArgumentNullException(nameof(onUiThread));

        Core = new McpBridgeStatusViewModel(runner, onUiThread, copyToClipboard);

        Core.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(McpBridgeStatusViewModel.IsExecutionEnabled))
            {
                if (_guard.IsExecutionEnabled != Core.IsExecutionEnabled)
                {
                    _guard.IsExecutionEnabled = Core.IsExecutionEnabled;
                }
                UpdateGating();
            }
        };

        _guard.StateChanged += () => _onUiThread(UpdateGating);
        _attachment.StateChanged += () => _onUiThread(UpdateAttachmentState);

        UpdateGating();
        UpdateAttachmentState();
    }

    public bool IsExecutionEnabled
    {
        get => _guard.IsExecutionEnabled;
        set
        {
            if (_guard.IsExecutionEnabled == value) return;
            _guard.IsExecutionEnabled = value;
            Core.IsExecutionEnabled = value;
            OnPropertyChanged();
            UpdateGating();
        }
    }

    public bool IsHeavyOperationsEnabled
    {
        get => _guard.IsHeavyOperationsEnabled;
        set
        {
            if (_guard.IsHeavyOperationsEnabled == value) return;
            _guard.IsHeavyOperationsEnabled = value;
            OnPropertyChanged();
            UpdateGating();
        }
    }

    private void UpdateGating()
    {
        OnPropertyChanged(nameof(IsExecutionEnabled));
        OnPropertyChanged(nameof(IsHeavyOperationsEnabled));
        CanEnableHeavyOperations = _guard.IsExecutionEnabled;
    }

    private void UpdateAttachmentState()
    {
        IsAttached = _attachment.IsAttached;
        AttachedPid = _attachment.AttachedPid;
        RobotVersion = _attachment.RobotVersion;
        ProgramVersion = _attachment.ProgramVersion;
        ActiveModelPath = _attachment.ActiveModelPath;
        ActiveModelFileName = _attachment.ActiveModelFileName ?? "(None / Unsaved)";
        StructureType = _attachment.StructureType ?? "None";
        IsCalculated = _attachment.IsCalculated;
        NodeCount = _attachment.NodeCount;
        BarCount = _attachment.BarCount;
        PanelCount = _attachment.PanelCount;
        LoadCaseCount = _attachment.LoadCaseCount;

        AttachStatusText = IsAttached
            ? $"Attached (PID: {AttachedPid ?? 0}, Ver: {RobotVersion ?? "2026"})"
            : "Disconnected";

        CalculatedStatusText = IsCalculated ? "Calculated (Results available)" : "Not calculated";
    }

    [RelayCommand]
    private async Task AttachAsync()
    {
        try
        {
            await _executor.StaWorker.RunOnControlLaneAsync("ui-attach", () =>
            {
                _attachment.Attach();
            });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Attach button failed");
        }
    }

    [RelayCommand]
    private void Detach()
    {
        _attachment.Detach("User clicked Detach");
    }

    [RelayCommand]
    private async Task RefreshStateAsync()
    {
        try
        {
            await _executor.StaWorker.RunOnControlLaneAsync("ui-refresh", () =>
            {
                _attachment.RefreshContext();
            });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Refresh state button failed");
        }
    }

    [RelayCommand]
    private void OpenSnapshotFolder()
    {
        try
        {
            var dir = _executor.Snapshots.ResolveSnapshotDirectory(_attachment.ActiveModelPath);
            if (Directory.Exists(dir))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to open snapshots directory");
        }
    }

    [RelayCommand]
    private void OpenLogFolder()
    {
        try
        {
            var dir = BridgeEntry.LogDirectory;
            if (Directory.Exists(dir))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to open logs directory");
        }
    }
}
