using System.Collections.Generic;
using HPRebar.ColumnRebar.ViewModel;
using HPRebar.ColumnRebar.Service;

namespace HPRebar.ColumnRebar.ViewModel.Tabs;

/// <summary>Intermediate cross-ties, horizontal and vertical legs configured separately.</summary>
public sealed partial class AdditionalStirrupsTabViewModel : ColumnRebarTabViewModel
{
    public AdditionalStirrupsTabViewModel(ColumnRebarSession session, LocalizationService localization)
        : base(session, localization)
    {
    }

    public override string Title => Localization.Strings.TabAdditionalStirrups;

    public override string IconKey => "TabIcon.AdditionalStirrups";

    /// <summary>
    ///     Type 0 is a closed tie sized by its leg length; the rest are single cross-ties differing only in
    ///     how their ends are hooked.
    /// </summary>
    public IReadOnlyList<string> TieTypes { get; } = new[]
    {
        "Closed tie (by leg length)",
        "Cross-tie, 90 degree hooks",
        "Cross-tie, 135 degree hooks",
        "Cross-tie, 180 degree hooks"
    };
}
