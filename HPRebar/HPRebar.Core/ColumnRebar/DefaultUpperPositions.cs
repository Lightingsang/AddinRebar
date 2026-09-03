using System;
using System.Collections.Generic;
using HPRebar.Core.ColumnRebar.Models;

namespace HPRebar.Core.ColumnRebar;

/// <summary>
///     Where a bar would land on the section above if it simply kept its place in the grid.
///     Used for bars that are not being redistributed, and as the single-bar fallback when a face has only
///     one bar carrying on upward.
///
///     The corner nudges of one bar diameter push the transition bars clear of the bars already standing in
///     the upper column, so the two do not fight for the same spot.
/// </summary>
public static class DefaultUpperPositions
{
    /// <summary>X of every bar number, in numbering order, on the upper section.</summary>
    public static IReadOnlyList<double> X(ColumnSection upper, BarLayoutSpec spec, double stirrupDiameter, double barDiameter)
    {
        if (upper is null) throw new ArgumentNullException(nameof(upper));
        if (spec is null) throw new ArgumentNullException(nameof(spec));

        if (upper.Shape == SectionShape.Circular)
        {
            return Circular(upper, spec, stirrupDiameter, barDiameter, Math.Cos, upper.CenterX);
        }

        var inset = spec.Cover + stirrupDiameter + barDiameter / 2;
        var deltaX = (upper.B - 2 * spec.Cover - 2 * stirrupDiameter - barDiameter) / (spec.Nx - 1);
        var xBase = upper.WestPosition + inset;
        var lastColumn = spec.Nx - 1;
        var values = new List<double>(spec.BarCount);

        for (var i = 0; i < spec.Nx; i++)
        {
            var nudge = i == lastColumn ? -barDiameter : barDiameter;
            values.Add(xBase + nudge + i * deltaX);
        }

        for (var j = 1; j < spec.Ny - 1; j++)
        {
            values.Add(xBase + lastColumn * deltaX);
        }

        for (var k = 0; k < spec.Nx; k++)
        {
            var nudge = k == 0 ? -barDiameter : barDiameter;
            values.Add(xBase + nudge + (lastColumn - k) * deltaX);
        }

        for (var l = 1; l < spec.Ny - 1; l++)
        {
            values.Add(xBase);
        }

        return values;
    }

    /// <summary>Y of every bar number, in numbering order, on the upper section.</summary>
    public static IReadOnlyList<double> Y(ColumnSection upper, BarLayoutSpec spec, double stirrupDiameter, double barDiameter)
    {
        if (upper is null) throw new ArgumentNullException(nameof(upper));
        if (spec is null) throw new ArgumentNullException(nameof(spec));

        if (upper.Shape == SectionShape.Circular)
        {
            return Circular(upper, spec, stirrupDiameter, barDiameter, Math.Sin, upper.CenterY);
        }

        var inset = spec.Cover + stirrupDiameter + barDiameter / 2;
        var deltaY = (upper.H - 2 * spec.Cover - 2 * stirrupDiameter - barDiameter) / (spec.Ny - 1);
        var yBase = upper.SouthPosition + inset;
        var topRow = spec.Ny - 1;
        var values = new List<double>(spec.BarCount);

        for (var i = 0; i < spec.Nx; i++)
        {
            values.Add(yBase);
        }

        for (var j = 1; j < spec.Ny - 1; j++)
        {
            values.Add(yBase + j * deltaY + barDiameter);
        }

        for (var k = 0; k < spec.Nx; k++)
        {
            values.Add(yBase + topRow * deltaY);
        }

        for (var l = 1; l < spec.Ny - 1; l++)
        {
            values.Add(yBase + (topRow - l) * deltaY + barDiameter);
        }

        return values;
    }

    private static IReadOnlyList<double> Circular(
        ColumnSection upper,
        BarLayoutSpec spec,
        double stirrupDiameter,
        double barDiameter,
        Func<double, double> component,
        double center)
    {
        var radius = upper.D * 0.5 - spec.Cover - stirrupDiameter - barDiameter * 0.5;

        // The source tool measures this seed offset against the full circumference rather than the radius,
        // so it is a very small rotation. Kept as-is: changing it would move every ported bar.
        var seed = barDiameter / (radius * 2 * Math.PI);
        var values = new List<double>(spec.Nd);

        for (var i = 0; i < spec.Nd; i++)
        {
            var angle = seed + i * Math.PI * 2 / spec.Nd;
            values.Add(center + radius * Math.Round(component(angle), 9));
        }

        return values;
    }
}
