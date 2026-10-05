using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// The concrete of Kata's elevation (T2-DY7.dwg, DY7 and DY14): each span's top (row 19, steps as faces) and soffit, a column support's stubs
/// <see cref="KataDrawingStyle.StubAbove"/> over the top and down to the stub bottom with a break line at both
/// ends, a crossing beam carrying the run drawn as the run going on (no stubs), the end faces; hidden lines over the
/// column tops and along the slab soffit, split at the grids; the grid lines.
/// </summary>
internal static class KataElevationOutline
{
    public static IEnumerable<KataDrawingLine> Lines(KataDrawingFrame f)
    {
        var st = f.St;
        int last = f.SpanCount;
        if (last == 0) yield break;

        foreach (var line in End(f, 0, st.SupportStart[0], st.SupportEnd[0], f.Soffit(0), f.Top(0, end: false))) yield return line;
        for (int i = 0; i < f.SpanCount; i++)
        {
            double a = st.SpanStart[i], b = st.SpanEnd[i], soffit = f.Soffit(i);
            var top = new List<(double, double)>();
            if (f.IsColumn(i)) top.Add((a, KataDrawingStyle.StubAbove));
            top.AddRange(f.TopLine(i));
            if (f.IsColumn(i + 1)) top.Add((b, KataDrawingStyle.StubAbove));
            yield return Line(KataDrawingPen.Outline, top);

            var bottom = new List<(double, double)>();
            if (f.IsColumn(i)) bottom.Add((a, f.StubBottom));
            bottom.Add((a, soffit));
            bottom.Add((b, soffit));
            if (f.IsColumn(i + 1)) bottom.Add((b, f.StubBottom));
            yield return Line(KataDrawingPen.Outline, bottom);
        }

        foreach (var line in End(f, last, st.SupportEnd[last], st.SupportStart[last], f.Soffit(last - 1), f.Top(last - 1, end: true))) yield return line;

        for (int k = 0; k < f.SupportCount; k++)
        {
            double s = st.SupportStart[k], e = st.SupportEnd[k];
            if (f.IsColumn(k))
            {
                yield return Line(KataDrawingPen.Hidden, new[] { (s, 0.0), (e, 0.0) });
                yield return Break(s, e, KataDrawingStyle.StubAbove);
                yield return Break(s, e, f.StubBottom);
            }
            else if (f.IsBeam(k) && k > 0 && k < last)
            {
                // A crossing beam inside the run: its top and soffit go on through it (no Kata drawing of one yet);
                // where the spans' tops differ (row 19) the top steps at the beam's centre.
                double depth = -f.SupportDepth(k);
                double left = f.Top(k - 1, end: true), right = f.Top(k, end: false), c = (s + e) / 2.0;
                yield return Line(KataDrawingPen.Outline, Math.Abs(left - right) > 1.0
                    ? new[] { (s, left), (c, left), (c, right), (e, right) }
                    : new[] { (s, left), (e, right) });
                yield return Line(KataDrawingPen.Outline, new[] { (s, depth), (e, depth) });
                foreach (var step in Step(s, depth, f.Soffit(k - 1))) yield return step;
                foreach (var step in Step(e, depth, f.Soffit(k))) yield return step;
            }
        }

        foreach (var line in Slab(f)) yield return line;

        foreach (double x in f.Grids())
            yield return Line(KataDrawingPen.Grid, new[] { (x, KataDrawingStyle.GridTopZ), (x, f.StubBottom - KataDrawingStyle.GridEndBelow) });
    }

    /// <summary>
    /// End support <paramref name="k"/> from its outer face <paramref name="outer"/> to its inner face: a column's outer
    /// face from its upper stub to its lower one; a crossing beam's top, end face and soffit; a free end's face.
    /// </summary>
    private static IEnumerable<KataDrawingLine> End(KataDrawingFrame f, int k, double outer, double inner, double soffit, double top)
    {
        if (f.IsColumn(k))
        {
            yield return Line(KataDrawingPen.Outline, new[] { (outer, KataDrawingStyle.StubAbove), (outer, 0.0), (outer, soffit), (outer, f.StubBottom) });
        }
        else if (f.IsBeam(k))
        {
            double depth = -f.SupportDepth(k);
            yield return Line(KataDrawingPen.Outline, new[] { (inner, top), (outer, top), (outer, depth) });
            yield return Line(KataDrawingPen.Outline, new[] { (inner, depth), (outer, depth) });
            foreach (var step in Step(inner, depth, soffit)) yield return step;
        }
        else
        {
            yield return Line(KataDrawingPen.Outline, new[] { (outer, top), (outer, soffit) });
        }
    }

    /// <summary>
    /// The slab soffit B7 under the beam top, hidden, from end to end and split at the grids as Kata draws it; at a
    /// column end it drops to the span's soffit at the outer face, at a crossing-beam end to the beam's soffit at both
    /// faces of that beam.
    /// </summary>
    private static IEnumerable<KataDrawingLine> Slab(KataDrawingFrame f)
    {
        if (f.Slab <= 0.0) yield break;
        var st = f.St;
        int last = f.SpanCount;
        double z = -f.Slab;

        var path = new List<(double X, double Z)>();
        if (f.IsBeam(0))
        {
            yield return Line(KataDrawingPen.Hidden, new[] { (st.SupportStart[0], z), (st.SupportStart[0], -f.SupportDepth(0)) });
            path.Add((st.SupportEnd[0], -f.SupportDepth(0)));
            path.Add((st.SupportEnd[0], z));
        }
        else
        {
            path.Add((0.0, f.Soffit(0)));
            path.Add((0.0, z));
        }

        double endX = f.IsBeam(last) ? st.SupportStart[last] : st.SupportEnd[last];
        foreach (double x in f.Grids().Where(g => g > path[path.Count - 1].X + 1.0 && g < endX - 1.0).OrderBy(g => g))
        {
            path.Add((x, z));
            yield return Line(KataDrawingPen.Hidden, path.ToList());
            path.Clear();
            path.Add((x, z));
        }

        path.Add((endX, z));
        if (f.IsBeam(last))
        {
            path.Add((endX, -f.SupportDepth(last)));
            yield return Line(KataDrawingPen.Hidden, path);
            yield return Line(KataDrawingPen.Hidden, new[] { (st.SupportEnd[last], -f.SupportDepth(last)), (st.SupportEnd[last], z) });
        }
        else
        {
            path.Add((endX, f.Soffit(last - 1)));
            yield return Line(KataDrawingPen.Hidden, path);
        }
    }

    /// <summary>The face joining a crossing beam's soffit to a deeper (or shallower) span's at <paramref name="x"/>.</summary>
    private static IEnumerable<KataDrawingLine> Step(double x, double beamSoffit, double spanSoffit)
    {
        if (Math.Abs(beamSoffit - spanSoffit) > 1.0) yield return Line(KataDrawingPen.Outline, new[] { (x, beamSoffit), (x, spanSoffit) });
    }

    /// <summary>
    /// Kata's break line across a column at height <paramref name="z"/>: it overruns both faces by k and zigzags ±k
    /// about the column's centre, k = min(50, width / 6) (DY14: 50 for 450 and 500, 41.7 for 250).
    /// </summary>
    private static KataDrawingLine Break(double start, double end, double z)
    {
        double k = Math.Min(KataDrawingStyle.BreakSize, (end - start) / 6.0), c = (start + end) / 2.0;
        return Line(KataDrawingPen.Thin, new[]
        {
            (start - k, z), (c - k, z), (c - k, z + k), (c + k, z - k), (c + k, z), (end + k, z)
        });
    }

    private static KataDrawingLine Line(KataDrawingPen pen, IReadOnlyList<(double X, double Z)> points) => new(pen, points);
}
