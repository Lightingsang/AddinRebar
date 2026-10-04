using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.Shared;

namespace HPRebar.Core.BeamRebar.Calculators;

/// <summary>
/// What has to hold before a beam run can be reinforced: two bars or more top and bottom, positive spacings and
/// cover, numbers (not NaN or infinity) for the start offset, lap factor and stock length, chosen bar types, view
/// names Revit accepts, spans wide and deep enough for the bars, and stirrup, side-bar and cross-tie counts
/// Revit will take.
/// </summary>
public static class BeamSpecRules
{
    /// <summary>
    /// The first problem found, as a sentence for the user, or null when the run can be reinforced. One problem at
    /// a time, so the user fixes one thing at a time.
    /// </summary>
    /// <param name="mainBars">Top and bottom bar counts and diameters.</param>
    /// <param name="stirrups">Spacings, node spacing, cover and stirrup diameter.</param>
    /// <param name="sideBars">
    /// Skin-bar and cross-tie settings, checked only when a span is deep enough for them.
    /// </param>
    /// <param name="specialBars">Hanging-stirrup settings, checked only when hanging stirrups are on.</param>
    /// <param name="barTypesChosen">
    /// Whether the top, bottom and stirrup bar types are all chosen; the bar diameters in the specs only mean
    /// something when they are, and nothing after this check is run when they are not.
    /// </param>
    /// <param name="viewNames">Every view name the run will create, concatenated.</param>
    /// <param name="spans">The spans of the run.</param>
    public static string? FirstProblem(
        BeamMainBarSpec mainBars,
        BeamStirrupSpec stirrups,
        BeamSideBarSpec sideBars,
        BeamSpecialBarSpec specialBars,
        bool barTypesChosen,
        string viewNames,
        IReadOnlyList<BeamSpan> spans)
    {
        if (mainBars is null)
        {
            throw new ArgumentNullException(nameof(mainBars));
        }

        if (stirrups is null)
        {
            throw new ArgumentNullException(nameof(stirrups));
        }

        if (sideBars is null)
        {
            throw new ArgumentNullException(nameof(sideBars));
        }

        if (specialBars is null)
        {
            throw new ArgumentNullException(nameof(specialBars));
        }

        if (viewNames is null)
        {
            throw new ArgumentNullException(nameof(viewNames));
        }

        if (spans is null)
        {
            throw new ArgumentNullException(nameof(spans));
        }

        if (mainBars.TopCount < 2 || mainBars.BottomCount < 2)
        {
            return "Top and bottom main longitudinal reinforcement must each have at least 2 bars.";
        }

        double cover = stirrups.Cover;
        if (!FiniteNumber.IsPositive(stirrups.SpacingDense)
            || !FiniteNumber.IsPositive(stirrups.SpacingSparse)
            || !FiniteNumber.IsPositive(cover))
        {
            return "Stirrup spacing and concrete cover must be positive values greater than zero.";
        }

        if (!FiniteNumber.IsFinite(stirrups.StartOffset))
        {
            return "Stirrup start offset must be a number.";
        }

        if (!FiniteNumber.IsFinite(mainBars.LapFactor))
        {
            return "Lap length factor must be a number.";
        }

        if (!FiniteNumber.IsFinite(mainBars.MaxStockLength))
        {
            return "Bar stock length must be a number.";
        }

        if (specialBars.EnableHangingStirrups && !FiniteNumber.IsPositive(specialBars.HangingStirrupSpacing))
        {
            return "Hanging stirrup spacing must be greater than zero.";
        }

        if (!barTypesChosen)
        {
            return "Please ensure main top, bottom, and stirrup rebar types are selected.";
        }

        if (stirrups.IncludeStirrupsInNodes && !FiniteNumber.IsPositive(stirrups.NodeSpacing))
        {
            return "Column node stirrup spacing must be greater than zero.";
        }

        bool sideBarsNeeded = sideBars.AutoSkinBars && spans.Any(span => NeedsSideBars(span, sideBars));
        if (sideBarsNeeded && !FiniteNumber.IsPositive(sideBars.MaxVerticalSpacing))
        {
            return "Side bar vertical spacing must be greater than zero.";
        }

        if (sideBarsNeeded && sideBars.IncludeCrossTies && !FiniteNumber.IsPositive(sideBars.CrossTieSpacing))
        {
            return "Cross-tie spacing must be greater than zero.";
        }

        if (RevitViewNames.TryFindForbiddenCharacter(viewNames, out var character))
        {
            return $"View names cannot contain '{character}' (Revit refuses {RevitViewNames.ForbiddenCharacters}).";
        }

        double minimumSize = (2.0 * cover) + (2.0 * stirrups.Diameter)
            + Math.Max(mainBars.TopDiameter, mainBars.BottomDiameter);
        foreach (var span in spans)
        {
            string? problem = SpanProblem(span, stirrups, minimumSize)
                ?? SideBarProblem(span, mainBars, stirrups, sideBars);
            if (problem is not null)
            {
                return problem;
            }
        }

        return null;
    }

    private static string? SpanProblem(BeamSpan span, BeamStirrupSpec stirrups, double minimumSize)
    {
        if (span.Width <= minimumSize)
        {
            return $"Span {span.Name}: Beam width ({span.Width:0.#} mm) is too narrow for cover ({stirrups.Cover:0.#} mm) and bar sizes.";
        }

        if (span.Height <= minimumSize)
        {
            return $"Span {span.Name}: Beam height ({span.Height:0.#} mm) is too shallow for cover ({stirrups.Cover:0.#} mm) and bar sizes.";
        }

        double dense = StirrupCount(span, stirrups.SpacingDense);
        if (dense > RevitRebarLimits.MaxBarPositions)
        {
            return $"Span {span.Name}: Dense stirrup spacing produces {dense:0} ties, exceeding Revit's {RevitRebarLimits.MaxBarPositions} limit.";
        }

        double sparse = StirrupCount(span, stirrups.SpacingSparse);
        if (sparse > RevitRebarLimits.MaxBarPositions)
        {
            return $"Span {span.Name}: Sparse stirrup spacing produces {sparse:0} ties, exceeding Revit's {RevitRebarLimits.MaxBarPositions} limit.";
        }

        return null;
    }

    /// <summary>
    /// Side-bar rows and cross-ties per row of a deep span, counted the way the calculator lays them out (with the
    /// window's cover, the stirrup and the bottom bar), against Revit's bar limit.
    /// </summary>
    private static string? SideBarProblem(
        BeamSpan span, BeamMainBarSpec mainBars, BeamStirrupSpec stirrups, BeamSideBarSpec sideBars)
    {
        if (!sideBars.AutoSkinBars || !NeedsSideBars(span, sideBars))
        {
            return null;
        }

        double rows = BeamSideBarCalculator.CountRows(
            span.Height, stirrups.Cover, stirrups.Diameter, mainBars.BottomDiameter, sideBars.MaxVerticalSpacing);
        if (rows > RevitRebarLimits.MaxBarPositions)
        {
            return $"Span {span.Name}: Side bar spacing produces {rows:0} rows, exceeding the {RevitRebarLimits.MaxBarPositions}-bar limit.";
        }

        double ties = BeamSideBarCalculator.CrossTiesPerRow(span.LengthClear, sideBars.CrossTieSpacing);
        if (sideBars.IncludeCrossTies && ties > RevitRebarLimits.MaxBarPositions)
        {
            return $"Span {span.Name}: Cross-tie spacing produces {ties:0} ties per row, exceeding the {RevitRebarLimits.MaxBarPositions}-bar limit.";
        }

        return null;
    }

    /// <summary>
    /// Whether the calculator gives this span side bars: not shallower than the user's threshold (a NaN threshold
    /// skips nothing there either) and at least the 700 mm the row count starts from.
    /// </summary>
    private static bool NeedsSideBars(BeamSpan span, BeamSideBarSpec sideBars) =>
        !(span.Height < sideBars.DepthThreshold) && span.Height >= BeamSideBarCalculator.HeightThresholdMm;

    /// <summary>
    /// Stirrups needed over the whole clear span at one spacing: a generous bound on any zone. Kept in double so a
    /// tiny spacing cannot overflow an int and slip under the limit.
    /// </summary>
    private static double StirrupCount(BeamSpan span, double spacing) => Math.Ceiling(span.LengthClear / spacing) + 1;
}
