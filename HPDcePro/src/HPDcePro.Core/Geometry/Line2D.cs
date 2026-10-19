using System;

namespace HPDcePro.Core.Geometry
{
    /// <summary>
    /// Represents an infinite 2D line defined by a base origin point and a unit direction vector.
    /// </summary>
    public readonly struct Line2D : IEquatable<Line2D>
    {
        public Point2D Origin { get; }
        public Vector2D Direction { get; }

        public Line2D(Point2D origin, Vector2D direction)
        {
            Origin = origin;
            Direction = direction.Normalize();
        }

        public static Line2D FromPoints(Point2D p1, Point2D p2)
        {
            return new Line2D(p1, (p2 - p1).Normalize());
        }

        public Point2D ProjectPoint(Point2D pt)
        {
            Vector2D w = pt - Origin;
            double t = w.Dot(Direction);
            return Origin + Direction * t;
        }

        public double DistanceTo(Point2D pt)
        {
            Point2D proj = ProjectPoint(pt);
            return pt.DistanceTo(proj);
        }

        public bool Intersect(Line2D other, out Point2D intersection, double tol = 1e-9)
        {
            return LineIntersection.FindLineIntersection(this, other, out intersection, tol);
        }

        public bool Equals(Line2D other)
        {
            double dist = DistanceTo(other.Origin);
            double cross = Math.Abs(Direction.Cross(other.Direction));
            return dist < 1e-9 && cross < 1e-9;
        }

        public override bool Equals(object? obj) => obj is Line2D other && Equals(other);
        public override int GetHashCode() => (Origin.GetHashCode() * 397) ^ Direction.GetHashCode();
        public static bool operator ==(Line2D a, Line2D b) => a.Equals(b);
        public static bool operator !=(Line2D a, Line2D b) => !a.Equals(b);

        public override string ToString() => $"Line2D[Origin: {Origin}, Dir: {Direction}]";
    }
}
