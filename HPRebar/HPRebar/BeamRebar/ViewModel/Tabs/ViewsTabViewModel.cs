using System.Collections.Generic;
using HPRebar.BeamRebar.Model;
using HPRebar.BeamRebar.Service;

namespace HPRebar.BeamRebar.ViewModel.Tabs;

/// <summary>
/// ViewModel for configuring automated detail elevation view, cross-sections per span, dimensioning, and tags.
/// </summary>
public sealed partial class ViewsTabViewModel : BeamRebarTabViewModel
{
    public ViewsTabViewModel(BeamRebarSession session, LocalizationService localization)
        : base(session, localization)
    {
    }

    public override string Title => Localization.Strings.TabSettings;

    public IReadOnlyList<int> ScaleOptions { get; } = new[] { 20, 25, 50, 100 };
    public IReadOnlyList<int> SectionsPerSpanOptions { get; } = new[] { 2, 3 };
}
