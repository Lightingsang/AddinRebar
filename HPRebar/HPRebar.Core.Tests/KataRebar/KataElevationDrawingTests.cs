using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// The elevation of T2-DY7 and T2-DY14 (E = 350) in T2-DY7.dwg, read entity by entity (layers kata_net thay / khuat,
/// kata_dim, kata_grid, kata_thep chu / dai, Defpoints; blocks SECBAL, GRID, CT, TD). Stations are mm from the outer
/// face of column C (drawing x − 6732), heights from the beam top (drawing y + 17251 for DY7, + 23910 for DY14).
/// The sheets carry B7 = 120, B10 = +3.550 and the grids of rows 22/23 the DWG shows.
/// </summary>
public sealed class KataElevationDrawingTests
{
    private static KataCellTable Dy7()
    {
        var t = KataDy7DrawingTests.Sheet();
        t.Set("B7", 120.0);
        t.Set("B10", "+3.550");
        foreach (var (col, name, offset) in new[] { ("C", "B.1a", 125.0), ("E", "D.1a", 150.0), ("G", "F.1a", 125.0), ("I", "H.1a", 0.0) })
        {
            t.Set(col + "22", name);
            t.Set(col + "23", offset);
        }

        return t;
    }

    private static KataCellTable Dy14()
    {
        var t = KataDy14DrawingTests.Sheet();
        t.Set("B7", 120.0);
        t.Set("B10", "+3.550");
        foreach (var (col, name, offset) in new[] { ("C", "B.1a", 125.0), ("E", "D.1a", 75.0), ("G", "F.1a", 125.0), ("I", "H.1a", -25.0) })
        {
            t.Set(col + "22", name);
            t.Set(col + "23", offset);
        }

        return t;
    }

    private static KataElevationDrawing Draw(KataCellTable sheet) => DrawWithLayout(sheet).Drawing;

    private static (KataElevationDrawing Drawing, KataRebarLayoutResult Layout) DrawWithLayout(KataCellTable sheet)
    {
        var spec = KataDamSheetParser.Parse(sheet);
        var layout = KataRebarCalculator.Calculate(spec);
        return (KataElevationDrawingBuilder.Build(spec, layout, KataSectionCuts.Build(spec, layout), 8.0), layout);
    }

    private static double[] FirstAndLastStirrups(KataRebarLayoutResult layout) =>
        KataStirrupRuns.Of(layout).SelectMany(r => r.Last - r.First < 1 ? new[] { r.First } : new[] { r.First, r.Last }).OrderBy(x => x).ToArray();

    private static List<double> Chain(KataElevationDrawing d, double lineZ) =>
        d.Dims.Where(x => !x.Vertical && x.Style == KataDimStyle.Kata && System.Math.Abs(x.LineAt - lineZ) < 0.5)
            .SelectMany(x => new[] { x.X1, x.X2 }).Select(x => System.Math.Round(x)).Distinct().OrderBy(x => x).ToList();

    private static void Stations(double[] kata, IReadOnlyList<double> ours, double tolerance, string what)
    {
        Assert.True(kata.Length == ours.Count, $"{what}: Kata {string.Join(" ", kata)}, HPRebar {string.Join(" ", ours)}");
        for (int i = 0; i < kata.Length; i++)
            Assert.True(System.Math.Abs(kata[i] - ours[i]) <= tolerance, $"{what} #{i}: Kata {kata[i]}, HPRebar {ours[i]:0.#} (all: {string.Join(" ", ours.Select(x => x.ToString("0", CultureInfo.InvariantCulture)))})");
    }

    private static string Key(IEnumerable<(double X, double Z)> points)
    {
        var list = new List<(double X, double Z)>();
        foreach (var p in points)
            if (list.Count == 0 || System.Math.Abs(list[list.Count - 1].X - p.X) > 0.5 || System.Math.Abs(list[list.Count - 1].Z - p.Z) > 0.5) list.Add(p);
        return string.Join(" ", list.Select(p => FormattableString.Invariant($"{System.Math.Round(p.X, 1)},{System.Math.Round(p.Z, 1)}")));
    }

    private static void Lines(KataElevationDrawing d, KataDrawingPen pen, string[] kata)
    {
        var ours = d.Lines.Where(l => l.Pen == pen).Select(l => Key(l.Points)).OrderBy(s => s, System.StringComparer.Ordinal).ToList();
        var expected = kata.OrderBy(s => s, System.StringComparer.Ordinal).ToList();
        Assert.True(expected.SequenceEqual(ours), $"{pen}:\nKata    {string.Join(" | ", expected)}\nHPRebar {string.Join(" | ", ours)}");
    }

    [Fact]
    public void T2_DY7_concrete_hidden_lines_breaks_and_grids_are_Kata_s()
    {
        var d = Draw(Dy7());

        Lines(d, KataDrawingPen.Outline, new[]
        {
            "0,150 0,0 0,-500 0,-750", "450,150 450,0 5950,0 5950,150", "450,-750 450,-500 5950,-500 5950,-750",
            "6450,150 6450,0 13400,0 13400,150", "6450,-750 6450,-600 13400,-600 13400,-750",
            "13850,150 13850,0 16050,0", "13850,-750 13850,-350 16050,-350", "16050,-350 16250,-350", "16050,0 16250,0 16250,-350"
        });
        Lines(d, KataDrawingPen.Hidden, new[]
        {
            "0,0 450,0", "5950,0 6450,0", "13400,0 13850,0",
            "0,-500 0,-120 350,-120", "350,-120 6350,-120", "6350,-120 13750,-120", "13750,-120 16050,-120 16050,-350", "16250,-350 16250,-120"
        });
        Lines(d, KataDrawingPen.Thin, new[]
        {
            "-50,150 175,150 175,200 275,100 275,150 500,150", "-50,-750 175,-750 175,-700 275,-800 275,-750 500,-750",
            "5900,150 6150,150 6150,200 6250,100 6250,150 6500,150", "5900,-750 6150,-750 6150,-700 6250,-800 6250,-750 6500,-750",
            "13350,150 13575,150 13575,200 13675,100 13675,150 13900,150", "13350,-750 13575,-750 13575,-700 13675,-800 13675,-750 13900,-750"
        });
        Lines(d, KataDrawingPen.Grid, new[] { "350,662.5 350,-1250", "6350,662.5 6350,-1250", "13750,662.5 13750,-1250", "16150,662.5 16150,-1250" });
    }

    [Fact]
    public void T2_DY7_dimension_chains_are_Kata_s()
    {
        var (d, layout) = DrawWithLayout(Dy7());

        // Support faces as Kata's; each span split where its first zone ends and its last begins. Kata's zones end at
        // 1850 4550 / 8200 11650 / 14400 15500, this layout's dense zones (face + 50 at a100 within L0/4) at its own.
        Stations(new[] { 0.0, 450, 1800, 4600, 5950, 6450, 8100, 11750, 13400, 13850, 14400, 15500, 16050, 16250 }, Chain(d, 462.0), 0.5, "top chain");
        Assert.NotNull(layout);
        Stations(new[] { 0.0, 350, 450, 1350, 5050, 5950, 6350, 6450, 7600, 12250, 13400, 13750, 13850, 16050, 16150, 16250 }, Chain(d, -1050.0), 1.0, "bar-cut chain");
        Stations(new[] { 350.0, 6350, 13750, 16150 }, Chain(d, -1225.0), 0.5, "grid chain");

        var stagger = d.Dims.Where(x => !x.Vertical && x.LineAt > -1000 && x.LineAt < 400 && x.Style == KataDimStyle.Kata)
            .Select(x => $"{System.Math.Min(x.X1, x.X2):0}-{System.Math.Max(x.X1, x.X2):0}@{x.Z1:0}/{x.LineAt:0}").OrderBy(s => s).ToList();
        Assert.Equal(new[]
        {
            "1850-2350@-29/96", "4050-4550@-29/96", "8200-8700@-29/96", "11150-11650@-29/96",
            "850-1350@-471/-346", "5050-5550@-471/-346", "7100-7600@-571/-446", "12250-12750@-571/-446"
        }.OrderBy(s => s), stagger);

        var depth = d.Dims.Where(x => x.Vertical).Select(x => $"{x.Value:0}@{x.LineAt:0}").OrderBy(s => s).ToList();
        Assert.Equal(new[] { "120@-200", "380@-200", "500@-350" }, depth);

        var runs = d.Dims.Where(x => x.Style == KataDimStyle.Run).ToList();
        Assert.Equal(9, runs.Count);
        Assert.All(runs, r => Assert.Equal(-120.0, r.LineAt));
    }

    [Fact]
    public void T2_DY7_flags_bubbles_level_and_title_are_Kata_s()
    {
        var d = Draw(Dy7());

        var above = d.Flags.Where(f => !f.Below).OrderBy(f => f.X).ToList();
        Stations(new[] { 975.0, 3050, 5425, 7150, 9775, 12700, 14000, 14900, 15900 }, above.Select(f => f.X).ToList(), 75.0, "flags");
        Assert.Equal(Enumerable.Range(1, 9), above.Select(f => f.Number));
        Assert.All(above, f => Assert.Equal(587.5, f.Z));
        Assert.All(d.Flags.Where(f => f.Below), f => Assert.Equal(-1425.0, f.Z));
        Assert.Equal(18, d.Flags.Count);

        Assert.Equal(new[] { "B.1a@350,-1425", "D.1a@6350,-1425", "F.1a@13750,-1425", "H.1a@16150,-1425" },
            d.Bubbles.Select(b => $"{b.Name}@{b.X:0},{b.Z:0}"));
        Assert.Equal(new KataDrawingLevel(-475.0, 0.0, "+3.550"), d.Level);
        Assert.Equal(new KataDrawingTitle(8125.0, -1700.0, "T2-DY7 (SL=1; L=16250)", "TL: 1/25"), d.Title);
    }

    [Fact]
    public void T2_DY7_stirrups_are_the_first_and_last_of_each_zone()
    {
        var (d, layout) = DrawWithLayout(Dy7());

        // Kata: 500 1850 2050 4350 4550 5900 | 6500 8200 8400 11450 11650 13350 | 13900 14400 14600 15300 15500 16000;
        // drawn here are the first and last stirrups this layout places (its own zones).
        var stirrups = d.Lines.Where(l => l.Pen == KataDrawingPen.Stirrup).ToList();
        Assert.Equal(18, stirrups.Count);
        Stations(FirstAndLastStirrups(layout), stirrups.Select(l => l.Points[0].X).OrderBy(x => x).ToList(), 0.5, "stirrups");
        Assert.Equal(new[] { 500.0, 5900, 6500, 13350, 13900, 16000 }, new[] { 0, 5, 6, 11, 12, 17 }.Select(i => System.Math.Round(stirrups.OrderBy(l => l.Points[0].X).ElementAt(i).Points[0].X)));
        Assert.All(stirrups, l => Assert.Equal(-25.0, l.Points[0].Z));
        Assert.Equal(new[] { -475.0, -575.0, -325.0 }, stirrups.OrderBy(l => l.Points[0].X).Select(l => l.Points[1].Z).Distinct());
    }

    [Fact]
    public void T2_DY7_bars_are_drawn_where_Kata_draws_them()
    {
        var d = Draw(Dy7());
        var ours = d.Lines.Where(l => l.Pen == KataDrawingPen.Bar).Select(l => l.Points).ToList();

        // kata_thep chu LWPOLYLINE vertices; the hook bends are filleted in ours, so only the straight runs are matched.
        var kata = new (string Handle, (double X, double Z)[] Run)[]
        {
            ("AA96", new[] { (50.0, -29.0), (16195.0, -29.0) }),
            // The bottom main bar's hook leg stands where this layout bends it, inside the top bar's (Kata draws it 5 mm in).
            ("AA97", new[] { (5950.0, -471.0), (6550.0, -571.0), (13795.0, -571.0) }),
            ("AA98", new[] { (16170.0, -321.0), (13310.0, -321.0) }),
            ("AA99", new[] { (50.0, -29.0), (2350.0, -29.0) }),
            ("AA9A", new[] { (100.0, -72.0), (1850.0, -72.0) }),
            ("AA9B", new[] { (4050.0, -29.0), (8700.0, -29.0) }),
            ("AA9C", new[] { (4550.0, -72.0), (8200.0, -72.0) }),
            ("AA9D", new[] { (16195.0, -29.0), (11150.0, -29.0) }),
            ("AA9E", new[] { (11650.0, -72.0), (14400.0, -72.0) }),
            ("AA9F", new[] { (850.0, -471.0), (5550.0, -471.0) }),
            ("AAA0", new[] { (1350.0, -428.0), (5050.0, -428.0) }),
            ("AAA1", new[] { (7100.0, -571.0), (12750.0, -571.0) }),
            ("AAA2", new[] { (7600.0, -528.0), (12250.0, -528.0) }),
            ("AAA3", new[] { (330.0, -250.0), (13520.0, -250.0) }),
        };

        foreach (var (handle, run) in kata)
        {
            // Layer-2 bars sit 5 mm further in here (clear gap 30 against Kata's 25).
            bool found = ours.Any(o => run.All(k => o.Any(p => System.Math.Abs(p.X - k.X) <= 25.0 && System.Math.Abs(p.Z - k.Z) <= 5.5)));
            Assert.True(found, $"{handle}: no bar through {string.Join(" ", run)}; ours: {string.Join(" | ", ours.Select(Key))}");
        }
    }

    [Fact]
    public void T2_DY14_end_beam_column_250_and_unnamed_grid_are_Kata_s()
    {
        var (d, layout) = DrawWithLayout(Dy14());

        Lines(d, KataDrawingPen.Grid, new[]
        {
            "350,662.5 350,-1250", "6350,662.5 6350,-1250", "13750,662.5 13750,-1250", "16150,662.5 16150,-1250", "16600,662.5 16600,-1250"
        });
        Assert.Equal(new[] { 350.0, 6350, 13750, 16150 }, d.Bubbles.Select(b => b.X));
        Assert.Contains(d.Lines, l => l.Pen == KataDrawingPen.Thin && Key(l.Points) == "16008.3,150 16133.3,150 16133.3,191.7 16216.7,108.3 16216.7,150 16341.7,150");
        Assert.Contains(d.Lines, l => l.Pen == KataDrawingPen.Outline && Key(l.Points) == "13850,-750 13850,-350 16050,-350 16050,-750");
        Assert.Contains(d.Lines, l => l.Pen == KataDrawingPen.Outline && Key(l.Points) == "16300,-750 16300,-350 16550,-350");
        Assert.Contains(d.Lines, l => l.Pen == KataDrawingPen.Outline && Key(l.Points) == "16550,0 16650,0 16650,-350");
        Assert.Contains(d.Lines, l => l.Pen == KataDrawingPen.Hidden && Key(l.Points) == "16150,-120 16550,-120 16550,-350");

        Stations(new[] { 0.0, 350, 450, 1400, 5150, 6100, 6350, 6450, 7600, 12250, 13400, 13750, 13850, 16050, 16150, 16300, 16550, 16600, 16650 },
            Chain(d, -1050.0), 1.0, "bar-cut chain");
        Stations(new[] { 350.0, 6350, 13750, 16150, 16600 }, Chain(d, -1225.0), 0.5, "grid chain");
        // Kata splits at 1900 4650 / 8200 11650 / 14400 15500 and leaves the 250 span whole (its two one-stirrup zones are one).
        Stations(new[] { 0.0, 450, 1800, 4750, 6100, 6450, 8100, 11750, 13400, 13850, 14400, 15500, 16050, 16300, 16550, 16650 },
            Chain(d, 462.0), 0.5, "top chain");
        Assert.Contains(d.Lines, l => l.Pen == KataDrawingPen.Stirrup && System.Math.Abs(l.Points[0].X - 16350) < 1);
        Assert.Contains(d.Dims, x => x.Style == KataDimStyle.Run && System.Math.Abs(x.Value - 150) < 1);
        Assert.NotNull(layout);
        Assert.Equal("T2-DY14 (SL=1; L=16650)", d.Title!.Name);
    }

    [Fact]
    public void A_crossing_beam_shallower_than_its_span_closes_the_soffit_with_a_face()
    {
        var sheet = Dy7();
        sheet.Set("I11", "200x300");
        var d = Draw(sheet);

        // The 350 span ends on a 300 deep beam: its soffit steps up at the beam's face.
        Assert.Contains(d.Lines, l => l.Pen == KataDrawingPen.Outline && Key(l.Points) == "16050,-300 16050,-350");
        Assert.Contains(d.Lines, l => l.Pen == KataDrawingPen.Outline && Key(l.Points) == "16050,-300 16250,-300");
    }

    [Fact]
    public void Stagger_dimensions_take_the_outermost_bar_of_each_layer_on_each_side()
    {
        var sheet = Dy7();
        sheet.Set("E13", "2f18;1f18");
        sheet.Set("E14", "3f18;2f18");
        var d = Draw(sheet);

        var overE = d.Dims.Where(x => !x.Vertical && System.Math.Abs(x.LineAt - 96) < 1 && x.X1 > 3000 && x.X2 < 10000).ToList();
        Assert.Equal(2, overE.Count);
        Assert.All(overE, x => Assert.Equal(500.0, x.Value, 0));
    }
}
