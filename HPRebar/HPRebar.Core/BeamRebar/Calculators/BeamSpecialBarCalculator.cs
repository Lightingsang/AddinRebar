using System;
using System.Collections.Generic;
using HPRebar.Core.BeamRebar.Models;

namespace HPRebar.Core.BeamRebar.Calculators;

/// <summary>
/// Calculates special reinforcement at secondary framing intersections:
/// Concentrated hanging stirrups flanking the joint and 45° diagonal bent ties.
/// </summary>
public static class BeamSpecialBarCalculator
{
    public const double DefaultHangingSpacingMm = 50.0;
    public const double DefaultHangingOffsetMm = 50.0;

    /// <summary>
    /// Computes longitudinal stations X for hanging stirrups flanking a secondary beam joint.
    /// Optionally filters stations to remain strictly within [minXMm, maxXMm].
    /// </summary>
    public static IReadOnlyList<double> ComputeHangingStirrupStations(
        double secondaryCenterXMm,
        double secondaryWidthMm,
        int countPerSide,
        double spacingMm = DefaultHangingSpacingMm,
        double minXMm = double.NegativeInfinity,
        double maxXMm = double.PositiveInfinity)
    {
        if (countPerSide <= 0)
            return Array.Empty<double>();

        double xSecL = secondaryCenterXMm - (secondaryWidthMm / 2.0);
        double xSecR = secondaryCenterXMm + (secondaryWidthMm / 2.0);

        var stations = new List<double>(countPerSide * 2);

        // Left flanking stations (ordered from left to right towards joint or vice-versa)
        for (int k = countPerSide; k >= 1; k--)
        {
            double x = xSecL - (k * spacingMm);
            if (x >= minXMm && x <= maxXMm)
            {
                stations.Add(x);
            }
        }

        // Right flanking stations
        for (int k = 1; k <= countPerSide; k++)
        {
            double x = xSecR + (k * spacingMm);
            if (x >= minXMm && x <= maxXMm)
            {
                stations.Add(x);
            }
        }

        return stations;
    }

    /// <summary>
    /// Merges closely spaced or overlapping hanging stirrup stations from multiple secondary beams.
    /// </summary>
    public static IReadOnlyList<double> MergeHangingStations(
        IReadOnlyList<double> stations,
        double minimumClearMm = 20.0)
    {
        if (stations.Count < 2)
            return stations;

        var sorted = new List<double>(stations);
        sorted.Sort();

        var merged = new List<double>(sorted.Count) { sorted[0] };
        for (int i = 1; i < sorted.Count; i++)
        {
            if (sorted[i] - merged[merged.Count - 1] >= minimumClearMm)
            {
                merged.Add(sorted[i]);
            }
        }

        return merged;
    }

    /// <summary>
    /// Computes the 3D polyline defining a 45° diagonal bent tie ("thép vai bò") under the secondary beam soffit,
    /// clamping anchor tips strictly within [minXMm, maxXMm].
    /// Returns empty collection if 45° bend cannot develop within the specified bounds.
    /// </summary>
    public static IReadOnlyList<Point3> ComputeDiagonalTiePolyline(
        double secondaryCenterXMm,
        double secondaryWidthMm,
        double primaryZTopMm,
        double primaryZBotMm,
        double coverMm,
        double barDiameterMm,
        double minXMm = double.NegativeInfinity,
        double maxXMm = double.PositiveInfinity)
    {
        double xSecL = secondaryCenterXMm - (secondaryWidthMm / 2.0);
        double xSecR = secondaryCenterXMm + (secondaryWidthMm / 2.0);

        double zTopBar = primaryZTopMm - coverMm - (barDiameterMm / 2.0);
        double zBotBar = primaryZBotMm + coverMm + (barDiameterMm / 2.0);

        double deltaZ = zTopBar - zBotBar;
        if (deltaZ <= 0.0)
            return Array.Empty<Point3>();

        // For 45 degrees, tan(45°) = 1.0 => deltaX = deltaZ
        double deltaX = deltaZ;
        double anchorLength = 30.0 * barDiameterMm;

        // Check if secondary beam soffit or 45° incline points violate bounds
        double xBendL = xSecL - deltaX;
        double xBendR = xSecR + deltaX;

        if (xSecL < minXMm || xSecR > maxXMm || xBendL < minXMm || xBendR > maxXMm)
        {
            // Joint too close to support to develop 45° inclined bar within span bounds
            return Array.Empty<Point3>();
        }

        // Clamp horizontal anchor legs to span bounds
        double x0 = Math.Max(minXMm, xBendL - anchorLength);
        double x5 = Math.Min(maxXMm, xBendR + anchorLength);

        var rawPts = new List<Point3>
        {
            new(x0, 0.0, zTopBar),
            new(xBendL, 0.0, zTopBar),
            new(xSecL, 0.0, zBotBar),
            new(xSecR, 0.0, zBotBar),
            new(xBendR, 0.0, zTopBar),
            new(x5, 0.0, zTopBar)
        };

        return BeamMainBarCalculator.SimplifyPolyline(rawPts);
    }

    /// <summary>
    /// Computes full hanging stirrups across all secondary intersections on the continuous beam stack.
    /// Throws ArgumentException if an intersection is located outside clear spans.
    /// </summary>
    public static IReadOnlyList<BarPolyline> ComputeHangingStirrups(
        BeamContinuousStack stack,
        BeamSpecialBarSpec spec)
    {
        if (!spec.EnableHangingStirrups || stack.SecondaryIntersections.Count == 0)
            return Array.Empty<BarPolyline>();

        var result = new List<BarPolyline>();
        int barId = 0;

        foreach (var sec in stack.SecondaryIntersections)
        {
            // Validate that secondary beam center is inside primary beam span
            var hostSpan = stack.FindSpanAt(sec.CenterX);
            if (hostSpan == null)
                continue; // Safely skip secondary beams framed into support/joint zones

            double minX = hostSpan.StartX + hostSpan.Cover;
            double maxX = hostSpan.EndX - hostSpan.Cover;

            var stations = ComputeHangingStirrupStations(
                sec.CenterX, sec.Width, spec.HangingStirrupsPerSide, spec.HangingStirrupSpacing, minX, maxX);

            double wStirrup = hostSpan.Width - (2.0 * hostSpan.Cover);
            double hStirrup = hostSpan.Height - (2.0 * hostSpan.Cover);
            double zTopStirrup = hostSpan.TopElevation - hostSpan.Cover;
            double zBotStirrup = hostSpan.BottomElevation + hostSpan.Cover;
            double yLeft = -wStirrup / 2.0;
            double yRight = +wStirrup / 2.0;

            for (int i = 0; i < stations.Count; i++)
            {
                double x = stations[i];
                var pts = new List<Point3>
                {
                    new(x, yLeft, zTopStirrup),
                    new(x, yRight, zTopStirrup),
                    new(x, yRight, zBotStirrup),
                    new(x, yLeft, zBotStirrup),
                    new(x, yLeft, zTopStirrup)
                };

                result.Add(new BarPolyline
                {
                    BarIndex = barId++,
                    Type = BarType.HangingStirrup,
                    Diameter = spec.HangingStirrupDiameter,
                    HostSpanIndex = hostSpan.Index,
                    Polyline = new Polyline3(pts, isClosed: true),
                    StartHookAngle = HookAngle.Hook135,
                    EndHookAngle = HookAngle.Hook135,
                    BarTypeName = spec.HangingStirrupTypeName
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Computes diagonal bent ties across all secondary intersections on the continuous beam stack.
    /// </summary>
    public static IReadOnlyList<BarPolyline> ComputeDiagonalTies(
        BeamContinuousStack stack,
        BeamSpecialBarSpec spec)
    {
        if (!spec.EnableDiagonalTies || stack.SecondaryIntersections.Count == 0)
            return Array.Empty<BarPolyline>();

        var result = new List<BarPolyline>();
        int barId = 0;

        foreach (var sec in stack.SecondaryIntersections)
        {
            var hostSpan = stack.FindSpanAt(sec.CenterX);
            if (hostSpan == null)
                continue;

            // Secondary beam depth below threshold check (e.g. at least 300 mm deep)
            if (sec.Height < 300.0)
                continue;

            double minX = hostSpan.StartX + hostSpan.Cover;
            double maxX = hostSpan.EndX - hostSpan.Cover;

            var rawPts = ComputeDiagonalTiePolyline(
                sec.CenterX, sec.Width, hostSpan.TopElevation, hostSpan.BottomElevation, hostSpan.Cover, spec.DiagonalTieDiameter, minX, maxX);

            if (rawPts.Count < 2)
                continue;

            var yPositions = BeamMainBarCalculator.ComputeTransverseYPositions(
                hostSpan.Width, hostSpan.Cover, 8.0, spec.DiagonalTieDiameter, spec.DiagonalTieCount);

            for (int i = 0; i < yPositions.Count; i++)
            {
                double y = yPositions[i];
                var pts = new List<Point3>(rawPts.Count);
                for (int p = 0; p < rawPts.Count; p++)
                {
                    pts.Add(new Point3(rawPts[p].X, y, rawPts[p].Z));
                }

                result.Add(new BarPolyline
                {
                    BarIndex = barId++,
                    Type = BarType.DiagonalTie,
                    Diameter = spec.DiagonalTieDiameter,
                    HostSpanIndex = hostSpan.Index,
                    Polyline = new Polyline3(pts),
                    TransverseY = y,
                    BarTypeName = spec.DiagonalTieTypeName
                });
            }
        }

        return result;
    }
}
