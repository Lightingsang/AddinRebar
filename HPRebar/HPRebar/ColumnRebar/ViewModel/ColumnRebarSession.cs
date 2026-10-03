using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using HPRebar.ColumnRebar.Model;
using HPRebar.Core.ColumnRebar.Models;

namespace HPRebar.ColumnRebar.ViewModel;

/// <summary>
///     The state every tab shares: the stack that was picked, the settings being edited, and which column
///     the user is looking at. Tabs hold a reference to this rather than talking to each other.
/// </summary>
public sealed partial class ColumnRebarSession : ObservableObject
{
    [ObservableProperty] private int _selectedColumnIndex;
    [ObservableProperty] private string _partitionName = "Column Rebar";
    [ObservableProperty] private string _detailViewName = ViewNaming.Default.DetailViewName;
    [ObservableProperty] private string _sectionSuffix = ViewNaming.Default.SectionSuffix;
    [ObservableProperty] private int _identicalColumns = 1;

    /// <summary>
    ///     Kept so the detail-item drawing mode can be switched on later without reshaping this class.
    ///     The tool only models real rebar today, and the checkbox bound to it is disabled.
    /// </summary>
    [ObservableProperty] private bool _useRealRebar = true;

    public ColumnRebarSession(
        ColumnStack stack,
        IReadOnlyList<ColumnRebarSpec> specs,
        IReadOnlyList<RebarTypeInfo> barTypes)
    {
        Stack = stack;
        BarTypes = new ObservableCollection<RebarTypeInfo>(barTypes);

        Columns = new ObservableCollection<ColumnSpecEditor>(
            stack.Sections.Select((section, i) => new ColumnSpecEditor(section, specs[i])));
    }

    public ColumnStack Stack { get; }

    public ObservableCollection<ColumnSpecEditor> Columns { get; }

    public ObservableCollection<RebarTypeInfo> BarTypes { get; }

    /// <summary>The column the tabs are currently editing. Never null while a stack is loaded.</summary>
    public ColumnSpecEditor? SelectedColumn =>
        SelectedColumnIndex >= 0 && SelectedColumnIndex < Columns.Count ? Columns[SelectedColumnIndex] : null;

    /// <summary>The segment stacked on top of the selected one, or null when it is the highest.</summary>
    public ColumnSpecEditor? ColumnAbove =>
        SelectedColumnIndex + 1 < Columns.Count ? Columns[SelectedColumnIndex + 1] : null;

    /// <summary>The segment carrying the selected one, or null when it is the lowest.</summary>
    public ColumnSpecEditor? ColumnBelow =>
        SelectedColumnIndex > 0 ? Columns[SelectedColumnIndex - 1] : null;

    public bool IsRectangular => Stack.Style == ColumnSectionStyle.Rectangle;

    /// <summary>Settings for every column, converted back to the immutable form the services take.</summary>
    public IReadOnlyList<ColumnRebarSpec> ToSpecs() =>
        Columns.Select(column => column.ToSpec(PartitionName)).ToList();

    /// <summary>The view names typed on the settings tab.</summary>
    public ViewNaming ToViewNaming() => ViewNaming.From(DetailViewName, SectionSuffix);

    /// <summary>
    ///     Whether the settings can actually build something. Mirrors the checks the original tool ran
    ///     before enabling its OK button.
    /// </summary>
    public bool IsValid(out string reason)
    {
        if (ToViewNaming().TryFindForbiddenCharacter(out var character))
        {
            reason = $"View names cannot contain '{character}' (Revit refuses {ViewNaming.ForbiddenCharacters}).";

            return false;
        }

        foreach (var column in Columns)
        {
            if (column.Validate(out var problem)) continue;

            reason = $"{column.DisplayName}: {problem}";

            return false;
        }

        reason = string.Empty;

        return true;
    }

    /// <summary>Copies the selected column's tie and bar settings onto every other column.</summary>
    public void ApplySelectedToAll()
    {
        var source = SelectedColumn;

        if (source is null) return;

        foreach (var target in Columns)
        {
            if (ReferenceEquals(target, source)) continue;

            target.MainBarType = source.MainBarType;
            target.StirrupBarType = source.StirrupBarType;
            target.TieBarType = source.TieBarType;
            target.Cover = source.Cover;
            target.SplitOverlap = source.SplitOverlap;
            target.OverlapFactor = source.OverlapFactor;

            target.DistributionType = source.DistributionType;
            target.Spacing = source.Spacing;
            target.SpacingDense = source.SpacingDense;
            target.SpacingSparse = source.SpacingSparse;
            target.TiesUpToBeams = source.TiesUpToBeams;

            target.AddHorizontalTies = source.AddHorizontalTies;
            target.HorizontalTieType = source.HorizontalTieType;
            target.HorizontalTieCount = source.HorizontalTieCount;
            target.HorizontalTieLeg = source.HorizontalTieLeg;

            target.AddVerticalTies = source.AddVerticalTies;
            target.VerticalTieType = source.VerticalTieType;
            target.VerticalTieCount = source.VerticalTieCount;
            target.VerticalTieLeg = source.VerticalTieLeg;

            if (source.IsRectangular && target.IsRectangular)
            {
                target.BarsAlongWidth = source.BarsAlongWidth;
                target.BarsAlongDepth = source.BarsAlongDepth;
            }
            else if (!source.IsRectangular && !target.IsRectangular)
            {
                target.BarsAround = source.BarsAround;
            }
        }
    }

    partial void OnSelectedColumnIndexChanged(int value)
    {
        OnPropertyChanged(nameof(SelectedColumn));
        OnPropertyChanged(nameof(ColumnAbove));
        OnPropertyChanged(nameof(ColumnBelow));
    }
}
