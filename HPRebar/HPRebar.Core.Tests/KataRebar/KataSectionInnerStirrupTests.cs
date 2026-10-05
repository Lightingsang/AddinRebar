using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// The inner stirrups of B01 span 1 (rows 25-27: U 3-4, C 2, C 5) in a section of the span, against T2-DY7.dwg section
/// 2-2 (x from the beam centre, z from the beam top, Ø10 in 500×1100, top bars 6Ø25 drawn at ±41.5 / ±124.5 / ±207.5).
/// </summary>
public sealed class KataSectionInnerStirrupTests
{
    private static KataSectionDrawing Section()
    {
        // With the beam framing into span 1 at 5300, where Kata cuts section 2-2.
        var parsed = KataDamSheetParser.Parse(KataB01DrawingTests.Sheet());
        var spans = parsed.Spans.ToList();
        spans[0] = spans[0] with { Loads = new[] { new KataSpanLoad(4900.0, 400.0, 900.0, false) } };
        var spec = parsed with { Spans = spans };
        var layout = KataRebarCalculator.Calculate(spec);
        var cut = KataSectionCuts.Build(spec, layout).First(c => c.SpanIndex == 0 && c.X > 5000 && c.X < 5600);
        return KataSectionDrawingBuilder.Build(spec, layout, KataDetailingRuleBuilder.Build(spec), cut);
    }

    private static bool Near(double a, double b) => System.Math.Abs(a - b) <= 3.0;

    [Fact]
    public void The_U_is_open_at_the_top_its_legs_round_bars_3_and_4_turned_in_and_down()
    {
        var u = Section().Lines.Single(l => l.Pen == KataDrawingPen.Stirrup && l.Vertices.Count == 10).Vertices;

        // Kata: −19,−57 · −19,−2 · −59,−2 · −59,−1012 · … · 59,−2 · 19,−2 · 19,−57 (hoop top line at +18 over the bar row).
        Assert.True(Near(u[2].X, -59) && Near(u[7].X, 59), $"{u[2].X} {u[7].X}");
        Assert.True(Near(u[0].X, -19) && Near(u[9].X, 19), $"{u[0].X} {u[9].X}");
        Assert.True(Near(u[1].Z - u[0].Z, 55) && Near(u[3].X, u[2].X));
        // Each leg ends in a 180° hook over its bar (Kata: bulge 1 from −19 to −59 and from 59 to 19), not a square turn.
        Assert.Equal(1.0, u[1].Bulge);
        Assert.Equal(1.0, u[7].Bulge);
    }

    [Fact]
    public void Both_Cs_have_their_long_leg_on_the_left_and_open_to_the_right()
    {
        var cs = Section().Lines.Where(l => l.Pen == KataDrawingPen.Stirrup && l.Vertices.Count == 6 && System.Math.Abs(l.Vertices[2].X - l.Vertices[3].X) < 1e-6
            && l.Vertices[3].Z - l.Vertices[2].Z < -500).OrderBy(l => l.Vertices[2].X).ToList();

        Assert.Equal(2, cs.Count);
        // Kata: C 2 long leg −145, open to −105; C 5 long leg 104, open to 144.
        Assert.True(Near(cs[0].Vertices[2].X, -145) && Near(cs[0].Vertices[0].X, -105), $"{cs[0].Vertices[2].X} {cs[0].Vertices[0].X}");
        Assert.True(Near(cs[1].Vertices[2].X, 104) && Near(cs[1].Vertices[0].X, 144), $"{cs[1].Vertices[2].X} {cs[1].Vertices[0].X}");
        // 180° hooks over the top bar and under the bottom bar (Kata: bulge 1 at both turns), not square corners.
        Assert.All(cs, c => Assert.True(c.Vertices[1].Bulge == 1.0 && c.Vertices[3].Bulge == 1.0));
    }

    [Fact]
    public void The_inner_stirrups_are_tagged_at_J7_as_Kata_does()
    {
        var tags = Section().Tags;

        Assert.Contains(tags, t => t.Text == "Ø10" && t.Spacing == "a500");
        Assert.Contains(tags, t => t.Text == "2xØ10" && t.Spacing == "a500");
        // The hoop tag is the span's a200, not the joint stirrups' a50.
        Assert.Contains(tags, t => t.Text == "Ø10" && t.Spacing == "a200");
        Assert.DoesNotContain(tags, t => t.Spacing == "a50");
    }
}
