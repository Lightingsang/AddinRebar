using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Main bars of a span of its own (rows 19 / 21, B01 L "3f20" → 3Ø20 in sections 11-14): the main bars are laid out
/// from B11 / B12 and cut where the bars change (<see cref="KataWidthProfile"/>); every piece then takes the bars of
/// the span it lies mostly in — as many as that span names, spread across its width, along the same path, their outer
/// face kept against the stirrup (a thinner bar's centre moves toward that face by half the difference).
/// </summary>
public static class KataMainBarSwap
{
    /// <param name="face">+1 for top bars (the top face), −1 for bottom bars.</param>
    public static List<KataRebarCurve> Apply(
        KataBeamRebarSpec spec, KataDetailingRules rules, KataBeamStations st, IReadOnlyList<KataRebarCurve> bars,
        Func<int, KataBarItem> mainBars, int face, ref int barId)
    {
        var result = new List<KataRebarCurve>(bars.Count);
        foreach (var piece in bars.GroupBy(PieceKey))
        {
            var group = piece.ToList();
            var first = group[0];
            int home = KataWidthProfile.HomeSpan(st, first);
            var wanted = mainBars(home);
            if (wanted.IsEmpty || (wanted.Count == group.Count && Math.Abs(wanted.Diameter - first.Diameter) < 0.5))
            {
                result.AddRange(group);
                continue;
            }

            var ys = KataRebarCalculator.ComputeTransverseYPositions(spec.WidthOf(home), rules.StirrupCover, rules.StirrupDiameter, wanted.Diameter, wanted.Count);
            for (int i = 0; i < ys.Count; i++)
            {
                double y = ys[i];
                double towardFace = face * (first.Diameter - wanted.Diameter) / 2.0;
                var points = first.Polyline.Points.Select(p => new Point3(p.X, y, p.Z + towardFace)).ToList();
                result.Add(first with
                {
                    BarId = i < group.Count ? group[i].BarId : barId++,
                    Diameter = wanted.Diameter,
                    Polyline = new Polyline3(points),
                    TransverseY = y,
                    DimR = first.DimR > 0.0 ? 2.0 * wanted.Diameter : 0.0
                });
            }
        }

        return result;
    }

    /// <summary>The bars of one piece share their path along the beam; only their offset across differs.</summary>
    private static string PieceKey(KataRebarCurve bar) =>
        string.Join(";", bar.Polyline.Points.Select(p => $"{Math.Round(p.X)},{Math.Round(p.Z)}"));
}
