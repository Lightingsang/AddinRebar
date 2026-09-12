using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPRebar.ColumnRebar.ViewModel;
using HPRebar.Core.ColumnRebar;
using HPRebar.Core.ColumnRebar.Models;
using HPRebar.ColumnRebar.Service;

namespace HPRebar.ColumnRebar.ViewModel.Tabs;

/// <summary>The bar schedule the current settings would produce, before anything is built.</summary>
public sealed partial class BarsDivisionTabViewModel : ColumnRebarTabViewModel
{
    public BarsDivisionTabViewModel(ColumnRebarSession session, LocalizationService localization)
        : base(session, localization)
    {
        Refresh();
    }

    public override string Title => Localization.Strings.TabBarsDivision;

    public override string IconKey => "TabIcon.BarsDivision";

    [ObservableProperty]
    private IReadOnlyList<BarScheduleItem> _items = new List<BarScheduleItem>();

    /// <summary>Recomputes the schedule from the current settings.</summary>
    [RelayCommand]
    public void Refresh()
    {
        var rows = new List<BarScheduleItem>();

        for (var i = 0; i < Session.Columns.Count; i++)
        {
            var column = Session.Columns[i];
            var layout = column.ToLayout();

            if (!column.IsLayoutValid || column.Splices.Count < layout.BarCount) continue;

            var above = i + 1 < Session.Columns.Count ? Session.Columns[i + 1] : null;
            var bars = BarLayoutCalculator.Compute(column.Section, layout);
            var splices = column.Splices.Take(layout.BarCount).Select(splice => splice.ToSpec()).ToList();

            var upper = SpliceCalculator.ComputeUpperPositions(
                above?.Section, layout,
                column.StirrupBarType.DiameterMm,
                (above ?? column).StirrupBarType.DiameterMm,
                bars, splices);

            var polylines = bars
                .Select((bar, b) => BarPolylineBuilder.Build(
                    column.Section, layout, bar, splices[b], upper[b], column.MainBarType.Name))
                .ToList();

            rows.AddRange(BarScheduleCalculator.Group(polylines, Session.IdenticalColumns));
        }

        Items = rows;
    }
}
