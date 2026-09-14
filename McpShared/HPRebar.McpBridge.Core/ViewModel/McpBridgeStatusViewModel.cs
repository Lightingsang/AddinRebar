using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPRebar.McpBridge.Core.Model;
using Serilog;

namespace HPRebar.McpBridge.Core.ViewModel;

/// <summary>
///     What the status window shows and can toggle. State comes from <see cref="IMcpBridgeRunner"/>, which
///     raises StateChanged on pipe or Revit threads; every refresh is marshalled onto the dispatcher the
///     window was created on. No Revit API is touched here.
/// </summary>
public sealed partial class McpBridgeStatusViewModel : ObservableObject
{
    private const int CompileCountWarningThreshold = 500;
    private const int PreviewLines = 20;

    private readonly IMcpBridgeRunner _runner;
    private readonly Action<Action> _onUiThread;
    private readonly Action<string>? _copyToClipboard;

    [ObservableProperty] private bool _isListening;
    [ObservableProperty] private bool _isExecutionEnabled;
    [ObservableProperty] private bool _autoStartListener;
    [ObservableProperty] private string _statusKind = nameof(BridgeStatus.Stopped);
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private string _statusDetail = string.Empty;
    [ObservableProperty] private string _toggleButtonText = "Start listener";
    [ObservableProperty] private string _pipeName = string.Empty;
    [ObservableProperty] private string _revitVersion = string.Empty;
    [ObservableProperty] private string _lastRunSummary = "No script has run yet.";
    [ObservableProperty] private string _lastScriptPreview = string.Empty;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(CopyLastScriptCommand))] private bool _hasLastScript;
    [ObservableProperty] private int _compiledScriptCount;
    [ObservableProperty] private bool _isCompileCountHigh;
    [ObservableProperty] private string _auditDirectory = string.Empty;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(RestartBridgeCommand))] private bool _isBusy;

    /// <param name="copyToClipboard">The view's clipboard (WPF `Clipboard.SetText`); the view model has no UI framework of its own.</param>
    /// <param name="onUiThread">
    ///     Marshals a refresh onto the window's thread — runner events arrive on pipe and host threads, and a
    ///     refresh raises PropertyChanged/CanExecuteChanged that WPF only accepts on its own thread. Required:
    ///     Core cannot know the host's UI framework, and a CAD host's native message loop does not install a
    ///     SynchronizationContext to fall back on. The WPF view passes `Dispatcher.CurrentDispatcher.InvokeAsync`.
    /// </param>
    public McpBridgeStatusViewModel(IMcpBridgeRunner runner, Action<Action> onUiThread, Action<string>? copyToClipboard = null)
    {
        _runner = runner;
        _onUiThread = onUiThread ?? throw new ArgumentNullException(nameof(onUiThread));
        _copyToClipboard = copyToClipboard;
        _runner.StateChanged += OnRunnerStateChanged;
        Refresh();
    }

    /// <summary>Raised when the user asks to close; the modeless window subscribes with its own Close.</summary>
    public event Action? CloseRequested;

    /// <summary>Stops listening to the runner once the window is gone; the runner outlives the window.</summary>
    public void Detach() => _runner.StateChanged -= OnRunnerStateChanged;

    private void OnRunnerStateChanged() => _onUiThread(Refresh);

    private void Refresh()
    {
        var status = _runner.Status;

        IsListening = status is BridgeStatus.Listening or BridgeStatus.Connected or BridgeStatus.Busy;
        IsBusy = status == BridgeStatus.Busy;
        StatusKind = status.ToString();
        StatusText = status switch
        {
            BridgeStatus.Stopped => $"Stopped — the MCP server cannot reach {_runner.HostName}",
            BridgeStatus.Listening => "Listening — waiting for an MCP server to connect",
            BridgeStatus.Connected => "Connected — MCP server attached, idle",
            BridgeStatus.Busy => "Running a script…",
            BridgeStatus.Error => "Error",
            _ => status.ToString(),
        };
        StatusDetail = _runner.StatusMessage ?? string.Empty;
        ToggleButtonText = IsListening ? "Stop listener" : "Start listener";

        IsExecutionEnabled = _runner.ExecutionEnabled;
        AutoStartListener = _runner.AutoStartListener;
        PipeName = _runner.PipeName;
        RevitVersion = _runner.RevitVersion;
        AuditDirectory = _runner.AuditDirectory;
        CompiledScriptCount = _runner.CompiledScriptCount;
        IsCompileCountHigh = CompiledScriptCount > CompileCountWarningThreshold;

        var run = _runner.LastRun;
        HasLastScript = run is not null;
        LastRunSummary = run is null ? "No script has run yet." : Summarise(run);
        LastScriptPreview = run is null ? string.Empty : Preview(run.Source);
    }

    private static string Summarise(LastRunInfo run)
    {
        var outcome = run.IsError ? "failed" : "ok";
        var changes = $"+{run.Added} ~{run.Modified} -{run.Deleted}";
        var rolled = run.RolledBack ? ", rolled back" : string.Empty;
        var message = run.IsError && run.Message is not null ? $"\n{run.Message}" : string.Empty;

        return $"{run.Timestamp:HH:mm:ss}  \"{run.Label}\"  {outcome} in {run.DurationMs} ms  ({changes}{rolled}){message}";
    }

    private static string Preview(string source)
    {
        var lines = source.Split('\n');
        return lines.Length <= PreviewLines
            ? source
            : string.Join("\n", lines.Take(PreviewLines)) + $"\n… ({lines.Length - PreviewLines} more lines — use Copy)";
    }

    partial void OnIsExecutionEnabledChanged(bool value)
    {
        if (_runner.ExecutionEnabled != value) _runner.ExecutionEnabled = value;
    }

    partial void OnAutoStartListenerChanged(bool value)
    {
        if (_runner.AutoStartListener != value) _runner.AutoStartListener = value;
    }

    [RelayCommand]
    private void ToggleListener()
    {
        if (IsListening) _runner.Stop();
        else _runner.Start();
    }

    [RelayCommand(CanExecute = nameof(CanRestart))]
    private void RestartBridge() => _runner.Restart();

    private bool CanRestart() => !IsBusy;

    [RelayCommand]
    private void OpenAuditFolder()
    {
        try
        {
            Directory.CreateDirectory(AuditDirectory);
            // Quoted: the roaming profile path contains the user name, which may contain spaces.
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{AuditDirectory}\"") { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Could not open the audit folder {Directory}", AuditDirectory);
            StatusDetail = "Could not open the audit folder: " + exception.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(HasLastScript))]
    private void CopyLastScript()
    {
        var source = _runner.LastRun?.Source;
        if (string.IsNullOrEmpty(source)) return;

        if (_copyToClipboard is null) StatusDetail = "Clipboard is not available in this window.";
        else _copyToClipboard(source);
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();
}
