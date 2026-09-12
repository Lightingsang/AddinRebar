using System.Collections.Generic;
using HPRebar.ColumnRebar.ViewModel;
using HPRebar.ColumnRebar.Service;

namespace HPRebar.ColumnRebar.ViewModel.Tabs;

/// <summary>How each bar starts at the base of its column segment.</summary>
public sealed partial class BottomDowelsTabViewModel : ColumnRebarTabViewModel
{
    public BottomDowelsTabViewModel(ColumnRebarSession session, LocalizationService localization)
        : base(session, localization)
    {
    }

    public override string Title => Localization.Strings.TabBottomDowels;

    public override string IconKey => "TabIcon.BottomDowels";

    /// <summary>
    ///     Type 0 starts the bar clear of the base; the others run it down past the base, with or without
    ///     a horizontal hook.
    /// </summary>
    public IReadOnlyList<string> DowelStyles { get; } = new[]
    {
        "Start above the base",
        "Run down past the base"
    };

    /// <summary>Copies the first bar's bottom settings onto every other bar of this column.</summary>
    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void ApplyToAllBars()
    {
        var column = Session.SelectedColumn;

        if (column is null || column.Splices.Count == 0) return;

        var source = column.Splices[0].ToSpec();

        foreach (var splice in column.Splices)
        {
            splice.IsBottomDowels = source.IsBottomDowels;
            splice.BottomDowelsType = source.BottomDowelsType;
            splice.LaBottom = source.LaBottom;
            splice.LbBottom = source.LbBottom;
            splice.LcBottom = source.LcBottom;
        }
    }
}
