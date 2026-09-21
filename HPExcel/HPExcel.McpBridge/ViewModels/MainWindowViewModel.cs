using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPExcel.McpBridge.Com;
using HPExcel.McpBridge.Discovery;
using HPExcel.McpBridge.Host;
using HPExcel.McpBridge.Safety;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.ViewModel;
using Serilog;

namespace HPExcel.McpBridge.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly ExcelBridgeExecutor _executor;
    private readonly ExcelAttachment _attachment;
    private readonly ExcelSafetyGuard _guard;
    private readonly Action<Action> _onUiThread;

    [ObservableProperty] private bool _isAttached;
    [ObservableProperty] private int? _attachedPid;
    [ObservableProperty] private string? _excelVersion;
    [ObservableProperty] private string? _activeWorkbookName;
    [ObservableProperty] private string? _activeWorksheetName;
    [ObservableProperty] private string? _selectionAddress;
    [ObservableProperty] private string _openWorkbooksSummary = "None";
    [ObservableProperty] private string _attachStatusText = "Not connected";
    [ObservableProperty] private bool _canEnableWrite;
    [ObservableProperty] private bool _canEnableDestructive;

    [ObservableProperty] private ExcelInstanceInfo? _selectedInstance;

    public ObservableCollection<ExcelInstanceInfo> RunningInstances { get; } = new();

    public McpBridgeStatusViewModel Core { get; }

    public MainWindowViewModel(
        IMcpBridgeRunner runner,
        ExcelBridgeExecutor executor,
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

        RefreshProcesses();
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

    public bool IsWriteEnabled
    {
        get => _guard.IsWriteEnabled;
        set
        {
            if (_guard.IsWriteEnabled == value) return;
            _guard.IsWriteEnabled = value;
            OnPropertyChanged();
            UpdateGating();
        }
    }

    public bool IsDestructiveEnabled
    {
        get => _guard.IsDestructiveEnabled;
        set
        {
            if (_guard.IsDestructiveEnabled == value) return;
            _guard.IsDestructiveEnabled = value;
            OnPropertyChanged();
            UpdateGating();
        }
    }

    private void UpdateGating()
    {
        OnPropertyChanged(nameof(IsExecutionEnabled));
        OnPropertyChanged(nameof(IsWriteEnabled));
        OnPropertyChanged(nameof(IsDestructiveEnabled));

        CanEnableWrite = _guard.IsExecutionEnabled;
        CanEnableDestructive = _guard.IsExecutionEnabled && _guard.IsWriteEnabled;
    }

    private void UpdateAttachmentState()
    {
        IsAttached = _attachment.IsAttached;
        AttachedPid = _attachment.AttachedPid;
        ExcelVersion = _attachment.ExcelVersion;
        ActiveWorkbookName = _attachment.ActiveWorkbookName ?? "<No Workbook>";
        ActiveWorksheetName = _attachment.ActiveWorksheetName ?? "<No Worksheet>";
        SelectionAddress = _attachment.SelectionAddress ?? "<None>";

        var count = _attachment.OpenWorkbookCount;
        OpenWorkbooksSummary = count > 0
            ? $"{count} open ({string.Join(", ", _attachment.OpenWorkbookNames)})"
            : "None";

        AttachStatusText = IsAttached
            ? $"Attached to PID {AttachedPid} (v{ExcelVersion})"
            : "Not connected to Microsoft Excel";

        AttachCommand.NotifyCanExecuteChanged();
        DetachCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    public void RefreshProcesses()
    {
        RunningInstances.Clear();
        var detected = ExcelProcessDetector.DetectRunningInstances();
        foreach (var inst in detected)
        {
            RunningInstances.Add(inst);
        }

        if (RunningInstances.Count > 0 && SelectedInstance == null)
        {
            SelectedInstance = RunningInstances[0];
        }
    }

    [RelayCommand(CanExecute = nameof(CanAttach))]
    public void Attach()
    {
        try
        {
            var targetPid = SelectedInstance?.ProcessId;
            _attachment.Attach(targetPid);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Attach failed");
            AttachStatusText = $"Attach failed: {ex.Message}";
        }
    }

    private bool CanAttach => !IsAttached;

    [RelayCommand(CanExecute = nameof(CanDetach))]
    public void Detach()
    {
        _attachment.Detach("User clicked Detach");
    }

    private bool CanDetach => IsAttached;

    [RelayCommand]
    public void OpenLogFolder()
    {
        try
        {
            if (Directory.Exists(BridgeEntry.LogDirectory))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = BridgeEntry.LogDirectory,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Could not open log folder");
        }
    }

    [RelayCommand]
    public void OpenSnapshotFolder()
    {
        try
        {
            var dir = _executor.Snapshots.ResolveSnapshotDirectory(_attachment.ActiveWorkbookPath);
            if (Directory.Exists(dir))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = dir,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Could not open snapshot folder");
        }
    }
}
