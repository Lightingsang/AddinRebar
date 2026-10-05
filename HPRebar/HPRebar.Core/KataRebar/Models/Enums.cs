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
    StirrupCap = 8,

    /// <summary>Hanger bar ("vai bò") under a beam framing into the span or a stub column standing on it.</summary>
    HangerBar = 9
}

/// <summary>
/// How the C ties (side-bar ties, layer spacer ties) are spaced along the beam: the option group
/// "Khoảng cách đai gia cường" of sheet Dam, whose linked cell I8 holds 1 or 2.
/// </summary>
public enum KataTieSpacingMode
{
    /// <summary>I8 = 2 ("Bố trí đều với"): evenly at the spacing of cell J7.</summary>
    Uniform = 0,

    /// <summary>I8 = 1 ("Giống đai ngoài"): one tie at every outer hoop.</summary>
    LikeHoops = 1
}
