namespace HPAutoCad.Aec.Geometry;

/// <summary>
///     The plan geometry of one entity as a polyline path in millimetres: straight entities verbatim,
///     curves tessellated to the chord tolerance. Closed shapes are rings (last vertex does not repeat
///     the first) and carry area/centroid; open shapes are chains with two ends. Exact lengths and areas
///     measured by AutoCAD are kept beside the tessellation when the reader had them.
/// </summary>
public sealed class PlanShape
{
    /// <summary>Tessellation never exceeds this many chords per arc — a full circle at the finest chord error stays bounded.</summary>
    public const int MaxArcSteps = 720;

    private Seg[]? _segments;

    public PlanShape(IReadOnlyList<Pt> vertices, bool closed, double? exactLengthMm = null, double? exactAreaMm2 = null, bool approximate = false)
    {
        Vertices = vertices;
        Closed = closed && vertices.Count >= 3;
        ExactLengthMm = exactLengthMm;
        ExactAreaMm2 = exactAreaMm2;
        Approximate = approximate;
        Bounds = Box.Of(vertices);
    }

    public IReadOnlyList<Pt> Vertices { get; }

    public bool Closed { get; }

    /// <summary>
    ///     The vertices are not the entity's own: a tessellated curve or a bounding rectangle standing in for text, a block or
    ///     a dimension. Predicates work on them; defect checks that look at individual segments (tiny segment, collinear
    ///     overlap, self-intersection) skip them — a chord is not a drafting error.
    /// </summary>
    public bool Approximate { get; }

    public Box Bounds { get; }

    /// <summary>Length AutoCAD measured (arc lengths exact); null when the reader had no curve to ask.</summary>
    public double? ExactLengthMm { get; }

    public double? ExactAreaMm2 { get; }

    public bool IsPoint => Vertices.Count == 1;

    public Pt Start => Vertices.Count > 0 ? Vertices[0] : Pt.Origin;

    public Pt End => Vertices.Count > 0 ? Vertices[^1] : Pt.Origin;

    /// <summary>Consecutive vertex pairs; a closed ring also has the closing segment.</summary>
    public IReadOnlyList<Seg> Segments
    {
        get
        {
            if (_segments is not null) return _segments;
            if (Vertices.Count < 2) return _segments = [];
            var count = Closed ? Vertices.Count : Vertices.Count - 1;
            var segments = new Seg[count];
            for (var i = 0; i < count; i++) segments[i] = new Seg(Vertices[i], Vertices[(i + 1) % Vertices.Count]);
            return _segments = segments;
        }
    }

    /// <summary>Tessellated length; prefer <see cref="ExactLengthMm"/> when present.</summary>
    public double LengthMm => ExactLengthMm ?? Segments.Sum(s => s.Length);

    public double AreaMm2 => ExactAreaMm2 ?? (Closed ? GeometryMath.AreaXY(Vertices) : 0);

    public Pt Centroid => Closed ? GeometryMath.CentroidXY(Vertices) : Bounds.Center;

    /// <summary>Open chains report both ends; rings and points none.</summary>
    public IReadOnlyList<Pt> Endpoints => Closed || Vertices.Count < 2 ? [] : [Start, End];

    public bool ContainsPointXY(Pt p, double tolerance) => Closed && GeometryMath.ContainsPointXY(Vertices, p, tolerance);

    /// <summary>Shortest plan distance from a point to the boundary (or to the single vertex).</summary>
    public double DistanceToBoundaryXY(Pt p)
    {
        if (Vertices.Count == 1) return Vertices[0].DistanceXY(p);
        var best = double.PositiveInfinity;
        foreach (var s in Segments) best = Math.Min(best, s.DistanceXY(p));
        return best;
    }

    public static PlanShape Point(Pt p) => new([p], false);

    public static PlanShape Segment(Pt a, Pt b, double? exactLength = null) => new([a, b], false, exactLength);

    /// <summary>A bounding rectangle standing in for an entity whose real geometry is not read (text, block, dimension): always approximate.</summary>
    public static PlanShape Rectangle(Box box) =>
        new([box.Min, new Pt(box.Max.X, box.Min.Y, box.Min.Z), new Pt(box.Max.X, box.Max.Y, box.Min.Z), new Pt(box.Min.X, box.Max.Y, box.Min.Z)], true, approximate: true);

    /// <summary>
    ///     Arc from a centre, radius and angles (degrees, counter-clockwise), tessellated so no chord deviates from the arc by more
    ///     than <paramref name="chordErrorMm"/>. A full 360° sweep yields a closed ring.
    /// </summary>
    public static IReadOnlyList<Pt> ArcPoints(Pt center, double radius, double startDegrees, double sweepDegrees, double chordErrorMm, double z = 0)
    {
        if (radius <= 0 || Math.Abs(sweepDegrees) < 1e-9) return [center];
        var sweep = Math.Abs(sweepDegrees) * Math.PI / 180;
        // Sagitta s = r(1 - cos(θ/2)) ≤ e  →  θ ≤ 2·acos(1 - e/r)
        var maxStep = chordErrorMm >= radius ? Math.PI / 2 : 2 * Math.Acos(1 - chordErrorMm / radius);
        var steps = Math.Max(4, (int)Math.Ceiling(sweep / Math.Max(maxStep, 1e-6)));
        steps = Math.Min(steps, MaxArcSteps);
        var direction = Math.Sign(sweepDegrees);
        var full = Math.Abs(Math.Abs(sweepDegrees) - 360) < 1e-9;
        var points = new List<Pt>(steps + 1);
        var start = startDegrees * Math.PI / 180;
        var count = full ? steps : steps + 1;
        for (var i = 0; i < count; i++)
        {
            var angle = start + direction * sweep * i / steps;
            points.Add(new Pt(center.X + radius * Math.Cos(angle), center.Y + radius * Math.Sin(angle), z));
        }

        return points;
    }
}
