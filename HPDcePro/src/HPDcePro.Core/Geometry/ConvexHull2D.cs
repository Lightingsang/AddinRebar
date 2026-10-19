using System;
using System.Collections.Generic;
using System.Linq;

namespace HPDcePro.Core.Geometry
{
    /// <summary>
    /// Computes the 2D convex hull using Andrew's Monotone Chain algorithm (O(n log n)).
    /// </summary>
    public static class ConvexHull2D
    {
        public static Polygon2D Compute(IEnumerable<Point2D> inputPoints)
        {
            if (inputPoints == null)
            {
                throw new ArgumentNullException(nameof(inputPoints));
            }

            var points = inputPoints.Distinct().ToList();
            if (points.Count < 3)
            {
                throw new ArgumentException("At least 3 distinct points are required to compute a convex hull.", nameof(inputPoints));
            }

            // Sort points lexicographically by X, then by Y
            points.Sort((a, b) =>
            {
                int cmp = a.X.CompareTo(b.X);
                return cmp != 0 ? cmp : a.Y.CompareTo(b.Y);
            });

            var lower = new List<Point2D>();
            foreach (var p in points)
            {
                while (lower.Count >= 2 && Cross(lower[lower.Count - 2], lower[lower.Count - 1], p) <= 0)
                {
                    lower.RemoveAt(lower.Count - 1);
                }
                lower.Add(p);
            }

            var upper = new List<Point2D>();
            for (int i = points.Count - 1; i >= 0; i--)
            {
                var p = points[i];
                while (upper.Count >= 2 && Cross(upper[upper.Count - 2], upper[upper.Count - 1], p) <= 0)
                {
                    upper.RemoveAt(upper.Count - 1);
                }
                upper.Add(p);
            }

            lower.RemoveAt(lower.Count - 1);
            upper.RemoveAt(upper.Count - 1);

            lower.AddRange(upper);
            return new Polygon2D(lower);
        }

        private static double Cross(Point2D o, Point2D a, Point2D b)
        {
            return (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);
        }
    }
}
