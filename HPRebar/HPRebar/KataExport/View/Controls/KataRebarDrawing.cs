using System;
using System.Collections.Generic;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// What the canvas draws of one rebar plan, in model millimetres: the tags, the section cuts and Kata's elevation
/// (outline, bars, stirrups, dimensions, flags), and each section as Kata draws it once it is asked for. Built once
/// per plan, so a pan or a wheel notch only maps millimetres to pixels.
/// </summary>
internal sealed class KataRebarDrawing
{
    private readonly Dictionary<(int Number, bool Mirror), KataSectionDrawing?> _sections = new();

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

    /// <summary>
    /// Section <paramref name="cut"/> as Kata draws it (one drawing per number: cuts sharing it cut the same bars);
    /// null when it cannot be drawn: logged once, not tried again on every repaint.
    /// </summary>
    /// <param name="mirror">Seen from the other side: the drawing lists the run backwards.</param>
    public KataSectionDrawing? Section(KataSectionCut cut, bool mirror)
    {
        if (cut is null) throw new ArgumentNullException(nameof(cut));
        if (_sections.TryGetValue((cut.Number, mirror), out var section)) return section;
        try
        {
            section = KataSectionDrawingBuilder.Build(Plan.Spec, Plan.Layout, Plan.Rules, cut, mirror);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Kata Export: drawing section {Number} failed", cut.Number);
            section = null;
        }

        return _sections[(cut.Number, mirror)] = section;
    }

    public static KataRebarDrawing For(KataRebarPlan plan, KataRebarDrawing? cached) =>
        cached is not null && ReferenceEquals(cached.Plan, plan) ? cached : new KataRebarDrawing(plan ?? throw new ArgumentNullException(nameof(plan)));
}
