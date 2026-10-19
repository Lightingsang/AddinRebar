namespace HPBim.Core.Geometry;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a closed planar polygon defined by an ordered list of 2D vertices.
/// Provides Shoelace area calculation, perimeter, centroid, and point containment.
/// </summary>
public sealed class Polygon2D : IEquatable<Polygon2D>
{
    private readonly Point2D[] _vertices;

    public static Polygon2D Empty => new(Array.Empty<Point2D>());

    [JsonConstructor]
    public Polygon2D(IReadOnlyList<Point2D> vertices)
    {
        if (vertices == null)
        {
            throw new ArgumentNullException(nameof(vertices));
        }

        var list = new List<Point2D>(vertices);
        if (list.Count >= 2 && list[0].IsAlmostEqualTo(list[list.Count - 1]))
        {
            list.RemoveAt(list.Count - 1);
        }

        if (list.Count > 0 && list.Count < 3)
        {
            throw new ArgumentException("A non-empty polygon must contain at least 3 distinct vertices.", nameof(vertices));
        }

        _vertices = list.ToArray();
    }

    public Polygon2D(IEnumerable<Point2D> vertices)
        : this(vertices as IReadOnlyList<Point2D> ?? new List<Point2D>(vertices ?? throw new ArgumentNullException(nameof(vertices))))
    {
    }

    public IReadOnlyList<Point2D> Vertices => _vertices;

    [JsonIgnore]
    public int VertexCount => _vertices.Length;

    [JsonIgnore]
    public double Perimeter
    {
        get
        {
            if (_vertices.Length < 3) return 0.0;
            double sum = 0.0;
            int count = _vertices.Length;
            for (int i = 0; i < count; i++)
            {
                Point2D p1 = _vertices[i];
                Point2D p2 = _vertices[(i + 1) % count];
                sum += p1.DistanceTo(p2);
            }
            return sum;
        }
    }

    [JsonIgnore]
    public double SignedArea
    {
        get
        {
            if (_vertices.Length < 3) return 0.0;
            double sum = 0.0;
            int count = _vertices.Length;
            for (int i = 0; i < count; i++)
            {
                Point2D p1 = _vertices[i];
                Point2D p2 = _vertices[(i + 1) % count];
                sum += (p1.X * p2.Y) - (p2.X * p1.Y);
            }
            return sum * 0.5;
        }
    }

    [JsonIgnore]
    public double Area => Math.Abs(SignedArea);

    [JsonIgnore]
    public bool IsCounterClockwise => SignedArea > 0.0;

    [JsonIgnore]
    public bool IsClockwise => !IsCounterClockwise;

    [JsonIgnore]
    public Point2D Centroid
    {
        get
        {
            if (_vertices.Length == 0) return Point2D.Zero;
            double signedArea = SignedArea;
            if (Math.Abs(signedArea) < 1e-9)
            {
                return _vertices[0];
            }

            double cx = 0.0;
            double cy = 0.0;
            int count = _vertices.Length;

            for (int i = 0; i < count; i++)
            {
                Point2D p1 = _vertices[i];
                Point2D p2 = _vertices[(i + 1) % count];
                double factor = (p1.X * p2.Y) - (p2.X * p1.Y);
                cx += (p1.X + p2.X) * factor;
                cy += (p1.Y + p2.Y) * factor;
            }

            double divisor = 6.0 * signedArea;
            return new Point2D(cx / divisor, cy / divisor);
        }
    }

    [JsonIgnore]
    public BoundingBox2D BoundingBox => GetBoundingBox();

    public BoundingBox2D GetBoundingBox()
    {
        if (_vertices.Length == 0) return new BoundingBox2D(0, 0, 0, 0);
        return BoundingBox2D.FromPoints(_vertices);
    }

    public bool Contains(Point2D pt, bool includeBoundary = true)
    {
        if (_vertices.Length < 3) return false;
        bool inside = false;
        int count = _vertices.Length;

        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            Point2D pi = _vertices[i];
            Point2D pj = _vertices[j];

            if (IsPointOnSegment(pt, pi, pj))
            {
                return includeBoundary;
            }

            bool intersect = ((pi.Y > pt.Y) != (pj.Y > pt.Y)) &&
                             (pt.X < (pj.X - pi.X) * (pt.Y - pi.Y) / (pj.Y - pi.Y) + pi.X);
            if (intersect)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static bool IsPointOnSegment(Point2D p, Point2D a, Point2D b, double tolerance = 1e-6)
    {
        double cross = (p.Y - a.Y) * (b.X - a.X) - (p.X - a.X) * (b.Y - a.Y);
        if (Math.Abs(cross) > tolerance)
        {
            return false;
        }

        double dot = (p.X - a.X) * (b.X - a.X) + (p.Y - a.Y) * (b.Y - a.Y);
        if (dot < 0.0)
        {
            return false;
        }

        double lenSq = a.DistanceSquaredTo(b);
        return dot <= lenSq;
    }

    /// <summary>
    /// Computes the minimum Euclidean distance from a point to the polygon boundary edges.
    /// </summary>
    public double DistanceToBoundary(Point2D pt)
    {
        if (_vertices.Length < 2) return double.PositiveInfinity;
        double minDistance = double.PositiveInfinity;
        int count = _vertices.Length;

        for (int i = 0; i < count; i++)
        {
            Point2D p1 = _vertices[i];
            Point2D p2 = _vertices[(i + 1) % count];
            double dist = DistancePointToSegment(pt, p1, p2);
            if (dist < minDistance)
            {
                minDistance = dist;
            }
        }

        return minDistance;
    }

    private static double DistancePointToSegment(Point2D p, Point2D a, Point2D b)
    {
        double dx = b.X - a.X;
        double dy = b.Y - a.Y;
        double l2 = (dx * dx) + (dy * dy);
        if (l2 < 1e-12)
        {
            return p.DistanceTo(a);
        }

        double t = Math.Max(0.0, Math.Min(1.0, (((p.X - a.X) * dx) + ((p.Y - a.Y) * dy)) / l2));
        double projX = a.X + (t * dx);
        double projY = a.Y + (t * dy);
        return p.DistanceTo(new Point2D(projX, projY));
    }

    public bool Equals(Polygon2D? other)
    {
        if (other is null || _vertices.Length != other._vertices.Length)
        {
            return false;
        }

        for (int i = 0; i < _vertices.Length; i++)
        {
            if (!_vertices[i].IsAlmostEqualTo(other._vertices[i]))
            {
                return false;
            }
        }
        return true;
    }

    public override bool Equals(object? obj) => obj is Polygon2D other && Equals(other);

    public override int GetHashCode()
    {
        int hash = _vertices.Length;
        for (int i = 0; i < _vertices.Length; i++)
        {
            hash = (hash * 397) ^ _vertices[i].GetHashCode();
        }
        return hash;
    }
}
