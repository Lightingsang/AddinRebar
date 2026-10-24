using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using HPBimToolkit.Core.Contracts;
using HPBimToolkit.Core.Statistics;
using HPBimToolkit.ExternalEvents;
using HPBimToolkit.Host;
using HPBimToolkit.Infrastructure;
using HPBimToolkit.Services;
using Serilog;

namespace HPBimToolkit.Bridge;

/// <summary>
///     Turns Revit events into page events. Every handler does O(1) work — counts from the event arguments, no
///     collector — and bursts are coalesced, so selecting or editing in Revit never re-scans the model.
/// </summary>
public sealed class RevitEventPublisher : IDisposable
{
    private static readonly TimeSpan SelectionQuietTime = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan ChangeQuietTime = TimeSpan.FromMilliseconds(400);

    private readonly UIControlledApplication _application;
    private readonly RevitRequestQueue _queue;
    private readonly WebMessageTransport _transport;
    private readonly ModelChangeTally _changes;
    private readonly HostThemeState _theme;
    private readonly ILogger _logger;
    private readonly CoalescingTimer _selectionTimer;
    private readonly CoalescingTimer _changeTimer;
    private int _selectionCount;

    public RevitEventPublisher(
        UIControlledApplication application,
        RevitRequestQueue queue,
        WebMessageTransport transport,
        ModelChangeTally changes,
        HostThemeState theme,
        ILogger logger)
    {
        _application = application;
        _queue = queue;
        _transport = transport;
        _changes = changes;
        _theme = theme;
        _logger = logger;
        _selectionTimer = new CoalescingTimer(SelectionQuietTime,
            () => _transport.Publish(EventNames.SelectionChanged, new SelectionChangedEvent(_selectionCount)));
        _changeTimer = new CoalescingTimer(ChangeQuietTime,
            () => _transport.Publish(EventNames.ModelStale, _changes.ToEvent()));
    }

    public void Subscribe()
    {
        _application.SelectionChanged += OnSelectionChanged;
        _application.ViewActivated += OnViewActivated;
        _application.ThemeChanged += OnThemeChanged;
        _application.DockableFrameVisibilityChanged += OnPaneVisibilityChanged;
        _application.ControlledApplication.DocumentChanged += OnDocumentChanged;
        _application.ControlledApplication.DocumentOpened += OnDocumentOpened;
        _application.ControlledApplication.DocumentClosed += OnDocumentClosed;
    }

    public void Dispose()
    {
        _application.SelectionChanged -= OnSelectionChanged;
        _application.ViewActivated -= OnViewActivated;
        _application.ThemeChanged -= OnThemeChanged;
        _application.DockableFrameVisibilityChanged -= OnPaneVisibilityChanged;
        _application.ControlledApplication.DocumentChanged -= OnDocumentChanged;
        _application.ControlledApplication.DocumentOpened -= OnDocumentOpened;
        _application.ControlledApplication.DocumentClosed -= OnDocumentClosed;
        _selectionTimer.Dispose();
        _changeTimer.Dispose();
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _selectionCount = e.GetSelectedElements().Count;
        _selectionTimer.Trigger();
    }

    /// <remarks>Changes in any open document count (a linked model edited in another window is rare and harmless).</remarks>
    private void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
    {
        _changes.Record(e.GetAddedElementIds().Count, e.GetDeletedElementIds().Count, e.GetModifiedElementIds().Count);
        _changeTimer.Trigger();
    }

    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        _theme.Refresh();
        _transport.Publish(EventNames.ThemeChanged, new ThemeChangedEvent(ThemeNames.From(_theme.IsDark)));
    }

    private void OnPaneVisibilityChanged(object? sender, DockableFrameVisibilityChangedEventArgs e)
    {
        if (e.PaneId != DashboardPaneProvider.PaneId)
        {
            return;
        }

        _transport.IsPaused = !e.DockableFrameShown;
        if (e.DockableFrameShown)
        {
            ReplayWhatHappenedWhileHidden();
        }
    }

    /// <summary>Events are dropped while the pane is hidden; on show the page gets the current state in one go.</summary>
    private void ReplayWhatHappenedWhileHidden()
    {
        PublishDocument();
        _transport.Publish(EventNames.SelectionChanged, new SelectionChangedEvent(_selectionCount));
        if (_changes.HasChanges)
        {
            _transport.Publish(EventNames.ModelStale, _changes.ToEvent());
        }
    }

    private void OnViewActivated(object? sender, ViewActivatedEventArgs e) => PublishDocument();

    private void OnDocumentOpened(object? sender, DocumentOpenedEventArgs e) => PublishDocument();

    private void OnDocumentClosed(object? sender, DocumentClosedEventArgs e) => PublishDocument();

    /// <summary>
    ///     Reads the active document through the queue: DocumentClosed comes from the DB application without a UI
    ///     context, and the active document is only settled after the event returns.
    /// </summary>
    private async void PublishDocument()
    {
        if (!_transport.CanPublish)
        {
            return; // nobody listens; the page reads the document itself when it is shown again
        }

        try
        {
            var snapshot = await _queue.RunAsync(DocumentInfoReader.Read);
            _transport.Publish(EventNames.DocumentChanged, snapshot);
        }
        catch (Exception exception)
        {
            _logger.Warning(exception, "Could not publish the active document");
        }
    }
}
