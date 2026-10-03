using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>The beam run as the elevation drawing sees it: stations, kinds of support, soffits, grids, stub bottom.</summary>
internal sealed class KataDrawingFrame
{
    private readonly KataBeamRebarSpec _spec;

    public KataDrawingFrame(KataBeamRebarSpec spec)
    {
        _spec = spec ?? throw new ArgumentNullException(nameof(spec));
        St = KataBeamStations.From(spec);
        double lowest = St.SpanCount == 0 ? -spec.Height : Enumerable.Range(0, St.SpanCount).Min(Soffit);
        StubBottom = lowest - KataDrawingStyle.StubBelow;
        Slab = spec.SlabThickness > 0.0 ? spec.SlabThickness : 0.0;
    }

    public KataBeamStations St { get; }

    public int SpanCount => St.SpanCount;

    public int SupportCount => St.SpanCount + 1;

    public double Length => St.TotalLength;

    /// <summary>Bottom of the column stubs: the lowest soffit and a stub under it. The rows under the beam hang from it.</summary>
    public double StubBottom { get; }

    /// <summary>Slab thickness B7; 0 when the sheet has none.</summary>
    public double Slab { get; }

    public double Soffit(int span) => -_spec.DepthOf(span);

    private KataSupportRebarSpec? Support(int k) => k >= 0 && k < _spec.Supports.Count ? _spec.Supports[k] : null;

    /// <summary>A crossing beam carrying the run (row 11 "b x h"): drawn as the run going on, no column stubs.</summary>
    public bool IsBeam(int k) => St.SupportWidth[k] > 0.0 && Support(k)?.SupportSection.Length > 0;

    public bool IsColumn(int k) => St.SupportWidth[k] > 0.0 && !IsBeam(k);

    /// <summary>Depth under a crossing-beam support: the shallower of its span(s) and the beam.</summary>
    public double SupportDepth(int k) => _spec.SupportDepth(k);

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
