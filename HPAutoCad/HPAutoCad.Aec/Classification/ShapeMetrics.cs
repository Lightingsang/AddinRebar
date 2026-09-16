using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Model;

namespace HPAutoCad.Aec.Classification;

/// <summary>
///     The few numbers a classification rule and an AEC property block need from a shape: the plan size
///     of a footprint (short side × long side, rotation-aware for rectangles), the length and direction of a
///     run, and the axis a relationship test aligns on.
/// </summary>
public static class ShapeMetrics
{
    /// <summary>Metrics of one entity in millimetres / degrees; nulls where the shape does not have the measure.</summary>
    public sealed record Metrics(double? WidthMm, double? DepthMm, double? LengthMm, double? AreaMm2, double? OrientationDeg, Pt? CentroidMm, bool Closed, bool IsCircular)
    {
        /// <summary>Long side / short side of a footprint; 1 for a square or a circle, null for a run.</summary>
        public double? AspectRatio => WidthMm is > 0 && DepthMm is not null ? DepthMm / WidthMm : null;

        /// <summary>The size a rule compares against: the long side of a footprint, the length of a run, the long side of a stand-in box.</summary>
        public double? SizeMm => Closed ? DepthMm : LengthMm ?? DepthMm;
    }

    public static Metrics Of(AecEntityRecord record)
    {
        var shape = record.Shape;
        if (shape is null)
        {
            var box = record.BoundsMm;
            return new Metrics(box?.ShortSideXY, box?.LongSideXY, null, record.AreaMm2, null, box?.Center, false, false);
        }

        if (shape.IsPoint) return new Metrics(0, 0, 0, 0, null, shape.Start, false, false);

        // A block reference, text or dimension is read as its bounding rectangle: sized, but not a drawn footprint — a rule that
        // asks for a closed outline must not accept it.
        if (shape.Approximate && IsStandIn(record.Type))
            return new Metrics(shape.Bounds.ShortSideXY, shape.Bounds.LongSideXY, null, null, null, shape.Bounds.Center, false, false);

        if (shape.Closed)
        {
            var circular = string.Equals(record.Type, "CIRCLE", StringComparison.OrdinalIgnoreCase);
            var (width, depth, orientation) = FootprintSides(shape);
            if (circular)
            {
                var diameter = shape.Bounds.LongSideXY;
                return new Metrics(diameter, diameter, null, record.AreaMm2 ?? shape.AreaMm2, null, shape.Centroid, true, true);
            }

            return new Metrics(width, depth, null, record.AreaMm2 ?? shape.AreaMm2, orientation, shape.Centroid, true, false);
        }

        var axis = MainAxis(shape);
        return new Metrics(null, null, record.LengthMm ?? shape.LengthMm, null, axis is null ? null : GeometryMath.DirectionDegreesXY(axis.Value) % 180, shape.Bounds.Center, false, false);
    }

    /// <summary>Entity types whose shape is a bounding rectangle standing in for geometry the reader does not trace.</summary>
    public static bool IsStandIn(string dxfType) => dxfType.ToUpperInvariant() is "INSERT" or "TEXT" or "MTEXT" or "DIMENSION" or "LEADER" or "MULTILEADER" or "ATTDEF" or "TABLE";

    /// <summary>A box counts as having a dominant direction when its long side exceeds the short side by this factor.</summary>
    public const double DominantDirectionRatio = 1.5;

    /// <summary>
    ///     Short and long side of a footprint plus the direction of the long side: from the edges when the ring is a
    ///     quadrilateral (rotated rectangles measure right), from the bounding box otherwise.
    /// </summary>
    public static (double Width, double Depth, double? OrientationDeg) FootprintSides(PlanShape ring)
    {
        if (ring.Vertices.Count == 4 && !ring.Approximate)
        {
            var e0 = ring.Segments[0];
            var e1 = ring.Segments[1];
            var a = e0.LengthXY;
            var b = e1.LengthXY;
            var longest = a >= b ? e0 : e1;
            return (Math.Min(a, b), Math.Max(a, b), GeometryMath.DirectionDegreesXY(longest) % 180);
        }

        var box = ring.Bounds;
        return (box.ShortSideXY, box.LongSideXY, box.Width >= box.Height ? 0 : 90);
    }

    /// <summary>The axis a linear member runs along: start→end of a chain, the long edge of a rectangle; null for circles and blobs.</summary>
    public static Seg? MainAxis(PlanShape shape)
    {
        if (shape.IsPoint) return null;
        if (!shape.Closed) return new Seg(shape.Start, shape.End);
        if (shape.Vertices.Count == 4 && !shape.Approximate)
        {
            var e0 = shape.Segments[0];
            var e1 = shape.Segments[1];
            var longest = e0.LengthXY >= e1.LengthXY ? e0 : e1;
            var opposite = e0.LengthXY >= e1.LengthXY ? shape.Segments[2] : shape.Segments[3];
            // centre line of the rectangle, along its long side
            return new Seg(Pt.Mid(longest.A, opposite.B), Pt.Mid(longest.B, opposite.A));
        }

        var box = shape.Bounds;
        if (box.LongSideXY <= box.ShortSideXY * DominantDirectionRatio) return null; // no dominant direction
        var c = box.Center;
        return box.Width >= box.Height ? new Seg(new Pt(box.Min.X, c.Y), new Pt(box.Max.X, c.Y)) : new Seg(new Pt(c.X, box.Min.Y), new Pt(c.X, box.Max.Y));
    }
}
