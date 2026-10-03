using System;
using System.Collections.Generic;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// What the canvas draws of one rebar plan, in model millimetres: the tags, the section cuts and Kata's elevation
/// (outline, bars, stirrups, dimensions, flags). Built once per plan, so a pan or a wheel notch only maps millimetres
/// to pixels.
/// </summary>
internal sealed class KataRebarDrawing
{
    private KataRebarDrawing(KataRebarPlan plan)
    {
        Plan = plan;
        Tags = KataBarTagBuilder.Build(plan.Spec, plan.Layout, plan.Rules.StirrupDiameter);
        Cuts = KataSectionCuts.Build(plan.Spec, plan.Layout);
        Elevation = KataElevationDrawingBuilder.Build(plan.Spec, plan.Layout, Cuts, plan.Rules.StirrupDiameter);
    }

    public KataRebarPlan Plan { get; }

    public IReadOnlyList<KataBarTag> Tags { get; }

    public IReadOnlyList<KataSectionCut> Cuts { get; }

    public KataElevationDrawing Elevation { get; }

    public static KataRebarDrawing For(KataRebarPlan plan, KataRebarDrawing? cached) =>
        cached is not null && ReferenceEquals(cached.Plan, plan) ? cached : new KataRebarDrawing(plan ?? throw new ArgumentNullException(nameof(plan)));
}
