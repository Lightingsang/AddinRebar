using HPAutoCad.Core.HPGeoLink.Model;

namespace HPAutoCad.Core.HPGeoLink.Geometry;

/// <summary>
/// Flattens an AutoCAD lightweight-polyline segment list (vertex + bulge) into straight chords so a KML
/// LineString/LinearRing follows the arc. Bulge = tan(θ/4) of the included angle. The chord count comes from
/// the sagitta: no chord deviates from its arc by more than <c>tolerance</c> (drawing units), so a 500 m road
/// curve and a 5 m fillet both stay within the same few millimetres instead of a fixed count that leaves
/// metres on large radii.
/// </summary>
public static class BulgeTessellator
{
    /// <summary>Default chord deviation in drawing units — 5 mm for a metre drawing.</summary>
    public const double DefaultToleranceM = 0.005;
    public const int MinSegmentsPerArc = 4;
    public const int MaxSegmentsPerArc = 512;

    public readonly record struct Vertex(double X, double Y, double Bulge);

    /// <summary>Chords needed so the sagitta r(1 − cos(θ/2n)) stays under the tolerance, clamped to [4, 512].</summary>
    public static int ChordCount(double radius, double includedAngleRad, double tolerance)
    {
        var angle = Math.Abs(includedAngleRad);
        if (radius <= 0 || angle <= 0 || tolerance <= 0) return MinSegmentsPerArc;
        var ratio = 1 - tolerance / radius;
        if (ratio <= -1) return MinSegmentsPerArc; // tolerance larger than the diameter: anything is within it
        var maxStep = 2 * Math.Acos(Math.Max(-1, Math.Min(1, ratio)));
        if (maxStep <= 0) return MaxSegmentsPerArc;
        var n = (int)Math.Ceiling(angle / maxStep);
        return Math.Clamp(n, MinSegmentsPerArc, MaxSegmentsPerArc);
    }

    /// <summary>Vertices of the flattened polyline (closed rings get the closing arc, but not a repeated first vertex).</summary>
    public static List<PlanePoint> Tessellate(IReadOnlyList<Vertex> vertices, bool closed, double tolerance = DefaultToleranceM)
    {
        var result = new List<PlanePoint>();
        if (vertices.Count == 0) return result;

        var segmentCount = closed ? vertices.Count : vertices.Count - 1;
        for (var i = 0; i < segmentCount; i++)
        {
            var from = vertices[i];
            var to = vertices[(i + 1) % vertices.Count];
            result.Add(new PlanePoint(from.X, from.Y));
            if (Math.Abs(from.Bulge) > 1e-12) AppendArcInterior(result, from, to, tolerance);
        }
        if (!closed) result.Add(new PlanePoint(vertices[^1].X, vertices[^1].Y));
        return result;
    }

    private static void AppendArcInterior(List<PlanePoint> into, Vertex from, Vertex to, double tolerance)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var chord = Math.Sqrt(dx * dx + dy * dy);
        if (chord < 1e-12) return;

        var theta = 4 * Math.Atan(from.Bulge); // included angle, signed (positive = counter-clockwise)
        var radius = chord / (2 * Math.Sin(Math.Abs(theta) / 2));
        var midX = (from.X + to.X) / 2;
        var midY = (from.Y + to.Y) / 2;
        var sagitta = radius * Math.Cos(theta / 2) * Math.Sign(theta);
        // Centre lies on the chord's perpendicular bisector, on the side the bulge sign dictates.
        var nx = -dy / chord;
        var ny = dx / chord;
        var cx = midX + nx * sagitta;
        var cy = midY + ny * sagitta;
        var startAngle = Math.Atan2(from.Y - cy, from.X - cx);

        var segments = ChordCount(radius, theta, tolerance);
        for (var k = 1; k < segments; k++)
        {
            var a = startAngle + theta * k / segments;
            into.Add(new PlanePoint(cx + radius * Math.Cos(a), cy + radius * Math.Sin(a)));
        }
    }
}
