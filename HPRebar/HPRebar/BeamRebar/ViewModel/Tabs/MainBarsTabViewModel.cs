using System.Collections.Generic;
using HPRebar.BeamRebar.Model;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.BeamRebar.Service;

namespace HPRebar.BeamRebar.ViewModel.Tabs;

/// <summary>
/// ViewModel for configuring continuous top and bottom longitudinal reinforcement, anchorage hooks, and lap splices.
/// </summary>
public sealed partial class MainBarsTabViewModel : BeamRebarTabViewModel
{
    public MainBarsTabViewModel(BeamRebarSession session, LocalizationService localization)
        : base(session, localization)
    {
    }

    public override string Title => Localization.Strings.TabMainBars;

    public IReadOnlyList<EndAnchorageType> TopAnchorageOptions { get; } = new[]
    {
        EndAnchorageType.Hook90Down,
        EndAnchorageType.None
    };

    public IReadOnlyList<EndAnchorageType> BottomAnchorageOptions { get; } = new[]
    {
        EndAnchorageType.Hook90Up,
        EndAnchorageType.None
    };
}
