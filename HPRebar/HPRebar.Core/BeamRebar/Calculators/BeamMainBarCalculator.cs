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

        var yPositions = ComputeTransverseYPositions(
            stack.Spans[0].Width, spec.TopCover, stirrupDiameterMm, spec.TopDiameter, spec.TopCount);
        var run = TopRun(stack, spec, stirrupDiameterMm);

        return run.Length <= StockLimit(spec)
            ? UnsplicedTopBars(run, yPositions, spec)
            : SplicedTopBars(stack, run, yPositions, spec);
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

        var cantilevers = CantileverEnds(stack);
        if (HasDepthStep(stack))
        {
            return SteppedBottomBars(stack, spec, stirrupDiameterMm, cantilevers);
        }

        var yPositions = ComputeTransverseYPositions(
            stack.Spans[0].Width, spec.BottomCover, stirrupDiameterMm, spec.BottomDiameter, spec.BottomCount);
        var run = BottomRun(stack, spec, stirrupDiameterMm, cantilevers);

        return run.Length <= StockLimit(spec)
            ? UnsplicedBottomBars(run, yPositions, spec)
            : SplicedBottomBars(stack, run, yPositions, spec);
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

    /// <summary>
    /// The straight part of a main bar from <see cref="XStart"/> to <see cref="XEnd"/> at height <see cref="Z"/>,
    /// with the hook legs at each end (0 = no hook).
    /// </summary>
    private readonly record struct BarRun(double XStart, double XEnd, double Z, double HookStart, double HookEnd)
    {
        /// <summary>Straight length plus both hook legs — compared with the stock length to decide on a splice.</summary>
        public double Length => (XEnd - XStart) + HookStart + HookEnd;
    }

    /// <summary>Whether the first and the last span are cantilevers (by span flag or by a cantilever-end support).</summary>
    private static (bool Left, bool Right) CantileverEnds(BeamContinuousStack stack)
    {
        bool left = stack.Spans[0].IsCantilever
                    || (stack.Supports.Count > 0 && stack.Supports[0].Type == SupportType.CantileverEnd);
        bool right = stack.Spans[stack.Spans.Count - 1].IsCantilever
                     || (stack.Supports.Count > 0 && stack.Supports[stack.Supports.Count - 1].Type == SupportType.CantileverEnd);
        return (left, right);
    }

    private static double StockLimit(BeamMainBarSpec spec) =>
        spec.MaxStockLength > 0.0 ? spec.MaxStockLength : CommercialStockLengthMm;

    /// <summary>
    /// The entered hook length, or the default leg cut to the depth between the two covers and stirrups.
    /// </summary>
    private static double EndHookLength(
        double enteredLength, double spanHeight, double cover, double stirrupDiameterMm, double barDiameter) =>
        enteredLength > 0.0
            ? enteredLength
            : Math.Min(spanHeight - (2.0 * cover) - (2.0 * stirrupDiameterMm), BeamHookLength.Default(barDiameter));

    /// <summary>Staggered splices: even bars lap on the near side of <paramref name="centre"/>, odd bars on the far side.</summary>
    private static double SpliceCentre(double centre, double staggerOffset, int barIndex) =>
        barIndex % 2 == 0 ? centre - (staggerOffset / 2.0) : centre + (staggerOffset / 2.0);

    private static double StaggerOffset(BeamMainBarSpec spec, double lapLength) =>
        spec.EnableStagger ? (spec.StaggerOffsetRatio * lapLength) : 0.0;

    /// <summary>
    /// Top bars run from cover to cover over the end supports (or the cantilever tips) under the top cover and
    /// stirrup of the first span, hooked down at both ends.
    /// </summary>
    private static BarRun TopRun(BeamContinuousStack stack, BeamMainBarSpec spec, double stirrupDiameterMm)
    {
        var (leftCantilever, rightCantilever) = CantileverEnds(stack);

        double xStart = leftCantilever
            ? stack.OverallStartX + spec.TopCover
            : (stack.Supports.Count > 0 ? stack.Supports[0].LeftFaceX + spec.TopCover : stack.OverallStartX + spec.TopCover);

        double xEnd = rightCantilever
            ? stack.OverallEndX - spec.TopCover
            : (stack.Supports.Count > 0
                ? stack.Supports[stack.Supports.Count - 1].RightFaceX - spec.TopCover
                : stack.OverallEndX - spec.TopCover);

        double z = stack.Spans[0].TopElevation - spec.TopCover - stirrupDiameterMm - (spec.TopDiameter / 2.0);
        double hookStart = EndHookLength(
            spec.TopStartHookLength, stack.Spans[0].Height, spec.TopCover, stirrupDiameterMm, spec.TopDiameter);
        double hookEnd = EndHookLength(
            spec.TopEndHookLength, stack.Spans[stack.Spans.Count - 1].Height, spec.TopCover, stirrupDiameterMm, spec.TopDiameter);

        return new BarRun(xStart, xEnd, z, hookStart, hookEnd);
    }

    /// <summary>One top bar per position, hooked down at both ends (the hook point is kept even when its leg is 0).</summary>
    private static IReadOnlyList<BarPolyline> UnsplicedTopBars(BarRun run, IReadOnlyList<double> yPositions, BeamMainBarSpec spec)
    {
        var result = new List<BarPolyline>();
        for (int i = 0; i < yPositions.Count; i++)
        {
            double y = yPositions[i];
            var rawPoints = new List<Point3>
            {
                new(run.XStart, y, run.Z - run.HookStart),
                new(run.XStart, y, run.Z),
                new(run.XEnd, y, run.Z),
                new(run.XEnd, y, run.Z - run.HookEnd)
            };

            result.Add(new BarPolyline
            {
                BarIndex = i,
                Type = BarType.MainTop,
                Diameter = spec.TopDiameter,
                Layer = 1,
                Polyline = new Polyline3(SimplifyPolyline(rawPoints)),
                StartHookAngle = HookAngle.Hook90,
                EndHookAngle = HookAngle.Hook90,
                TransverseY = y,
                BarTypeName = spec.TopBarTypeName
            });
        }

        return result;
    }

    /// <summary>
    /// Top bars longer than the stock length are lapped at the middle of the middle span, alternate bars
    /// staggered; each position gives a left piece hooked at the start and a right piece hooked at the end.
    /// </summary>
    private static IReadOnlyList<BarPolyline> SplicedTopBars(
        BeamContinuousStack stack, BarRun run, IReadOnlyList<double> yPositions, BeamMainBarSpec spec)
    {
        double lapLength = spec.LapFactor * spec.TopDiameter;
        double staggerOffset = StaggerOffset(spec, lapLength);
        var targetSpan = stack.Spans[stack.Spans.Count / 2];
        double midspanCenter = targetSpan.StartX + (targetSpan.LengthClear / 2.0);

        var result = new List<BarPolyline>();
        for (int i = 0; i < yPositions.Count; i++)
        {
            double y = yPositions[i];
            double spliceCenter = SpliceCentre(midspanCenter, staggerOffset, i);
            double seg1EndX = spliceCenter + (lapLength / 2.0);
            double seg2StartX = spliceCenter - (lapLength / 2.0);

            var pts1 = new List<Point3>
            {
                new(run.XStart, y, run.Z - run.HookStart),
                new(run.XStart, y, run.Z),
                new(seg1EndX, y, run.Z)
            };

            var pts2 = new List<Point3>
            {
                new(seg2StartX, y, run.Z),
                new(run.XEnd, y, run.Z),
                new(run.XEnd, y, run.Z - run.HookEnd)
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

    /// <summary>A step in depth between two adjacent spans of more than 1 mm.</summary>
    private static bool HasDepthStep(BeamContinuousStack stack)
    {
        for (int i = 0; i < stack.Spans.Count - 1; i++)
        {
            if (Math.Abs(stack.Spans[i].Height - stack.Spans[i + 1].Height) > 1.0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// With a depth step each span gets its own bottom bars, hooked up at both ends: from cover over an end
    /// support, from the span ends elsewhere.
    /// </summary>
    private static IReadOnlyList<BarPolyline> SteppedBottomBars(
        BeamContinuousStack stack, BeamMainBarSpec spec, double stirrupDiameterMm, (bool Left, bool Right) cantilevers)
    {
        var result = new List<BarPolyline>();
        for (int s = 0; s < stack.Spans.Count; s++)
        {
            var span = stack.Spans[s];
            var spanYPositions = ComputeTransverseYPositions(
                span.Width, spec.BottomCover, stirrupDiameterMm, spec.BottomDiameter, spec.BottomCount);
            double zBotBar = span.BottomElevation + spec.BottomCover + stirrupDiameterMm + (spec.BottomDiameter / 2.0);
            double hookLen = Math.Min(span.Height - (2.0 * spec.BottomCover), BeamHookLength.Default(spec.BottomDiameter));

            double xStart = (s == 0 && !cantilevers.Left && stack.Supports.Count > 0)
                ? stack.Supports[0].LeftFaceX + spec.BottomCover
                : span.StartX;

            double xEnd = (s == stack.Spans.Count - 1 && !cantilevers.Right && stack.Supports.Count > s + 1)
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
                    BarIndex = result.Count,
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

    /// <summary>
    /// Bottom bars run from cover to cover over the end supports, hooked up; next to a cantilever they stop at
    /// the face of the first interior support, unhooked.
    /// </summary>
    private static BarRun BottomRun(
        BeamContinuousStack stack, BeamMainBarSpec spec, double stirrupDiameterMm, (bool Left, bool Right) cantilevers)
    {
        double xStart;
        double hookStart;
        if (cantilevers.Left && stack.Supports.Count > 1)
        {
            xStart = stack.Supports[1].LeftFaceX;
            hookStart = 0.0;
        }
        else
        {
            xStart = stack.Supports.Count > 0
                ? stack.Supports[0].LeftFaceX + spec.BottomCover
                : stack.OverallStartX + spec.BottomCover;
            hookStart = EndHookLength(
                spec.BottomStartHookLength, stack.Spans[0].Height, spec.BottomCover, stirrupDiameterMm, spec.BottomDiameter);
        }

        double xEnd;
        double hookEnd;
        if (cantilevers.Right && stack.Supports.Count > 1)
        {
            xEnd = stack.Supports[stack.Supports.Count - 2].RightFaceX;
            hookEnd = 0.0;
        }
        else
        {
            xEnd = stack.Supports.Count > 0
                ? stack.Supports[stack.Supports.Count - 1].RightFaceX - spec.BottomCover
                : stack.OverallEndX - spec.BottomCover;
            hookEnd = EndHookLength(
                spec.BottomEndHookLength, stack.Spans[stack.Spans.Count - 1].Height, spec.BottomCover, stirrupDiameterMm,
                spec.BottomDiameter);
        }

        double z = stack.Spans[0].BottomElevation + spec.BottomCover + stirrupDiameterMm + (spec.BottomDiameter / 2.0);
        return new BarRun(xStart, xEnd, z, hookStart, hookEnd);
    }

    /// <summary>One bottom bar per position; a hook point is added only for a positive hook leg.</summary>
    private static IReadOnlyList<BarPolyline> UnsplicedBottomBars(
        BarRun run, IReadOnlyList<double> yPositions, BeamMainBarSpec spec)
    {
        var result = new List<BarPolyline>();
        for (int i = 0; i < yPositions.Count; i++)
        {
            double y = yPositions[i];
            var pts = new List<Point3>();
            if (run.HookStart > 0.0)
                pts.Add(new(run.XStart, y, run.Z + run.HookStart));
            pts.Add(new(run.XStart, y, run.Z));
            pts.Add(new(run.XEnd, y, run.Z));
            if (run.HookEnd > 0.0)
                pts.Add(new(run.XEnd, y, run.Z + run.HookEnd));

            result.Add(new BarPolyline
            {
                BarIndex = i,
                Type = BarType.MainBottom,
                Diameter = spec.BottomDiameter,
                Layer = 1,
                Polyline = new Polyline3(SimplifyPolyline(pts)),
                StartHookAngle = run.HookStart > 0.0 ? HookAngle.Hook90 : HookAngle.None,
                EndHookAngle = run.HookEnd > 0.0 ? HookAngle.Hook90 : HookAngle.None,
                TransverseY = y,
                BarTypeName = spec.BottomBarTypeName
            });
        }

        return result;
    }

    /// <summary>
    /// Bottom bars longer than the stock length are lapped over the middle support, alternate bars staggered.
    /// </summary>
    private static IReadOnlyList<BarPolyline> SplicedBottomBars(
        BeamContinuousStack stack, BarRun run, IReadOnlyList<double> yPositions, BeamMainBarSpec spec)
    {
        double lapLength = spec.LapFactor * spec.BottomDiameter;
        double staggerOffset = StaggerOffset(spec, lapLength);
        int targetSupportIndex = stack.Supports.Count / 2;
        if (targetSupportIndex == 0) targetSupportIndex = 1;
        double supportCenter = stack.Supports[targetSupportIndex].CenterX;

        var result = new List<BarPolyline>();
        for (int i = 0; i < yPositions.Count; i++)
        {
            double y = yPositions[i];
            double spliceCenter = SpliceCentre(supportCenter, staggerOffset, i);

            double seg1EndX = spliceCenter + (lapLength / 2.0);
            var pts1 = new List<Point3>();
            if (run.HookStart > 0.0)
                pts1.Add(new(run.XStart, y, run.Z + run.HookStart));
            pts1.Add(new(run.XStart, y, run.Z));
            pts1.Add(new(seg1EndX, y, run.Z));

            double seg2StartX = spliceCenter - (lapLength / 2.0);
            var pts2 = new List<Point3>
            {
                new(seg2StartX, y, run.Z),
                new(run.XEnd, y, run.Z)
            };
            if (run.HookEnd > 0.0)
                pts2.Add(new(run.XEnd, y, run.Z + run.HookEnd));

            result.Add(new BarPolyline
            {
                BarIndex = (i * 2),
                Type = BarType.MainBottom,
                Diameter = spec.BottomDiameter,
                Layer = 1,
                Polyline = new Polyline3(SimplifyPolyline(pts1)),
                StartHookAngle = run.HookStart > 0.0 ? HookAngle.Hook90 : HookAngle.None,
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
                EndHookAngle = run.HookEnd > 0.0 ? HookAngle.Hook90 : HookAngle.None,
                TransverseY = y,
                BarTypeName = spec.BottomBarTypeName
            });
        }

        return result;
    }
}
