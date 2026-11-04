using System;
using System.Collections.Generic;

namespace HPRebar.Core.KataRebar.ColumnStack;

/// <summary>Hoops of one zone, from the first to the last hoop, in mm above the storey base.</summary>
public sealed record KataColumnHoopZone(double FromMm, double ToMm, double SpacingMm);

/// <summary>
/// Hoop zones along a storey as Kata's VC command draws them (drawings of 2026-11-03): first and last hoop 50 mm from
/// the storey ends; a dense zone of length Ld at each end of the clear height, Ld = max(longer side, clear / 6, 450)
/// rounded to the nearest 50 (TCVN 9386 critical length); the middle zone one middle spacing inside both dense zones,
/// dropped when it has no room; with "đai xuyên dầm" the top zone goes on through the beam at the beam spacing.
/// </summary>
public static class KataColumnHoopPlanner
{
    private const double EndGapMm = 50;
    private const double MinimumCriticalMm = 450;
    private const double RoundingMm = 50;

    public static IReadOnlyList<KataColumnHoopZone> Zones(KataColumnStoreyRow row, KataColumnSpec spec)
    {
        var hoops = spec.Hoops;
        double clear = row.HeightMm - row.BeamDepthMm;
        double critical = CriticalLength(Math.Max(row.WidthMm, row.DepthMm), clear);
        double clearTop = clear - EndGapMm;
        var zones = new List<KataColumnHoopZone>();
        if (clearTop <= EndGapMm)
        {
            // A beam as deep as the storey (or deeper): no clear height to hoop, only the joint.
            if (spec.HoopsThroughBeam && row.HeightMm - EndGapMm > Math.Max(clear, 0) + EndGapMm)
            {
                zones.Add(new KataColumnHoopZone(Math.Max(clear, 0) + EndGapMm, row.HeightMm - EndGapMm, hoops.BeamSpacingMm));
            }

            return zones;
        }

        double middleFrom = critical + hoops.MiddleSpacingMm;
        double middleTo = clear - critical - hoops.MiddleSpacingMm;
        if (middleTo - middleFrom > 0)
        {
            zones.Add(new KataColumnHoopZone(EndGapMm, critical, hoops.DenseSpacingMm));
            zones.Add(new KataColumnHoopZone(middleFrom, middleTo, hoops.MiddleSpacingMm));
            zones.Add(new KataColumnHoopZone(clear - critical, clearTop, hoops.DenseSpacingMm));
        }
        else
        {
            zones.Add(new KataColumnHoopZone(EndGapMm, clearTop, hoops.DenseSpacingMm));
        }

        if (spec.HoopsThroughBeam && row.BeamDepthMm > 0)
        {
            zones.Add(new KataColumnHoopZone(clear + EndGapMm, row.HeightMm - EndGapMm, hoops.BeamSpacingMm));
        }

        return zones;
    }

    public static double CriticalLength(double longerSideMm, double clearHeightMm)
    {
        double length = Math.Max(Math.Max(longerSideMm, clearHeightMm / 6), MinimumCriticalMm);
        return Math.Round(length / RoundingMm, MidpointRounding.AwayFromZero) * RoundingMm;
    }
}
