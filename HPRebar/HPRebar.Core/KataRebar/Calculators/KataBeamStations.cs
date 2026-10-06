using System;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Stations along the beam axis in mm, x = 0 at the outer face of the first support in sheet order.
/// Support k sits between <see cref="SupportStart"/>[k] and <see cref="SupportEnd"/>[k]; span i between
/// <see cref="SpanStart"/>[i] and <see cref="SpanEnd"/>[i], with support i on its left and i + 1 on its right.
/// </summary>
public sealed class KataBeamStations
{
    private KataBeamStations(int spanCount)
    {
        SupportStart = new double[spanCount + 1];
        SupportEnd = new double[spanCount + 1];
        SupportWidth = new double[spanCount + 1];
        SpanStart = new double[spanCount];
        SpanEnd = new double[spanCount];
    }

    public double[] SupportStart { get; }
    public double[] SupportEnd { get; }
    public double[] SupportWidth { get; }
    public double[] SpanStart { get; }
    public double[] SpanEnd { get; }
    public double TotalLength { get; private set; }
    public bool IsLeftCantilever { get; private set; }
    public bool IsRightCantilever { get; private set; }

    public int SpanCount => SpanStart.Length;

    public double SupportCentre(int k) => (SupportStart[k] + SupportEnd[k]) / 2.0;

    /// <summary>
    /// How far the beam runs past the outer face of its first / last column, to the outer face of a crossing beam set
    /// off the column's centre (rows 20 / 21: B03's "400x500" at −150 under a 400 column ends the beam at −150).
    /// </summary>
    public double StartOverhang { get; private set; }

    public double EndOverhang { get; private set; }

    /// <summary>The room an end bar has in support <paramref name="k"/>: its width and the beam past it.</summary>
    public double AnchorWidth(int k) =>
        SupportWidth[k] + (k == 0 ? StartOverhang : 0.0) + (k == SpanCount ? EndOverhang : 0.0);

    public static KataBeamStations From(KataBeamRebarSpec spec)
    {
        if (spec is null) throw new ArgumentNullException(nameof(spec));

        int spanCount = spec.Spans.Count;
        var stations = new KataBeamStations(spanCount);
        double x = 0.0;

        for (int i = 0; i <= spanCount; i++)
        {
            double width = i < spec.Supports.Count ? Math.Max(0.0, spec.Supports[i].ColumnWidth) : 0.0;
            stations.SupportStart[i] = x;
            stations.SupportWidth[i] = width;
            x += width;
            stations.SupportEnd[i] = x;

            if (i == spanCount) break;

            stations.SpanStart[i] = x;
            x += Math.Max(0.0, spec.Spans[i].Length);
            stations.SpanEnd[i] = x;
        }

        stations.TotalLength = x;
        if (spec.Supports.Count > spanCount)
        {
            stations.StartOverhang = Overhang(spec.Supports[0], start: true);
            stations.EndOverhang = Overhang(spec.Supports[spanCount], start: false);
        }

        stations.IsLeftCantilever = spec.Supports.Count > 0 && spec.Supports[0].ColumnWidth <= 0.0;
        stations.IsRightCantilever = spec.Supports.Count > spanCount && spec.Supports[spanCount].ColumnWidth <= 0.0;
        return stations;
    }

    /// <summary>The crossing beam's outer face past the column's outer face: centre at the column's centre plus row 21.</summary>
    private static double Overhang(KataSupportRebarSpec support, bool start)
    {
        if (support.ColumnWidth <= 0.0 || support.CrossingBeamWidth <= 0.0) return 0.0;
        double centre = support.ColumnWidth / 2.0 + support.CrossingBeamOffset;
        return start
            ? Math.Max(0.0, support.CrossingBeamWidth / 2.0 - centre)
            : Math.Max(0.0, centre + support.CrossingBeamWidth / 2.0 - support.ColumnWidth);
    }
}
