using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>Edge cases of stepped beams and "-" continuations around the T2-DY7 rules.</summary>
public sealed class KataSteppedBeamEdgeTests
{
    private static KataMeasuredBeam Measured(params (bool Support, double Length, double Height)[] segments) =>
        new(300.0, segments.First(s => !s.Support).Height, segments
            .Select(s => new KataMeasuredSegment(s.Support ? KataMeasuredSupportKind.Column : KataMeasuredSupportKind.None, s.Length, s.Support ? 0.0 : s.Height))
            .ToList(), 2);

    private static KataCellTable TwoSteps(double d21, double f21)
    {
        var t = KataRebarTestSheets.TwoSpans();
        t.Set("D21", d21);
        t.Set("F21", f21);
        return t;
    }

    [Fact]
    public void A_stepped_beam_read_from_the_far_end_compares_B5_with_the_sheets_first_span()
    {
        // Sheet 400 | 6000 (500) | 400 | 4500 (650) | 400; Revit lists the run the other way round.
        var table = TwoSteps(0.0, -150.0);
        table.Set("B5", 500.0);
        var measured = Measured((true, 400.0, 0), (false, 4500.0, 650.0), (true, 400.0, 0), (false, 6000.0, 500.0), (true, 400.0, 0));

        var plan = KataRebarPlanner.Plan(KataDamSheetParser.Parse(table), measured);

        Assert.True(plan.Reversed);
        Assert.DoesNotContain(plan.Blocking, b => b.StartsWith("B5"));
        Assert.Equal(new[] { 500.0, 650.0 }, new[] { plan.Spec.DepthOf(0), plan.Spec.DepthOf(1) });
    }

    [Fact]
    public void Two_cranks_that_would_overlap_cut_the_bar_and_keep_every_path_moving_forward()
    {
        // 4000 | 150 | 4000 spans on 500 supports, 100 mm steps either side of the short span: (100 − 18) / 500 ≤ 1/6
        // allows each 600-long crank, but they run 100 past their supports and would cross in the 150 span.
        var spec = new KataBeamRebarSpec
        {
            Width = 300.0, Height = 500.0,
            TopContinuous = new KataBarItem(2, 18.0), BottomContinuous = new KataBarItem(2, 18.0),
            GlobalStirrup = new KataStirrupSpec { Diameter = 8.0, SupportSpacing = 100.0, MidspanSpacing = 200.0 },
            Supports = Enumerable.Range(0, 4).Select(i => new KataSupportRebarSpec { SupportIndex = i, ColumnWidth = 500.0 }).ToList(),
            Spans = new[]
            {
                new KataSpanRebarSpec { SpanIndex = 0, Length = 4000.0, Depth = 500.0 },
                new KataSpanRebarSpec { SpanIndex = 1, Length = 150.0, Depth = 600.0 },
                new KataSpanRebarSpec { SpanIndex = 2, Length = 4000.0, Depth = 500.0 }
            }
        };

        var result = KataRebarCalculator.Calculate(spec);

        Assert.Contains(result.Warnings, w => w.Contains("không đủ chỗ uốn chuyển cao độ"));
        foreach (var bar in result.MainBottomBars)
        {
            var xs = bar.Polyline.Points.Select(p => p.X).ToList();
            for (int i = 1; i < xs.Count; i++) Assert.True(xs[i] >= xs[i - 1] - 1e-6, $"{bar.BarDescription}: {xs[i - 1]} → {xs[i]}");
        }
    }

    [Fact]
    public void A_dash_beside_a_cantilever_tip_stops_the_bar_a_cover_short_of_it()
    {
        var spec = KataDamSheetParser.Parse(KataRebarTestSheets.TwoSpans()) with { };
        var supports = spec.Supports.ToList();
        supports[0] = supports[0] with { ColumnWidth = 0.0, TopExtraSides = Sides("-") };
        supports[1] = supports[1] with { TopExtraSides = Sides("2f18") };
        var result = KataRebarCalculator.Calculate(spec with { Supports = supports });

        var row13 = result.ExtraTopBars.Where(b => b.HostSupportIndex == 1 && b.Layer == 1).ToList();
        Assert.NotEmpty(row13);
        Assert.All(row13, b => Assert.Equal(KataDetailingRuleBuilder.Build(spec).StirrupCover, b.Polyline.Points.Min(p => p.X), 0));
    }

    [Fact]
    public void A_dash_in_row_14_is_reported_and_never_doubles_a_level()
    {
        var table = KataRebarTestSheets.TwoSpans();
        table.Set("C14", "2f18");
        table.Set("E13", "2f18");
        table.Set("E14", "-");
        table.Set("E15", "2f18");

        var plan = KataRebarPlanner.Plan(KataDamSheetParser.Parse(table), KataRebarTestSheets.MeasuredTwoSpans());
        var top = plan.Layout.ExtraTopBars;

        Assert.Contains(plan.Warnings, w => w.StartsWith("E14 '-'"));
        var overlapping = top.GroupBy(b => (b.TransverseY, Z: System.Math.Round(b.Polyline.Points.Max(p => p.Z))))
            .Where(g => g.Count() > 1)
            .Where(g => g.Any(a => g.Any(b => !ReferenceEquals(a, b) && a.Polyline.Points.Max(p => p.X) > b.Polyline.Points.Min(p => p.X) && b.Polyline.Points.Max(p => p.X) > a.Polyline.Points.Min(p => p.X))));
        Assert.Empty(overlapping);
    }

    [Fact]
    public void A_crossing_beam_support_limits_the_legs_to_its_own_depth()
    {
        var table = KataDy7DrawingTests.Sheet();
        table.Set("I11", "200x250");

        var result = KataRebarCalculator.Calculate(KataDamSheetParser.Parse(table));
        var top = result.MainTopBars[0];

        // Span 3 is 350 deep but beam I only 250: the top leg stays within 250 − 42 − 42 = 166.
        Assert.True(top.EndHookLength <= 166.0 + 1e-6, $"leg {top.EndHookLength}");
        Assert.Contains(result.Warnings, w => w.StartsWith("Neo thép chủ trên ở gối phải"));
    }

    [Fact]
    public void The_deep_bar_at_a_cut_step_keeps_its_leg_under_the_shallow_bar()
    {
        // Steps of 150 cut ((150 − 22) / 400 > 1/6): Ø22, G3 30 → the deep leg would be about 250 and cross the shallow bar.
        var table = TwoSteps(-150.0, 0.0);
        table.Set("B12", "2f22");

        var result = KataRebarCalculator.Calculate(KataDamSheetParser.Parse(table));
        var deep = result.MainBottomBars.First(b => b.TransverseY < 0 && b.Polyline.Points.Min(p => p.X) < 1000);
        var shallow = result.MainBottomBars.First(b => b.TransverseY < 0 && b.Polyline.Points.Min(p => p.X) > 1000);
        double legTop = deep.Polyline.Points.Last().Z;
        double shallowZ = shallow.Polyline.Points.First().Z;

        Assert.True(shallowZ - legTop >= 22.0 + 30.0 - 1e-6, $"leg top {legTop}, shallow bar {shallowZ}");
    }

    [Fact]
    public void A_settings_file_saved_with_the_old_defaults_moves_to_the_new_ones()
    {
        var back = KataSettingsJson.Read("{ \"MinimumLegFactor\": 15, \"DenseZoneHeightFactor\": 2, \"BottomExtraCutFraction\": 0.15, \"LayerClearGap\": 35 }");

        Assert.Equal((0.0, 0.0, 1.0 / 6.0, 35.0), (back.MinimumLegFactor, back.DenseZoneHeightFactor, back.BottomExtraCutFraction, back.LayerClearGap));
        var mine = KataSettingsJson.Read(KataSettingsJson.Write(KataSettings.Default with { MinimumLegFactor = 15.0 }));
        Assert.Equal(15.0, mine.MinimumLegFactor);
    }

    private static IReadOnlyList<KataSideBars> Sides(string row13) =>
        new[] { KataBarNotationParser.ParseSides(row13, 1), KataSideBars.None, KataSideBars.None, KataSideBars.None };
}
