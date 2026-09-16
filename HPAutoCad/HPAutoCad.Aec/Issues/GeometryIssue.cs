using HPAutoCad.Aec.Geometry;

namespace HPAutoCad.Aec.Issues;

public static class IssueSeverity
{
    public const string Critical = "critical";
    public const string Warning = "warning";
    public const string Info = "info";
}

/// <summary>Kinds <c>detect_geometry_issues</c> can look for; the tool argument uses these snake_case names.</summary>
public static class GeometryIssueType
{
    public const string Duplicate = "duplicate";
    public const string NearDuplicate = "near_duplicate";
    public const string OverlappingSegments = "overlapping_segments";
    public const string TinySegment = "tiny_segment";
    public const string ZeroLength = "zero_length";
    public const string OpenPolyline = "open_polyline";
    public const string EndpointGap = "endpoint_gap";
    public const string SelfIntersection = "self_intersection";
    public const string InvalidGeometry = "invalid_geometry";

    public static readonly IReadOnlyList<string> All =
        [Duplicate, NearDuplicate, OverlappingSegments, TinySegment, ZeroLength, OpenPolyline, EndpointGap, SelfIntersection, InvalidGeometry];
}

/// <summary>
///     One defect, located and measured, so the AI never re-derives geometry to explain it. Ids are
///     sequential per run (<c>GEO-0001</c>); handles list every entity involved.
/// </summary>
public sealed record GeometryIssue(
    string IssueId,
    string Type,
    string Severity,
    IReadOnlyList<string> Handles,
    Pt? LocationMm,
    double? ValueMm,
    double? ToleranceMm,
    string Description,
    string? SuggestedAction = null);
