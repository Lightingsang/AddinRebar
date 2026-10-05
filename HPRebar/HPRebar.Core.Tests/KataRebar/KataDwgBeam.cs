using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Parsers;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// A beam Kata drew in T2-DY7.dwg at TL 1/25 and the sheet Dam it was drawn from: its fixture
/// (<see cref="KataDwgFixture"/>), its cells, Kata's cuts, and what the DWG checks leave out for it.
/// </summary>
internal sealed record KataDwgBeam(
    string Name,
    Func<KataCellTable> Sheet,
    IReadOnlyList<double> Cuts,
    IReadOnlyList<(double From, double To)> Loads,
    IReadOnlyList<(double From, double To)> LoadedSpans,
    IReadOnlyCollection<(int Section, int Number)> UnresolvedStirrupTags)
{
    private static readonly double[] B0xCuts = { 1250, 5450, 9950, 11850, 14300, 17025, 18750, 20250, 22650, 23950, 25600, 27950, 30600, 32266.7 };

    /// <summary>
    /// Kata's U and C tags of the sections with side bars (1-1…3-3: U at −820.8, C at −945.8 or over the beam) follow a
    /// rule not found yet.
    /// </summary>
    private static readonly (int, int)[] SideBarSectionInnerTags = { (1, 24), (1, 25), (2, 24), (2, 25), (3, 24), (3, 25) };

    /// <summary>
    /// B01 (KataB1.xlsm, drawn with J9 50/25): the crossing beam in span D and the stub column in span F come from the
    /// Revit model, not the sheet, so the stretches Kata reinforces round them are left out.
    /// </summary>
    public static readonly KataDwgBeam B01 = new("B01", KataB01DrawingTests.Sheet, B0xCuts,
        new[] { (4650.0, 6050.0), (14100.0, 15500.0) }, new[] { (3000.0, 8200.0), (12850.0, 16050.0) }, SideBarSectionInnerTags);

    /// <summary>
    /// B02 (KataB02.xlsm, 2026-10-05): B01's cells with J9 30/25, a "*" under span F (F24) and a C tie on bar 2 in span L
    /// (K25/L25), drawn without loads.
    /// </summary>
    public static readonly KataDwgBeam B02 = new("B02", B02Sheet, B0xCuts,
        Array.Empty<(double, double)>(), Array.Empty<(double, double)>(), SideBarSectionInnerTags);

    public static IReadOnlyList<KataDwgBeam> All { get; } = new[] { B01, B02 };

    public static KataDwgBeam Named(string name) => All.Single(b => b.Name == name);

    public static IEnumerable<object[]> Names() => All.Select(b => new object[] { b.Name });

    public static IEnumerable<object[]> Sections() =>
        All.SelectMany(b => Enumerable.Range(1, b.Cuts.Count).Select(n => new object[] { b.Name, n }));

    public bool NearLoad(double x) => Loads.Any(l => x >= l.From && x <= l.To);

    public bool InLoadedSpan(double x) => LoadedSpans.Any(l => x > l.From && x < l.To);

    private static KataCellTable B02Sheet()
    {
        var t = KataB01DrawingTests.Sheet();
        t.Set("J9", "30/25");
        t.Set("F24", "*");
        t.Set("K25", "Đai C");
        t.Set("L25", "2");
        return t;
    }

    public override string ToString() => Name;
}
