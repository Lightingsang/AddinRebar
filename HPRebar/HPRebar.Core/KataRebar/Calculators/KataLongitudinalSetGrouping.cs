using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// One Revit element for longitudinal bars: <see cref="Count"/> copies of <see cref="Base"/> (the bar with the
/// smallest transverse Y), spaced <see cref="Spacing"/> apart towards +Y. A count of 1 is a single bar.
/// </summary>
public sealed record KataLongitudinalSet(KataRebarCurve Base, int Count, double Spacing, IReadOnlyList<KataRebarCurve> Bars)
{
    public bool IsSingle => Count <= 1;

    /// <summary>Distance between the first and the last bar of the set (mm).</summary>
    public double ArrayLength => (Count - 1) * Spacing;
}

/// <summary>
/// Groups the longitudinal bars of a layout into Revit sets: bars of one sheet cell with the same role, diameter
/// and shape (the same polyline apart from its transverse offset) whose offsets are evenly spaced become one
/// fixed-number set; a cell whose bars are not evenly spaced is split into evenly spaced runs of two or more,
/// and every bar left over is a single bar. The bars' positions never change.
/// </summary>
public static class KataLongitudinalSetGrouping
{
    /// <summary>Two positions or spacings closer than this are equal (mm).</summary>
    public const double Tolerance = 0.5;

    public static IReadOnlyList<KataLongitudinalSet> Group(IEnumerable<KataRebarCurve> bars)
    {
        if (bars is null) throw new ArgumentNullException(nameof(bars));

        var sets = new List<KataLongitudinalSet>();
        foreach (var group in bars.GroupBy(Key))
        {
            var ordered = group.OrderBy(b => b.TransverseY).ToList();
            int i = 0;
            while (i < ordered.Count)
            {
                int end = RunEnd(ordered, i);
                var run = ordered.GetRange(i, end - i + 1);
                double spacing = run.Count > 1 ? (run[run.Count - 1].TransverseY - run[0].TransverseY) / (run.Count - 1) : 0.0;
                sets.Add(new KataLongitudinalSet(run[0], run.Count, spacing, run));
                i = end + 1;
            }
        }

        return sets;
    }

    /// <summary>
    /// Last index of the longest evenly spaced run starting at <paramref name="start"/> (itself when none): every
    /// bar within the tolerance of the even spacing fitted from the run's first to its last bar.
    /// </summary>
    private static int RunEnd(IReadOnlyList<KataRebarCurve> bars, int start)
    {
        int end = start;
        while (end + 1 < bars.Count && IsEven(bars, start, end + 1)) end++;
        return end;
    }

    private static bool IsEven(IReadOnlyList<KataRebarCurve> bars, int first, int last)
    {
        double step = (bars[last].TransverseY - bars[first].TransverseY) / (last - first);
        if (step <= Tolerance) return false;
        for (int k = first + 1; k < last; k++)
            if (Math.Abs(bars[k].TransverseY - (bars[first].TransverseY + (k - first) * step)) > Tolerance) return false;
        return true;
    }

    /// <summary>What two bars must share to be copies of each other: everything but the transverse offset.</summary>
    private static string Key(KataRebarCurve bar)
    {
        var sb = new StringBuilder();
        sb.Append(bar.Role).Append('|').Append(bar.BarMark).Append('|').Append(bar.Layer).Append('|')
          .Append(bar.HostSpanIndex).Append('|').Append(bar.HostSupportIndex).Append('|')
          .Append(Round(bar.Diameter)).Append('|').Append(bar.StartHookAngle).Append('|').Append(bar.EndHookAngle);
        foreach (var p in bar.Polyline.Points)
            sb.Append('|').Append(Round(p.X)).Append(',').Append(Round(p.Y - bar.TransverseY)).Append(',').Append(Round(p.Z));
        return sb.ToString();
    }

    private static string Round(double mm) => Math.Round(mm / Tolerance).ToString(CultureInfo.InvariantCulture);
}
