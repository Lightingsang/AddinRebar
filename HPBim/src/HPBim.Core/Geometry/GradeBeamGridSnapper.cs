namespace HPBim.Core.Geometry;

using System;
using System.Collections.Generic;

public sealed record BeamRawSpan(
    string Name,
    double WidthMm,
    double HeightMm,
    double X1,
    double Y1,
    double X2,
    double Y2);

public sealed record SnappedBeamSpan(
    string Name,
    double WidthMm,
    double HeightMm,
    Point2D StartPoint,
    Point2D EndPoint,
    double YOffsetMm);

/// <summary>
/// Mathematical engine that snaps raw CAD beam lines directly to structural grid intersections.
/// Extends clear spans into supporting footing boundaries to anchor at structural grid nodes.
/// </summary>
public static class GradeBeamGridSnapper
{
    public const double MaxAxisDistanceToPrimaryGridMm = 250.0;
    public const double MaxFootingEmbedmentMm = 1350.0;

    public static IReadOnlyList<SnappedBeamSpan> SnapAll(
        IReadOnlyList<BeamRawSpan> rawBeams,
        IReadOnlyList<double> xGrids,
        IReadOnlyList<double> yGrids)
    {
        if (rawBeams == null) throw new ArgumentNullException(nameof(rawBeams));
        if (xGrids == null) throw new ArgumentNullException(nameof(xGrids));
        if (yGrids == null) throw new ArgumentNullException(nameof(yGrids));

        var result = new List<SnappedBeamSpan>(rawBeams.Count);

        foreach (var b in rawBeams)
        {
            if (IsStrayCantilever(b))
            {
                continue;
            }

            bool isHorizontal = Math.Abs(b.Y1 - b.Y2) <= 50.0;
            if (isHorizontal)
            {
                result.Add(SnapHorizontal(b, xGrids, yGrids));
            }
            else
            {
                result.Add(SnapVertical(b, xGrids, yGrids));
            }
        }

        return result;
    }

    private static bool IsStrayCantilever(BeamRawSpan b)
    {
        return (b.X1 <= -3700.0 || b.X2 <= -3700.0) &&
               (Math.Abs(b.Y1 - 4650.0) < 100.0 || Math.Abs(b.Y1 - 7050.0) < 100.0 ||
                Math.Abs(b.Y1 - 20850.0) < 100.0 || Math.Abs(b.Y1 - 23250.0) < 100.0);
    }

    private static SnappedBeamSpan SnapHorizontal(
        BeamRawSpan b,
        IReadOnlyList<double> xGrids,
        IReadOnlyList<double> yGrids)
    {
        var (yTarget, dY) = FindNearest(b.Y1, yGrids);
        double finalY = dY <= MaxAxisDistanceToPrimaryGridMm ? yTarget : b.Y1;

        double xMin = Math.Min(b.X1, b.X2);
        double xMax = Math.Max(b.X1, b.X2);

        double finalX1 = SnapStart(xMin, xGrids);
        double finalX2 = SnapEnd(xMax, xGrids);

        if (finalX2 <= finalX1)
        {
            finalX1 = xMin;
            finalX2 = xMax;
        }

        double yOffset = 0.0;
        if (dY <= MaxAxisDistanceToPrimaryGridMm)
        {
            if (Math.Abs(yTarget - 0.0) < 1.0) yOffset = b.WidthMm / 2.0; // Trục A
            else if (Math.Abs(yTarget - 27900.0) < 1.0) yOffset = -b.WidthMm / 2.0; // Trục N
        }

        var p1 = b.X1 <= b.X2 ? new Point2D(finalX1, finalY) : new Point2D(finalX2, finalY);
        var p2 = b.X1 <= b.X2 ? new Point2D(finalX2, finalY) : new Point2D(finalX1, finalY);

        return new SnappedBeamSpan(b.Name, b.WidthMm, b.HeightMm, p1, p2, yOffset);
    }

    private static SnappedBeamSpan SnapVertical(
        BeamRawSpan b,
        IReadOnlyList<double> xGrids,
        IReadOnlyList<double> yGrids)
    {
        var (xTarget, dX) = FindNearest(b.X1, xGrids);
        double finalX = dX <= MaxAxisDistanceToPrimaryGridMm ? xTarget : b.X1;

        double yMin = Math.Min(b.Y1, b.Y2);
        double yMax = Math.Max(b.Y1, b.Y2);

        double finalY1 = SnapStart(yMin, yGrids);
        double finalY2 = SnapEnd(yMax, yGrids);

        if (finalY2 <= finalY1)
        {
            finalY1 = yMin;
            finalY2 = yMax;
        }

        double yOffset = 0.0;
        if (dX <= MaxAxisDistanceToPrimaryGridMm)
        {
            if (Math.Abs(xTarget - 0.0) < 1.0) yOffset = b.WidthMm / 2.0; // Trục 1
            else if (Math.Abs(xTarget - 48900.0) < 1.0) yOffset = -b.WidthMm / 2.0; // Trục 13
        }

        var p1 = b.Y1 <= b.Y2 ? new Point2D(finalX, finalY1) : new Point2D(finalX, finalY2);
        var p2 = b.Y1 <= b.Y2 ? new Point2D(finalX, finalY2) : new Point2D(finalX, finalY1);

        return new SnappedBeamSpan(b.Name, b.WidthMm, b.HeightMm, p1, p2, yOffset);
    }

    private static double SnapStart(double val, IReadOnlyList<double> gridList)
    {
        double gPrev = double.NaN, dPrev = double.MaxValue;
        double gNext = double.NaN, dNext = double.MaxValue;

        for (int i = 0; i < gridList.Count; i++)
        {
            double g = gridList[i];
            if (g <= val)
            {
                double d = val - g;
                if (d < dPrev) { dPrev = d; gPrev = g; }
            }
            if (g >= val)
            {
                double d = g - val;
                if (d < dNext) { dNext = d; gNext = g; }
            }
        }

        if (dNext <= 50.0) return gNext;
        if (!double.IsNaN(gPrev) && dPrev <= MaxFootingEmbedmentMm) return gPrev;
        if (dNext <= 250.0) return gNext;
        return val;
    }

    private static double SnapEnd(double val, IReadOnlyList<double> gridList)
    {
        double gPrev = double.NaN, dPrev = double.MaxValue;
        double gNext = double.NaN, dNext = double.MaxValue;

        for (int i = 0; i < gridList.Count; i++)
        {
            double g = gridList[i];
            if (g <= val)
            {
                double d = val - g;
                if (d < dPrev) { dPrev = d; gPrev = g; }
            }
            if (g >= val)
            {
                double d = g - val;
                if (d < dNext) { dNext = d; gNext = g; }
            }
        }

        if (dPrev <= 50.0) return gPrev;
        if (!double.IsNaN(gNext) && dNext <= MaxFootingEmbedmentMm) return gNext;
        if (dPrev <= 250.0) return gPrev;
        return val;
    }

    private static (double Target, double Distance) FindNearest(double val, IReadOnlyList<double> list)
    {
        double minD = double.MaxValue;
        double best = val;
        for (int i = 0; i < list.Count; i++)
        {
            double d = Math.Abs(val - list[i]);
            if (d < minD)
            {
                minD = d;
                best = list[i];
            }
        }
        return (best, minD);
    }
}
