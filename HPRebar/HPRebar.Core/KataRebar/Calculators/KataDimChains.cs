using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>One point a dimension chain measures to: its station along the measured axis and where it lies across it.</summary>
public sealed record KataDimStation(double Along, double Across);

/// <summary>
/// A chain of touching dimensions on one line, as one Revit dimension draws it: <paramref name="Stations"/> in order
/// along the measured axis (X, or Z when <paramref name="Vertical"/>), the line at <paramref name="LineAt"/>.
/// </summary>
public sealed record KataDimChain(bool Vertical, double LineAt, IReadOnlyList<KataDimStation> Stations);

/// <summary>
/// Kata's dimensions (<see cref="KataDrawingDim"/>, one segment each) grouped into the chains a Revit dimension draws:
/// segments on the same line that touch end to end become one chain. A stirrup run (<see cref="KataDimStyle.Run"/>,
/// no text) is not a dimension in Revit and is left out.
/// </summary>
public static class KataDimChains
{
    private const double Tolerance = 0.5;

    public static IReadOnlyList<KataDimChain> From(IEnumerable<KataDrawingDim> dims)
    {
        if (dims is null) throw new ArgumentNullException(nameof(dims));

        var chains = new List<KataDimChain>();
        var lines = dims.Where(d => d.Style == KataDimStyle.Kata && d.Value > Tolerance)
            .GroupBy(d => (d.Vertical, Line: Math.Round(d.LineAt / Tolerance)));
        foreach (var line in lines)
        {
            var segments = line.Select(Segment).OrderBy(s => s.From.Along).ThenBy(s => s.To.Along).ToList();
            var current = new List<KataDimStation> { segments[0].From, segments[0].To };
            for (int i = 1; i < segments.Count; i++)
            {
                var s = segments[i];
                double end = current[current.Count - 1].Along;
                if (Math.Abs(s.From.Along - end) <= Tolerance)
                {
                    current.Add(s.To);
                    continue;
                }

                if (s.From.Along < end - Tolerance && s.To.Along <= end + Tolerance && current.Any(c => Math.Abs(c.Along - s.From.Along) <= Tolerance))
                    continue; // a segment the chain already measures

                chains.Add(new KataDimChain(line.Key.Vertical, line.First().LineAt, current));
                current = new List<KataDimStation> { s.From, s.To };
            }

            chains.Add(new KataDimChain(line.Key.Vertical, line.First().LineAt, current));
        }

        return chains;
    }

    /// <summary>The segment's two points ordered along its measured axis.</summary>
    private static (KataDimStation From, KataDimStation To) Segment(KataDrawingDim d)
    {
        var a = d.Vertical ? new KataDimStation(d.Z1, d.X1) : new KataDimStation(d.X1, d.Z1);
        var b = d.Vertical ? new KataDimStation(d.Z2, d.X2) : new KataDimStation(d.X2, d.Z2);
        return a.Along <= b.Along ? (a, b) : (b, a);
    }
}
