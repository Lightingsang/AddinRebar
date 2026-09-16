namespace HPAutoCad.Aec.Spatial;

/// <summary>
///     Plan-view relations between a source shape and a target shape. Names match the tool argument
///     (<c>snake_case</c> accepted, see <see cref="SpatialRelations.Parse"/>).
/// </summary>
public enum SpatialRelation
{
    /// <summary>Source lies inside the (closed) target.</summary>
    Within,
    /// <summary>Source is closed and the target lies inside it.</summary>
    Contains,
    /// <summary>Boundaries cross or touch, or one lies inside the other.</summary>
    Intersects,
    /// <summary>Boundaries cross at a point strictly inside both segments.</summary>
    Crosses,
    /// <summary>Share a boundary run (collinear overlap) or, for two rings, partially overlap in area.</summary>
    Overlaps,
    /// <summary>Boundaries meet within the tolerance without crossing or containment.</summary>
    Touches,
    /// <summary>The closest target per source (distance reported).</summary>
    Nearest,
    /// <summary>Every target within <c>maxDistance</c> (distance reported).</summary>
    DistanceTo,
    /// <summary>Source bounding box inside the target bounding box.</summary>
    InsideBbox,
    /// <summary>Source inside the target ring — the target must be closed.</summary>
    InsidePolygon,
}

public static class SpatialRelations
{
    public static readonly IReadOnlyList<string> Names = ["within", "contains", "intersects", "crosses", "overlaps", "touches", "nearest", "distance_to", "inside_bbox", "inside_polygon"];

    public static bool TryParse(string? text, out SpatialRelation relation)
    {
        relation = SpatialRelation.Intersects;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var key = text.Trim().Replace("-", "_").Replace(" ", "_").ToLowerInvariant();
        switch (key)
        {
            case "within": relation = SpatialRelation.Within; return true;
            case "contains": relation = SpatialRelation.Contains; return true;
            case "intersects": case "intersect": relation = SpatialRelation.Intersects; return true;
            case "crosses": case "cross": relation = SpatialRelation.Crosses; return true;
            case "overlaps": case "overlap": relation = SpatialRelation.Overlaps; return true;
            case "touches": case "touch": relation = SpatialRelation.Touches; return true;
            case "nearest": relation = SpatialRelation.Nearest; return true;
            case "distance_to": case "distanceto": case "distance": relation = SpatialRelation.DistanceTo; return true;
            case "inside_bbox": case "insidebbox": relation = SpatialRelation.InsideBbox; return true;
            case "inside_polygon": case "insidepolygon": relation = SpatialRelation.InsidePolygon; return true;
            default: return false;
        }
    }

    public static string Name(SpatialRelation relation) => relation switch
    {
        SpatialRelation.DistanceTo => "distance_to",
        SpatialRelation.InsideBbox => "inside_bbox",
        SpatialRelation.InsidePolygon => "inside_polygon",
        _ => relation.ToString().ToLowerInvariant(),
    };
}
