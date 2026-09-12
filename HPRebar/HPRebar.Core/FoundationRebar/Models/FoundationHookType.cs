namespace HPRebar.Core.FoundationRebar.Models;

/// <summary>
/// Hook types for foundation rebar anchorage.
/// </summary>
public enum FoundationHookType
{
    /// <summary>Straight bar without anchorage hooks.</summary>
    None = 0,

    /// <summary>Standard 90-degree bend hook.</summary>
    Hook90Degrees = 1,

    /// <summary>Alias for Hook90Degrees.</summary>
    Hook90 = 1,

    /// <summary>Alias for upward 90-degree bend hook.</summary>
    Hook90Up = 1,

    /// <summary>Alias for downward 90-degree bend hook.</summary>
    Hook90Down = 2
}

/// <summary>
/// Identification of the 4 vertical reinforcement layers in a foundation mat.
/// </summary>
public enum FoundationBarLayer
{
    /// <summary>Layer 1: Bottom Mat, Direction X (outermost bottom).</summary>
    BottomX = 1,

    /// <summary>Layer 2: Bottom Mat, Direction Y (rests directly on Layer 1).</summary>
    BottomY = 2,

    /// <summary>Layer 3: Top Mat, Direction Y (hung under Layer 4).</summary>
    TopY = 3,

    /// <summary>Layer 4: Top Mat, Direction X (outermost top).</summary>
    TopX = 4
}
