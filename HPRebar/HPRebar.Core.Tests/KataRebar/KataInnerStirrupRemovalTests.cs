using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>Striking hoop zones on B01 takes the inner U/C stirrups that belong to them.</summary>
public sealed class KataInnerStirrupRemovalTests
{
    [Fact]
    public void Even_inner_stirrups_cut_at_a_top_step_go_with_the_rest_of_their_span()
    {
        // Span H-J of B01 steps its top: its even run is cut in two, "Đều a500" and "Đều a500 [2]".
        var plan = Plan(i8: 2.0);
        var split = plan.Layout.BarSets.First(s => s.ZoneName.StartsWith(KataTieStations.EvenRunPrefix) && s.ZoneName.EndsWith("]"));
        var zones = plan.Layout.StirrupZones.Where(z => z.SpanIndex == split.SpanIndex && z.Count > 0).Select(KataLayoutRemoval.Key).ToList();

        var edited = KataLayoutRemoval.Remove(plan, zones, out _);

        Assert.DoesNotContain(edited.Layout.BarSets, s => s.SpanIndex == split.SpanIndex && !KataBarNumbering.IsTie(s));
    }

    [Fact]
    public void Inner_stirrups_spread_evenly_go_once_their_span_keeps_no_hoops()
    {
        var plan = Plan(i8: 2.0);
        var even = plan.Layout.BarSets.First(s => s.ZoneName.StartsWith(KataTieStations.EvenRunPrefix));
        var zones = plan.Layout.StirrupZones.Where(z => z.SpanIndex == even.SpanIndex && z.Count > 0).ToList();

        var one = KataLayoutRemoval.Remove(plan, new[] { KataLayoutRemoval.Key(zones[0]) }, out _);
        var all = KataLayoutRemoval.Remove(plan, zones.Select(KataLayoutRemoval.Key).ToList(), out _);

        Assert.Contains(one.Layout.BarSets, s => s.SpanIndex == even.SpanIndex && s.ZoneName == even.ZoneName);
        Assert.DoesNotContain(all.Layout.BarSets, s => s.SpanIndex == even.SpanIndex && s.ZoneName == even.ZoneName);
    }

    private static KataRebarPlan Plan(double i8)
    {
        var table = KataB01DrawingTests.Sheet();
        table.Set("I8", i8);
        var spec = KataDamSheetParser.Parse(table);
        return new KataRebarPlan { Spec = spec, Rules = KataDetailingRuleBuilder.Build(spec), Layout = KataRebarCalculator.Calculate(spec) };
    }
}
