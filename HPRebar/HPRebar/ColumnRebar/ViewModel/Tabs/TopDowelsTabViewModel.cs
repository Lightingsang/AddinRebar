using HPRebar.ColumnRebar.ViewModel;
using HPRebar.ColumnRebar.Service;

namespace HPRebar.ColumnRebar.ViewModel.Tabs;

/// <summary>How each bar finishes at the top of its column segment.</summary>
public sealed partial class TopDowelsTabViewModel : ColumnRebarTabViewModel
{
    public TopDowelsTabViewModel(ColumnRebarSession session, LocalizationService localization)
        : base(session, localization)
    {
    }

    public override string Title => Localization.Strings.TabTopDowels;

    public override string IconKey => "TabIcon.TopDowels";


    /// <summary>Copies the first bar's top settings onto every other bar of this column.</summary>
    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void ApplyToAllBars()
    {
        var column = Session.SelectedColumn;

        if (column is null || column.Splices.Count == 0) return;

        var source = column.Splices[0].ToSpec();

        foreach (var splice in column.Splices)
        {
            splice.IsTopDowels = source.IsTopDowels;
            splice.TopDowelsType = source.TopDowelsType;
            splice.LaTop = source.LaTop;
            splice.LbTop = source.LbTop;
        }
    }
}
