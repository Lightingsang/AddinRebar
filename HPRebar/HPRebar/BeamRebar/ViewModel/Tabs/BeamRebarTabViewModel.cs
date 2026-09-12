using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPRebar.BeamRebar.Model;
using HPRebar.BeamRebar.Service;

namespace HPRebar.BeamRebar.ViewModel.Tabs;

/// <summary>
/// Abstract base class for tab view models in the Continuous Beam Rebar configuration window.
/// </summary>
public abstract partial class BeamRebarTabViewModel : ObservableObject
{
    protected BeamRebarTabViewModel(BeamRebarSession session, LocalizationService localization)
    {
        Session = session;
        Localization = localization;
    }

    public BeamRebarSession Session { get; }
    public LocalizationService Localization { get; }

    /// <summary>Localized navigation header label.</summary>
    public abstract string Title { get; }

    /// <summary>Refreshes Title property when language toggle is invoked.</summary>
    public void NotifyTitleChanged() => OnPropertyChanged(nameof(Title));

    /// <summary>Applies current span's parameters to all continuous spans.</summary>
    [RelayCommand]
    protected virtual void ApplyToAllSpans() => Session.ApplySelectedSpanToAll();

    /// <summary>Applies current support's parameters to all supports.</summary>
    [RelayCommand]
    protected virtual void ApplyToAllSupports() => Session.ApplySelectedSupportToAll();
}
