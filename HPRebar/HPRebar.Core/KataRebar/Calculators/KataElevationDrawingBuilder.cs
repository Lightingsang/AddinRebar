using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Kata's elevation of a beam run (<see cref="KataElevationDrawing"/>), laid out the way T2-DY7.dwg draws DY7 and
/// DY14: outline (<see cref="KataElevationOutline"/>), bars at the levels Kata draws them (<see cref="KataDrawingLevels"/>,
/// <see cref="KataBarDrafting"/>), each stirrup zone's first and last stirrup, dimensions (<see cref="KataElevationDims"/>),
/// a flag over and under every section cut, grid bubbles, the level mark and the title.
/// </summary>
public static class KataElevationDrawingBuilder
{
    /// <param name="stirrupRow">Row of the stirrup tags (<see cref="KataBarTagBuilder.StirrupRowOf"/>): what is over it moves with it.</param>
    public static KataElevationDrawing Build(KataBeamRebarSpec spec, KataRebarLayoutResult layout, IReadOnlyList<KataSectionCut> cuts, double stirrupDiameter,
        double stirrupRow = KataTagStyle.StirrupRow)
    {
        if (spec is null) throw new ArgumentNullException(nameof(spec));
        if (layout is null) throw new ArgumentNullException(nameof(layout));
        if (cuts is null) throw new ArgumentNullException(nameof(cuts));

        var f = new KataDrawingFrame(spec, stirrupRow);
        var lines = KataElevationOutline.Lines(f).ToList();
        lines.AddRange(Stirrups(f, layout, KataStirrupRuns.Of(layout)));
        lines.AddRange(Bars(layout, stirrupDiameter));
        var dims = KataElevationDims.Build(f, layout, stirrupDiameter).ToList();

        double bubbleZ = f.StubBottom - KataDrawingStyle.BubbleBelow;
        var flags = cuts.SelectMany(c => new[]
        {
            new KataDrawingFlag(c.X, KataDrawingStyle.FlagAboveZ + f.Lift, c.Number, false),
            new KataDrawingFlag(c.X, bubbleZ, c.Number, true)
        }).ToList();
        var bubbles = Enumerable.Range(0, f.SupportCount)
            .Where(k => f.HasGrid(k) && f.GridName(k).Length > 0)
            .Select(k => new KataDrawingBubble(f.GridX(k), bubbleZ, f.GridName(k)))
            .ToList();

        string levelText = spec.LevelElevation?.Trim() ?? "";
        var level = levelText.Length > 0 ? new KataDrawingLevel(KataDrawingStyle.LevelX, 0.0, levelText) : null;
        // Over the whole beam, past its end columns to a crossing beam too (B03: L=33750 centred on 16725).
        double beamStart = -f.St.StartOverhang, beamEnd = f.Length + f.St.EndOverhang;
        var title = new KataDrawingTitle((beamStart + beamEnd) / 2.0, f.StubBottom - KataDrawingStyle.TitleBelow,
            string.Format(CultureInfo.InvariantCulture, "{0} (SL={1}; L={2:0})", spec.BeamName, Math.Max(1, spec.BeamCount), beamEnd - beamStart),
            KataDrawingStyle.TitleScale);

        double minX = Math.Min(KataDrawingStyle.LevelX - KataDrawingStyle.BubbleTickEnd, KataDrawingStyle.DepthDimX - KataDrawingStyle.DimTextHeight * 2.0);
        double maxX = beamEnd + KataDrawingStyle.BubbleTickEnd;
        double topZ = KataDrawingStyle.FlagAboveZ + f.Lift + KataDrawingStyle.FlagHeight;
        double bottomZ = title.Z - KataDrawingStyle.TitleScaleDrop - KataDrawingStyle.DimTextHeight;
        return new KataElevationDrawing(lines, dims, flags, bubbles, level, title, minX, maxX, topZ, bottomZ);
    }

    /// <summary>
    /// The first and last stirrup of each zone, <see cref="KataTagStyle.StirrupInset"/> inside the beam's top and soffit
    /// where it stands (B01's console, its top 200 down: −225…−1075).
    /// </summary>
    private static IEnumerable<KataDrawingLine> Stirrups(KataDrawingFrame f, KataRebarLayoutResult layout, IReadOnlyList<KataStirrupRun> runs)
    {
        foreach (var run in runs)
        {
            double soffit = run.Span >= 0 && run.Span < f.SpanCount ? f.Soffit(run.Span) : f.SoffitNear(run.First);
            var ends = run.Last - run.First < 1.0 ? new[] { run.First } : new[] { run.First, run.Last };
            // The zones merged into this run: same span and number, stirrups inside it.
            var keys = layout.StirrupZones
                .Where(z => z.Count > 0 && z.Stations.Count > 0 && z.SpanIndex == run.Span && z.BarNumber == run.Number
                    && z.Stations[0] >= run.First - 0.5 && z.Stations[z.Stations.Count - 1] <= run.Last + 0.5)
                .Select(KataLayoutRemoval.Key)
                .ToList();
            foreach (double x in ends)
                yield return new KataDrawingLine(KataDrawingPen.Stirrup, new[] { (x, f.TopAt(x) - KataTagStyle.StirrupInset), (x, soffit + KataTagStyle.StirrupInset) }, keys);
        }
    }

    /// <summary>Each bar line once (bars lying on one another across the beam are one line), drafted at Kata's level.</summary>
    /// <remarks>A line carries the keys of every bar drafted on it: deleting the line deletes them all.</remarks>
    private static IEnumerable<KataDrawingLine> Bars(KataRebarLayoutResult layout, double stirrupDiameter)
    {
        var lines = new List<(string Seen, KataRebarCurve First, List<(double X, double Z)> Points, List<string> Keys)>();
        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var bar in layout.SideBars.Concat(layout.MainBottomBars).Concat(layout.ExtraBottomBars).Concat(layout.MainTopBars).Concat(layout.ExtraTopBars)
                     .Concat(layout.HangerBars))
        {
            double shift = KataDrawingLevels.Shift(bar, stirrupDiameter);
            var points = bar.Polyline.Points.Select(p => (p.X, Z: p.Z + shift)).ToList();
            if (points.Count < 2) continue;
            ShiftLeg(points, 0, 1, Math.Abs(shift));
            ShiftLeg(points, points.Count - 1, points.Count - 2, Math.Abs(shift));
            string seen = string.Join(";", points.Select(p => FormattableString.Invariant($"{p.X:0.0},{p.Z:0.0}")));
            if (index.TryGetValue(seen, out int at))
            {
                lines[at].Keys.Add(KataLayoutRemoval.Key(bar));
                continue;
            }

            index[seen] = lines.Count;
            lines.Add((seen, bar, points, new List<string> { KataLayoutRemoval.Key(bar) }));
        }

        foreach (var line in lines)
        {
            bool top = line.First.Role is KataBarRole.MainTop or KataBarRole.ExtraTop;
            yield return new KataDrawingLine(KataDrawingPen.Bar, KataBarDrafting.Outline(line.Points, top), line.Keys);
        }
    }

    /// <summary>
    /// A hook leg at the bar's end (<paramref name="tip"/>, its neighbour <paramref name="next"/>) drawn the same
    /// <paramref name="shift"/> nearer the beam's end as the bar is nearer its face: the leg of a top bar on the
    /// stirrup's centre line, as Kata draws DY7's (30 from the column's outer face).
    /// </summary>
    private static void ShiftLeg(List<(double X, double Z)> points, int tip, int next, double shift)
    {
        if (shift <= 0.0 || Math.Abs(points[tip].X - points[next].X) > 1.0 || Math.Abs(points[tip].Z - points[next].Z) < 1.0) return;
        double body = points.Average(p => p.X);
        double dx = points[tip].X < body ? -shift : shift;
        points[tip] = (points[tip].X + dx, points[tip].Z);
        points[next] = (points[next].X + dx, points[next].Z);
    }
}
