using System;
using System.Collections.Generic;
using HPRebar.Core.ColumnRebar.Models;

namespace HPRebar.Core.ColumnRebar;

/// <summary>
///     Places the main bars on a section. All millimetres, measured from the stack datum.
/// </summary>
public static class BarLayoutCalculator
{
    /// <summary>
    ///     Bar positions in numbering order: south face west to east, east face south to north,
    ///     north face east to west, west face north to south. Circular sections run counter-clockwise
    ///     from the positive X axis.
    /// </summary>
    public static IReadOnlyList<BarPosition> Compute(ColumnSection section, BarLayoutSpec spec)
    {
        if (section is null) throw new ArgumentNullException(nameof(section));
        if (spec is null) throw new ArgumentNullException(nameof(spec));

        return section.Shape == SectionShape.Rectangle
            ? ComputeRectangle(section, spec)
            : ComputeCircular(section, spec);
    }

    private static IReadOnlyList<BarPosition> ComputeRectangle(ColumnSection section, BarLayoutSpec spec)
    {
        if (spec.Nx < 2) throw new ArgumentOutOfRangeException(nameof(spec), spec.Nx, "Nx must be at least 2.");
        if (spec.Ny < 2) throw new ArgumentOutOfRangeException(nameof(spec), spec.Ny, "Ny must be at least 2.");

        var d = spec.BarDiameter;
        var inset = spec.Cover + spec.StirrupDiameter + d / 2;
        var deltaX = (section.B - 2 * spec.Cover - 2 * spec.StirrupDiameter - d) / (spec.Nx - 1);
        var deltaY = (section.H - 2 * spec.Cover - 2 * spec.StirrupDiameter - d) / (spec.Ny - 1);
        var xBase = section.WestPosition + inset;
        var yBase = section.SouthPosition + inset;

        var bars = new List<BarPosition>(spec.BarCount);

        // South face, west to east. Corners belong here.
        for (var i = 0; i < spec.Nx; i++)
        {
            bars.Add(new BarPosition
            {
                BarNumber = bars.Count + 1,
                X0 = xBase + i * deltaX,
                Y0 = yBase,
                Side = BarSide.South
            });
        }

        var lastColumn = spec.Nx - 1;

        // East face, climbing between the two south/north corners.
        for (var j = 1; j < spec.Ny - 1; j++)
        {
            bars.Add(new BarPosition
            {
                BarNumber = bars.Count + 1,
                X0 = xBase + lastColumn * deltaX,
                Y0 = yBase + j * deltaY,
                Side = BarSide.East
            });
        }

        var topRow = spec.Ny - 1;

        // North face, east back to west.
        for (var k = 0; k < spec.Nx; k++)
        {
            bars.Add(new BarPosition
            {
                BarNumber = bars.Count + 1,
                X0 = xBase + (lastColumn - k) * deltaX,
                Y0 = yBase + topRow * deltaY,
                Side = BarSide.North
            });
        }

        // West face, descending back towards the south-west corner.
        for (var l = 1; l < spec.Ny - 1; l++)
        {
            bars.Add(new BarPosition
            {
                BarNumber = bars.Count + 1,
                X0 = xBase,
                Y0 = yBase + (topRow - l) * deltaY,
                Side = BarSide.West
            });
        }

        return bars;
    }

    private static IReadOnlyList<BarPosition> ComputeCircular(ColumnSection section, BarLayoutSpec spec)
    {
        if (spec.Nd <= 0 || spec.Nd % 4 != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(spec), spec.Nd, "Nd must be a positive multiple of four.");
        }

        var radius = BarRadius(section, spec);
        var bars = new List<BarPosition>(spec.Nd);

        for (var i = 0; i < spec.Nd; i++)
        {
            var angle = i * Math.PI * 2 / spec.Nd;

            bars.Add(new BarPosition
            {
                BarNumber = i + 1,
                X0 = section.CenterX + radius * Math.Round(Math.Cos(angle), 9),
                Y0 = section.CenterY + radius * Math.Round(Math.Sin(angle), 9),
                // A circular section has no faces; callers use BarSideClassifier.QuadrantOf instead.
                Side = BarSide.South
            });
        }

        return bars;
    }

    /// <summary>Radius the bar centres sit on, inside cover and ties.</summary>
    public static double BarRadius(ColumnSection section, BarLayoutSpec spec) =>
        section.D * 0.5 - spec.Cover - spec.StirrupDiameter - spec.BarDiameter * 0.5;
}
