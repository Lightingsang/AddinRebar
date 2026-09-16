using System.Text.Json.Serialization;
using HPAutoCad.Aec.Classification;
using HPAutoCad.Aec.Geometry;

namespace HPAutoCad.Aec.Structural;

/// <summary>The kinds the structural tools work with; the tool argument <c>kinds</c> uses these names.</summary>
public static class MemberKind
{
    public const string Column = "column";
    public const string Beam = "beam";
    public const string Wall = "wall";
    public const string Slab = "slab";
    public const string Opening = "opening";

    public static readonly IReadOnlyList<string> All = [Column, Beam, Wall, Slab, Opening];

    /// <summary>Position in <see cref="All"/> — the order schedules and summaries list kinds in.</summary>
    public static int Rank(string kind) => Math.Max(0, Array.IndexOf([Column, Beam, Wall, Slab, Opening], kind));

    /// <summary>Supports a beam end may land on.</summary>
    public static readonly IReadOnlyList<string> Supports = [Column, Wall, Beam];

    /// <summary>A run-like kind is scheduled by its length (L 5600) even when drawn as a closed outline.</summary>
    public static bool IsRun(string kind) => kind is Beam or Wall;

    public static string? FromAecType(string aecType) => aecType switch
    {
        AecType.StructuralColumn => Column,
        AecType.StructuralBeam => Beam,
        AecType.StructuralWall => Wall,
        AecType.StructuralSlab => Slab,
        AecType.StructuralOpening => Opening,
        _ => null,
    };

    /// <summary>The mark prefix a kind gets by default: C1, B1, W1, S1, O1.</summary>
    public static string DefaultPrefix(string kind) => kind switch
    {
        Column => "C",
        Beam => "B",
        Wall => "W",
        Slab => "S",
        Opening => "O",
        _ => "M",
    };
}

/// <summary>
///     A classified structural member with the numbers the structural tools reason about: the footprint (width × depth) of a
///     column / opening / slab, the axis and length of a beam or wall, the centre, and the plan shape kept for geometry checks.
///     No capacity, no material — the drawing does not know them.
/// </summary>
public sealed record StructuralMember(
    string Handle,
    string Kind,
    string AecType,
    string Layer,
    double Confidence,
    Pt CenterMm,
    Box BoundsMm,
    double? WidthMm,
    double? DepthMm,
    double? LengthMm,
    double? AreaMm2,
    double? OrientationDeg,
    string? Mark)
{
    /// <summary>The mark came from the block reference's own MARK attribute (the member's handle) or from a TEXT beside it.</summary>
    public const string MarkFromAttribute = "attribute";
    public const string MarkFromText = "text";

    /// <summary>The attribute tag a block member carries its mark in.</summary>
    public const string MarkAttributeTag = "MARK";

    [JsonIgnore]
    public required PlanShape Shape { get; init; }

    /// <summary>The run a beam/wall follows, or the long axis of a footprint; null for a square or a circle.</summary>
    [JsonIgnore]
    public Seg? Axis { get; init; }

    /// <summary>A round column: the section reads Ø400, not 400×400.</summary>
    public bool IsCircular { get; init; }

    /// <summary>The entity that carries <see cref="Mark"/>: the member itself (attribute) or the TEXT beside it. Null when unmarked.</summary>
    public string? MarkHandle { get; init; }

    /// <summary><see cref="MarkFromAttribute"/> or <see cref="MarkFromText"/>; null when unmarked.</summary>
    public string? MarkSource { get; init; }

    /// <summary>The section as a builder writes it: 400×600 or Ø400 for footprints, "L 5600" for a beam or wall whatever it is drawn as.</summary>
    public string Section =>
        MemberKind.IsRun(Kind) ? LengthMm is { } l ? $"L {l:0}" : "?"
        : IsCircular && WidthMm is { } d ? $"Ø{d:0}"
        : WidthMm is { } w && DepthMm is { } dp ? $"{w:0}×{dp:0}" : "?";

    public static StructuralMember? From(AecObject o, ShapeMetrics.Metrics m)
    {
        var kind = MemberKind.FromAecType(o.AecType);
        if (kind is null || o.Shape is null) return null;
        var runLike = MemberKind.IsRun(kind);
        // A footprint only has an axis when one side dominates: a square column or a round one has none to align or connect along.
        var axis = !m.Closed || runLike || m.AspectRatio >= ShapeMetrics.DominantDirectionRatio ? ShapeMetrics.MainAxis(o.Shape) : null;
        // A footprint (drawn outline, or a block reference's stand-in box) has width × depth; a run has a length. A run-like kind drawn closed
        // (a beam outline) keeps its long side as the length too.
        var footprint = m.Closed || (!runLike && m.WidthMm is not null);
        var attributeMark = AttributeMark(o);
        return new StructuralMember(o.Handle, kind, o.AecType, o.Layer, o.Confidence, m.CentroidMm ?? o.Shape.Centroid, o.Shape.Bounds,
            footprint ? m.WidthMm : null, footprint ? m.DepthMm : null, runLike ? m.LengthMm ?? m.DepthMm : footprint ? null : m.LengthMm,
            m.Closed ? m.AreaMm2 : null, m.OrientationDeg, attributeMark)
        { Shape = o.Shape, Axis = axis, IsCircular = m.IsCircular, MarkHandle = attributeMark is null ? null : o.Handle, MarkSource = attributeMark is null ? null : MarkFromAttribute };
    }

    /// <summary>A block member's own MARK attribute, whatever it says: it is the member's mark by construction, never someone else's text.</summary>
    private static string? AttributeMark(AecObject o)
    {
        if (!o.Properties.TryGetValue("attributes", out var raw) || raw is not IDictionary<string, string> attributes) return null;
        return attributes.TryGetValue(MarkAttributeTag, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;
    }
}
