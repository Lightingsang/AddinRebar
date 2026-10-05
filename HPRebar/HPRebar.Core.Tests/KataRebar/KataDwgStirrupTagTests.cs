using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// The stirrup tags of the sections of each beam of <see cref="KataDwgBeam"/> against Kata's (<see cref="KataDwgFixture"/>): hoop, side-bar ties, inner
/// U and C ties — each tag's insertion, text and number, and where each of its leaders starts, within 1 mm.
/// </summary>
public sealed class KataDwgStirrupTagTests
{
    private const double Tol = 1.0;

    public static IEnumerable<object[]> Sections() => KataDwgBeam.Sections();

    [Theory]
    [MemberData(nameof(Sections))]
    public void Every_stirrup_tag_and_its_leaders_are_where_Kata_draws_them(string name, int n)
    {
        var beam = KataDwgBeam.Named(name);
        var spec = KataDamSheetParser.Parse(beam.Sheet());
        var layout = KataRebarCalculator.Calculate(spec);
        var st = KataBeamStations.From(spec);
        double x = beam.Cuts[n - 1];
        int span = Enumerable.Range(0, st.SpanCount).First(s => x >= st.SpanStart[s] - 1.0 && x <= st.SpanEnd[s] + 1.0);
        var canvas = KataSectionDrawingBuilder.Build(spec, layout, KataDetailingRuleBuilder.Build(spec), new KataSectionCut(n, x, span));
        var kata = KataDwgFixture.View(beam, $"section-{n}");
        double top = kata.Where(e => e.Layer == "kata_net thay" && e.Type == "AcDbPolyline").Max(e => e.Max[1]);

        var tags = kata.Where(e => e.Type == "AcDbBlockReference" && e.Layer == "kata_net manh" && e.Attributes.Count > 3
                && Regex.IsMatch(e.Attributes[2] + e.Attributes[3], @"a\d"))
            .ToList();
        Assert.NotEmpty(tags);
        foreach (var tag in tags)
        {
            int number = int.Parse(tag.Attributes[1]);
            if (beam.UnresolvedStirrupTags.Contains((n, number))) continue;
            double tx = tag.At[0], tz = tag.At[1] - top;
            string text = tag.Attributes[2] + tag.Attributes[3];

            var mine = canvas.Tags.Where(t => t.Numbers.Contains(number)).ToList();
            Assert.True(mine.Count == 1, $"{name} {n}-{n} #{number}: {mine.Count} canvas tags");
            var c = mine[0];
            Assert.True(Math.Abs(c.X - tx) <= Tol && Math.Abs(c.Z - tz) <= Tol,
                $"{name} {n}-{n} #{number} {text}: Kata ({tx:0.##}, {tz:0.##}) canvas ({c.X:0.##}, {c.Z:0.##})");
            Assert.Equal(text, c.Text + c.Spacing);

            var expected = kata.Where(e => e.Type == "AcDbLeader" && Near(e.Points[e.Points.Count - 1], tx, tz + top))
                .Select(e => (X: e.Points[0][0], Z: e.Points[0][1] - top)).OrderBy(p => p.X).ToList();
            var actual = canvas.Leaders.Where(l => Math.Abs(l.Points[l.Points.Count - 1].X - c.X) < Tol && Math.Abs(l.Points[l.Points.Count - 1].Z - c.Z) < Tol)
                .Select(l => l.Points[0]).OrderBy(p => p.X).ToList();
            Assert.True(expected.Count == actual.Count && expected.Zip(actual, (a, b) => Math.Abs(a.X - b.X) <= Tol && Math.Abs(a.Z - b.Z) <= Tol).All(ok => ok),
                $"{name} {n}-{n} #{number} leaders: Kata {string.Join(" ", expected.Select(p => $"({p.X:0.##},{p.Z:0.##})"))} canvas {string.Join(" ", actual.Select(p => $"({p.X:0.##},{p.Z:0.##})"))}");
        }
    }

    private static bool Near(double[] point, double x, double y) => Math.Abs(point[0] - x) < Tol && Math.Abs(point[1] - y) < Tol;
}
