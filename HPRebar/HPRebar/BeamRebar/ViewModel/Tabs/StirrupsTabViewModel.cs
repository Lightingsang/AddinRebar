using System.Collections.Generic;
using HPRebar.BeamRebar.Model;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.BeamRebar.Service;

namespace HPRebar.BeamRebar.ViewModel.Tabs;

/// <summary>
/// ViewModel for configuring shear stirrup distributions, deep beam skin reinforcement, and secondary hanging stirrups.
/// </summary>
public sealed partial class StirrupsTabViewModel : BeamRebarTabViewModel
{
    public StirrupsTabViewModel(BeamRebarSession session, LocalizationService localization)
        : base(session, localization)
    {
    }

    public override string Title => Localization.Strings.TabStirrups;

    public IReadOnlyList<StirrupLayout> LayoutOptions { get; } = new[]
    {
        StirrupLayout.ThreeZoneL4,
        StirrupLayout.ThreeZoneL3,
        StirrupLayout.Uniform
    };
}
