using Autodesk.Windows;

namespace HPAutoCad.McpBridge.Loader.Ribbon;

/// <summary>
///     Keeps the status label and the auto-start toggle on the Ribbon in step with the bridge. The bridge
///     raises its state changes on whatever thread noticed them (the pipe thread for a client connecting),
///     so every update is posted to the Ribbon's dispatcher; the text itself comes from the bridge as a
///     plain string across the load contexts. The three states a user must tell apart — listener off,
///     listening, server connected — are never collapsed into one word.
/// </summary>
internal sealed class RibbonStatusPresenter : IDisposable
{
    private readonly RibbonLabel _label;
    private readonly RibbonToggleButton _autoStart;
    private Action? _unsubscribe;

    public RibbonStatusPresenter(RibbonLabel label, RibbonToggleButton autoStart)
    {
        _label = label;
        _autoStart = autoStart;
        if (!BridgeActions.BridgeAvailable)
        {
            label.Text = "⚠ Bridge chưa khởi động";
            label.ToolTip = "Xem loader.log (nút Mở nhật ký).";
            return;
        }

        // The bridge calls back once immediately, then on every change; it hands back the unsubscribe action.
        _unsubscribe = BridgeActions.Query<Action>("status.subscribe", new Action<string, string, bool>(OnStatus));
        if (_unsubscribe is null) label.Text = "⚠ Không đọc được trạng thái bridge";
    }

    private void OnStatus(string kind, string text, bool autoStart)
    {
        var marker = kind switch
        {
            "Connected" or "Busy" => "●",
            "Listening" => "◐",
            "Error" => "⚠",
            _ => "○",
        };
        var line = $"{marker} {text}";
        var dispatcher = ComponentManager.Ribbon?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess()) Apply(line, autoStart);
        else dispatcher.InvokeAsync(() => Apply(line, autoStart));
    }

    private void Apply(string line, bool autoStart)
    {
        _label.Text = line;
        // The window's checkbox writes the same setting: the toggle follows the bridge, never its own last click.
        if (_autoStart.IsChecked != autoStart) _autoStart.IsChecked = autoStart;
    }

    public void Dispose()
    {
        try { _unsubscribe?.Invoke(); }
        catch (System.Exception exception) { LoaderLog.Write("status unsubscribe failed", exception); }
        _unsubscribe = null;
    }
}
