using System;

namespace HPDcePro.Core.Geometry
{
    /// <summary>
    /// Geometric algorithms for computing line and segment intersections in 2D.
    /// </summary>
    public static class LineIntersection
    {
        public static bool FindLineIntersection(Line2D l1, Line2D l2, out Point2D intersection, double tol = 1e-9)
        {
            return FindRayIntersection(l1.Origin, l1.Direction, l2.Origin, l2.Direction, out intersection, tol);
        }

        public static bool FindRayIntersection(
            Point2D p1, Vector2D d1,
            Point2D p2, Vector2D d2,
            out Point2D intersection, double tol = 1e-9)
        {
            intersection = Point2D.Zero;
            double cross = d1.Cross(d2);
            if (Math.Abs(cross) < tol)
            {
                return false;
            }

            Vector2D dp = p2 - p1;
            double t = (dp.X * d2.Y - dp.Y * d2.X) / cross;
            intersection = p1 + d1 * t;
            return true;
        }

        public static bool FindSegmentIntersection(
            Segment2D s1, Segment2D s2,
            out Point2D intersection, double tol = 1e-9)
        {
            intersection = Point2D.Zero;

            Vector2D d1 = s1.End - s1.Start;
            Vector2D d2 = s2.End - s2.Start;
            double cross = d1.Cross(d2);

            Vector2D dp = s2.Start - s1.Start;

            if (Math.Abs(cross) < tol)
            {
                if (Math.Abs(dp.Cross(d1)) < tol)
                {
                    return CheckCollinearOverlap(s1, s2, out intersection, tol);
                }
                return false;
            }

            double t1 = (dp.X * d2.Y - dp.Y * d2.X) / cross;
            double t2 = (dp.X * d1.Y - dp.Y * d1.X) / cross;

            if (t1 >= -tol && t1 <= 1.0 + tol && t2 >= -tol && t2 <= 1.0 + tol)
            {
                double clampedT1 = Math.Max(0.0, Math.Min(1.0, t1));
                intersection = s1.Start + d1 * clampedT1;
                return true;
            }

            return false;
        }

        private static bool CheckCollinearOverlap(
            Segment2D s1, Segment2D s2,
            out Point2D intersection, double tol)
        {
            intersection = Point2D.Zero;
            double t0 = s1.GetProjectionParameter(s2.Start);
            double t1 = s1.GetProjectionParameter(s2.End);

            double minT = Math.Min(t0, t1);
            double maxT = Math.Max(t0, t1);

            if (maxT < -tol || minT > 1.0 + tol)
            {
                return false;
            }

            double overlapMid = (Math.Max(0.0, minT) + Math.Min(1.0, maxT)) * 0.5;
            intersection = s1.Start + (s1.End - s1.Start) * overlapMid;
            return true;
        }

        public static bool IntersectSegmentLine(
            Segment2D seg, Line2D line,
            out Point2D intersection, double tol = 1e-9)
        {
            intersection = Point2D.Zero;
            Vector2D d1 = seg.End - seg.Start;
            double cross = d1.Cross(line.Direction);
            if (Math.Abs(cross) < tol)
            {
                return false;
            }

            Vector2D dp = line.Origin - seg.Start;
            double t = (dp.X * line.Direction.Y - dp.Y * line.Direction.X) / cross;
            if (t >= -tol && t <= 1.0 + tol)
            {
                double clampedT = Math.Max(0.0, Math.Min(1.0, t));
                intersection = seg.Start + d1 * clampedT;
                return true;
            }
            return false;
        }
    }
}
