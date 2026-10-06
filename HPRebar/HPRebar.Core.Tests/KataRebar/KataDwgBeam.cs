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

    /// <summary>
    /// B03 (KataB03.xlsm, 2026-10-06): four spans 10400 | 13400 | 6200 | console 2000, span 2 soffit 300 up, span 3 and
    /// the console 300 wide on 3Ø20 (H19/H21), console top 200 down; the beam runs 150 past column 1 to the face of the
    /// crossing beam (C20/C21). Drawn without loads.
    /// </summary>
    /// <remarks>
    /// Its hoop tag in span 2 (4-4…6-6, 900 deep under side bars laid out at span 1's 1200) sits at −762.5, 75 under
    /// the rule every other section follows; why is not known.
    /// </remarks>
    public static readonly KataDwgBeam B03 = new("B03", B03Sheet, new[] { 1250, 5450, 9950, 12050, 17750, 23750, 25600, 27950, 30600, 32266.67 },
        Array.Empty<(double, double)>(), Array.Empty<(double, double)>(), new[] { (4, 27), (5, 27), (6, 27) });

    public static IReadOnlyList<KataDwgBeam> All { get; } = new[] { B01, B02, B03 };

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

    /// <summary>The cells of KataB03.xlsm that decide the bars.</summary>
    private static KataCellTable B03Sheet()
    {
        var t = new KataCellTable();
        foreach (var (address, value) in new (string, object)[]
                 {
                     ("B3", "B03"), ("B5", 1200.0), ("B6", 500.0), ("B7", 150.0), ("B11", "6f25"), ("B12", "6f25"),
                     ("G1", 500.0), ("G2", 40.0), ("G3", 30.0), ("G4", 12.0), ("G5", 2.0), ("G6", 10.0),
                     ("G7", "a150"), ("G8", "a200"), ("G9", 150.0), ("J7", "a500"), ("I8", 2.0), ("J9", "30/25"),
                     ("H3", 0.2), ("I3", "L từ mép cột"), ("H5", 0.25), ("I5", "L từ mép cột"),
                     ("C14", "6f25"), ("E14", "6f20"), ("G14", "2f20;2f16"), ("I14", "2f20"), ("E15", "6f20;0"),
                     ("D17", "6f25"), ("E17", "-"), ("F17", "2f20"), ("G17", "-;0"), ("I17", "2f20"), ("J17", "-"),
                     ("C19", "400;0"), ("D19", 0.0), ("E19", "400;0"), ("F19", 0.0), ("G19", "400;0"), ("H19", "0;3f20"),
                     ("I19", "400;0"), ("J19", -200.0), ("K19", 0.0),
                     ("C20", "400x500"), ("D20", 500.0), ("E20", 400.0), ("F20", 500.0), ("G20", 400.0), ("H20", "300;1f12"),
                     ("J20", "300;1f12"),
                     ("C21", -150.0), ("D21", 0.0), ("E21", 0.0), ("F21", 300.0), ("G21", 0.0), ("H21", "300;3f20"), ("I21", 0.0),
                     ("J21", 100.0),
                     ("C22", 1.0), ("E22", 2.0), ("G22", 5.0), ("I22", 6.0), ("C23", -150.0), ("E23", 0.0), ("G23", 0.0), ("I23", 0.0),
                     ("F24", "*"), ("I24", "*"),
                     ("C25", "Đai U"), ("D25", "3-4"), ("I25", "Đai C"), ("J25", 2.0), ("C26", "Đai C"), ("D26", 2.0),
                     ("C27", "Đai C"), ("D27", 5.0)
                 })
            t.Set(address, value);

        object[] row11 = { 400.0, 10400.0, 400.0, 13400.0, 400.0, 6200.0, 400.0, 2000.0, 0.0 };
        for (int i = 0; i < row11.Length; i++)
        {
            string column = ((char)('C' + i)).ToString();
            t.Set(column + "10", i % 2 == 0 ? "Cột" : "Nhịp");
            t.Set(column + "11", row11[i]);
        }

        return t;
    }

    public override string ToString() => Name;
}
