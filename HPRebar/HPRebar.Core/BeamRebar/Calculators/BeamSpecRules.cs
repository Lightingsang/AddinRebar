using System;
using System.Collections.Generic;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.Shared;

namespace HPRebar.Core.BeamRebar.Calculators;

/// <summary>
/// What has to hold before a beam run can be reinforced: two bars or more top and bottom, positive spacings and
/// cover, chosen bar types, view names Revit accepts, and spans wide and deep enough for the bars and a stirrup
/// count Revit will take.
/// </summary>
public static class BeamSpecRules
{
    /// <summary>
    /// The first problem found, as a sentence for the user, or null when the run can be reinforced. One problem at
    /// a time, so the user fixes one thing at a time.
    /// </summary>
    /// <param name="mainBars">Top and bottom bar counts and diameters.</param>
    /// <param name="stirrups">Spacings, node spacing, cover and stirrup diameter.</param>
    /// <param name="barTypesChosen">
    /// Whether the top, bottom and stirrup bar types are all chosen; the bar diameters in the specs only mean
    /// something when they are, and nothing after this check is run when they are not.
    /// </param>
    /// <param name="viewNames">Every view name the run will create, concatenated.</param>
    /// <param name="spans">The spans of the run.</param>
    public static string? FirstProblem(
        BeamMainBarSpec mainBars,
        BeamStirrupSpec stirrups,
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
        if (stirrups.SpacingDense <= 0 || stirrups.SpacingSparse <= 0 || !(cover > 0) || double.IsInfinity(cover))
        {
            return "Stirrup spacing and concrete cover must be positive values greater than zero.";
        }

        if (!barTypesChosen)
        {
            return "Please ensure main top, bottom, and stirrup rebar types are selected.";
        }

        if (stirrups.IncludeStirrupsInNodes && stirrups.NodeSpacing <= 0)
        {
            return "Column node stirrup spacing must be greater than zero.";
        }

        if (RevitViewNames.TryFindForbiddenCharacter(viewNames, out var character))
        {
            return $"View names cannot contain '{character}' (Revit refuses {RevitViewNames.ForbiddenCharacters}).";
        }

        double minimumSize = (2.0 * cover) + (2.0 * stirrups.Diameter)
            + Math.Max(mainBars.TopDiameter, mainBars.BottomDiameter);
        foreach (var span in spans)
        {
            string? problem = SpanProblem(span, stirrups, minimumSize);
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

        int dense = StirrupCount(span, stirrups.SpacingDense);
        if (dense > RevitRebarLimits.MaxBarPositions)
        {
            return $"Span {span.Name}: Dense stirrup spacing produces {dense} ties, exceeding Revit's {RevitRebarLimits.MaxBarPositions} limit.";
        }

        int sparse = StirrupCount(span, stirrups.SpacingSparse);
        if (sparse > RevitRebarLimits.MaxBarPositions)
        {
            return $"Span {span.Name}: Sparse stirrup spacing produces {sparse} ties, exceeding Revit's {RevitRebarLimits.MaxBarPositions} limit.";
        }

        return null;
    }

    /// <summary>Stirrups needed over the whole clear span at one spacing: a generous bound on any zone.</summary>
    private static int StirrupCount(BeamSpan span, double spacing) => (int)Math.Ceiling(span.LengthClear / spacing) + 1;
}
