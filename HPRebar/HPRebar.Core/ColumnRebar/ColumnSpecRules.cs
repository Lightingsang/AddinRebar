using System;
using HPRebar.Core.ColumnRebar.Models;
using HPRebar.Core.Shared;

namespace HPRebar.Core.ColumnRebar;

/// <summary>
///     What has to hold before one column segment can be built: a bar layout the calculator accepts, bars that
///     fit inside the cover and ties, a tie run Revit will take, and complete cross-tie settings.
/// </summary>
public static class ColumnSpecRules
{
    /// <summary>
    ///     Whether the bar counts are ones the layout calculator will accept: at least two bars along each side
    ///     of a rectangle, a positive multiple of four around a circle. The calculator throws on anything else.
    /// </summary>
    public static bool IsLayoutValid(SectionShape shape, BarLayoutSpec layout) =>
        shape == SectionShape.Rectangle
            ? layout.Nx >= 2 && layout.Ny >= 2
            : layout.Nd > 0 && layout.Nd % 4 == 0;

    /// <summary>
    ///     The first problem found, worded to follow the column's name ("C1: …"), or null when the column can be built.
    ///     One problem at a time, so the user fixes one thing at a time.
    /// </summary>
    public static string? FirstProblem(
        ColumnSection section,
        BarLayoutSpec layout,
        StirrupSpec stirrups,
        AdditionalTieSpec ties)
    {
        if (section is null) throw new ArgumentNullException(nameof(section));
        if (layout is null) throw new ArgumentNullException(nameof(layout));
        if (stirrups is null) throw new ArgumentNullException(nameof(stirrups));
        if (ties is null) throw new ArgumentNullException(nameof(ties));

        bool rectangular = section.Shape == SectionShape.Rectangle;

        if (!IsLayoutValid(section.Shape, layout))
        {
            return rectangular
                ? "at least two bars are needed along each side."
                : "the bar count around a circular column must be a positive multiple of four.";
        }

        if (!FiniteNumber.IsFinite(layout.Cover))
        {
            return "the cover must be a number.";
        }

        // The bars have to physically fit inside the cover and the ties.
        var clearance = 2 * layout.Cover + 2 * layout.StirrupDiameter + layout.BarDiameter;
        var narrowest = rectangular ? Math.Min(section.B, section.H) : section.D;

        if (clearance >= narrowest)
        {
            return $"cover and bar sizes leave no room inside a {narrowest:0} mm section.";
        }

        return TieRunProblem(section, stirrups) ?? CrossTieProblem(ties);
    }

    /// <summary>
    ///     Tie spacing has to produce a bar count Revit will accept. A spacing of a few millimetres over a
    ///     storey height quietly asks for thousands of ties, which the API refuses.
    /// </summary>
    private static string? TieRunProblem(ColumnSection section, StirrupSpec stirrups)
    {
        var run = StirrupDistributionCalculator.ComputeRunLength(section, stirrups.IsTiesUp);

        if (run <= 0)
        {
            return "the beam is as deep as the column, leaving nowhere to put ties.";
        }

        var spacings = stirrups.TypeDis == 0
            ? new[] { stirrups.S }
            : new[] { stirrups.S1, stirrups.S2 };

        foreach (var spacing in spacings)
        {
            if (!FiniteNumber.IsPositive(spacing))
            {
                return "tie spacing must be greater than zero.";
            }

            // In double so a tiny spacing cannot overflow; Truncate keeps the old int cast's rounding.
            if (Math.Truncate(run / spacing) + 1 > RevitRebarLimits.MaxBarPositions)
            {
                return $"a spacing of {spacing:0} mm needs more than {RevitRebarLimits.MaxBarPositions} ties, which Revit will not accept.";
            }
        }

        return null;
    }

    private static string? CrossTieProblem(AdditionalTieSpec ties)
    {
        if (ties.AddH && ties.TypeH == 0 && !FiniteNumber.IsPositive(ties.AH))
        {
            return "give the horizontal cross-tie a leg length.";
        }

        if (ties.AddV && ties.TypeV == 0 && !FiniteNumber.IsPositive(ties.AV))
        {
            return "give the vertical cross-tie a leg length.";
        }

        if (ties.AddH && ties.TypeH != 0 && ties.NH < 1)
        {
            return "at least one horizontal cross-tie is required.";
        }

        if (ties.AddV && ties.TypeV != 0 && ties.NV < 1)
        {
            return "at least one vertical cross-tie is required.";
        }

        return null;
    }
}
