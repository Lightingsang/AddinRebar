namespace HPBim.Core.Geometry;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents an immutable 2D axis-aligned bounding box (AABB) in millimeters.
/// </summary>
public readonly struct BoundingBox2D : IEquatable<BoundingBox2D>
{
    public BoundingBox2D(Point2D min, Point2D max)
    {
        double minX = Math.Min(min.X, max.X);
        double minY = Math.Min(min.Y, max.Y);
        double maxX = Math.Max(min.X, max.X);
        double maxY = Math.Max(min.Y, max.Y);

        Min = new Point2D(minX, minY);
        Max = new Point2D(maxX, maxY);
    }

    public BoundingBox2D(double minX, double minY, double maxX, double maxY)
        : this(new Point2D(minX, minY), new Point2D(maxX, maxY))
    {
    }

    public Point2D Min { get; }
    public Point2D Max { get; }

    public double MinX => Min.X;
    public double MinY => Min.Y;
    public double MaxX => Max.X;
    public double MaxY => Max.Y;

    public double Width => Max.X - Min.X;
    public double Height => Max.Y - Min.Y;
    public double Area => Width * Height;
    public Point2D Center => new((Min.X + Max.X) * 0.5, (Min.Y + Max.Y) * 0.5);
    public bool IsValid => Max.X >= Min.X && Max.Y >= Min.Y;

    public static BoundingBox2D FromPoints(IEnumerable<Point2D> points)
    {
        if (points == null)
        {
            throw new ArgumentNullException(nameof(points));
        }

        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;
        bool hasAny = false;

        foreach (Point2D pt in points)
        {
            hasAny = true;
            if (pt.X < minX) minX = pt.X;
            if (pt.Y < minY) minY = pt.Y;
            if (pt.X > maxX) maxX = pt.X;
            if (pt.Y > maxY) maxY = pt.Y;
        }

        if (!hasAny)
        {
            throw new ArgumentException("Cannot compute bounding box for empty sequence of points.", nameof(points));
        }

        return new BoundingBox2D(minX, minY, maxX, maxY);
    }

    public bool Contains(Point2D point)
    {
        return point.X >= MinX && point.X <= MaxX &&
               point.Y >= MinY && point.Y <= MaxY;
    }

    public bool Contains(BoundingBox2D other)
    {
        return other.MinX >= MinX && other.MaxX <= MaxX &&
               other.MinY >= MinY && other.MaxY <= MaxY;
    }

    public bool Intersects(BoundingBox2D other)
    {
        return !(other.MinX > MaxX || other.MaxX < MinX ||
                 other.MinY > MaxY || other.MaxY < MinY);
    }

    public BoundingBox2D Union(BoundingBox2D other)
    {
        return new BoundingBox2D(
            Math.Min(MinX, other.MinX),
            Math.Min(MinY, other.MinY),
            Math.Max(MaxX, other.MaxX),
            Math.Max(MaxY, other.MaxY));
    }

    public BoundingBox2D? Intersection(BoundingBox2D other)
    {
        if (!Intersects(other))
        {
            return null;
        }

        return new BoundingBox2D(
            Math.Max(MinX, other.MinX),
            Math.Max(MinY, other.MinY),
            Math.Min(MaxX, other.MaxX),
            Math.Min(MaxY, other.MaxY));
    }

    public BoundingBox2D Inflate(double deltaX, double deltaY)
    {
        return new BoundingBox2D(MinX - deltaX, MinY - deltaY, MaxX + deltaX, MaxY + deltaY);
    }

    public BoundingBox2D Deflate(double fraction)
    {
        double dx = Width * fraction * 0.5;
        double dy = Height * fraction * 0.5;
        return new BoundingBox2D(MinX + dx, MinY + dy, MaxX - dx, MaxY - dy);
    }

    public static bool operator ==(BoundingBox2D left, BoundingBox2D right) => left.Equals(right);
    public static bool operator !=(BoundingBox2D left, BoundingBox2D right) => !left.Equals(right);

    public bool Equals(BoundingBox2D other) => Min.Equals(other.Min) && Max.Equals(other.Max);
    public override bool Equals(object? obj) => obj is BoundingBox2D other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return (Min.GetHashCode() * 397) ^ Max.GetHashCode();
        }
    }

    public override string ToString() => $"[Min: {Min}, Max: {Max}]";
}
