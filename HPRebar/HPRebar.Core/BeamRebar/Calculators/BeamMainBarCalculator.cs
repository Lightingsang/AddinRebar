using System;
using System.Collections.Generic;
using HPRebar.Core.BeamRebar.Models;

namespace HPRebar.Core.BeamRebar.Calculators;

/// <summary>
/// Calculates continuous 3D polyline geometry for longitudinal top and bottom reinforcement.
/// Handles transverse bar distribution, exterior 90° anchorage hooks, cantilevers,
/// depth transition steps, and 50% staggered lap splices for bars exceeding commercial stock length.
/// </summary>
public static class BeamMainBarCalculator
{
    public const double CommercialStockLengthMm = 11700.0;
    public const double DefaultLapMultiplier = 40.0;

    /// <summary>
    /// Computes transverse coordinates Y across the beam width for N bars.
    /// Centered at Y = 0.
    /// </summary>
    public static IReadOnlyList<double> ComputeTransverseYPositions(
        double widthMm,
        double coverMm,
        double stirrupDiameterMm,
        double barDiameterMm,
        int count)
    {
        if (count <= 1)
            return new[] { 0.0 };

        double y0 = -(widthMm / 2.0) + coverMm + stirrupDiameterMm + (barDiameterMm / 2.0);
        double yn = +(widthMm / 2.0) - coverMm - stirrupDiameterMm - (barDiameterMm / 2.0);

        if (count == 2)
            return new[] { y0, yn };

        double deltaY = (yn - y0) / (count - 1);
        var positions = new List<double>(count);
        for (int i = 0; i < count; i++)
        {
            positions.Add(y0 + (i * deltaY));
        }

        return positions;
    }

    /// <summary>
    /// Computes continuous top main longitudinal reinforcing bars across the beam stack.
    /// </summary>
    public static IReadOnlyList<BarPolyline> ComputeTopMainBars(
        BeamContinuousStack stack,
        BeamMainBarSpec spec,
        double stirrupDiameterMm)
    {
        if (stack.Spans.Count == 0)
            return Array.Empty<BarPolyline>();

        double width = stack.Spans[0].Width;
        var yPositions = ComputeTransverseYPositions(width, spec.TopCover, stirrupDiameterMm, spec.TopDiameter, spec.TopCount);

        // Determine X start and end boundaries
        bool isLeftCantilever = stack.Spans[0].IsCantilever || (stack.Supports.Count > 0 && stack.Supports[0].Type == SupportType.CantileverEnd);
        bool isRightCantilever = stack.Spans[stack.Spans.Count - 1].IsCantilever || (stack.Supports.Count > 0 && stack.Supports[stack.Supports.Count - 1].Type == SupportType.CantileverEnd);

        double xStart = isLeftCantilever
            ? stack.OverallStartX + spec.TopCover
            : (stack.Supports.Count > 0 ? stack.Supports[0].LeftFaceX + spec.TopCover : stack.OverallStartX + spec.TopCover);

        double xEnd = isRightCantilever
            ? stack.OverallEndX - spec.TopCover
            : (stack.Supports.Count > 0 ? stack.Supports[stack.Supports.Count - 1].RightFaceX - spec.TopCover : stack.OverallEndX - spec.TopCover);

        double zTop = stack.Spans[0].TopElevation;
        double zTopBar = zTop - spec.TopCover - stirrupDiameterMm - (spec.TopDiameter / 2.0);

        // Determine Hook Lengths
        double hStart = stack.Spans[0].Height;
        double hookStart = spec.TopStartHookLength > 0.0
            ? spec.TopStartHookLength
            : Math.Min(hStart - (2.0 * spec.TopCover) - (2.0 * stirrupDiameterMm), BeamHookLength.Default(spec.TopDiameter));

        double hEnd = stack.Spans[stack.Spans.Count - 1].Height;
        double hookEnd = spec.TopEndHookLength > 0.0
            ? spec.TopEndHookLength
            : Math.Min(hEnd - (2.0 * spec.TopCover) - (2.0 * stirrupDiameterMm), BeamHookLength.Default(spec.TopDiameter));

        double totalBarLength = (xEnd - xStart) + hookStart + hookEnd;
        double stockLimit = spec.MaxStockLength > 0.0 ? spec.MaxStockLength : CommercialStockLengthMm;

        var result = new List<BarPolyline>();

        // Case 1: Bar fits within single stock length without splicing
        if (totalBarLength <= stockLimit)
        {
            for (int i = 0; i < yPositions.Count; i++)
            {
                double y = yPositions[i];
                var rawPoints = new List<Point3>
                {
                    new(xStart, y, zTopBar - hookStart),
                    new(xStart, y, zTopBar),
                    new(xEnd, y, zTopBar),
                    new(xEnd, y, zTopBar - hookEnd)
                };

                var simplified = SimplifyPolyline(rawPoints);
                result.Add(new BarPolyline
                {
                    BarIndex = i,
                    Type = BarType.MainTop,
                    Diameter = spec.TopDiameter,
                    Layer = 1,
                    Polyline = new Polyline3(simplified),
                    StartHookAngle = HookAngle.Hook90,
                    EndHookAngle = HookAngle.Hook90,
                    TransverseY = y,
                    BarTypeName = spec.TopBarTypeName
                });
            }
            return result;
        }

        // Case 2: Bar exceeds stock length -> 50% staggered midspan lap splices
        double lapLength = spec.LapFactor * spec.TopDiameter;
        double staggerOffset = spec.EnableStagger ? (spec.StaggerOffsetRatio * lapLength) : 0.0;

        // Choose splice zone in span 0 or intermediate span
        int targetSpanIndex = stack.Spans.Count / 2;
        var targetSpan = stack.Spans[targetSpanIndex];
        double midspanCenter = targetSpan.StartX + (targetSpan.LengthClear / 2.0);

        for (int i = 0; i < yPositions.Count; i++)
        {
            double y = yPositions[i];
            bool isGroupA = (i % 2 == 0);
            double spliceCenter = isGroupA ? (midspanCenter - (staggerOffset / 2.0)) : (midspanCenter + (staggerOffset / 2.0));

            // Segment 1 (Left to Splice)
            double seg1EndX = spliceCenter + (lapLength / 2.0);
            var pts1 = new List<Point3>
            {
                new(xStart, y, zTopBar - hookStart),
                new(xStart, y, zTopBar),
                new(seg1EndX, y, zTopBar)
            };

            // Segment 2 (Splice to Right)
            double seg2StartX = spliceCenter - (lapLength / 2.0);
            var pts2 = new List<Point3>
            {
                new(seg2StartX, y, zTopBar),
                new(xEnd, y, zTopBar),
                new(xEnd, y, zTopBar - hookEnd)
            };

            result.Add(new BarPolyline
            {
                BarIndex = (i * 2),
                Type = BarType.MainTop,
                Diameter = spec.TopDiameter,
                Layer = 1,
                Polyline = new Polyline3(SimplifyPolyline(pts1)),
                StartHookAngle = HookAngle.Hook90,
                EndHookAngle = HookAngle.None,
                TransverseY = y,
                BarTypeName = spec.TopBarTypeName
            });

            result.Add(new BarPolyline
            {
                BarIndex = (i * 2) + 1,
                Type = BarType.MainTop,
                Diameter = spec.TopDiameter,
                Layer = 1,
                Polyline = new Polyline3(SimplifyPolyline(pts2)),
                StartHookAngle = HookAngle.None,
                EndHookAngle = HookAngle.Hook90,
                TransverseY = y,
                BarTypeName = spec.TopBarTypeName
            });
        }

        return result;
    }

    /// <summary>
    /// Computes continuous bottom main longitudinal reinforcing bars across the beam stack.
    /// </summary>
    public static IReadOnlyList<BarPolyline> ComputeBottomMainBars(
        BeamContinuousStack stack,
        BeamMainBarSpec spec,
        double stirrupDiameterMm)
    {
        if (stack.Spans.Count == 0)
            return Array.Empty<BarPolyline>();

        double width = stack.Spans[0].Width;
        var yPositions = ComputeTransverseYPositions(width, spec.BottomCover, stirrupDiameterMm, spec.BottomDiameter, spec.BottomCount);

        bool isLeftCantilever = stack.Spans[0].IsCantilever || (stack.Supports.Count > 0 && stack.Supports[0].Type == SupportType.CantileverEnd);
        bool isRightCantilever = stack.Spans[stack.Spans.Count - 1].IsCantilever || (stack.Supports.Count > 0 && stack.Supports[stack.Supports.Count - 1].Type == SupportType.CantileverEnd);

        // Check if there is a depth step change between spans
        bool hasDepthStep = false;
        for (int i = 0; i < stack.Spans.Count - 1; i++)
        {
            if (Math.Abs(stack.Spans[i].Height - stack.Spans[i + 1].Height) > 1.0)
            {
                hasDepthStep = true;
                break;
            }
        }

        var result = new List<BarPolyline>();

        // If there's a depth step, generate separate bottom bars per span with upward hooks at intermediate support
        if (hasDepthStep)
        {
            int barId = 0;
            for (int s = 0; s < stack.Spans.Count; s++)
            {
                var span = stack.Spans[s];
                double spanWidth = span.Width;
                var spanYPositions = ComputeTransverseYPositions(spanWidth, spec.BottomCover, stirrupDiameterMm, spec.BottomDiameter, spec.BottomCount);
                double zBot = span.BottomElevation;
                double zBotBar = zBot + spec.BottomCover + stirrupDiameterMm + (spec.BottomDiameter / 2.0);

                double hookLen = Math.Min(span.Height - (2.0 * spec.BottomCover), BeamHookLength.Default(spec.BottomDiameter));

                double xStart = (s == 0 && !isLeftCantilever && stack.Supports.Count > 0)
                    ? stack.Supports[0].LeftFaceX + spec.BottomCover
                    : span.StartX;

                double xEnd = (s == stack.Spans.Count - 1 && !isRightCantilever && stack.Supports.Count > s + 1)
                    ? stack.Supports[s + 1].RightFaceX - spec.BottomCover
                    : span.EndX;

                for (int i = 0; i < spanYPositions.Count; i++)
                {
                    double y = spanYPositions[i];
                    var pts = new List<Point3>
                    {
                        new(xStart, y, zBotBar + hookLen),
                        new(xStart, y, zBotBar),
                        new(xEnd, y, zBotBar),
                        new(xEnd, y, zBotBar + hookLen)
                    };

                    result.Add(new BarPolyline
                    {
                        BarIndex = barId++,
                        Type = BarType.MainBottom,
                        Diameter = spec.BottomDiameter,
                        Layer = 1,
                        HostSpanIndex = s,
                        Polyline = new Polyline3(SimplifyPolyline(pts)),
                        StartHookAngle = HookAngle.Hook90,
                        EndHookAngle = HookAngle.Hook90,
                        TransverseY = y,
                        BarTypeName = spec.BottomBarTypeName
                    });
                }
            }
            return result;
        }

        // Determine global start and end X
        // Cantilevers stop bottom bars at interior column face
        double xStartGlobal;
        double hookStart;
        if (isLeftCantilever && stack.Supports.Count > 1)
        {
            xStartGlobal = stack.Supports[1].LeftFaceX;
            hookStart = 0.0;
        }
        else
        {
            xStartGlobal = stack.Supports.Count > 0 ? stack.Supports[0].LeftFaceX + spec.BottomCover : stack.OverallStartX + spec.BottomCover;
            double hStart = stack.Spans[0].Height;
            hookStart = spec.BottomStartHookLength > 0.0
                ? spec.BottomStartHookLength
                : Math.Min(hStart - (2.0 * spec.BottomCover) - (2.0 * stirrupDiameterMm), BeamHookLength.Default(spec.BottomDiameter));
        }

        double xEndGlobal;
        double hookEnd;
        if (isRightCantilever && stack.Supports.Count > 1)
        {
            xEndGlobal = stack.Supports[stack.Supports.Count - 2].RightFaceX;
            hookEnd = 0.0;
        }
        else
        {
            xEndGlobal = stack.Supports.Count > 0 ? stack.Supports[stack.Supports.Count - 1].RightFaceX - spec.BottomCover : stack.OverallEndX - spec.BottomCover;
            double hEnd = stack.Spans[stack.Spans.Count - 1].Height;
            hookEnd = spec.BottomEndHookLength > 0.0
                ? spec.BottomEndHookLength
                : Math.Min(hEnd - (2.0 * spec.BottomCover) - (2.0 * stirrupDiameterMm), BeamHookLength.Default(spec.BottomDiameter));
        }

        double zBotGlobal = stack.Spans[0].BottomElevation;
        double zBotBarGlobal = zBotGlobal + spec.BottomCover + stirrupDiameterMm + (spec.BottomDiameter / 2.0);

        double totalBarLength = (xEndGlobal - xStartGlobal) + hookStart + hookEnd;
        double stockLimit = spec.MaxStockLength > 0.0 ? spec.MaxStockLength : CommercialStockLengthMm;

        // Fits in stock length
        if (totalBarLength <= stockLimit)
        {
            for (int i = 0; i < yPositions.Count; i++)
            {
                double y = yPositions[i];
                var pts = new List<Point3>();
                if (hookStart > 0.0)
                    pts.Add(new(xStartGlobal, y, zBotBarGlobal + hookStart));
                pts.Add(new(xStartGlobal, y, zBotBarGlobal));
                pts.Add(new(xEndGlobal, y, zBotBarGlobal));
                if (hookEnd > 0.0)
                    pts.Add(new(xEndGlobal, y, zBotBarGlobal + hookEnd));

                result.Add(new BarPolyline
                {
                    BarIndex = i,
                    Type = BarType.MainBottom,
                    Diameter = spec.BottomDiameter,
                    Layer = 1,
                    Polyline = new Polyline3(SimplifyPolyline(pts)),
                    StartHookAngle = hookStart > 0.0 ? HookAngle.Hook90 : HookAngle.None,
                    EndHookAngle = hookEnd > 0.0 ? HookAngle.Hook90 : HookAngle.None,
                    TransverseY = y,
                    BarTypeName = spec.BottomBarTypeName
                });
            }
            return result;
        }

        // Exceeds stock length: splice bottom bars at intermediate supports
        double lapLengthBot = spec.LapFactor * spec.BottomDiameter;
        double staggerOffsetBot = spec.EnableStagger ? (spec.StaggerOffsetRatio * lapLengthBot) : 0.0;
        int targetSupportIndex = stack.Supports.Count / 2;
        if (targetSupportIndex == 0) targetSupportIndex = 1;
        double supportCenter = stack.Supports[targetSupportIndex].CenterX;

        for (int i = 0; i < yPositions.Count; i++)
        {
            double y = yPositions[i];
            bool isGroupA = (i % 2 == 0);
            double spliceCenter = isGroupA ? (supportCenter - (staggerOffsetBot / 2.0)) : (supportCenter + (staggerOffsetBot / 2.0));

            double seg1EndX = spliceCenter + (lapLengthBot / 2.0);
            var pts1 = new List<Point3>();
            if (hookStart > 0.0)
                pts1.Add(new(xStartGlobal, y, zBotBarGlobal + hookStart));
            pts1.Add(new(xStartGlobal, y, zBotBarGlobal));
            pts1.Add(new(seg1EndX, y, zBotBarGlobal));

            double seg2StartX = spliceCenter - (lapLengthBot / 2.0);
            var pts2 = new List<Point3>
            {
                new(seg2StartX, y, zBotBarGlobal),
                new(xEndGlobal, y, zBotBarGlobal)
            };
            if (hookEnd > 0.0)
                pts2.Add(new(xEndGlobal, y, zBotBarGlobal + hookEnd));

            result.Add(new BarPolyline
            {
                BarIndex = (i * 2),
                Type = BarType.MainBottom,
                Diameter = spec.BottomDiameter,
                Layer = 1,
                Polyline = new Polyline3(SimplifyPolyline(pts1)),
                StartHookAngle = hookStart > 0.0 ? HookAngle.Hook90 : HookAngle.None,
                EndHookAngle = HookAngle.None,
                TransverseY = y,
                BarTypeName = spec.BottomBarTypeName
            });

            result.Add(new BarPolyline
            {
                BarIndex = (i * 2) + 1,
                Type = BarType.MainBottom,
                Diameter = spec.BottomDiameter,
                Layer = 1,
                Polyline = new Polyline3(SimplifyPolyline(pts2)),
                StartHookAngle = HookAngle.None,
                EndHookAngle = hookEnd > 0.0 ? HookAngle.Hook90 : HookAngle.None,
                TransverseY = y,
                BarTypeName = spec.BottomBarTypeName
            });
        }

        return result;
    }

    /// <summary>
    /// Simplifies polyline vertices: culls segments &lt; 1.0 mm and removes collinear intermediate vertices.
    /// </summary>
    public static IReadOnlyList<Point3> SimplifyPolyline(IReadOnlyList<Point3> vertices)
    {
        if (vertices.Count < 2)
            return vertices;

        var culled = new List<Point3>(vertices.Count) { vertices[0] };
        for (int i = 1; i < vertices.Count; i++)
        {
            if (vertices[i].DistanceTo(culled[culled.Count - 1]) >= Tolerance.MinimumSegmentMm)
            {
                culled.Add(vertices[i]);
            }
        }

        if (culled.Count < 3)
            return culled;

        // Remove intermediate collinear vertices
        var simplified = new List<Point3>(culled.Count) { culled[0] };
        for (int i = 1; i < culled.Count - 1; i++)
        {
            var pPrev = simplified[simplified.Count - 1];
            var pCurr = culled[i];
            var pNext = culled[i + 1];

            var v1 = (pCurr - pPrev).Normalize();
            var v2 = (pNext - pCurr).Normalize();

            var cross = v1.Cross(v2);
            double dot = v1.Dot(v2);

            // An intermediate vertex is redundant only if vectors are collinear AND point in the same direction.
            // Hairpin turnaround vertices (dot <= 0.0) must be preserved.
            bool isCodirectionalCollinear = cross.Length <= Tolerance.CollinearToleranceMm && dot > 0.0;
            if (!isCodirectionalCollinear)
            {
                simplified.Add(pCurr);
            }
        }
        simplified.Add(culled[culled.Count - 1]);

        return simplified;
    }
}
