using System.Collections.Generic;
using HPRebar.ColumnRebar.ViewModel;
using HPRebar.Core.ColumnRebar;
using HPRebar.Core.ColumnRebar.Models;

namespace HPRebar.ColumnRebar.View.Controls;

/// <summary>
///     Builds the bar centre-lines the preview draws, from exactly the same calculators the creation
///     service uses — so what the user sees is what gets built.
/// </summary>
internal static class ElevationBars
{
    /// <summary>Centre-lines for one column segment, empty when the settings are not yet workable.</summary>
    public static IReadOnlyList<BarPolyline> For(ColumnSpecEditor column, ColumnSpecEditor? above = null)
    {
        var layout = column.ToLayout();

        if (layout.BarDiameter <= 0 || column.Splices.Count < layout.BarCount) return new List<BarPolyline>();

        // Bar counts mid-edit (a cleared text box) are not a layout; nothing to draw until they are valid again.
        if (!column.IsLayoutValid) return new List<BarPolyline>();

        var splices = new List<SpliceSpec>(layout.BarCount);

        for (var i = 0; i < layout.BarCount; i++) splices.Add(column.Splices[i].ToSpec());

        return ColumnBarPolylines.Compute(
            column.Section, layout, splices, above?.Section,
            column.StirrupBarType?.DiameterMm ?? 0,
            (above ?? column).StirrupBarType?.DiameterMm ?? 0);
    }
}
