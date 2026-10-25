using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;
using HPRebar.KataExport.View.Controls;
using HPRebar.KataExport.ViewModel;
using HPRebar.Resources.Themes;
using HPWebHost.Protocol;
using HPWebHost.Rendering;
using Serilog;

// WPF types, not the Revit ones the SDK imports globally.
using Point = System.Windows.Point;

namespace HPRebar.KataExport.View.Web;

/// <summary>
///     The web window's code-behind: turns <see cref="KataExportViewModel" /> into <see cref="KataViewState" /> for the
///     page, runs the page's commands on the same view-model commands the WPF window binds, and forwards gestures on the
///     drawing to the <see cref="KataElevationCanvas" /> the window keeps hidden behind the browser — the elevation is
///     painted by the WPF painters and sent as SVG. Everything runs on the window's UI thread (the transport dispatches
///     page messages there); state and drawing are each sent at most once per dispatcher pass.
/// </summary>
internal sealed partial class KataWebBridge : IDisposable
{
    private const double MinViewport = 50.0;
    private const double MaxViewport = 8000.0;

    private readonly KataExportViewModel _viewModel;
    private readonly KataElevationCanvas _canvas;
    private readonly IHostTheme _theme;
    private readonly Action<string, object> _publish;
    private readonly Dispatcher _dispatcher;
    private bool _statePending;
    private bool _drawingPending;
    private string? _lastSvg;
    private int _revision;

    /// <param name="publish">Sends an event to the page (dropped while the page is not up).</param>
    public KataWebBridge(KataExportViewModel viewModel, KataElevationCanvas canvas, IHostTheme theme, Action<string, object> publish)
    {
        _viewModel = viewModel;
        _canvas = canvas;
        _theme = theme;
        _publish = publish;
        _dispatcher = canvas.Dispatcher;
        _viewModel.PropertyChanged += OnViewModelChanged;
        _canvas.Rendered += ScheduleDrawing;
        _theme.Changed += OnThemeChanged;
    }

    public void Register(CommandDispatcher dispatcher)
    {
        dispatcher.Register(KataWebCommands.GetState, (_, _) => Answer(State()));
        dispatcher.Register<KataUpdateRequest>(KataWebCommands.Update, (request, _) => Answer(Update(request)));
        dispatcher.Register<KataRunRequest>(KataWebCommands.Run, (request, _) => Answer(Run(request.Action)));
        dispatcher.Register<KataViewportRequest>(KataWebCommands.Viewport, (request, _) => Answer(Resize(request)));
        dispatcher.Register<KataCanvasInput>(KataWebCommands.Canvas, (input, _) => Answer(new KataCanvasResult(Gesture(input))));
        dispatcher.Register<KataSectionRequest>(KataWebCommands.Section, (request, _) => Answer(Section(request)));
        dispatcher.Register<KataRemoveBarsRequest>(KataWebCommands.RemoveBars, (request, _) => Answer(RemoveBars(request)));
    }

    public void Dispose()
    {
        _viewModel.PropertyChanged -= OnViewModelChanged;
        _canvas.Rendered -= ScheduleDrawing;
        _theme.Changed -= OnThemeChanged;
    }

    /// <summary>Sends the state and the drawing again, e.g. once the page has loaded.</summary>
    public void Resend()
    {
        _lastSvg = null;
        ScheduleState();
        ScheduleDrawing();
        ScheduleScene();
    }

    private static Task<object?> Answer(object? value) => Task.FromResult(value);

    private KataViewState State()
    {
        var vm = _viewModel;
        return new KataViewState(
            ThemeName(),
            new KataHeaderState(vm.BeamNameValue, vm.BeamCountValue, vm.HeightValue, vm.WidthValue, vm.SlabThicknessValue,
                vm.AxisGridNameValue, vm.AxisOffsetValue, vm.ElevationValue, vm.BeamCountText, vm.TotalLengthText),
            vm.AvailableParameters,
            vm.SelectedNameParameter,
            vm.SelectedCountParameter,
            vm.IsReverse,
            vm.IsBusy,
            vm.StatusMessage,
            vm.HasError,
            vm.TargetWorkbook,
            vm.Warnings,
            vm.PreviewColumns,
            vm.SelectedColumnIndex,
            vm.SelectionText,
            vm.ShowRebar,
            vm.ShowSection,
            vm.ShowBarTags,
            new KataCommandStates(
                vm.ExportCommand.CanExecute(null),
                vm.RepickCommand.CanExecute(null),
                vm.LoadRebarCommand.CanExecute(null),
                vm.GenerateRebarCommand.CanExecute(null),
                vm.UndoRemoveBarsCommand.CanExecute(null)));
    }

    private KataViewState Update(KataUpdateRequest request)
    {
        var vm = _viewModel;
        if (request.NameParameter is { } name) vm.SelectedNameParameter = Known(name);
        if (request.CountParameter is { } count) vm.SelectedCountParameter = Known(count);
        if (request.Reverse is { } reverse) vm.IsReverse = reverse;
        if (request.ShowRebar is { } showRebar) vm.ShowRebar = showRebar;
        if (request.ShowSection is { } showSection) vm.ShowSection = showSection;
        if (request.ShowBarTags is { } showBarTags) vm.ShowBarTags = showBarTags;
        if (request.SelectedColumn is { } column)
        {
            if (column < -1 || column >= vm.PreviewColumns.Count)
                throw new BridgeException(BridgeErrorCodes.BadRequest, $"Column {column} is not in the table (0…{vm.PreviewColumns.Count - 1}, -1 for none).");
            vm.SelectedColumnIndex = column;
        }

        return State();
    }

    /// <summary>An empty name (no parameter) or one the beams carry; anything else is the page's mistake.</summary>
    private string Known(string parameter) =>
        parameter.Length == 0 || _viewModel.AvailableParameters.Contains(parameter)
            ? parameter
            : throw new BridgeException(BridgeErrorCodes.BadRequest, $"The beams have no parameter '{parameter}'.");

    /// <summary>
    ///     Starts the command and answers at once: picking beams or generating bars can take minutes, longer than any
    ///     request should wait. The view model reports progress and the outcome in its status, sent as state.
    /// </summary>
    private KataRunResult Run(string action)
    {
        var vm = _viewModel;
        ICommand command = action switch
        {
            KataWebActions.Export => vm.ExportCommand,
            KataWebActions.Repick => vm.RepickCommand,
            KataWebActions.RefreshExcel => vm.RefreshProbeCommand,
            KataWebActions.LoadRebar => vm.LoadRebarCommand,
            KataWebActions.GenerateRebar => vm.GenerateRebarCommand,
            KataWebActions.UndoRemoveBars => vm.UndoRemoveBarsCommand,
            KataWebActions.PreviousSpan => vm.PreviousSpanCommand,
            KataWebActions.NextSpan => vm.NextSpanCommand,
            KataWebActions.FrameAll => vm.FrameAllCommand,
            KataWebActions.Close => vm.CloseCommand,
            _ => throw new BridgeException(BridgeErrorCodes.BadRequest, $"Unknown action '{action}'."),
        };

        if (!command.CanExecute(null))
            throw new BridgeException(BridgeErrorCodes.Busy, $"'{action}' is not available now ({vm.StatusMessage}).");

        // The view model's async commands catch and report their own failures; this only guards against a bug there.
        if (command is IAsyncRelayCommand asyncCommand)
            asyncCommand.ExecuteAsync(null).ContinueWith(
                task => Log.Error(task.Exception, "Kata web: action {Action} failed", action),
                CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
        else
            command.Execute(null);

        return new KataRunResult(true);
    }

    private object? Resize(KataViewportRequest request)
    {
        if (!double.IsFinite(request.Width) || !double.IsFinite(request.Height))
            throw new BridgeException(BridgeErrorCodes.BadRequest, "Width and height must be numbers.");

        _canvas.Width = Math.Clamp(Math.Round(request.Width), MinViewport, MaxViewport);
        _canvas.Height = Math.Clamp(Math.Round(request.Height), MinViewport, MaxViewport);
        Resend();
        return null;
    }

    private bool Gesture(KataCanvasInput input)
    {
        var at = new Point(input.X, input.Y);
        switch (input.Kind)
        {
            case KataCanvasGestures.Zoom:
                return _canvas.ZoomAt(at, Math.Clamp(input.Notches, -10.0, 10.0));
            case KataCanvasGestures.Pan:
                _canvas.PanFrom(at, new Vector(input.Dx, input.Dy));
                return true;
            case KataCanvasGestures.Click:
                _canvas.ClickAt(at);
                return true;
            case KataCanvasGestures.ZoomExtents:
                _canvas.ZoomExtentsAt(at);
                return true;
            case KataCanvasGestures.Key:
                return KeyOf(input.Key) is { } key && _canvas.KeyPressed(key, input.Ctrl ? ModifierKeys.Control : ModifierKeys.None);
            default:
                throw new BridgeException(BridgeErrorCodes.BadRequest, $"Unknown gesture '{input.Kind}'.");
        }
    }

    private static Key? KeyOf(string? key) => key switch
    {
        "Delete" => Key.Delete,
        "Escape" => Key.Escape,
        "z" or "Z" => Key.Z,
        _ => null,
    };

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        ScheduleState();
        OnSceneInputChanged(e.PropertyName);
    }

    private void OnThemeChanged() => _dispatcher.BeginInvoke(ScheduleState);

    private void ScheduleState()
    {
        if (_statePending) return;
        _statePending = true;
        _dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
        {
            _statePending = false;
            _publish(KataWebEvents.State, State());
        });
    }

    /// <summary>After a paint, reads what WPF recorded (nothing is painted twice) and sends it when it changed.</summary>
    private void ScheduleDrawing()
    {
        if (_drawingPending) return;
        _drawingPending = true;
        _dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
        {
            _drawingPending = false;
            try
            {
                var svg = DrawingSvgWriter.Write(VisualTreeHelper.GetDrawing(_canvas), _canvas.ActualWidth, _canvas.ActualHeight);
                if (svg == _lastSvg) return;
                _lastSvg = svg;
                _publish(KataWebEvents.Drawing, new KataDrawingFrame(++_revision, _canvas.ActualWidth, _canvas.ActualHeight, svg, _canvas.HasBarSelection));
            }
            catch (Exception ex)
            {
                // A drawing that cannot be written leaves the page on the previous one; the WPF canvas logs its own failures.
                Log.Error(ex, "Kata web: the elevation could not be written as SVG");
            }
        });
    }

    private string ThemeName() => _theme.IsDark ? "dark" : "light";
}
