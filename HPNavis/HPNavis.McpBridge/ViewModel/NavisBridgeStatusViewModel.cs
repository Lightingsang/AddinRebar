using CommunityToolkit.Mvvm.ComponentModel;
using HPRebar.McpBridge.Core.ViewModel;

namespace HPNavis.McpBridge.ViewModel;

/// <summary>
///     What the Navisworks status window binds to: the shared bridge view model (listener, execution
///     opt-in, last run, audit) plus the one thing only this host has — the second opt-in for heavy
///     operations, which lives on the executor in memory and is never persisted. Kept apart from the
///     shared view model on purpose: Revit and AutoCAD must not grow a heavy switch.
/// </summary>
public sealed partial class NavisBridgeStatusViewModel : ObservableObject
{
    private readonly NavisMainThreadExecutor _executor;
    private readonly Action<Action> _onUiThread;

    [ObservableProperty] private bool _isHeavyEnabled;
    [ObservableProperty] private bool _canEnableHeavy;
    [ObservableProperty] private string _hostState = string.Empty;

    public NavisBridgeStatusViewModel(IMcpBridgeRunner runner, NavisMainThreadExecutor executor, Action<Action> onUiThread,
        Action<string>? copyToClipboard, bool selfCheckOk)
    {
        _executor = executor;
        _onUiThread = onUiThread;
        Core = new McpBridgeStatusViewModel(runner, onUiThread, copyToClipboard);
        SelfCheckOk = selfCheckOk;
        SelfCheckText = selfCheckOk ? "Scripting self-check OK" : "Scripting self-check FAILED — see the log; the listener will not start";

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

    partial void OnIsHeavyEnabledChanged(bool value)
    {
        if (_executor.HeavyOperationsEnabled != value) _executor.HeavyOperationsEnabled = value;
    }

    private void OnExecutorStateChanged() => _onUiThread(Refresh);

    private void Refresh()
    {
        // Heavy needs execution; when the user switches execution off, heavy goes with it.
        CanEnableHeavy = Core.IsExecutionEnabled;
        if (!Core.IsExecutionEnabled && _executor.HeavyOperationsEnabled) _executor.HeavyOperationsEnabled = false;
        IsHeavyEnabled = _executor.HeavyOperationsEnabled;
        HostState = _executor.Quiescence.Describe(Autodesk.Navisworks.Api.Application.ActiveDocument);
    }
}
