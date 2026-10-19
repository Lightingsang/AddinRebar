using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPRebar.BeamRebar.Model;
using HPRebar.BeamRebar.Service;
using HPRebar.BeamRebar.ViewModel.Tabs;
using Serilog;

namespace HPRebar.BeamRebar.ViewModel;

/// <summary>
/// Display modes for the continuous beam preview pane.
/// </summary>
public enum BeamPreviewMode
{
    TwoD,
    ThreeD,
    Split
}

/// <summary>
/// Master ViewModel managing the continuous beam reinforcement window, tabs, runner execution,
/// progress reporting, language toggling, and theming.
/// </summary>
public sealed partial class BeamRebarViewModel : ObservableObject
{
    private readonly IBeamRebarRunner _runner;

    [ObservableProperty] private BeamRebarTabViewModel _selectedTab = null!;
    [ObservableProperty] private int _progress;
    [ObservableProperty] private int _progressMaximum = 1;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private BeamPreviewMode _previewMode = BeamPreviewMode.TwoD;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private bool _isBusy;

    public event Action? CloseRequested;

    public BeamRebarViewModel(
        BeamRebarSession session,
        LocalizationService localization,
        IBeamRebarRunner runner)
    {
        Session = session;
        Localization = localization;
        _runner = runner;

        Tabs = new ObservableCollection<BeamRebarTabViewModel>
        {
            new GeometryTabViewModel(session, localization),
            new MainBarsTabViewModel(session, localization),
            new AdditionalBarsTabViewModel(session, localization),
            new StirrupsTabViewModel(session, localization),
            new ViewsTabViewModel(session, localization)
        };

        SelectedTab = Tabs[0];
    }

    public BeamRebarSession Session { get; }
    public LocalizationService Localization { get; }
    public ObservableCollection<BeamRebarTabViewModel> Tabs { get; }

    public bool IsTwoDMode
    {
        get => PreviewMode == BeamPreviewMode.TwoD;
        set
        {
            if (value)
            {
                PreviewMode = BeamPreviewMode.TwoD;
            }
        }
    }

    public bool IsThreeDMode
    {
        get => PreviewMode == BeamPreviewMode.ThreeD;
        set
        {
            if (value)
            {
                PreviewMode = BeamPreviewMode.ThreeD;
            }
        }
    }

    public bool IsSplitMode
    {
        get => PreviewMode == BeamPreviewMode.Split;
        set
        {
            if (value)
            {
                PreviewMode = BeamPreviewMode.Split;
            }
        }
    }

    partial void OnPreviewModeChanged(BeamPreviewMode value)
    {
        OnPropertyChanged(nameof(IsTwoDMode));
        OnPropertyChanged(nameof(IsThreeDMode));
        OnPropertyChanged(nameof(IsSplitMode));
    }

    [RelayCommand]
    private void SetPreviewMode(BeamPreviewMode mode)
    {
        PreviewMode = mode;
    }

    [RelayCommand]
    private void ToggleLanguage()
    {
        Localization.Toggle();
        foreach (var tab in Tabs)
        {
            tab.NotifyTitleChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync()
    {
        if (!Session.Validate(out var validationError))
        {
            StatusMessage = validationError;
            RevitDialogs.Warning(Localization.Strings.WindowTitle, validationError);
            return;
        }

        IsBusy = true;
        StatusMessage = Localization.Strings.Working;
        Progress = 0;

        try
        {
            var spec = Session.ToSpec();
            ProgressMaximum = Math.Max(1, _runner.PlannedCount(spec));

            var reporter = new Progress<int>(value => Progress = value);
            int created = await _runner.RunAsync(spec, reporter);

            if (created == 0)
            {
                StatusMessage = Localization.Strings.NothingToCreate;
                return;
            }

            Log.Information("Beam Rebar created {Count} elements successfully", created);
            CloseRequested?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Beam Rebar run failed");
            StatusMessage = ex.Message;
            RevitDialogs.Error(Localization.Strings.WindowTitle, ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanRun() => !IsBusy;

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke();
}
