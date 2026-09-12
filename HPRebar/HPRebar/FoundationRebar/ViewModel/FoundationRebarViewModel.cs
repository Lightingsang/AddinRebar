using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPRebar.Core.FoundationRebar.Calculators;
using HPRebar.FoundationRebar.Model;
using Serilog;

namespace HPRebar.FoundationRebar.ViewModel;

/// <summary>
///     Root view model for the Foundation Rebar window: geometry review, parameter customization, and the
///     validation that has to pass before anything is written.
/// </summary>
public sealed partial class FoundationRebarViewModel : ObservableObject
{
    private readonly IFoundationRebarRunner _runner;

    [ObservableProperty] private string _validationMessage = string.Empty;

    [ObservableProperty] private bool _hasValidationError;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    private bool _isBusy;

    public FoundationRebarViewModel(FoundationSession session, IFoundationRebarRunner runner)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));

        Geometry = new FoundationGeometryViewModel(session.Snapshot);
        Setting = new FoundationSettingViewModel(session.Spec);
    }

    public FoundationSession Session { get; }

    public FoundationGeometryViewModel Geometry { get; }

    public FoundationSettingViewModel Setting { get; }

    /// <summary>Raised once the user is finished. The view closes itself on this.</summary>
    public event Action? CloseRequested;

    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task ApplyAsync()
    {
        var spec = Setting.ToSpec();
        var validation = FoundationValidationCalculator.Validate(Session.Snapshot, spec);

        if (!validation.IsValid)
        {
            ValidationMessage = validation.ErrorMessage ?? "Validation failed.";
            HasValidationError = true;
            return;
        }

        HasValidationError = false;
        ValidationMessage = string.Empty;
        Session.Spec = spec;

        IsBusy = true;

        try
        {
            await _runner.RunAsync(Session);

            CloseRequested?.Invoke();
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Foundation Rebar run failed");
            ValidationMessage = exception.Message;
            HasValidationError = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanApply() => !IsBusy;

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke();
}
