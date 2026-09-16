using System.Text.Json.Serialization;
using HPAutoCad.Aec.Geometry;

namespace HPAutoCad.Aec.Model;

/// <summary>
///     The plan geometry of an entity as the AI sees it (detail mode only). <see cref="Vertices"/> is capped at
///     <see cref="MaxVertices"/> so a tessellated circle cannot fill the response; <see cref="VertexCount"/> tells the real
///     number and <see cref="Approximate"/> whether the vertices are the entity's own.
/// </summary>
public sealed record EntityGeometry(IReadOnlyList<Pt> Vertices, int VertexCount, bool Closed, bool Approximate, double LengthMm, double? AreaMm2, Pt? Centroid)
{
    public const int MaxVertices = 64;

    public bool VerticesTruncated => VertexCount > Vertices.Count;
}

/// <summary>
///     One entity read through the bridge's transaction, in millimetres, identified by its handle. The
///     plain properties are the summary the AI gets by default; <see cref="Shape"/> stays in memory for the
///     engine and is exposed as <see cref="Geometry"/> only when a tool asks for detail. Optional members
///     serialise only when set (the bridge's JSON options drop nulls).
/// </summary>
public sealed class AecEntityRecord
{
    public required string Handle { get; init; }

    /// <summary>DXF name: LINE, LWPOLYLINE, CIRCLE, ARC, INSERT, TEXT, MTEXT, DIMENSION, HATCH, …</summary>
    public required string Type { get; init; }

    public required string Layer { get; init; }

    /// <summary>"ByLayer", "ByBlock" or the ACI index / true colour as text.</summary>
    public string? Color { get; init; }

    public string? Linetype { get; init; }

    public string? Lineweight { get; init; }

    /// <summary>"Model" or the layout name.</summary>
    public string? Space { get; init; }

    public bool? Visible { get; init; }

    public Box? BoundsMm { get; init; }

    public double? LengthMm { get; init; }

    public double? AreaMm2 { get; init; }

    public string? Text { get; init; }

    public string? BlockName { get; init; }

    public IReadOnlyDictionary<string, string>? Attributes { get; init; }

    /// <summary>Block insertion point / text position / circle centre, when the entity has one natural point.</summary>
    public Pt? PositionMm { get; init; }

    public EntityGeometry? Geometry { get; set; }

    [JsonIgnore]
    public PlanShape? Shape { get; init; }

    /// <summary>Why no geometry could be read (custom object, empty block, infinite extents) — set instead of <see cref="Shape"/>.</summary>
    public string? GeometryNote { get; init; }

    public EntityGeometry? ToGeometry() => Shape is null
        ? null
        : new EntityGeometry(Shape.Vertices.Take(EntityGeometry.MaxVertices).Select(v => v.Rounded()).ToArray(), Shape.Vertices.Count, Shape.Closed, Shape.Approximate,
            Math.Round(Shape.LengthMm, 1), Shape.Closed ? Math.Round(Shape.AreaMm2, 1) : null, Shape.Closed ? Shape.Centroid.Rounded() : null);
}
