using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Calculators;

namespace HPRebar.Core.KataRebar.ColumnStack;

/// <summary>A point of the elevation along b: X from the lowest storey's centre, Z absolute, mm.</summary>
public readonly record struct KataColumnElevationPoint(double X, double Z);

/// <summary>A storey's concrete in the elevation, with the beam depth at its head (dashed).</summary>
public sealed record KataColumnDrawingStorey(int Storey, string Name, double BaseMm, double TopMm, double LeftMm, double RightMm, double BeamDepthMm);

/// <summary>A level mark: name and absolute elevation (mm).</summary>
public sealed record KataColumnDrawingLevel(string Name, double ZMm);

/// <summary>
/// A bar as Kata's elevation draws it: the outermost bar of its group on its face for each storey and layer (group A
/// on the left, B on the right), the starters included; <see cref="Count"/> bars of that group, storey and Ø share
/// one label ("20Ø25"), written on the first such bar only (<see cref="Labelled"/>), as VC labels group and Ø.
/// </summary>
public sealed record KataColumnDrawingBar(
    int Number,
    int Storey,
    KataColumnBarKind Kind,
    KataColumnBarGroup Group,
    int Layer,
    double DiameterMm,
    double LengthMm,
    int Count,
    bool Labelled,
    IReadOnlyList<KataColumnElevationPoint> Points);

/// <summary>A hoop zone between the storey's hoop centre lines (elevation X), its spacing, hoop count and the hoop's number and Ø.</summary>
public sealed record KataColumnDrawingHoopZone(int Storey, int Number, double DiameterMm, double SpacingMm, int Count, double FromMm, double ToMm, double LeftMm, double RightMm);

/// <summary>Where a section cuts the stack ("1-1" for the lowest storey).</summary>
public sealed record KataColumnDrawingCut(int Storey, string Name, double ZMm);

/// <summary>The elevation along b, as Kata's VC draws it, in elevation mm; <see cref="Title"/> under it ("C1 (SL=1)").</summary>
public sealed record KataColumnElevationDrawing(
    string Title,
    double LeftMm,
    double RightMm,
    double BottomMm,
    double TopMm,
    double PileCapLeftMm,
    double PileCapRightMm,
    IReadOnlyList<KataColumnDrawingStorey> Storeys,
    IReadOnlyList<KataColumnDrawingLevel> Levels,
    IReadOnlyList<KataColumnDrawingBar> Bars,
    IReadOnlyList<KataColumnDrawingHoopZone> HoopZones,
    IReadOnlyList<KataColumnDrawingCut> Cuts,
    IReadOnlyList<KataDimChain> Chains);

/// <summary>
/// What the page draws for the bars the run will create (on the real faces): the elevation along b and, by storey,
/// the section names; the sections themselves are <see cref="KataColumnRebarSummary.Sections"/>.
/// </summary>
public static class KataColumnDrawingMapper
{
    /// <summary>The pile cap is drawn this far beyond the lowest storey on each side (its plan size is not on the sheet).</summary>
    private const double PileCapOverhangMm = 400;

    /// <summary>Kata's own label of the pile-cap row (A9).</summary>
    public const string PileCapLevel = "PILECAPS";

    /// <returns>Null while the sheet has errors (nothing would be created).</returns>
    public static KataColumnElevationDrawing? Map(KataColumnSpec spec, IReadOnlyList<KataColumnStoreyGeometry> storeys,
        KataColumnRebarDesign design, IReadOnlyList<KataColumnSectionDrawing> sections)
    {
        if (design.Errors.Count > 0 || storeys.Count == 0)
        {
            return null;
        }

        var elevation = KataColumnElevationDims.Build(spec, storeys, design);
        double cx = elevation.CentreXMm;
        var bars = Bars(design, cx);
        bool cap = spec.PileCapHeightMm > 0;
        // Without a pile cap the starters still reach one lap below the first storey.
        double bottom = Math.Min(elevation.BottomMm, bars.SelectMany(b => b.Points).Select(p => p.Z).DefaultIfEmpty(elevation.BottomMm).Min());
        var outlines = storeys.Select((s, k) => new KataColumnDrawingStorey(
            k, Name(spec, k), s.BaseMm, s.TopMm, s.MinXMm - cx, s.MaxXMm - cx, k < spec.Rows.Count ? spec.Rows[k].BeamDepthMm : 0)).ToList();
        return new KataColumnElevationDrawing(
            $"{spec.Name} (SL={spec.Count})",
            elevation.LeftMm,
            elevation.RightMm,
            bottom,
            elevation.TopMm,
            outlines[0].LeftMm - (cap ? PileCapOverhangMm : 0),
            outlines[0].RightMm + (cap ? PileCapOverhangMm : 0),
            outlines,
            Levels(spec, storeys, cap ? elevation.BottomMm : null),
            bars,
            HoopZones(spec, storeys, design, cx),
            sections.Select((s, k) => new KataColumnDrawingCut(k, $"{k + 1}-{k + 1}", s.CutElevationMm)).ToList(),
            elevation.Chains);
    }

    private static string Name(KataColumnSpec spec, int k) => k < spec.Rows.Count ? spec.Rows[k].Name : string.Empty;

    /// <summary>Cap bottom (when there is a cap; Kata's A9 label), every storey base and the top, named as the sheet's rows.</summary>
    private static List<KataColumnDrawingLevel> Levels(KataColumnSpec spec, IReadOnlyList<KataColumnStoreyGeometry> storeys, double? capBottomMm)
    {
        var levels = new List<KataColumnDrawingLevel>();
        if (capBottomMm is { } capBottom)
        {
            levels.Add(new KataColumnDrawingLevel(PileCapLevel, capBottom));
        }

        levels.AddRange(storeys.Select((s, k) => new KataColumnDrawingLevel(Name(spec, k), s.BaseMm)));
        levels.Add(new KataColumnDrawingLevel(Name(spec, storeys.Count), storeys[^1].TopMm));
        return levels;
    }

    private static List<KataColumnDrawingBar> Bars(KataColumnRebarDesign design, double cx)
    {
        var drawn = new List<KataColumnDrawingBar>();
        var labelled = new HashSet<(int, KataColumnBarGroup, double)>();
        var sets = design.Bars
            .GroupBy(b => (Key: KataColumnElevationDims.StoreyKey(b.Run), b.Run.Group, b.Run.Layer))
            .OrderBy(g => g.Key.Key).ThenBy(g => g.Key.Group).ThenBy(g => g.Key.Layer);
        foreach (var set in sets)
        {
            var bar = KataColumnElevationDims.Outermost(set, b => b.Run, leftmost: set.Key.Group == KataColumnBarGroup.A);
            var run = bar.Run;
            int count = design.Bars.Count(b => KataColumnElevationDims.StoreyKey(b.Run) == set.Key.Key && b.Run.Group == run.Group && b.Run.DiameterMm == run.DiameterMm);
            drawn.Add(new KataColumnDrawingBar(bar.Number, run.Storey, run.Kind, run.Group, run.Layer, run.DiameterMm, Length(run.Points), count,
                labelled.Add((set.Key.Key, run.Group, run.DiameterMm)),
                run.Points.Select(p => new KataColumnElevationPoint(p.X - cx, p.Z)).ToList()));
        }

        return drawn;
    }

    private static List<KataColumnDrawingHoopZone> HoopZones(KataColumnSpec spec, IReadOnlyList<KataColumnStoreyGeometry> storeys, KataColumnRebarDesign design, double cx)
    {
        double offset = spec.CoverMm + spec.Hoops.DiameterMm / 2;
        return design.Storeys.SelectMany(d => d.HoopZones.Select(z =>
        {
            var g = storeys[d.Storey];
            return new KataColumnDrawingHoopZone(d.Storey, d.HoopNumber, spec.Hoops.DiameterMm, z.SpacingMm, KataColumnRebarSummaryMapper.Count(z),
                g.BaseMm + z.FromMm, g.BaseMm + z.ToMm, g.MinXMm + offset - cx, g.MaxXMm - offset - cx);
        })).ToList();
    }

    private static double Length(IReadOnlyList<KataPoint3> points) =>
        points.Zip(points.Skip(1), (a, b) => Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2) + Math.Pow(b.Z - a.Z, 2))).Sum();
}
