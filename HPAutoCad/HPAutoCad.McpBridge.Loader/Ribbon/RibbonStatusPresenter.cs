using Autodesk.Windows;

namespace HPAutoCad.McpBridge.Loader.Ribbon;

/// <summary>
///     Keeps the status label on the Ribbon in step with the bridge. The bridge raises its state changes on
///     whatever thread noticed them (the pipe thread for a client connecting), so every update is posted to
///     the Ribbon's dispatcher; the text itself comes from the bridge as a plain string across the load
///     contexts. The three states a user must tell apart — listener off, listening, server connected — are
///     never collapsed into one word.
/// </summary>
internal sealed class RibbonStatusPresenter : IDisposable
{
    private readonly RibbonLabel _label;
    private Action? _unsubscribe;

    public RibbonStatusPresenter(RibbonLabel label)
    {
        _label = label;
        if (!BridgeActions.BridgeAvailable)
        {
            label.Text = "⚠ Bridge chưa khởi động — xem loader.log";
            return;
        }

        // The bridge calls back once immediately, then on every change; it hands back the unsubscribe action.
        _unsubscribe = BridgeActions.Query<Action>("status.subscribe", new Action<string, string>(OnStatus));
        if (_unsubscribe is null) label.Text = "⚠ Không đọc được trạng thái bridge";
    }

    private void OnStatus(string kind, string text)
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
        if (dispatcher is null || dispatcher.CheckAccess()) _label.Text = line;
        else dispatcher.InvokeAsync(() => _label.Text = line);
    }

    public void Dispose()
    {
        try { _unsubscribe?.Invoke(); }
        catch (System.Exception exception) { LoaderLog.Write("status unsubscribe failed", exception); }
        _unsubscribe = null;
    }
}
