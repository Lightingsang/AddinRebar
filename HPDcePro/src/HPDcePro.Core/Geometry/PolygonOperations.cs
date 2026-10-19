using System;
using System.Collections.Generic;

namespace HPDcePro.Core.Geometry
{
    /// <summary>
    /// Advanced computational geometry operations on 2D polygons.
    /// </summary>
    public static class PolygonOperations
    {
        public static bool RayCastContains(Polygon2D polygon, Point2D pt)
        {
            var vertices = polygon.Vertices;
            int n = vertices.Count;
            bool inside = false;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                Point2D vi = vertices[i];
                Point2D vj = vertices[j];

                bool crossesY = (vi.Y > pt.Y) != (vj.Y > pt.Y);
                if (crossesY)
                {
                    double intersectX = vi.X + (pt.Y - vi.Y) * (vj.X - vi.X) / (vj.Y - vi.Y);
                    if (pt.X < intersectX)
                    {
                        inside = !inside;
                    }
                }
            }
            return inside;
        }

        public static bool WindingNumberContains(Polygon2D polygon, Point2D pt)
        {
            var vertices = polygon.Vertices;
            int n = vertices.Count;
            int windingNumber = 0;

            for (int i = 0; i < n; i++)
            {
                Point2D v1 = vertices[i];
                Point2D v2 = vertices[(i + 1) % n];

                if (v1.Y <= pt.Y)
                {
                    if (v2.Y > pt.Y && IsLeft(v1, v2, pt) > 0)
                    {
                        windingNumber++;
                    }
                }
                else
                {
                    if (v2.Y <= pt.Y && IsLeft(v1, v2, pt) < 0)
                    {
                        windingNumber--;
                    }
                }
            }
            return windingNumber != 0;
        }

        private static double IsLeft(Point2D p0, Point2D p1, Point2D p2)
        {
            return (p1.X - p0.X) * (p2.Y - p0.Y) - (p2.X - p0.X) * (p1.Y - p0.Y);
        }

        public static bool Overlaps(Polygon2D poly1, Polygon2D poly2)
        {
            foreach (Point2D v in poly1.Vertices)
            {
                if (poly2.ContainsPoint(v)) return true;
            }
            foreach (Point2D v in poly2.Vertices)
            {
                if (poly1.ContainsPoint(v)) return true;
            }
            foreach (Segment2D e1 in poly1.GetEdges())
            {
                foreach (Segment2D e2 in poly2.GetEdges())
                {
                    if (e1.Intersect(e2, out _)) return true;
                }
            }
            return false;
        }

        public static double ClipSegmentLength(Polygon2D polygon, Segment2D seg)
        {
            if (seg.Length < 1e-9)
            {
                return polygon.ContainsPoint(seg.Start) ? seg.Length : 0.0;
            }

            var tParams = new List<double> { 0.0, 1.0 };
            foreach (Segment2D edge in polygon.GetEdges())
            {
                if (seg.Intersect(edge, out Point2D pt))
                {
                    double t = seg.GetProjectionParameter(pt);
                    if (t > 1e-6 && t < 1.0 - 1e-6)
                    {
                        tParams.Add(t);
                    }
                }
            }

            tParams.Sort();
            double insideLength = 0.0;

            for (int i = 0; i < tParams.Count - 1; i++)
            {
                double tMid = (tParams[i] + tParams[i + 1]) * 0.5;
                Point2D midPt = seg.Start + (seg.End - seg.Start) * tMid;
                if (polygon.ContainsPoint(midPt))
                {
                    double subLength = (tParams[i + 1] - tParams[i]) * seg.Length;
                    insideLength += subLength;
                }
            }
            return insideLength;
        }
    }
}
