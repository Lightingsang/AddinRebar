using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPSap2000.McpBridge.Service;
using HPRebar.McpBridge.Core.ViewModel;

namespace HPSap2000.McpBridge.ViewModel;

/// <summary>
///     What the status window binds to: the shared bridge view model plus SAP2000 COM attachment and destructive opt-in.
/// </summary>
public sealed partial class SapBridgeStatusViewModel : ObservableObject
{
    private readonly SapExecutor _executor;
    private readonly Action<Action> _onUiThread;

    [ObservableProperty] private bool _isDestructiveEnabled;
    [ObservableProperty] private bool _canEnableDestructive;
    [ObservableProperty] private string _attachState = "Not attached";
    [ObservableProperty] private string _attachWarning = string.Empty;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(AttachCommand))] [NotifyCanExecuteChangedFor(nameof(DetachCommand))] private bool _isAttached;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(AttachCommand))] [NotifyCanExecuteChangedFor(nameof(DetachCommand))] private bool _isAttaching;

    public SapBridgeStatusViewModel(IMcpBridgeRunner runner, SapExecutor executor, Action<Action> onUiThread,
        Action<string>? copyToClipboard, bool selfCheckOk, bool apiAvailable, string logDirectory, string snapshotDirectory)
    {
        _executor = executor;
        _onUiThread = onUiThread;
        LogDirectory = logDirectory;
        SnapshotDirectory = snapshotDirectory;
        Core = new McpBridgeStatusViewModel(runner, onUiThread, copyToClipboard);
        SelfCheckOk = selfCheckOk;
        SelfCheckText = !apiAvailable
            ? "SAP2000v1.dll not found — install SAP2000 27 or set HPSAP2000_SAP2000_DIR; nothing can run"
            : selfCheckOk ? "Scripting self-check OK" : "Scripting self-check FAILED — see the log; the listener will not start";

        Core.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(McpBridgeStatusViewModel.IsExecutionEnabled)) Refresh();
        };
        _executor.StateChanged += OnExecutorStateChanged;
        Refresh();
    }

    public McpBridgeStatusViewModel Core { get; }

    public bool SelfCheckOk { get; }

    public string SelfCheckText { get; }

    public string LogDirectory { get; }

    public string SnapshotDirectory { get; }

    private bool CanAttach => !IsAttached && !IsAttaching;

    private bool CanDetach => IsAttached && !IsAttaching;

    [RelayCommand(CanExecute = nameof(CanAttach))]
    private async Task AttachAsync()
    {
        IsAttaching = true;
        AttachWarning = string.Empty;
        try
        {
            await _executor.AttachAsync();
        }
        catch (Exception exception)
        {
            AttachWarning = exception.Message;
        }
        finally
        {
            IsAttaching = false;
            Refresh();
        }
    }

    [RelayCommand(CanExecute = nameof(CanDetach))]
    private async Task DetachAsync()
    {
        IsAttaching = true;
        AttachWarning = string.Empty;
        try { await _executor.DetachAsync(); }
        finally
        {
            IsAttaching = false;
            Refresh();
        }
    }

    [RelayCommand]
    private void OpenLogFolder() => OpenFolder(LogDirectory, "log");

    [RelayCommand]
    private void OpenSnapshotFolder() => OpenFolder(SnapshotDirectory, "snapshot");

    private void OpenFolder(string directory, string what)
    {
        try
        {
            Directory.CreateDirectory(directory);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{directory}\"") { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            Core.StatusDetail = $"Could not open the {what} folder: " + exception.Message;
        }
    }

    public event Action? CloseRequested
    {
        add => Core.CloseRequested += value;
        remove => Core.CloseRequested -= value;
    }

    public void Detach()
    {
        _executor.StateChanged -= OnExecutorStateChanged;
        Core.Detach();
    }

    partial void OnIsDestructiveEnabledChanged(bool value)
    {
        if (_executor.DestructiveOperationsEnabled != value) _executor.DestructiveOperationsEnabled = value;
    }

    private void OnExecutorStateChanged() => _onUiThread(Refresh);

    private void Refresh()
    {
        CanEnableDestructive = Core.IsExecutionEnabled;
        if (!Core.IsExecutionEnabled && _executor.DestructiveOperationsEnabled) _executor.DestructiveOperationsEnabled = false;
        IsDestructiveEnabled = _executor.DestructiveOperationsEnabled;

        var attachment = _executor.Attachment;
        IsAttached = attachment.Attached;
        AttachState = attachment.Attached
            ? $"Attached to SAP2000 pid {(attachment.Pid == 0 ? "?" : attachment.Pid)} (OAPI {attachment.OapiVersion ?? "?"})"
            : "Not attached — start SAP2000 27, open a model, then click Attach";
        if (attachment.Warning is { } warning) AttachWarning = warning;
    }
}
