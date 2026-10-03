using System;
using System.Collections.Generic;
using HPRebar.Core.FoundationRebar.Models;

namespace HPRebar.Core.FoundationRebar.Calculators;

/// <summary>
/// One edge of the footing's bottom outline (mm). <see cref="Length"/> is the edge's own length as the model
/// reports it; it only ranks straight edges against each other.
/// </summary>
public sealed record FoundationOutlineEdge(Point3 Start, Point3 End, bool IsStraight, double Length);

/// <summary>
/// The footing's plan rectangle: <see cref="Origin"/> is the bottom corner where both local coordinates are
/// smallest, <see cref="LocalX"/> runs along <see cref="Length"/> and <see cref="LocalY"/> along <see cref="Width"/>.
/// </summary>
public sealed record FoundationPlanFrame(Point3 Origin, Vector3 LocalX, Vector3 LocalY, double Length, double Width);

/// <summary>
/// Fits a rectangle around a footing's bottom outline, turned to follow its longest straight edge,
/// so a rotated footing is meshed along its own sides rather than the project axes.
/// </summary>
public static class FoundationPlanFrameCalculator
{
    /// <summary>
    /// A straight edge whose unit direction has a smaller horizontal part than this is too steep to set the plan
    /// direction.
    /// </summary>
    private const double MinHorizontalComponent = 0.01;

    private const double SignTolerance = 1e-6;

    /// <summary>The frame turned to the longest straight edge, around every edge end point.</summary>
    /// <exception cref="ArgumentException">The outline has no edges.</exception>
    public static FoundationPlanFrame Compute(
        IReadOnlyList<FoundationOutlineEdge> bottomEdges, Point3 referenceOrigin, double bottomZ)
    {
        if (bottomEdges is null)
        {
            throw new ArgumentNullException(nameof(bottomEdges));
        }

        if (bottomEdges.Count == 0)
        {
            throw new ArgumentException("The bottom outline has no edges.", nameof(bottomEdges));
        }

        var points = new List<Point3>(2 * bottomEdges.Count);
        foreach (var edge in bottomEdges)
        {
            points.Add(edge.Start);
            points.Add(edge.End);
        }

        return Fit(DominantDirection(bottomEdges), points, referenceOrigin, bottomZ);
    }

    /// <summary>
    /// The horizontal direction of the longest straight edge (the first one on equal lengths), turned to point to
    /// +X, or to +Y when it is parallel to Y; +X when no straight edge is horizontal enough.
    /// </summary>
    public static Vector3 DominantDirection(IEnumerable<FoundationOutlineEdge> edges)
    {
        if (edges is null)
        {
            throw new ArgumentNullException(nameof(edges));
        }

        var dominant = Vector3.UnitX;
        double longest = 0.0;
        foreach (var edge in edges)
        {
            if (!edge.IsStraight || edge.Length <= longest)
            {
                continue;
            }

            var direction = (edge.End - edge.Start).Normalize();
            var horizontal = new Vector3(direction.X, direction.Y, 0.0);
            if (horizontal.Length > MinHorizontalComponent)
            {
                longest = edge.Length;
                dominant = horizontal.Normalize();
            }
        }

        bool pointsBackwards =
            dominant.X < -SignTolerance || (Math.Abs(dominant.X) <= SignTolerance && dominant.Y < 0);
        return pointsBackwards ? -dominant : dominant;
    }

    /// <summary>
    /// The smallest rectangle along the horizontal part of <paramref name="direction"/> holding every point,
    /// its corner at <paramref name="bottomZ"/>.
    /// </summary>
    /// <exception cref="ArgumentException">There are no points, or the direction has no horizontal part.</exception>
    public static FoundationPlanFrame Fit(
        Vector3 direction, IReadOnlyList<Point3> points, Point3 referenceOrigin, double bottomZ)
    {
        if (points is null)
        {
            throw new ArgumentNullException(nameof(points));
        }

        if (points.Count == 0)
        {
            throw new ArgumentException("A frame needs at least one point.", nameof(points));
        }

        var localX = new Vector3(direction.X, direction.Y, 0.0).Normalize();
        if (localX == Vector3.Zero)
        {
            throw new ArgumentException("The frame direction has no horizontal part.", nameof(direction));
        }

        var localY = Vector3.UnitZ.Cross(localX).Normalize();
        double minU = double.MaxValue, maxU = double.MinValue;
        double minV = double.MaxValue, maxV = double.MinValue;
        foreach (var point in points)
        {
            var offset = point - referenceOrigin;
            double u = offset.Dot(localX);
            double v = offset.Dot(localY);
            minU = Math.Min(minU, u);
            maxU = Math.Max(maxU, u);
            minV = Math.Min(minV, v);
            maxV = Math.Max(maxV, v);
        }

        var corner = referenceOrigin + (localX * minU) + (localY * minV);
        return new FoundationPlanFrame(
            new Point3(corner.X, corner.Y, bottomZ), localX, localY, maxU - minU, maxV - minV);
    }
}
