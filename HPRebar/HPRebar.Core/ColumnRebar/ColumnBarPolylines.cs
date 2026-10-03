using System;
using System.Collections.Generic;
using HPRebar.Core.ColumnRebar.Models;

namespace HPRebar.Core.ColumnRebar;

/// <summary>
///     The main-bar centre-lines of one column segment, from the same three steps wherever they are needed —
///     the model, the bar schedule and the preview — so what the user sees is what gets built: bar positions
///     from the layout, where each bar continues in the column above, then the bent bar itself.
/// </summary>
public static class ColumnBarPolylines
{
    /// <param name="section">The segment the bars are placed in.</param>
    /// <param name="layout">Bar counts, diameters and cover of the segment.</param>
    /// <param name="splices">Exactly one splice per bar, in bar-number order.</param>
    /// <param name="above">The segment above, or null at the top of the stack.</param>
    /// <param name="stirrupDiameter">Tie diameter of this segment (mm).</param>
    /// <param name="stirrupAboveDiameter">Tie diameter of the segment above, or of this one at the top (mm).</param>
    /// <param name="barTypeName">Revit bar type name carried on each polyline; empty for the preview.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     The bar counts are not a layout (<see cref="ColumnSpecRules.IsLayoutValid"/> is false) — check it first.
    /// </exception>
    /// <exception cref="ArgumentException">The splice count differs from the bar count.</exception>
    public static IReadOnlyList<BarPolyline> Compute(
        ColumnSection section,
        BarLayoutSpec layout,
        IReadOnlyList<SpliceSpec> splices,
        ColumnSection? above,
        double stirrupDiameter,
        double stirrupAboveDiameter,
        string barTypeName = "")
    {
        if (splices is null) throw new ArgumentNullException(nameof(splices));

        var bars = BarLayoutCalculator.Compute(section, layout);
        var upper = SpliceCalculator.ComputeUpperPositions(
            above, layout, stirrupDiameter, stirrupAboveDiameter, bars, splices);

        var polylines = new List<BarPolyline>(bars.Count);
        for (var b = 0; b < bars.Count; b++)
        {
            polylines.Add(BarPolylineBuilder.Build(section, layout, bars[b], splices[b], upper[b], barTypeName));
        }

        return polylines;
    }
}
