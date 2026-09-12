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

        IReadOnlyList<BarPosition> bars;

        try
        {
            bars = BarLayoutCalculator.Compute(column.Section, layout);
        }
        catch (System.ArgumentOutOfRangeException)
        {
            // Bar counts mid-edit, e.g. a cleared text box. Nothing to draw until they are valid again.
            return new List<BarPolyline>();
        }

        var splices = new List<SpliceSpec>(bars.Count);

        for (var i = 0; i < bars.Count; i++) splices.Add(column.Splices[i].ToSpec());

        var upper = SpliceCalculator.ComputeUpperPositions(
            above?.Section,
            layout,
            column.StirrupBarType?.DiameterMm ?? 0,
            (above ?? column).StirrupBarType?.DiameterMm ?? 0,
            bars,
            splices);

        var polylines = new List<BarPolyline>(bars.Count);

        for (var i = 0; i < bars.Count; i++)
        {
            polylines.Add(BarPolylineBuilder.Build(column.Section, layout, bars[i], splices[i], upper[i]));
        }

        return polylines;
    }
}
