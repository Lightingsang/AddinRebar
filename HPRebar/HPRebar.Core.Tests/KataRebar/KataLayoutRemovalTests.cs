using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>Groups of bars struck off Kata's elevation: what is left, its numbers, and every drawing still built from it.</summary>
public sealed class KataLayoutRemovalTests
{
    [Fact]
    public void Every_bar_and_stirrup_zone_has_its_own_key_and_the_same_one_when_planned_again()
    {
        var first = Keys(Plan().Layout);
        var again = Keys(Plan().Layout);

        Assert.Equal(first.Count, first.Distinct().Count());
        Assert.Equal(first, again);
    }

    [Fact]
    public void Every_bar_and_stirrup_line_of_the_elevation_names_the_groups_it_stands_for()
    {
        var plan = Plan();
        var drawing = Elevation(plan);
        var lines = drawing.Lines.Where(l => l.Pen is KataDrawingPen.Bar or KataDrawingPen.Stirrup).ToList();

        Assert.All(lines, l => Assert.NotEmpty(l.Keys!));
        Assert.All(drawing.Lines.Where(l => l.Pen is not (KataDrawingPen.Bar or KataDrawingPen.Stirrup)), l => Assert.Null(l.Keys));
        var drawn = lines.SelectMany(l => l.Keys!).ToHashSet();
        Assert.All(plan.Layout.LongitudinalBars, b => Assert.Contains(KataLayoutRemoval.Key(b), drawn));
        Assert.All(plan.Layout.StirrupZones.Where(z => z.Count > 0), z => Assert.Contains(KataLayoutRemoval.Key(z), drawn));
    }

    [Fact]
    public void Striking_an_additional_top_line_removes_its_bars_and_numbers_the_rest_from_1_without_gaps()
    {
        var plan = Plan();
        var line = Elevation(plan).Lines.First(l => l.Pen == KataDrawingPen.Bar && l.Keys!.All(k => k.StartsWith("bar|ExtraTop|")));

        var edited = KataLayoutRemoval.Remove(plan, line.Keys!, out var unknown);

        Assert.Empty(unknown);
        Assert.Equal(plan.Layout.ExtraTopBars.Count - line.Keys!.Count, edited.Layout.ExtraTopBars.Count);
        Assert.DoesNotContain(edited.Layout.ExtraTopBars, b => line.Keys!.Contains(KataLayoutRemoval.Key(b)));
        Assert.Equal(plan.Layout.MainTopBars.Count, edited.Layout.MainTopBars.Count);
        Assert.True(edited.Layout.TotalSteelWeightKg < plan.Layout.TotalSteelWeightKg);
        AssertNumberedWithoutGaps(edited.Layout);
        AssertStillDrawn(edited);
    }

    [Fact]
    public void Striking_a_stirrup_run_removes_its_zones_and_their_single_stirrups()
    {
        var plan = Plan();
        var line = Elevation(plan).Lines.First(l => l.Pen == KataDrawingPen.Stirrup);
        var zones = plan.Layout.StirrupZones.Where(z => line.Keys!.Contains(KataLayoutRemoval.Key(z))).ToList();

        var edited = KataLayoutRemoval.Remove(plan, line.Keys!, out _);

        Assert.Equal(plan.Layout.StirrupZones.Count - zones.Count, edited.Layout.StirrupZones.Count);
        Assert.Equal(plan.Layout.IndividualStirrups.Count - zones.Sum(z => z.Count), edited.Layout.IndividualStirrups.Count);
        AssertNumberedWithoutGaps(edited.Layout);
        AssertStillDrawn(edited);
    }

    [Fact]
    public void A_beam_without_its_top_main_bars_or_any_stirrup_is_still_drawn()
    {
        var plan = Plan();
        var keys = plan.Layout.MainTopBars.Select(KataLayoutRemoval.Key).Concat(plan.Layout.StirrupZones.Select(KataLayoutRemoval.Key)).ToList();

        var edited = KataLayoutRemoval.Remove(plan, keys, out _);

        Assert.Empty(edited.Layout.MainTopBars);
        Assert.Empty(edited.Layout.StirrupZones);
        AssertNumberedWithoutGaps(edited.Layout);
        AssertStillDrawn(edited);
    }

    [Fact]
    public void Keys_the_plan_does_not_have_are_reported_and_change_nothing()
    {
        var plan = Plan();

        var edited = KataLayoutRemoval.Remove(plan, new[] { "bar|MainTop|9999|20|1" }, out var unknown);

        Assert.Same(plan, edited);
        Assert.Equal(new[] { "bar|MainTop|9999|20|1" }, unknown);
        Assert.Same(plan, KataLayoutRemoval.Remove(plan, new string[0], out _));
    }

    [Fact]
    public void Removing_from_a_plan_built_again_gives_the_same_numbers()
    {
        var plan = Plan();
        var keys = plan.Layout.ExtraBottomBars.Take(1).Select(KataLayoutRemoval.Key).ToList();

        var a = KataLayoutRemoval.Remove(plan, keys, out _);
        var b = KataLayoutRemoval.Remove(Plan(), keys, out _);

        Assert.Equal(Numbers(a.Layout), Numbers(b.Layout));
    }

    [Fact]
    public void Striking_the_middle_zone_of_a_span_leaves_its_two_dense_zones_drawn_apart()
    {
        var plan = Plan();
        var middle = plan.Layout.StirrupZones.Where(z => z.SpanIndex == 1).OrderBy(z => z.Stations[0]).ElementAt(1);
        Assert.Equal(3, plan.Layout.StirrupZones.Count(z => z.SpanIndex == 1));

        var edited = KataLayoutRemoval.Remove(plan, new[] { KataLayoutRemoval.Key(middle) }, out _);

        var runs = KataStirrupRuns.Of(edited.Layout).Where(r => r.Span == 1).ToList();
        Assert.Equal(2, runs.Count);
        Assert.All(runs, r => Assert.True(r.Last < middle.Stations[0] || r.First > middle.Stations[middle.Stations.Count - 1]));
        var lines = Elevation(edited).Lines.Where(l => l.Pen == KataDrawingPen.Stirrup && l.Keys!.Any(k => k.StartsWith("zone|1|", System.StringComparison.Ordinal)));
        Assert.All(lines, l => Assert.Single(l.Keys!));
    }

    [Fact]
    public void Inner_stirrups_go_with_their_zone_and_the_C_ties_stay()
    {
        // Rows 25-44: a U stirrup around bars 1-2 in every zone of the single-span live-check beam.
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("C25", "Đai U");
        table.Set("D25", "1-2");
        var plan = KataRebarPlanner.Plan(KataDamSheetParser.Parse(table), KataRebarTestSheets.MeasuredSingleSpan());
        Assert.Contains(plan.Layout.BarSets, s => !KataBarNumbering.IsTie(s));
        var zone = plan.Layout.StirrupZones.First(z => z.Count > 0);
        int inner = plan.Layout.BarSets.Count(s => !KataBarNumbering.IsTie(s) && s.SpanIndex == zone.SpanIndex && s.ZoneName == zone.ZoneName);
        int ties = plan.Layout.BarSets.Count(KataBarNumbering.IsTie);

        var edited = KataLayoutRemoval.Remove(plan, new[] { KataLayoutRemoval.Key(zone) }, out _);

        Assert.Equal(plan.Layout.BarSets.Count - inner, edited.Layout.BarSets.Count);
        Assert.Equal(ties, edited.Layout.BarSets.Count(KataBarNumbering.IsTie));
        AssertNumberedWithoutGaps(edited.Layout);
    }

    [Fact]
    public void The_fingerprint_changes_when_the_beam_plans_other_bars()
    {
        var table = KataDy7DrawingTests.Sheet();
        table.Set("B7", 120.0);
        table.Set("C14", "4f18");
        var spec = KataDamSheetParser.Parse(table);

        Assert.Equal(KataLayoutRemoval.Fingerprint(Plan().Layout), KataLayoutRemoval.Fingerprint(Plan().Layout));
        Assert.NotEqual(KataLayoutRemoval.Fingerprint(Plan().Layout), KataLayoutRemoval.Fingerprint(KataRebarCalculator.Calculate(spec)));
    }

    private static KataRebarPlan Plan()
    {
        var table = KataDy7DrawingTests.Sheet();
        table.Set("B7", 120.0);
        var spec = KataDamSheetParser.Parse(table);
        return new KataRebarPlan { Spec = spec, Rules = KataDetailingRuleBuilder.Build(spec), Layout = KataRebarCalculator.Calculate(spec) };
    }

    private static KataElevationDrawing Elevation(KataRebarPlan plan) =>
        KataElevationDrawingBuilder.Build(plan.Spec, plan.Layout, KataSectionCuts.Build(plan.Spec, plan.Layout), plan.Rules.StirrupDiameter);

    private static List<string> Keys(KataRebarLayoutResult layout) =>
        layout.LongitudinalBars.Select(KataLayoutRemoval.Key).Concat(layout.StirrupZones.Select(KataLayoutRemoval.Key)).ToList();

    private static List<int> Numbers(KataRebarLayoutResult layout) =>
        layout.LongitudinalBars.Select(b => b.BarNumber).Concat(layout.BarSets.Select(s => s.BarNumber)).Concat(layout.StirrupZones.Select(z => z.BarNumber)).ToList();

    private static void AssertNumberedWithoutGaps(KataRebarLayoutResult layout)
    {
        var used = Numbers(layout).Distinct().OrderBy(n => n).ToList();
        Assert.Equal(Enumerable.Range(1, used.Count), used);
    }

    /// <summary>The tags, the cuts, the elevation and every section are built from what is left.</summary>
    private static void AssertStillDrawn(KataRebarPlan plan)
    {
        KataBarTagBuilder.Build(plan.Spec, plan.Layout, plan.Rules.StirrupDiameter);
        var cuts = KataSectionCuts.Build(plan.Spec, plan.Layout);
        KataElevationDrawingBuilder.Build(plan.Spec, plan.Layout, cuts, plan.Rules.StirrupDiameter);
        foreach (var cut in cuts)
            Assert.NotNull(KataSectionDrawingBuilder.Build(plan.Spec, plan.Layout, plan.Rules, cut, false));
    }
}
