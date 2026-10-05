using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// B01 against T2-DY7.dwg where the canvas used to differ: stirrups inside the console's lowered top, the console's
/// "Đai C 2" round the middle of its own three top bars, the stirrup tags under the zone chain and the flags.
/// </summary>
public sealed class KataB01ParityTests
{
    private static KataRebarPlan Plan()
    {
        var spec = KataDamSheetParser.Parse(KataB01DrawingTests.Sheet());
        var layout = KataRebarCalculator.Calculate(spec);
        return new KataRebarPlan { Spec = spec, Layout = layout, Rules = KataDetailingRuleBuilder.Build(spec) };
    }

    [Fact]
    public void Every_elevation_stirrup_stays_inside_the_concrete_and_the_console_ones_start_under_its_lowered_top()
    {
        var plan = Plan();
        var st = KataBeamStations.From(plan.Spec);
        var drawing = KataElevationDrawingBuilder.Build(plan.Spec, plan.Layout, KataSectionCuts.Build(plan.Spec, plan.Layout), 10.0);
        int console = st.SpanCount - 1;

        foreach (var line in drawing.Lines.Where(l => l.Pen == KataDrawingPen.Stirrup))
        {
            double x = line.Points[0].X, top = line.Points.Max(p => p.Z);
            double level = KataTopProfile.LevelAt(plan.Spec, st, x, right: true);
            Assert.True(top <= level - 24.0, $"stirrup at {x:0} reaches {top:0}, the top there is {level:0}");
        }

        // Kata: the console's stirrups run −225…−1075 (its top 200 down).
        var inConsole = drawing.Lines.Where(l => l.Pen == KataDrawingPen.Stirrup && l.Points[0].X > st.SpanStart[console]).ToList();
        Assert.NotEmpty(inConsole);
        Assert.All(inConsole, l => Assert.InRange(l.Points.Max(p => p.Z), -226.0, -224.0));
    }

    [Fact]
    public void The_consoles_C_wraps_the_middle_of_its_own_three_top_bars()
    {
        var plan = Plan();
        int console = KataBeamStations.From(plan.Spec).SpanCount - 1;

        var cs = plan.Layout.BarSets.Where(s => s.SpanIndex == console && s.Role == KataBarRole.CrossTie
            && s.ZoneName != KataSideBarLayout.TieZoneName && s.ZoneName != KataLayerSpacerTieLayout.ZoneName).ToList();

        // Kata section 14-14: long leg at −20.5, hooks at +19.5 round the middle bar (−0.5) of 3Ø20 in 300.
        Assert.NotEmpty(cs);
        Assert.All(cs, c => Assert.InRange(c.Shape.Points.Average(p => p.Y), -25.0, 25.0));
    }

    [Fact]
    public void The_zone_chain_and_the_flags_stay_over_the_stirrup_tags_when_bar_tags_take_three_rows()
    {
        var plan = Plan();
        var tags = KataBarTagBuilder.Build(plan.Spec, plan.Layout, 10.0);
        double row = KataBarTagBuilder.StirrupRowOf(tags);
        var drawing = KataElevationDrawingBuilder.Build(plan.Spec, plan.Layout, KataSectionCuts.Build(plan.Spec, plan.Layout), 10.0, row);

        // B01 carries three rows of top tags (support 2: 9 and 1 over 8), so the stirrup row leaves Kata's usual 387.5.
        Assert.True(row > KataTagStyle.StirrupRow, $"stirrup row {row}");
        // Kata, as at 1:25: chain 75 over the stirrup tags, flags 200 over them (B01 at 1:50: 1050 · 1200 · 1450).
        var chain = drawing.Dims.Where(d => !d.Vertical && d.LineAt > 0.0).Select(d => d.LineAt).Distinct().ToList();
        Assert.Contains(chain, z => System.Math.Abs(z - (row + 74.5)) <= 1.0);
        Assert.All(drawing.Flags.Where(f => !f.Below), f => Assert.Equal(row + 200.0, f.Z, 1));
    }
}
