using System;
using System.Collections.Generic;
using System.Linq;

namespace HPDcePro.Core.Geometry
{
    /// <summary>
    /// Represents a 2D closed polygon defined by an ordered list of vertices.
    /// </summary>
    public class Polygon2D
    {
        private readonly List<Point2D> _vertices;

        public IReadOnlyList<Point2D> Vertices => _vertices;
        public int VertexCount => _vertices.Count;

        public Polygon2D(IEnumerable<Point2D> vertices)
        {
            if (vertices == null)
            {
                throw new ArgumentNullException(nameof(vertices));
            }

            _vertices = new List<Point2D>(vertices);
            if (_vertices.Count > 1 && _vertices[0] == _vertices[_vertices.Count - 1])
            {
                _vertices.RemoveAt(_vertices.Count - 1);
            }

            if (_vertices.Count < 3)
            {
                throw new ArgumentException("A polygon must have at least 3 non-coincident vertices.", nameof(vertices));
            }
        }

        public double Area => Math.Abs(SignedArea);

        public double SignedArea
        {
            get
            {
                double sum = 0.0;
                int n = _vertices.Count;
                for (int i = 0; i < n; i++)
                {
                    Point2D curr = _vertices[i];
                    Point2D next = _vertices[(i + 1) % n];
                    sum += curr.X * next.Y - next.X * curr.Y;
                }
                return sum * 0.5;
            }
        }

        public bool IsClockwise => SignedArea < 0.0;

        public Point2D Centroid
        {
            get
            {
                double signedArea = SignedArea;
                if (Math.Abs(signedArea) < 1e-12)
                {
                    return _vertices[0];
                }

                double cx = 0.0;
                double cy = 0.0;
                int n = _vertices.Count;
                for (int i = 0; i < n; i++)
                {
                    Point2D curr = _vertices[i];
                    Point2D next = _vertices[(i + 1) % n];
                    double cross = curr.X * next.Y - next.X * curr.Y;
                    cx += (curr.X + next.X) * cross;
                    cy += (curr.Y + next.Y) * cross;
                }
                double factor = 1.0 / (6.0 * signedArea);
                return new Point2D(cx * factor, cy * factor);
            }
        }

        public double Perimeter
        {
            get
            {
                double sum = 0.0;
                int n = _vertices.Count;
                for (int i = 0; i < n; i++)
                {
                    sum += _vertices[i].DistanceTo(_vertices[(i + 1) % n]);
                }
                return sum;
            }
        }

        public Polygon2D ReverseOrientation()
        {
            var reversed = new List<Point2D>(_vertices);
            reversed.Reverse();
            return new Polygon2D(reversed);
        }

        public IEnumerable<Segment2D> GetEdges()
        {
            int n = _vertices.Count;
            for (int i = 0; i < n; i++)
            {
                yield return new Segment2D(_vertices[i], _vertices[(i + 1) % n]);
            }
        }

        public bool ContainsPoint(Point2D pt, double tol = 1e-9)
        {
            if (IsPointOnBoundary(pt, tol))
            {
                return true;
            }
            return RayCastContains(pt);
        }

        public bool RayCastContains(Point2D pt)
        {
            return PolygonOperations.RayCastContains(this, pt);
        }

        public bool WindingNumberContains(Point2D pt)
        {
            return PolygonOperations.WindingNumberContains(this, pt);
        }

        public bool IsPointOnBoundary(Point2D pt, double tol = 1e-9)
        {
            foreach (Segment2D edge in GetEdges())
            {
                if (edge.ContainsPointOnLine(pt, tol))
                {
                    return true;
                }
            }
            return false;
        }

        public static Polygon2D CreateCircle(Point2D center, double radius, int numSides = 36)
        {
            if (numSides < 3)
            {
                throw new ArgumentOutOfRangeException(nameof(numSides), "Number of sides must be at least 3.");
            }
            if (radius <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(radius), "Radius must be positive.");
            }

            var points = new Point2D[numSides];
            double angleStep = 2.0 * Math.PI / numSides;
            for (int i = 0; i < numSides; i++)
            {
                double angle = i * angleStep;
                points[i] = new Point2D(center.X + radius * Math.Cos(angle), center.Y + radius * Math.Sin(angle));
            }
            return new Polygon2D(points);
        }

        public bool IntersectsLine(Line2D line)
        {
            foreach (Segment2D edge in GetEdges())
            {
                if (LineIntersection.IntersectSegmentLine(edge, line, out _))
                {
                    return true;
                }
            }
            return false;
        }

        public bool Overlaps(Polygon2D other)
        {
            return PolygonOperations.Overlaps(this, other);
        }

        public double ClipSegmentLength(Segment2D seg)
        {
            return PolygonOperations.ClipSegmentLength(this, seg);
        }

        public static Polygon2D ConvexHull(IEnumerable<Point2D> points)
        {
            return ConvexHull2D.Compute(points);
        }
    }
}
