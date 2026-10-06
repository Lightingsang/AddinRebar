using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// The canvas elevation of each beam of <see cref="KataDwgBeam"/> against Kata's own drawing of it at TL 1/25
/// (<see cref="KataDwgFixture"/>), ±2 mm. A sheet carries no load: B01's crossing beam in span D and stub column in span
/// F come from the Revit model, so the stretches Kata reinforces round them are left out here and checked with the loads
/// in <see cref="KataJointLoadTests"/>.
/// </summary>
public sealed class KataDwgElevationTests
{
    private const double Tol = 2.0;

    public static IEnumerable<object[]> Beams() => KataDwgBeam.Names();

    private static (KataElevationDrawing Drawing, IReadOnlyList<KataBarTag> Tags) Canvas(KataDwgBeam beam)
    {
        var spec = KataDamSheetParser.Parse(beam.Sheet());
        var layout = KataRebarCalculator.Calculate(spec);
        var tags = KataBarTagBuilder.Build(spec, layout, 10.0);
        var drawing = KataElevationDrawingBuilder.Build(spec, layout, KataSectionCuts.Build(spec, layout), 10.0, KataBarTagBuilder.StirrupRowOf(tags));
        return (drawing, tags);
    }

    private static IReadOnlyList<KataDwgFixture.Entity> Dwg(KataDwgBeam beam) => KataDwgFixture.View(beam, "elevation");

    /// <summary>A Kata tag: a dynamic block on kata_net manh (its anonymous name is not kept).</summary>
    private static bool IsTag(KataDwgFixture.Entity e) => e.Type == "AcDbBlockReference" && e.Layer == "kata_net manh" && e.Attributes.Count > 1;

    [Theory]
    [MemberData(nameof(Beams))]
    public void The_first_and_last_stirrup_of_every_zone_stand_where_Kata_draws_them_top_to_bottom(string name)
    {
        var beam = KataDwgBeam.Named(name);
        var expected = Dwg(beam).Where(e => e.Type == "AcDbLine" && e.Layer == "kata_thep dai")
            .Select(e => (X: e.Points[0][0], Top: e.Points.Max(p => p[1]), Bottom: e.Points.Min(p => p[1])))
            .Where(s => !beam.NearLoad(s.X)).Distinct().OrderBy(s => s.X).ToList();
        var actual = Canvas(beam).Drawing.Lines.Where(l => l.Pen == KataDrawingPen.Stirrup)
            .Select(l => (X: l.Points[0].X, Top: l.Points.Max(p => p.Z), Bottom: l.Points.Min(p => p.Z)))
            .Where(s => !beam.NearLoad(s.X)).Distinct().OrderBy(s => s.X).ToList();

        Assert.True(expected.Count == actual.Count, $"Kata {string.Join(" ", expected)} | canvas {string.Join(" ", actual)}");
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.True(Math.Abs(expected[i].X - actual[i].X) <= Tol && Math.Abs(expected[i].Top - actual[i].Top) <= Tol
                && Math.Abs(expected[i].Bottom - actual[i].Bottom) <= Tol, $"Kata {expected[i]} canvas {actual[i]}");
        }
    }

    [Theory]
    [MemberData(nameof(Beams))]
    public void The_stirrup_tags_and_the_zone_chain_sit_on_Katas_rows_and_the_flags_over_them(string name)
    {
        var beam = KataDwgBeam.Named(name);
        var (drawing, tags) = Canvas(beam);

        // Kata: stirrup tags at 525, chain dimension line at 600 (its text 647.5), flags at 725.
        var kataTags = Dwg(beam).Where(e => IsTag(e) && Math.Abs(e.At[1] - 525.0) < 1.0).ToList();
        Assert.NotEmpty(kataTags);
        Assert.All(tags.Where(t => t.Kind == KataTagKind.Stirrups), t => Assert.Equal(525.0, t.RowZ, Tol));
        Assert.Contains(drawing.Dims, d => !d.Vertical && Math.Abs(d.LineAt - 600.0) <= Tol);
        Assert.All(drawing.Flags.Where(f => !f.Below), f => Assert.Equal(725.0, f.Z, Tol));
        Assert.All(Dwg(beam).Where(e => e.Block == "kata_block_SECBAL" && e.At[1] > 0.0), e => Assert.Equal(725.0, e.At[1], Tol));
    }

    [Theory]
    [MemberData(nameof(Beams))]
    public void Every_stirrup_tag_away_from_the_loads_is_where_Kata_puts_it(string name)
    {
        var beam = KataDwgBeam.Named(name);
        var expected = Dwg(beam).Where(e => IsTag(e) && Math.Abs(e.At[1] - 525.0) < 1.0 && !beam.NearLoad(e.At[0]))
            .Select(e => e.At[0]).ToList();
        var actual = Canvas(beam).Tags.Where(t => t.Kind == KataTagKind.Stirrups).Select(t => t.X).ToList();

        // Spans D and F: Kata tags each half of the middle zone the joint stirrups split; the sheet alone has one zone.
        var missing = expected.Where(x => !actual.Any(a => Math.Abs(a - x) <= Tol)).ToList();
        Assert.True(missing.All(beam.InLoadedSpan), $"{beam}: missing {string.Join(", ", missing)}");
    }

    [Theory]
    [MemberData(nameof(Beams))]
    public void The_zone_chain_away_from_the_loads_measures_what_Kata_measures(string name)
    {
        var beam = KataDwgBeam.Named(name);
        var expected = Dwg(beam).Where(e => e.Type == "AcDbRotatedDimension" && e.At[1] > 600.0 && !beam.InLoadedSpan(e.At[0]))
            .Select(e => (e.Measurement, X: e.At[0])).OrderBy(d => d.X).ToList();
        var actual = Canvas(beam).Drawing.Dims.Where(d => !d.Vertical && Math.Abs(d.LineAt - 600.0) <= Tol && !beam.InLoadedSpan((d.X1 + d.X2) / 2.0))
            .Select(d => (Measurement: d.Value, X: (d.X1 + d.X2) / 2.0)).OrderBy(d => d.X).ToList();

        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
            Assert.True(Math.Abs(expected[i].Measurement - actual[i].Measurement) <= Tol && Math.Abs(expected[i].X - actual[i].X) <= Tol,
                $"Kata {expected[i]} canvas {actual[i]}");
    }

    [Theory]
    [MemberData(nameof(Beams))]
    public void The_cuts_HPRebar_places_by_its_tenth_of_the_span_rule_and_the_console_cut_are_Katas(string name)
    {
        var beam = KataDwgBeam.Named(name);
        var kata = Dwg(beam).Where(e => e.Block == "kata_block_SECBAL" && e.At[1] > 0.0).Select(e => e.At[0]).ToList();
        var canvas = Canvas(beam).Drawing.Flags.Where(f => !f.Below).Select(f => f.X).ToList();

        // Kata's own rule is not known and HPRebar keeps 0.1 L (user decision 2026-10-05): on B01 / B02 these seven
        // agree, 1250, 9950, 17025, 20250, 22650, 25600 and 30600 do not; on B03 four agree. The console is cut once,
        // 2000 / 3 out (B01 14-14, B03 10-10).
        var agreed = name == "B03"
            ? new[] { 5450.0, 17750.0, 27950.0, 32266.7 }
            : new[] { 5450.0, 11850.0, 14300.0, 18750.0, 23950.0, 27950.0, 32266.7 };
        foreach (double x in agreed)
        {
            Assert.Contains(kata, k => Math.Abs(k - x) <= Tol);
            Assert.Contains(canvas, c => Math.Abs(c - x) <= Tol);
        }
    }

}
