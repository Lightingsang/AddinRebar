namespace HPRebar.BeamRebar.Model;

/// <summary>
/// Cross-section geometry classification of a structural framing beam element.
/// </summary>
public enum BeamSectionStyle
{
    /// <summary>Unsupported cross-section (e.g. non-rectangular, chamfered, tapered, curved, or multi-solid).</summary>
    Other = 0,

    /// <summary>Prismatic rectangular concrete beam.</summary>
    Rectangle = 1
}
