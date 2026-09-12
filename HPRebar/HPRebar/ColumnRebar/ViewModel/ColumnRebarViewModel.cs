using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPRebar.ColumnRebar.Model;
using HPRebar.ColumnRebar.ViewModel.Tabs;
using Serilog;
using HPRebar.ColumnRebar.Service;

namespace HPRebar.ColumnRebar.ViewModel;

/// <summary>
///     Whatever actually builds the reinforcement. The window only needs to know it reports progress and
///     says how much it made, which keeps this view model free of Revit types.
/// </summary>
public interface IColumnRebarRunner
{
    /// <summary>Total elements the settings will produce, for sizing the progress bar.</summary>
    int PlannedCount(IReadOnlyList<ColumnRebarSpec> specs);

    /// <summary>
    ///     Builds everything. Returns how many elements were created, or throws, in which case nothing is
    ///     left behind. Completes once Revit has run it, which is not the call that started it.
    /// </summary>
    Task<int> RunAsync(IReadOnlyList<ColumnRebarSpec> specs, IProgress<int> progress);
}

/// <summary>The window itself: navigation, the footer buttons and the language toggle.</summary>
public sealed partial class ColumnRebarViewModel : ObservableObject
{
    private readonly IColumnRebarRunner _runner;

    [ObservableProperty] private ColumnRebarTabViewModel _selectedTab = null!;
    [ObservableProperty] private int _progress;
    [ObservableProperty] private int _progressMaximum = 1;
    [ObservableProperty] private string _statusMessage = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private bool _isBusy;

    public ColumnRebarViewModel(
        ColumnRebarSession session,
        LocalizationService localization,
        IColumnRebarRunner runner)
    {
        Session = session;
        Localization = localization;
        _runner = runner;

        Tabs = new ObservableCollection<ColumnRebarTabViewModel>
        {
            new SettingTabViewModel(session, localization),
            new GeometryTabViewModel(session, localization),
            new StirrupsTabViewModel(session, localization),
            new AdditionalStirrupsTabViewModel(session, localization),
            new BarsTabViewModel(session, localization),
            new TopDowelsTabViewModel(session, localization),
            new BottomDowelsTabViewModel(session, localization),
            new BarsDivisionTabViewModel(session, localization)
        };

        SelectedTab = Tabs[0];
    }

    public ColumnRebarSession Session { get; }

    public LocalizationService Localization { get; }

    public ObservableCollection<ColumnRebarTabViewModel> Tabs { get; }

    /// <summary>Raised once the user is finished. The view closes itself on this.</summary>
    public event Action? CloseRequested;

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync()
    {
        if (!Session.IsValid(out var reason))
        {
            StatusMessage = reason;
            return;
        }

        var specs = Session.ToSpecs();

        IsBusy = true;
        StatusMessage = Localization.Strings.Working;

        try
        {
            ProgressMaximum = Math.Max(1, _runner.PlannedCount(specs));
            Progress = 0;

            var created = await _runner.RunAsync(specs, new Progress<int>(value => Progress = value));

            if (created == 0)
            {
                StatusMessage = Localization.Strings.NothingToCreate;
                return;
            }

            CloseRequested?.Invoke();
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Column Rebar could not build the reinforcement");
            StatusMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanRun() => !IsBusy;

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke();

    /// <summary>Swaps every label to the other language without closing the window.</summary>
    [RelayCommand]
    private void ToggleLanguage()
    {
        Localization.Toggle();

        // Tab titles read from the language record, which the toolkit cannot know about.
        foreach (var tab in Tabs) tab.NotifyTitleChanged();
    }

    /// <summary>Recomputes the schedule tab whenever the user opens it, so it never shows stale rows.</summary>
    partial void OnSelectedTabChanged(ColumnRebarTabViewModel value)
    {
        switch (value)
        {
            case BarsDivisionTabViewModel schedule:
                schedule.Refresh();
                break;

            case BarsTabViewModel bars:
                bars.Refresh();
                break;
        }
    }
}
