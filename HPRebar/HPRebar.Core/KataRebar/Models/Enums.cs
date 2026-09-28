namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// Origin reference for calculating cutoff lengths of top additional bars.
/// </summary>
public enum KataCutoffOrigin
{
    /// <summary>L measured from column clear face ("L từ mép cột").</summary>
    FromColumnFace = 0,

    /// <summary>L measured from column centerline ("L từ tâm cột").</summary>
    FromColumnCenter = 1
}

/// <summary>
/// Shape classification for stirrup components.
/// </summary>
public enum KataStirrupShapeType
{
    /// <summary>Closed rectangular outer hoop (Đai □).</summary>
    ClosedHoop = 0,

    /// <summary>Open cap stirrup (Đai U).</summary>
    CapStirrup = 1,

    /// <summary>Cross-tie or horizontal tie (Đai C).</summary>
    CrossTie = 2
}

/// <summary>
/// Structural role and classification of reinforcing bars in Kata beam layout.
/// </summary>
public enum KataBarRole
{
    MainTop = 1,
    MainBottom = 2,
    ExtraTop = 3,
    ExtraBottom = 4,
    SideBar = 5,
    CrossTie = 6,
    StirrupClosed = 7,
    StirrupCap = 8
}
