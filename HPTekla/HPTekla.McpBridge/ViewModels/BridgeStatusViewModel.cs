using System;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPRebar.McpBridge.Core.ViewModel;

namespace HPTekla.McpBridge.ViewModels;

/// <summary>
///     View model for the HPTekla modeless status window.
///     Binds to core McpBridgeStatusViewModel for pipe listener state and execution opt-in,
///     and manages Tekla-specific state including active model info and AllowHeavyOperations.
/// </summary>
public sealed partial class BridgeStatusViewModel : ObservableObject
{
    private readonly TeklaBridgeExecutor _executor;
    private readonly Action<Action> _onUiThread;

    [ObservableProperty] private bool _isHeavyEnabled;
    [ObservableProperty] private bool _canEnableHeavy;
    [ObservableProperty] private string _modelName = "None";
    [ObservableProperty] private string _modelPath = "No model open";
    [ObservableProperty] private bool _isDarkMode = true;

    public BridgeStatusViewModel(
        IMcpBridgeRunner runner,
        TeklaBridgeExecutor executor,
        Action<Action> onUiThread,
        Action<string>? copyToClipboard,
        bool selfCheckOk,
        string logDirectory)
    {
        _executor = executor;
        _onUiThread = onUiThread;
        LogDirectory = logDirectory;
        Core = new McpBridgeStatusViewModel(runner, onUiThread, copyToClipboard);
        SelfCheckOk = selfCheckOk;
        SelfCheckText = selfCheckOk ? "Scripting self-check OK" : "Scripting self-check FAILED — see log";

        Core.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(McpBridgeStatusViewModel.IsExecutionEnabled))
            {
                Refresh();
            }
        };

        _executor.StateChanged += OnExecutorStateChanged;
        Refresh();
    }

    public McpBridgeStatusViewModel Core { get; }

    public bool SelfCheckOk { get; }

    public string SelfCheckText { get; }

    public string LogDirectory { get; }

    public event Action? CloseRequested
    {
        add => Core.CloseRequested += value;
        remove => Core.CloseRequested -= value;
    }

    public event Action? ThemeChanged;

    public void Detach()
    {
        _executor.StateChanged -= OnExecutorStateChanged;
        Core.Detach();
    }

    [RelayCommand]
    private void OpenLogFolder()
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{LogDirectory}\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Core.StatusDetail = "Could not open log folder: " + ex.Message;
        }
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
        ThemeChanged?.Invoke();
    }

    partial void OnIsHeavyEnabledChanged(bool value)
    {
        if (_executor.AllowHeavyOperations != value)
        {
            _executor.AllowHeavyOperations = value;
        }
    }

    private void OnExecutorStateChanged() => _onUiThread(Refresh);

    private void Refresh()
    {
        CanEnableHeavy = Core.IsExecutionEnabled;
        if (!Core.IsExecutionEnabled && _executor.AllowHeavyOperations)
        {
            _executor.AllowHeavyOperations = false;
        }

        IsHeavyEnabled = _executor.AllowHeavyOperations;
        ModelName = _executor.ActiveDocumentTitle ?? "None";
    }
}
