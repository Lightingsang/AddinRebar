using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// The 14 sections of B01 drawn by the canvas at Kata's own cuts, against Kata's drawing of them at TL 1/25
/// (<see cref="KataB01DwgFixture"/>): every bar (centre ±2, diameter) and every stirrup line — hoop, U, C, the C ties of
/// the side bars — vertex by vertex (±2) with its bulges.
/// </summary>
public sealed class KataB01DwgSectionTests
{
    private const double Tol = 2.0;

    /// <summary>Kata's cuts (its flags over the elevation, mm from the outer face of column C).</summary>
    private static readonly double[] KataCuts = { 1250, 5450, 9950, 11850, 14300, 17025, 18750, 20250, 22650, 23950, 25600, 27950, 30600, 32266.7 };

    public static IEnumerable<object[]> Numbers() => Enumerable.Range(1, 14).Select(n => new object[] { n });

    private static (KataSectionDrawing Canvas, IReadOnlyList<KataB01DwgFixture.Entity> Kata, double Top) Section(int n)
    {
        var spec = KataDamSheetParser.Parse(KataB01DrawingTests.Sheet());
        var layout = KataRebarCalculator.Calculate(spec);
        var st = KataBeamStations.From(spec);
        double x = KataCuts[n - 1];
        int span = Enumerable.Range(0, st.SpanCount).First(s => x >= st.SpanStart[s] - 1.0 && x <= st.SpanEnd[s] + 1.0);
        var canvas = KataSectionDrawingBuilder.Build(spec, layout, KataDetailingRuleBuilder.Build(spec), new KataSectionCut(n, x, span));
        var kata = KataB01DwgFixture.View($"section-{n}");
        // The section's top: the top line of its outline (slab top), the title's point being the origin.
        double top = kata.Where(e => e.Layer == "kata_net thay" && e.Type == "AcDbPolyline").Max(e => e.Max[1]);
        return (canvas, kata, top);
    }

    [Theory]
    [MemberData(nameof(Numbers))]
    public void Every_bar_is_where_Kata_draws_it(int n)
    {
        var (canvas, kata, top) = Section(n);
        var expected = kata.Where(e => e.Layer == "kata_thep chu" && e.Type == "AcDbBlockReference")
            .Select(e => (X: (e.Min[0] + e.Max[0]) / 2.0, Z: (e.Min[1] + e.Max[1]) / 2.0 - top, D: e.Max[0] - e.Min[0])).ToList();

        Assert.Equal(expected.Count, canvas.Bars.Count);
        foreach (var bar in expected)
            Assert.True(canvas.Bars.Any(b => Math.Abs(b.X - bar.X) <= Tol && Math.Abs(b.Z - bar.Z) <= Tol && Math.Abs(b.Diameter - bar.D) <= 0.5),
                $"{n}-{n}: no bar Ø{bar.D:0} at ({bar.X:0.#}, {bar.Z:0.#})");
    }

    [Theory]
    [MemberData(nameof(Numbers))]
    public void Every_stirrup_line_runs_and_bends_as_Kata_draws_it(int n)
    {
        var (canvas, kata, top) = Section(n);
        var expected = kata.Where(e => e.Layer == "kata_thep dai" && e.Type == "AcDbPolyline")
            .Select(e => e.Points.Select(p => (X: p[0], Z: p[1] - top, B: p[2])).ToList()).ToList();
        var actual = canvas.Lines.Where(l => l.Pen == KataDrawingPen.Stirrup)
            .Select(l => l.Vertices.Select(v => (X: v.X, Z: v.Z, B: v.Bulge)).ToList()).ToList();

        Assert.Equal(expected.Count, actual.Count);
        foreach (var line in expected)
            Assert.True(actual.Any(a => Same(a, line)), $"{n}-{n}: no stirrup line {string.Join(" ", line.Select(p => $"{p.X:0.#},{p.Z:0.#}"))}");
    }

    /// <summary>
    /// The bar tags ("6Ø25", "2x2Ø12") where Kata inserts them, ±2 — but the tags of an upper inner layer (Kata 30
    /// further right in B01 than in T2-DY7 at the same width: its rule is unknown) and the tag joining two layers of
    /// side bars (7 to the left).
    /// </summary>
    [Theory]
    [MemberData(nameof(Numbers))]
    public void Every_bar_tag_is_inserted_where_Kata_inserts_it(int n)
    {
        var (canvas, kata, top) = Section(n);
        var expected = kata.Where(e => e.Type == "AcDbBlockReference" && e.Layer == "kata_net manh"
                && e.Attributes.Count > 2 && System.Text.RegularExpressions.Regex.IsMatch(e.Attributes[2], @"^\d+Ø\d+$"))
            .Select(e => (X: e.At[0], Z: e.At[1] - top, Text: e.Attributes[2]))
            .Where(t => !(t.X > 0.0 && t.Z > -300.0) && !t.Text.Contains('x'))
            .ToList();

        Assert.NotEmpty(expected);
        foreach (var tag in expected)
            Assert.True(canvas.Tags.Any(t => t.Text == tag.Text && Math.Abs(t.X - tag.X) <= Tol && Math.Abs(t.Z - tag.Z) <= Tol),
                $"{n}-{n}: no tag {tag.Text} at ({tag.X:0.#}, {tag.Z:0.#}); canvas {string.Join(" ", canvas.Tags.Select(t => $"{t.Text}@{t.X:0.#},{t.Z:0.#}"))}");
    }

    private static bool Same(List<(double X, double Z, double B)> a, List<(double X, double Z, double B)> b) =>
        a.Count == b.Count && a.Zip(b, (p, q) => Math.Abs(p.X - q.X) <= Tol && Math.Abs(p.Z - q.Z) <= Tol && Math.Abs(p.B - q.B) <= 0.05).All(ok => ok);
}
