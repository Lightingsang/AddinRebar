namespace HPRebar.Core.BeamRebar.Models;

/// <summary>Classification of structural bearing supports under a continuous beam.</summary>
public enum SupportType
{
    None = 0,
    Column = 1,
    InteriorColumn = 1,
    Wall = 2,
    Girder = 3,
    CantileverLeft = 4,
    CantileverRight = 5,
    ExteriorColumn = 6,
    CantileverEnd = 7
}

/// <summary>Stirrup distribution layout algorithm across a clear span.</summary>
public enum StirrupLayout
{
    /// <summary>Uniform spacing throughout the clear span.</summary>
    Uniform = 0,

    /// <summary>Dense support zones (Ln/4) and sparse midspan zone (Ln/2).</summary>
    ThreeZoneL4 = 1,

    /// <summary>Dense support zones (Ln/3) and sparse midspan zone (Ln/3).</summary>
    ThreeZoneL3 = 2
}

/// <summary>Alias for StirrupLayout.</summary>
public enum StirrupDistributionType
{
    Uniform = 0,
    ThreeZoneL4 = 1,
    ThreeZoneL3 = 2
}

/// <summary>End anchorage hook bend type for longitudinal reinforcing bars.</summary>
public enum EndAnchorageType
{
    None = 0,
    Hook90Down = 1,
    Hook90Up = 2,
    Hook135 = 3,
    Hook180 = 4
}

/// <summary>Structural role and classification of reinforcing bars.</summary>
public enum BarType
{
    MainTop = 1,
    MainBottom = 2,
    AdditionalTop = 3,
    AdditionalBottom = 4,
    SideSkin = 5,
    CrossTie = 6,
    HangingStirrup = 7,
    DiagonalTie = 8
}

/// <summary>Standard rebar hook bend angles in degrees.</summary>
public enum HookAngle
{
    None = 0,
    Hook90 = 90,
    Hook135 = 135,
    Hook180 = 180
}

/// <summary>Overhang cantilever placement on a continuous beam assembly.</summary>
public enum CantileverPosition
{
    None = 0,
    Left = 1,
    Right = 2,
    Both = 3
}

/// <summary>Side where a secondary framing beam intersects the primary beam.</summary>
public enum IntersectionSide
{
    Both = 0,
    Left = 1,
    Right = 2
}

/// <summary>Hook configurations for transverse anti-buckling cross-ties.</summary>
public enum CrossTieHookType
{
    /// <summary>90° hook at one end, 135° hook at the other end (recommended for ease of placement).</summary>
    Hook90And135 = 0,

    /// <summary>135° seismic hook at both ends.</summary>
    Hook135And135 = 1,

    /// <summary>180° hook at both ends.</summary>
    Hook180And180 = 2
}
