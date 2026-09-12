using System.Collections.Generic;
using HPRebar.ColumnRebar.ViewModel;
using HPRebar.ColumnRebar.Service;

namespace HPRebar.ColumnRebar.ViewModel.Tabs;

/// <summary>Tie size and how the ties are spread up the column.</summary>
public sealed partial class StirrupsTabViewModel : ColumnRebarTabViewModel
{
    public StirrupsTabViewModel(ColumnRebarSession session, LocalizationService localization)
        : base(session, localization)
    {
    }

    public override string Title => Localization.Strings.TabStirrups;

    public override string IconKey => "TabIcon.Stirrups";

    /// <summary>
    ///     The four spacing patterns, in the order their numbers run: even, then progressively longer
    ///     sparse middles.
    /// </summary>
    public IReadOnlyList<string> DistributionTypes { get; } = new[]
    {
        "Even",
        "Dense ends, 1/2 sparse",
        "Dense ends, 2/3 sparse",
        "Dense ends, 3/4 sparse"
    };
}
