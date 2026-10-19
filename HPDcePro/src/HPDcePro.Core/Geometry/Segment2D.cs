using System;

namespace HPDcePro.Core.Geometry
{
    /// <summary>
    /// Represents a finite directed 2D line segment between two endpoints.
    /// </summary>
    public readonly struct Segment2D : IEquatable<Segment2D>
    {
        public Point2D Start { get; }
        public Point2D End { get; }

        public Segment2D(Point2D start, Point2D end)
        {
            Start = start;
            End = end;
        }

        public Segment2D(double x1, double y1, double x2, double y2)
        {
            Start = new Point2D(x1, y1);
            End = new Point2D(x2, y2);
        }

        public double Length => Start.DistanceTo(End);
        public double LengthSquared => Start.DistanceToSquared(End);
        public Point2D MidPoint => Start.MidPoint(End);
        public Vector2D Direction => (End - Start).Normalize();
        public Vector2D Delta => End - Start;

        public double GetProjectionParameter(Point2D pt)
        {
            double lenSq = LengthSquared;
            if (lenSq < 1e-12)
            {
                return 0.0;
            }
            Vector2D v = End - Start;
            Vector2D w = pt - Start;
            return (w.X * v.X + w.Y * v.Y) / lenSq;
        }

        public Point2D ProjectPoint(Point2D pt)
        {
            double t = GetProjectionParameter(pt);
            if (t <= 0.0) return Start;
            if (t >= 1.0) return End;
            return new Point2D(Start.X + t * (End.X - Start.X), Start.Y + t * (End.Y - Start.Y));
        }

        public double DistanceTo(Point2D pt)
        {
            Point2D projected = ProjectPoint(pt);
            return pt.DistanceTo(projected);
        }

        public bool ContainsPointOnLine(Point2D pt, double tol = 1e-9)
        {
            double minX = Math.Min(Start.X, End.X) - tol;
            double maxX = Math.Max(Start.X, End.X) + tol;
            double minY = Math.Min(Start.Y, End.Y) - tol;
            double maxY = Math.Max(Start.Y, End.Y) + tol;

            if (pt.X < minX || pt.X > maxX || pt.Y < minY || pt.Y > maxY)
            {
                return false;
            }

            Vector2D v1 = End - Start;
            Vector2D v2 = pt - Start;
            double cross = Math.Abs(v1.Cross(v2));
            return cross <= tol * Math.Max(1.0, Length);
        }

        public bool Intersect(Segment2D other, out Point2D intersection, double tol = 1e-9)
        {
            return LineIntersection.FindSegmentIntersection(this, other, out intersection, tol);
        }

        public bool Equals(Segment2D other) => Start == other.Start && End == other.End;
        public override bool Equals(object? obj) => obj is Segment2D other && Equals(other);
        public override int GetHashCode() => (Start.GetHashCode() * 397) ^ End.GetHashCode();
        public static bool operator ==(Segment2D a, Segment2D b) => a.Equals(b);
        public static bool operator !=(Segment2D a, Segment2D b) => !a.Equals(b);

        public override string ToString() => $"[{Start} -> {End}]";
    }
}
