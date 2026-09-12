using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using HPRebar.ColumnRebar.ViewModel;
using HPRebar.Core.ColumnRebar;
using HPRebar.Core.ColumnRebar.Models;
using HPRebar.ColumnRebar.Service;

namespace HPRebar.ColumnRebar.ViewModel.Tabs;

/// <summary>Main bar count, size and lap settings, with the resulting layout shown back to the user.</summary>
public sealed partial class BarsTabViewModel : ColumnRebarTabViewModel
{
    private ColumnSpecEditor? _watched;

    public BarsTabViewModel(ColumnRebarSession session, LocalizationService localization)
        : base(session, localization)
    {
        session.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ColumnRebarSession.SelectedColumn)) Watch();
        };

        Watch();
    }

    /// <summary>
    ///     Follows the column currently being edited. The table has to react to the bar counts changing,
    ///     not just to a different column being picked.
    /// </summary>
    private void Watch()
    {
        if (_watched is not null) _watched.PropertyChanged -= OnColumnChanged;

        _watched = Session.SelectedColumn;

        if (_watched is not null) _watched.PropertyChanged += OnColumnChanged;

        Refresh();
    }

    private void OnColumnChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args) => Refresh();

    public override string Title => Localization.Strings.TabBars;

    public override string IconKey => "TabIcon.Bars";

    /// <summary>Where each bar lands on the section, recomputed whenever the counts change.</summary>
    [ObservableProperty]
    private IReadOnlyList<BarPosition> _positions = new List<BarPosition>();

    /// <summary>Percentages of bars spliced at the same level. 50 staggers alternate bars.</summary>
    public IReadOnlyList<double> SplitOverlaps { get; } = new[] { 50d, 100d };

    public void Refresh()
    {
        var column = Session.SelectedColumn;

        // Bar counts are half-typed as often as they are finished; the calculator rejects the in-between
        // states, so the table simply empties until they make sense again.
        if (column is null || !column.IsLayoutValid)
        {
            Positions = new List<BarPosition>();
            return;
        }

        Positions = BarLayoutCalculator.Compute(column.Section, column.ToLayout()).ToList();
    }
}
