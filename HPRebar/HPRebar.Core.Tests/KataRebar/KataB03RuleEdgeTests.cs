using System;
using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>The rules B03 brought in, on beams other than B03: they must not put a bar outside the concrete or on another.</summary>
public sealed class KataB03RuleEdgeTests
{
    [Fact]
    public void Side_bars_of_a_deep_span_stop_before_a_span_too_shallow_for_their_levels()
    {
        var table = KataRebarTestSheets.TwoSpans();
        table.Set("B5", 900.0);
        table.Set("G4", 12.0);
        table.Set("G5", 2.0);
        table.Set("F21", 400.0); // span 2: 500 deep
        var spec = KataDamSheetParser.Parse(table);
        var st = KataBeamStations.From(spec);
        var rules = KataDetailingRuleBuilder.Build(spec);
        var layout = KataRebarCalculator.Calculate(spec, rules);

        double floor = -spec.DepthOf(1) + rules.BottomBarCentreDepth;
        Assert.All(layout.SideBars.Where(b => b.Polyline.Points.Max(p => p.X) > st.SpanStart[1] + 1.0),
            b => Assert.True(b.Polyline.Points[0].Z > floor + 20.0, $"{b.BarMark} at {b.Polyline.Points[0].Z:0} under span 2's bottom bars at {floor:0}"));
    }

    [Fact]
    public void Three_bars_run_on_beside_three_of_the_span_s_own_never_share_a_place()
    {
        var table = KataRebarTestSheets.TwoSpans();
        table.Set("B6", 500.0);
        table.Set("D17", "3f16");
        table.Set("E17", "-");
        table.Set("F17", "3f16");
        var layout = KataRebarCalculator.Calculate(KataDamSheetParser.Parse(table));

        var ys = layout.ExtraBottomBars.Where(b => b.HostSpanIndex == 1 && b.Layer == 2).Select(b => Math.Round(b.TransverseY, 1)).ToList();
        Assert.Equal(6, ys.Count);
        Assert.Equal(ys.Count, ys.Distinct().Count());
    }

    [Fact]
    public void A_crossing_beam_wider_than_the_end_column_does_not_lengthen_the_beam_Revit_measures()
    {
        var table = KataRebarTestSheets.TwoSpans();
        table.Set("C20", "500x600"); // 50 past each face of the 400 column
        Assert.Equal(50.0, KataBeamStations.From(KataDamSheetParser.Parse(table)).StartOverhang, 3);

        var plan = KataRebarPlanner.Plan(KataDamSheetParser.Parse(table), KataRebarTestSheets.MeasuredTwoSpans());

        Assert.Equal(0.0, KataBeamStations.From(plan.Spec).StartOverhang, 3);
        Assert.All(plan.Layout.MainTopBars.Concat(plan.Layout.MainBottomBars), b => Assert.True(b.Polyline.Points.Min(p => p.X) >= 0.0, b.BarMark));
    }

    [Fact]
    public void Row_17_at_an_end_support_is_reported_not_dropped()
    {
        var table = KataRebarTestSheets.TwoSpans();
        table.Set("C17", "2f16");

        Assert.Contains(KataRebarCalculator.Calculate(KataDamSheetParser.Parse(table)).Warnings, w => w.StartsWith("C17"));
    }
}
