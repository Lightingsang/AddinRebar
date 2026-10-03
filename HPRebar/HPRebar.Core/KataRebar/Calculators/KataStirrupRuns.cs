using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>One stirrup zone as Kata draws, dimensions and labels it.</summary>
/// <param name="First">Station of its first and last stirrup.</param>
public readonly record struct KataStirrupRun(int Span, int Number, double Spacing, double First, double Last);

/// <summary>
/// The stirrup zones as Kata draws them: neighbouring zones of one span with the same hoop and spacing are one
/// (T2-DY14's 250 mm span: two one-stirrup zones, one tag, one run, one pair of stirrup lines).
/// </summary>
public static class KataStirrupRuns
{
    public static IReadOnlyList<KataStirrupRun> Of(KataRebarLayoutResult layout)
    {
        if (layout is null) throw new ArgumentNullException(nameof(layout));
        var runs = new List<KataStirrupRun>();
        foreach (var zone in layout.StirrupZones.Where(z => z.Count > 0 && z.Stations.Count > 0).OrderBy(z => z.Stations[0]))
        {
            double first = zone.Stations[0], last = zone.Stations[zone.Stations.Count - 1];
            if (runs.Count > 0 && runs[runs.Count - 1] is var prev && prev.Span == zone.SpanIndex
                && prev.Number == zone.BarNumber && Math.Abs(prev.Spacing - zone.Spacing) < 0.5)
                runs[runs.Count - 1] = prev with { Last = last };
            else
                runs.Add(new KataStirrupRun(zone.SpanIndex, zone.BarNumber, zone.Spacing, first, last));
        }

        return runs;
    }
}
