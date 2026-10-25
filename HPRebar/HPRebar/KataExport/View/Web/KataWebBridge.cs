using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;
using HPRebar.KataExport.ViewModel;
using HPRebar.Resources.Themes;
using HPWebHost.Protocol;
using Serilog;

namespace HPRebar.KataExport.View.Web;

/// <summary>
///     The web window's code-behind: turns <see cref="KataExportViewModel" /> into <see cref="KataViewState" /> for the
///     page, runs the page's commands on the view-model commands, and sends what the page draws as model-space records
///     (the Scene partial). Everything runs on the window's UI thread (the transport dispatches page messages there);
///     state and scene are each sent at most once per dispatcher pass.
/// </summary>
internal sealed partial class KataWebBridge : IDisposable
{
    private readonly KataExportViewModel _viewModel;
    private readonly IHostTheme _theme;
    private readonly Action<string, object> _publish;
    private readonly Dispatcher _dispatcher;
    private bool _statePending;

    /// <param name="publish">Sends an event to the page (dropped while the page is not up).</param>
    /// <param name="dispatcher">The window's: page messages and view-model changes arrive on it.</param>
    public KataWebBridge(KataExportViewModel viewModel, Dispatcher dispatcher, IHostTheme theme, Action<string, object> publish)
    {
        _viewModel = viewModel;
        _theme = theme;
        _publish = publish;
        _dispatcher = dispatcher;
        _viewModel.PropertyChanged += OnViewModelChanged;
        _theme.Changed += OnThemeChanged;
    }

    public void Register(CommandDispatcher dispatcher)
    {
        dispatcher.Register(KataWebCommands.GetState, (_, _) => Answer(State()));
        dispatcher.Register(KataWebCommands.Ready, (_, _) => Answer(Ready()));
        dispatcher.Register<KataUpdateRequest>(KataWebCommands.Update, (request, _) => Answer(Update(request)));
        dispatcher.Register<KataRunRequest>(KataWebCommands.Run, (request, _) => Answer(Run(request.Action)));
        dispatcher.Register<KataSectionRequest>(KataWebCommands.Section, (request, _) => Answer(Section(request)));
        dispatcher.Register<KataRemoveBarsRequest>(KataWebCommands.RemoveBars, (request, _) => Answer(RemoveBars(request)));
        dispatcher.Register(KataWebCommands.Model3d, (_, _) => Answer(Model3d()));
    }

    public void Dispose()
    {
        _viewModel.PropertyChanged -= OnViewModelChanged;
        _theme.Changed -= OnThemeChanged;
    }

    private static Task<object?> Answer(object? value) => Task.FromResult(value);

    /// <summary>The page says so once it has loaded (or reloaded): the scene it draws follows as an event.</summary>
    private object? Ready()
    {
        ScheduleScene();
        return null;
    }

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

    private string ThemeName() => _theme.IsDark ? "dark" : "light";
}
