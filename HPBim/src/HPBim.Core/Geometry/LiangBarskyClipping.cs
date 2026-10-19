namespace HPBim.Core.Geometry;

using System;

/// <summary>
/// High-performance O(1) line segment vs 2D bounding box clipping using the Liang-Barsky algorithm.
/// Ported from vncad/qc_geom.py:_cat_hop.
/// </summary>
public static class LiangBarskyClipping
{
    /// <summary>
    /// Checks whether the line segment from p0 to p1 intersects or is contained within the bounding box.
    /// </summary>
    public static bool Intersects(Point2D p0, Point2D p1, BoundingBox2D box)
    {
        return ClipParameters(p0, p1, box, out _, out _);
    }

    /// <summary>
    /// Clips the line segment from p0 to p1 against the bounding box, returning the clipped endpoints if an intersection exists.
    /// </summary>
    public static bool Clip(Point2D p0, Point2D p1, BoundingBox2D box, out Point2D clippedP0, out Point2D clippedP1)
    {
        if (!ClipParameters(p0, p1, box, out double t0, out double t1))
        {
            clippedP0 = p0;
            clippedP1 = p1;
            return false;
        }

        double dx = p1.X - p0.X;
        double dy = p1.Y - p0.Y;

        clippedP0 = new Point2D(p0.X + t0 * dx, p0.Y + t0 * dy);
        clippedP1 = new Point2D(p0.X + t1 * dx, p0.Y + t1 * dy);
        return true;
    }

    /// <summary>
    /// Computes the entry (t0) and exit (t1) parametric coordinates (0 &lt;= t0 &lt;= t1 &lt;= 1) of the line segment intersecting the box.
    /// </summary>
    public static bool ClipParameters(Point2D p0, Point2D p1, BoundingBox2D box, out double t0, out double t1)
    {
        double dx = p1.X - p0.X;
        double dy = p1.Y - p0.Y;

        t0 = 0.0;
        t1 = 1.0;

        // Boundaries: Left, Right, Bottom, Top
        if (!ClipTest(-dx, p0.X - box.MinX, ref t0, ref t1)) return false;
        if (!ClipTest(dx, box.MaxX - p0.X, ref t0, ref t1)) return false;
        if (!ClipTest(-dy, p0.Y - box.MinY, ref t0, ref t1)) return false;
        if (!ClipTest(dy, box.MaxY - p0.Y, ref t0, ref t1)) return false;

        return true;
    }

    private static bool ClipTest(double p, double q, ref double t0, ref double t1)
    {
        if (Math.Abs(p) < 1e-12)
        {
            // Parallel to boundary: if outside, line cannot intersect
            return q >= 0.0;
        }

        double r = q / p;
        if (p < 0.0)
        {
            // Entering boundary
            if (r > t1) return false;
            if (r > t0) t0 = r;
        }
        else
        {
            // Exiting boundary
            if (r < t0) return false;
            if (r < t1) t1 = r;
        }

        return true;
    }
}
