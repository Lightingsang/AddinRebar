namespace HPGrasshopper.McpBridge.UI;

using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.ViewModel;

/// <summary>
///     View model for the HPGrasshopper MCP Bridge status dialog.
/// </summary>
public sealed class GrasshopperBridgeStatusViewModel : ObservableObject
{
    private readonly McpBridgeStatusViewModel _inner;

    public GrasshopperBridgeStatusViewModel(IMcpBridgeRunner runner, Action<Action> onUiThread, Action<string>? copyToClipboard = null)
    {
        _inner = new McpBridgeStatusViewModel(runner, onUiThread, copyToClipboard);
        _inner.PropertyChanged += (_, e) => OnPropertyChanged(e.PropertyName);
        _inner.CloseRequested += () => CloseRequested?.Invoke();
    }

    public event Action? CloseRequested;

    public void Detach() => _inner.Detach();

    public bool IsListening => _inner.IsListening;
    public bool IsBusy => _inner.IsBusy;
    public string StatusKind => _inner.StatusKind;
    public string StatusText => _inner.StatusText;
    public string StatusDetail => _inner.StatusDetail;
    public string ToggleButtonText => _inner.ToggleButtonText;
    public string PipeName => _inner.PipeName;
    public string RevitVersion => _inner.RevitVersion;
    public string LastRunSummary => _inner.LastRunSummary;
    public string LastScriptPreview => _inner.LastScriptPreview;
    public bool HasLastScript => _inner.HasLastScript;
    public int CompiledScriptCount => _inner.CompiledScriptCount;
    public bool IsCompileCountHigh => _inner.IsCompileCountHigh;
    public string AuditDirectory => _inner.AuditDirectory;

    public bool IsExecutionEnabled
    {
        get => _inner.IsExecutionEnabled;
        set => _inner.IsExecutionEnabled = value;
    }

    public bool AutoStartListener
    {
        get => _inner.AutoStartListener;
        set => _inner.AutoStartListener = value;
    }

    public ICommand ToggleListenerCommand => _inner.ToggleListenerCommand;
    public ICommand RestartBridgeCommand => _inner.RestartBridgeCommand;
    public ICommand OpenAuditFolderCommand => _inner.OpenAuditFolderCommand;
    public ICommand CopyLastScriptCommand => _inner.CopyLastScriptCommand;
    public ICommand CloseCommand => _inner.CloseCommand;
}
