using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPEtabs.McpBridge.Service;
using HPRebar.McpBridge.Core.ViewModel;

namespace HPEtabs.McpBridge.ViewModel;

/// <summary>
///     What the status window binds to: the shared bridge view model (listener, execution opt-in, last run,
///     audit) plus what only this host has — the COM attachment (Attach/Detach, which ETABS, the more-than-one
///     warning), the second opt-in for destructive operations (in memory on the executor, never persisted) and
///     the three spike probes the user runs on a throw-away model. The window is the only way to flip the
///     destructive flag: scripts cannot name this assembly.
/// </summary>
public sealed partial class EtabsBridgeStatusViewModel : ObservableObject
{
    private readonly EtabsExecutor _executor;
    private readonly Action<Action> _onUiThread;

    [ObservableProperty] private bool _isDestructiveEnabled;
    [ObservableProperty] private bool _canEnableDestructive;
    [ObservableProperty] private string _attachState = "Not attached";
    [ObservableProperty] private string _attachWarning = string.Empty;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(AttachCommand))] [NotifyCanExecuteChangedFor(nameof(DetachCommand))] private bool _isAttached;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(AttachCommand))] [NotifyCanExecuteChangedFor(nameof(DetachCommand))] private bool _isAttaching;
    [ObservableProperty] private string _probeResult = string.Empty;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ProbeSaveAsCommand))] [NotifyCanExecuteChangedFor(nameof(ProbeUnlockCommand))] [NotifyCanExecuteChangedFor(nameof(ProbeApartmentCommand))] private bool _canProbe;

    public EtabsBridgeStatusViewModel(IMcpBridgeRunner runner, EtabsExecutor executor, Action<Action> onUiThread,
        Action<string>? copyToClipboard, bool selfCheckOk, bool apiAvailable, string logDirectory)
    {
        _executor = executor;
        _onUiThread = onUiThread;
        LogDirectory = logDirectory;
        Core = new McpBridgeStatusViewModel(runner, onUiThread, copyToClipboard);
        SelfCheckOk = selfCheckOk;
        SelfCheckText = !apiAvailable
            ? "ETABSv1.dll not found — install ETABS 22 or set HPETABS_ETABS_DIR; nothing can run"
            : selfCheckOk ? "Scripting self-check OK" : "Scripting self-check FAILED — see the log; the listener will not start";

        Core.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(McpBridgeStatusViewModel.IsExecutionEnabled)) Refresh();
        };
        _executor.StateChanged += OnExecutorStateChanged;
        Refresh();
    }

    /// <summary>The shared part of the window.</summary>
    public McpBridgeStatusViewModel Core { get; }

    public bool SelfCheckOk { get; }

    public string SelfCheckText { get; }

    public string LogDirectory { get; }

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

    [RelayCommand(CanExecute = nameof(CanProbe))]
    private Task ProbeSaveAsAsync() => RunProbeAsync("save-as probe", SpikeProbes.SaveAsSemantics);

    [RelayCommand(CanExecute = nameof(CanProbe))]
    private Task ProbeUnlockAsync() => RunProbeAsync("unlock probe", SpikeProbes.UnlockSemantics);

    [RelayCommand(CanExecute = nameof(CanProbe))]
    private Task ProbeApartmentAsync() => RunProbeAsync("apartment probe", SpikeProbes.ApartmentSemantics);

    private async Task RunProbeAsync(string name, Func<ETABSv1.cSapModel, string> probe)
    {
        ProbeResult = name + " running…";
        try
        {
            var text = await _executor.RunOnWorkerAsync(name, () => probe(_executor.Attachment.Require().sapModel));
            ProbeResult = text as string ?? string.Empty;
        }
        catch (Exception exception)
        {
            ProbeResult = $"{name} failed: {exception.GetType().Name}: {exception.Message}";
        }
    }

    [RelayCommand]
    private void OpenLogFolder()
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            // Quoted: the local profile path contains the user name, which may contain spaces.
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{LogDirectory}\"") { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            Core.StatusDetail = "Could not open the log folder: " + exception.Message;
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
        // Destructive needs execution; when the user switches execution off, destructive goes with it.
        CanEnableDestructive = Core.IsExecutionEnabled;
        if (!Core.IsExecutionEnabled && _executor.DestructiveOperationsEnabled) _executor.DestructiveOperationsEnabled = false;
        IsDestructiveEnabled = _executor.DestructiveOperationsEnabled;

        var attachment = _executor.Attachment;
        IsAttached = attachment.Attached;
        AttachState = attachment.Attached
            ? $"Attached to ETABS pid {(attachment.Pid == 0 ? "?" : attachment.Pid)} (OAPI {attachment.OapiVersion ?? "?"})"
            : "Not attached — start ETABS 22, open a model, then click Attach";
        if (attachment.Warning is { } warning) AttachWarning = warning;
        CanProbe = attachment.Attached && Core.IsExecutionEnabled && !_executor.IsBusy;
    }
}
