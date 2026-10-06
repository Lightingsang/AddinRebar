using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>The beam run as the elevation drawing sees it: stations, kinds of support, soffits, grids, stub bottom.</summary>
internal sealed class KataDrawingFrame
{
    private readonly KataBeamRebarSpec _spec;

    /// <param name="stirrupRow">Row of the stirrup tags over the beam (<see cref="KataBarTagBuilder.StirrupRowOf"/>).</param>
    public KataDrawingFrame(KataBeamRebarSpec spec, double stirrupRow = KataTagStyle.StirrupRow)
    {
        _spec = spec ?? throw new ArgumentNullException(nameof(spec));
        Lift = Math.Max(0.0, stirrupRow - KataTagStyle.StirrupRow);
        St = KataBeamStations.From(spec);
        double lowest = St.SpanCount == 0 ? -spec.Height : Enumerable.Range(0, St.SpanCount).Min(Soffit);
        StubBottom = lowest - KataDrawingStyle.StubBelow;
        Slab = spec.SlabThickness > 0.0 ? spec.SlabThickness : 0.0;
    }

    public KataBeamStations St { get; }

    /// <summary>
    /// How far the stirrup tags are pushed out by a third or fourth row of bar tags; the zone chain, the flags and the
    /// grid tops over the beam keep their distance to that row (B01: tags at 1050, chain 1200, flags 1450 at 1:50).
    /// </summary>
    public double Lift { get; }

    public int SpanCount => St.SpanCount;

    public int SupportCount => St.SpanCount + 1;

    public double Length => St.TotalLength;

    /// <summary>Bottom of the column stubs: the lowest soffit and a stub under it. The rows under the beam hang from it.</summary>
    public double StubBottom { get; }

    /// <summary>Slab thickness B7; 0 when the sheet has none.</summary>
    public double Slab { get; }

    public double Soffit(int span) => -_spec.DepthOf(span);

    /// <summary>The beam past an end column's outer face to a crossing beam's (rows 20 / 21), and that beam's width and depth.</summary>
    public (double Overhang, double Width, double Depth) EndCrossing(int k)
    {
        double overhang = k == 0 ? St.StartOverhang : k == SpanCount ? St.EndOverhang : 0.0;
        var support = Support(k);
        if (overhang <= 0.0 || support is null) return (0.0, 0.0, 0.0);
        return (overhang, support.CrossingBeamWidth, support.CrossingBeamDepth > 0.0 ? support.CrossingBeamDepth : _spec.Height);
    }

    private KataSupportRebarSpec? Support(int k) => k >= 0 && k < _spec.Supports.Count ? _spec.Supports[k] : null;

    /// <summary>A crossing beam carrying the run (row 11 "b x h"): drawn as the run going on, no column stubs.</summary>
    public bool IsBeam(int k) => St.SupportWidth[k] > 0.0 && Support(k)?.SupportSection.Length > 0;

    public bool IsColumn(int k) => St.SupportWidth[k] > 0.0 && !IsBeam(k);

    /// <summary>Depth under a crossing-beam support: the shallower of its span(s) and the beam.</summary>
    public double SupportDepth(int k) => _spec.SupportDepth(k);

    /// <summary>The beam top over span <paramref name="span"/> as (station, level) points, steps drawn as vertical faces.</summary>
    public IReadOnlyList<(double X, double Z)> TopLine(int span)
    {
        double a = St.SpanStart[span], b = St.SpanEnd[span];
        var points = new List<(double X, double Z)> { (a, _spec.TopAt(span, 0.0)) };
        foreach (var step in _spec.Spans[span].TopSteps)
        {
            points.Add((a + step.AtMm, points[points.Count - 1].Z));
            points.Add((a + step.AtMm, step.TopDrop));
        }

        points.Add((b, points[points.Count - 1].Z));
        return points;
    }

    /// <summary>Beam top at station <paramref name="x"/> (over a support between two levels, the right one).</summary>
    public double TopAt(double x) => KataTopProfile.LevelAt(_spec, St, x, right: true);

    /// <summary>Beam top at the start (<paramref name="end"/> false) or end of span <paramref name="span"/>.</summary>
    public double Top(int span, bool end) => _spec.TopAt(span, end ? _spec.Spans[span].Length : 0.0);

    /// <summary>Every support with a width has its grid line, at its centre plus row 23; only named ones get a bubble.</summary>
    public bool HasGrid(int k) => St.SupportWidth[k] > 0.0;

    public double GridX(int k) => St.SupportCentre(k) + (Support(k)?.GridOffset ?? 0.0);

    public string GridName(int k) => Support(k)?.GridName?.Trim() ?? "";

    public IEnumerable<double> Grids() => Enumerable.Range(0, SupportCount).Where(HasGrid).Select(GridX);

    /// <summary>Soffit of the span nearest <paramref name="x"/> (the one it lies in, else the closest end).</summary>
    public double SoffitNear(double x)
    {
        if (SpanCount == 0) return -_spec.Height;
        int best = 0;
        double bestGap = double.MaxValue;
        for (int i = 0; i < SpanCount; i++)
        {
            double gap = x < St.SpanStart[i] ? St.SpanStart[i] - x : x > St.SpanEnd[i] ? x - St.SpanEnd[i] : 0.0;
            if (gap < bestGap - 1e-9)
            {
                best = i;
                bestGap = gap;
            }
        }

        return Soffit(best);
    }
}
