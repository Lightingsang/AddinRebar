using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using HPRebar.BeamRebar.Model;
using HPRebar.BeamRebar.Service;

namespace HPRebar.BeamRebar.ViewModel.Tabs;

/// <summary>
/// ViewModel for configuring additional negative moment top bars over supports and positive moment bottom bars at midspan.
/// </summary>
public sealed partial class AdditionalBarsTabViewModel : BeamRebarTabViewModel
{
    public AdditionalBarsTabViewModel(BeamRebarSession session, LocalizationService localization)
        : base(session, localization)
    {
    }

    public override string Title => Localization.Strings.TabAddTopBars;

    public ObservableCollection<SupportTopBarEditor> SupportTopBars => Session.SupportTopBars;
    public ObservableCollection<SpanBottomBarEditor> SpanBottomBars => Session.SpanBottomBars;

    public SupportTopBarEditor? SelectedSupport => Session.SelectedSupportEditor;
    public SpanBottomBarEditor? SelectedSpan => Session.SelectedSpanEditor;

    [RelayCommand]
    private void ApplySupportToAll() => Session.ApplySelectedSupportToAll();

    [RelayCommand]
    private void ApplySpanToAll() => Session.ApplySelectedSpanToAll();
}
