namespace HPBim.Core.Geometry;

using System;
using System.Collections.Generic;

/// <summary>
/// Pure mathematical generator for 3-pile footings (đài tam giác vát 2 góc đỉnh / đài tổ ong 6 đỉnh).
/// Generates a symmetric 6-vertex polygon (trapezoid over rectangle) matching CAD structural details.
/// </summary>
public static class TriangularFootingGenerator
{
    public const double DefaultClearanceMm = 350.0;
    public const double CanonicalBaseWidthMm = 1600.0;
    public const double CanonicalTopWidthMm = 600.0;
    public const double CanonicalTotalHeightMm = 1500.0;
    public const double CanonicalStraightHeightMm = 700.0;

    public static Polygon2D Generate(Point2D p1, Point2D p2, Point2D p3, double offsetDistanceMm = DefaultClearanceMm)
    {
        var (b1, b2, apex) = IdentifyBaseAndApex(p1, p2, p3);
        var mid = new Point2D((b1.X + b2.X) * 0.5, (b1.Y + b2.Y) * 0.5);

        double hx = apex.X - mid.X;
        double hy = apex.Y - mid.Y;
        double hLen = Math.Sqrt((hx * hx) + (hy * hy));
        if (hLen < 1e-6)
        {
            hx = 0.0;
            hy = 1.0;
            hLen = 1.0;
        }

        var uAlt = new Point2D(hx / hLen, hy / hLen);
        var uTrans = new Point2D(uAlt.Y, -uAlt.X);

        double baseWidth = CanonicalBaseWidthMm;
        double topWidth = CanonicalTopWidthMm;
        double totalHeight = CanonicalTotalHeightMm;
        double straightHeight = CanonicalStraightHeightMm;

        if (Math.Abs(offsetDistanceMm - DefaultClearanceMm) > 1e-3)
        {
            double pileSpacing = b1.DistanceTo(b2);
            baseWidth = pileSpacing + (2.0 * offsetDistanceMm);
            topWidth = Math.Max(200.0, 2.0 * (offsetDistanceMm - 50.0));
            totalHeight = hLen + (2.0 * offsetDistanceMm);
            straightHeight = offsetDistanceMm + 350.0;
        }

        double halfBase = baseWidth * 0.5;
        double halfTop = topWidth * 0.5;
        double aBottom = -offsetDistanceMm;
        double aShoulder = aBottom + straightHeight;
        double aTop = aBottom + totalHeight;

        var localVertices = new[]
        {
            (-halfBase, aBottom),
            (+halfBase, aBottom),
            (+halfBase, aShoulder),
            (+halfTop, aTop),
            (-halfTop, aTop),
            (-halfBase, aShoulder)
        };

        var vertices = new List<Point2D>(6);
        foreach (var (t, a) in localVertices)
        {
            double gx = mid.X + (t * uTrans.X) + (a * uAlt.X);
            double gy = mid.Y + (t * uTrans.Y) + (a * uAlt.Y);
            vertices.Add(new Point2D(gx, gy));
        }

        return new Polygon2D(vertices);
    }

    private static (Point2D Base1, Point2D Base2, Point2D Apex) IdentifyBaseAndApex(Point2D p1, Point2D p2, Point2D p3)
    {
        var pairs = new[]
        {
            (A: p1, B: p2, C: p3),
            (A: p2, B: p3, C: p1),
            (A: p3, B: p1, C: p2)
        };

        var best = pairs[0];
        double bestScore = double.MaxValue;

        foreach (var pair in pairs)
        {
            double baseLen = pair.A.DistanceTo(pair.B);
            var mid = new Point2D((pair.A.X + pair.B.X) * 0.5, (pair.A.Y + pair.B.Y) * 0.5);
            double altitude = pair.C.DistanceTo(mid);

            // Target base is ~900mm, target altitude is ~800mm
            double score = Math.Abs(baseLen - 900.0) + Math.Abs(altitude - 800.0);
            if (score < bestScore)
            {
                bestScore = score;
                best = pair;
            }
        }

        return (best.A, best.B, best.C);
    }
}
