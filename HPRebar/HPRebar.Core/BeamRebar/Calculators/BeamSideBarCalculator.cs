using System;
using System.Collections.Generic;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.Shared;

namespace HPRebar.Core.BeamRebar.Calculators;

/// <summary>
/// Calculates longitudinal skin / side reinforcement and transverse cross-ties for deep concrete beams.
/// Enforces TCVN 5574:2018 §10.3.2 and ACI 318 §9.7.2.3 (h >= 700 mm).
/// </summary>
public static class BeamSideBarCalculator
{
    public const double HeightThresholdMm = 700.0;
    public const double MaxVerticalSpacingMm = 300.0;
    public const double DefaultTieSpacingMm = 400.0;

    /// <summary>Gap between each end of the clear span and the nearest cross-tie.</summary>
    private const double TieEndOffsetMm = 50.0;

    /// <summary>
    /// Checks if a beam cross-section requires side reinforcement (h &gt;= 700 mm).
    /// </summary>
    public static bool RequiresSideBars(double heightMm) => heightMm >= HeightThresholdMm;

    /// <summary>
    /// Computes the number of side bar pairs (rows) based on clear vertical depth between main bars.
    /// Enforces TCVN 5574:2018 §10.3.2 and ACI 318 §9.7.2.3 vertical spacing limit (<= 300 mm).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The spacing needs more rows than Revit's bar limit.</exception>
    public static int ComputeRowCount(
        double heightMm,
        double coverMm = 25.0,
        double stirrupDiameterMm = 8.0,
        double mainDiameterMm = 20.0,
        double maxVerticalSpacingMm = MaxVerticalSpacingMm)
    {
        if (heightMm < HeightThresholdMm)
        {
            return 0;
        }

        double rows = CountRows(heightMm, coverMm, stirrupDiameterMm, mainDiameterMm, maxVerticalSpacingMm);
        if (rows > RevitRebarLimits.MaxBarPositions)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxVerticalSpacingMm),
                $"Side bar row count {rows:0} exceeds maximum {RevitRebarLimits.MaxBarPositions}.");
        }

        return (int)rows;
    }

    /// <summary>
    /// Side-bar rows on each face, before any limit: the clear depth between the main bars split into spaces no
    /// larger than the spacing, at least one row. A spacing that is not a positive finite number means 300 mm.
    /// Kept in double so a tiny spacing cannot overflow an int.
    /// </summary>
    public static double CountRows(
        double heightMm, double coverMm, double stirrupDiameterMm, double mainDiameterMm, double maxVerticalSpacingMm)
    {
        double spacing = FiniteNumber.IsPositive(maxVerticalSpacingMm) ? maxVerticalSpacingMm : MaxVerticalSpacingMm;
        double zOffset = coverMm + stirrupDiameterMm + (mainDiameterMm / 2.0);
        double clearVerticalSpanMm = heightMm - (2.0 * zOffset);
        if (clearVerticalSpanMm <= 0.0)
        {
            return 1;
        }

        return Math.Max(1.0, Math.Ceiling(clearVerticalSpanMm / spacing) - 1);
    }

    /// <summary>
    /// Cross-ties along one row of a span: one every spacing over the clear span less 50 mm at each end, at least
    /// one. A spacing that is not a positive finite number means 400 mm. Kept in double so a tiny spacing cannot
    /// overflow an int.
    /// </summary>
    public static double CrossTiesPerRow(double clearLengthMm, double crossTieSpacingMm)
    {
        double length = clearLengthMm - (2.0 * TieEndOffsetMm);
        return length > 0.0 ? Math.Floor(length / TieSpacing(crossTieSpacingMm)) + 1 : 1;
    }

    /// <summary>
    /// Computes longitudinal side (skin) reinforcing bars for deep spans in the continuous beam stack.
    /// </summary>
    public static IReadOnlyList<BarPolyline> ComputeLongitudinalSideBars(
        BeamContinuousStack stack,
        BeamSideBarSpec spec,
        double stirrupDiameterMm,
        double mainBarDiameterMm)
    {
        var result = new List<BarPolyline>();
        int barId = 0;

        for (int s = 0; s < stack.Spans.Count; s++)
        {
            var span = stack.Spans[s];
            if (!spec.AutoSkinBars || span.Height < spec.DepthThreshold)
            {
                continue;
            }

            int nRows = ComputeRowCount(span.Height, span.Cover, stirrupDiameterMm, mainBarDiameterMm, spec.MaxVerticalSpacing);
            if (nRows == 0)
            {
                continue;
            }

            double zBotMain = span.BottomElevation + span.Cover + stirrupDiameterMm + (mainBarDiameterMm / 2.0);
            double zTopMain = span.TopElevation - span.Cover - stirrupDiameterMm - (mainBarDiameterMm / 2.0);
            double deltaZ = (zTopMain - zBotMain) / (nRows + 1);

            double yLeft = -(span.Width / 2.0) + spec.Cover + stirrupDiameterMm + (spec.Diameter / 2.0);
            double yRight = +(span.Width / 2.0) - spec.Cover - stirrupDiameterMm - (spec.Diameter / 2.0);

            double xStart = span.StartX;
            double xEnd = span.EndX;

            for (int r = 1; r <= nRows; r++)
            {
                double z = zBotMain + (r * deltaZ);

                // Left lateral face bar
                var ptsLeft = new List<Point3>
                {
                    new(xStart, yLeft, z),
                    new(xEnd, yLeft, z)
                };
                result.Add(new BarPolyline
                {
                    BarIndex = barId++,
                    Type = BarType.SideSkin,
                    Diameter = spec.Diameter,
                    Layer = r,
                    HostSpanIndex = s,
                    Polyline = new Polyline3(ptsLeft),
                    TransverseY = yLeft,
                    BarTypeName = spec.SideBarTypeName
                });

                // Right lateral face bar
                var ptsRight = new List<Point3>
                {
                    new(xStart, yRight, z),
                    new(xEnd, yRight, z)
                };
                result.Add(new BarPolyline
                {
                    BarIndex = barId++,
                    Type = BarType.SideSkin,
                    Diameter = spec.Diameter,
                    Layer = r,
                    HostSpanIndex = s,
                    Polyline = new Polyline3(ptsRight),
                    TransverseY = yRight,
                    BarTypeName = spec.SideBarTypeName
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Computes transverse anti-buckling cross-ties connecting opposite side bars across the beam web.
    /// </summary>
    public static IReadOnlyList<BarPolyline> ComputeCrossTies(
        BeamContinuousStack stack,
        BeamSideBarSpec spec,
        double stirrupDiameterMm,
        double mainBarDiameterMm)
    {
        if (!spec.IncludeCrossTies)
        {
            return Array.Empty<BarPolyline>();
        }

        var result = new List<BarPolyline>();
        int tieId = 0;

        for (int s = 0; s < stack.Spans.Count; s++)
        {
            var span = stack.Spans[s];
            if (!spec.AutoSkinBars || span.Height < spec.DepthThreshold)
            {
                continue;
            }

            int nRows = ComputeRowCount(span.Height, span.Cover, stirrupDiameterMm, mainBarDiameterMm, spec.MaxVerticalSpacing);
            if (nRows == 0)
            {
                continue;
            }

            double zBotMain = span.BottomElevation + span.Cover + stirrupDiameterMm + (mainBarDiameterMm / 2.0);
            double zTopMain = span.TopElevation - span.Cover - stirrupDiameterMm - (mainBarDiameterMm / 2.0);
            double deltaZ = (zTopMain - zBotMain) / (nRows + 1);

            double yLeft = -(span.Width / 2.0) + spec.Cover + stirrupDiameterMm + (spec.Diameter / 2.0);
            double yRight = +(span.Width / 2.0) - spec.Cover - stirrupDiameterMm - (spec.Diameter / 2.0);

            double tieSpacing = TieSpacing(spec.CrossTieSpacing);
            double lDist = span.LengthClear - (2.0 * TieEndOffsetMm);
            double ties = CrossTiesPerRow(span.LengthClear, spec.CrossTieSpacing);
            if (ties > RevitRebarLimits.MaxBarPositions)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(spec),
                    $"Cross-tie count {ties:0} per row exceeds maximum {RevitRebarLimits.MaxBarPositions}.");
            }

            int tieCount = (int)ties;
            double slack = lDist > 0.0 ? (lDist - ((tieCount - 1) * tieSpacing)) / 2.0 : 0.0;
            double startX = span.StartX + TieEndOffsetMm + slack;

            for (int r = 1; r <= nRows; r++)
            {
                double z = zBotMain + (r * deltaZ);

                for (int m = 0; m < tieCount; m++)
                {
                    double x = startX + (m * tieSpacing);
                    var pts = new List<Point3>
                    {
                        new(x, yLeft, z),
                        new(x, yRight, z)
                    };

                    bool flip = (m % 2 == 1);
                    var startHook = flip ? HookAngle.Hook90 : HookAngle.Hook135;
                    var endHook = flip ? HookAngle.Hook135 : HookAngle.Hook90;

                    result.Add(new BarPolyline
                    {
                        BarIndex = tieId++,
                        Type = BarType.CrossTie,
                        Diameter = spec.CrossTieDiameter,
                        Layer = r,
                        HostSpanIndex = s,
                        Polyline = new Polyline3(pts),
                        StartHookAngle = startHook,
                        EndHookAngle = endHook,
                        StartHookLength = 6.0 * spec.CrossTieDiameter,
                        EndHookLength = 6.0 * spec.CrossTieDiameter,
                        BarTypeName = spec.CrossTieBarTypeName
                    });
                }
            }
        }

        return result;
    }

    private static double TieSpacing(double crossTieSpacingMm) =>
        FiniteNumber.IsPositive(crossTieSpacingMm) ? crossTieSpacingMm : DefaultTieSpacingMm;
}
