using System;
using System.Collections.Generic;
using System.Linq;

namespace HPDcePro.Core.Geometry
{
    /// <summary>
    /// Topological and network algorithms for structural framing plans (replaces MBKC_* native exports).
    /// </summary>
    public class FramingTopology
    {
        public List<Point2D> ColumnNodes { get; set; } = new List<Point2D>();
        public List<Segment2D> BeamCenterlines { get; set; } = new List<Segment2D>();
        public List<Polygon2D> SlabPanels { get; set; } = new List<Polygon2D>();
        public List<Point2D> PileNodes { get; set; } = new List<Point2D>();

        public static double AngleBetween(Vector2D v1, Vector2D v2) => v1.AngleBetween(v2);

        public static bool AreConnected(Segment2D b1, Segment2D b2, double tol = 1.0)
        {
            return b1.Start.DistanceTo(b2.Start) <= tol ||
                   b1.Start.DistanceTo(b2.End) <= tol ||
                   b1.End.DistanceTo(b2.Start) <= tol ||
                   b1.End.DistanceTo(b2.End) <= tol;
        }

        public static bool CalculateCircumcircle(
            Point2D p1, Point2D p2, Point2D p3,
            out Point2D center, out double radius)
        {
            center = Point2D.Zero;
            radius = 0.0;

            double d = 2.0 * (p1.X * (p2.Y - p3.Y) + p2.X * (p3.Y - p1.Y) + p3.X * (p1.Y - p2.Y));
            if (Math.Abs(d) < 1e-9)
            {
                return false;
            }

            double p1Sq = p1.X * p1.X + p1.Y * p1.Y;
            double p2Sq = p2.X * p2.X + p2.Y * p2.Y;
            double p3Sq = p3.X * p3.X + p3.Y * p3.Y;

            double ux = (p1Sq * (p2.Y - p3.Y) + p2Sq * (p3.Y - p1.Y) + p3Sq * (p1.Y - p2.Y)) / d;
            double uy = (p1Sq * (p3.X - p2.X) + p2Sq * (p1.X - p3.X) + p3Sq * (p2.X - p1.X)) / d;

            center = new Point2D(ux, uy);
            radius = center.DistanceTo(p1);
            return true;
        }

        public static bool InCircumcircle(Point2D p1, Point2D p2, Point2D p3, Point2D testPt)
        {
            if (!CalculateCircumcircle(p1, p2, p3, out Point2D center, out double radius))
            {
                return false;
            }
            return center.DistanceTo(testPt) < radius - 1e-9;
        }

        public static Vector2D GetGroupDirection(IEnumerable<Segment2D> segments)
        {
            if (segments == null) throw new ArgumentNullException(nameof(segments));
            var list = segments.ToList();
            if (list.Count == 0) return Vector2D.UnitX;

            double sumDx = 0.0, sumDy = 0.0;
            foreach (var seg in list)
            {
                Vector2D v = seg.Direction;
                // Normalize to positive X quadrant
                if (v.X < 0 || (Math.Abs(v.X) < 1e-9 && v.Y < 0))
                {
                    v = -v;
                }
                sumDx += v.X;
                sumDy += v.Y;
            }
            return new Vector2D(sumDx, sumDy).Normalize();
        }

        public static Point2D GetGroupOrigin(IEnumerable<Segment2D> segments)
        {
            if (segments == null) throw new ArgumentNullException(nameof(segments));
            var pts = new List<Point2D>();
            foreach (var s in segments)
            {
                pts.Add(s.Start);
                pts.Add(s.End);
            }
            if (pts.Count == 0) return Point2D.Zero;

            // Pick minimum X, then minimum Y
            return pts.OrderBy(p => p.X).ThenBy(p => p.Y).First();
        }

        public static Point2D? FindNearestNode(Point2D pt, IEnumerable<Point2D> nodes, double tol)
        {
            if (nodes == null) return null;
            Point2D? nearest = null;
            double minDist = tol;

            foreach (var node in nodes)
            {
                double d = pt.DistanceTo(node);
                if (d <= minDist)
                {
                    minDist = d;
                    nearest = node;
                }
            }
            return nearest;
        }

        public static double TriangleArea(Point2D p1, Point2D p2, Point2D p3)
        {
            return 0.5 * Math.Abs((p2.X - p1.X) * (p3.Y - p1.Y) - (p3.X - p1.X) * (p2.Y - p1.Y));
        }

        public static List<Segment2D> SortContinuousSpans(IEnumerable<Segment2D> spans)
        {
            if (spans == null) return new List<Segment2D>();
            var remaining = spans.ToList();
            if (remaining.Count <= 1) return remaining;

            var sorted = new List<Segment2D>();
            var first = remaining.OrderBy(s => Math.Min(s.Start.X, s.End.X)).First();
            sorted.Add(first);
            remaining.Remove(first);

            while (remaining.Count > 0)
            {
                Point2D currentEnd = sorted.Last().End;
                int bestIdx = -1;
                double bestDist = double.MaxValue;
                bool flip = false;

                for (int i = 0; i < remaining.Count; i++)
                {
                    double dStart = currentEnd.DistanceTo(remaining[i].Start);
                    double dEnd = currentEnd.DistanceTo(remaining[i].End);
                    if (dStart < bestDist)
                    {
                        bestDist = dStart;
                        bestIdx = i;
                        flip = false;
                    }
                    if (dEnd < bestDist)
                    {
                        bestDist = dEnd;
                        bestIdx = i;
                        flip = true;
                    }
                }

                if (bestIdx >= 0)
                {
                    var seg = remaining[bestIdx];
                    sorted.Add(flip ? new Segment2D(seg.End, seg.Start) : seg);
                    remaining.RemoveAt(bestIdx);
                }
                else
                {
                    break;
                }
            }
            return sorted;
        }
    }
}
