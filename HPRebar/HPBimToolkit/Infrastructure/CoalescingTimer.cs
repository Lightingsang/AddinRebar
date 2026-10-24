using System.Windows.Threading;

namespace HPBimToolkit.Infrastructure;

/// <summary>
///     Runs an action once a burst of triggers has been quiet for <c>delay</c> — a box selection raises dozens of
///     SelectionChanged events, the page needs one. Runs on the dispatcher it was created on (Revit's UI thread).
/// </summary>
public sealed class CoalescingTimer : IDisposable
{
    private readonly DispatcherTimer _timer;
    private readonly Action _action;

    public CoalescingTimer(TimeSpan delay, Action action)
    {
        _action = action;
        _timer = new DispatcherTimer { Interval = delay };
        _timer.Tick += OnTick;
    }

    public void Trigger()
    {
        _timer.Stop();
        _timer.Start();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _timer.Stop();
        _action();
    }
}
