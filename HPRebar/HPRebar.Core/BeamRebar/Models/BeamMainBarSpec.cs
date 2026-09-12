namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// Detailing specification for continuous top and bottom longitudinal reinforcement.
/// All lengths and spacings are in millimetres.
/// </summary>
public sealed record BeamMainBarSpec
{
    public BeamMainBarSpec()
    {
    }

    public BeamMainBarSpec(
        int topCount = 2,
        double topDiameter = 20.0,
        int bottomCount = 2,
        double bottomDiameter = 20.0,
        double? leftHookLength = null,
        double? rightHookLength = null,
        double maxStockLength = 11700.0,
        double lapFactor = 40.0,
        bool enableStagger = true,
        double staggerOffsetRatio = 1.3,
        double? LeftHookLengthMm = null,
        double? RightHookLengthMm = null,
        double? MaxStockLengthMm = null,
        double? LapLengthMultiplier = null,
        bool? StaggerSplice = null)
    {
        TopCount = topCount;
        TopDiameter = topDiameter;
        BottomCount = bottomCount;
        BottomDiameter = bottomDiameter;
        TopStartHookLength = LeftHookLengthMm ?? leftHookLength ?? 0.0;
        TopEndHookLength = RightHookLengthMm ?? rightHookLength ?? 0.0;
        BottomStartHookLength = LeftHookLengthMm ?? leftHookLength ?? 0.0;
        BottomEndHookLength = RightHookLengthMm ?? rightHookLength ?? 0.0;
        MaxStockLength = MaxStockLengthMm ?? maxStockLength;
        LapFactor = LapLengthMultiplier ?? lapFactor;
        EnableStagger = StaggerSplice ?? enableStagger;
        StaggerOffsetRatio = staggerOffsetRatio;
    }

    // --- Top Main Bars ---
    /// <summary>Number of top continuous bars (minimum 2 to engage stirrup top corners).</summary>
    public int TopCount { get; init; } = 2;

    /// <summary>Top bar diameter in millimetres (e.g. 18, 20, 22).</summary>
    public double TopDiameter { get; init; } = 20.0;

    /// <summary>Top concrete cover in millimetres.</summary>
    public double TopCover { get; init; } = 25.0;

    /// <summary>Exterior start anchorage type for top bars.</summary>
    public EndAnchorageType TopStartAnchorage { get; init; } = EndAnchorageType.Hook90Down;

    /// <summary>Hook length at start exterior support (mm, 0 for auto h - 2*Cover).</summary>
    public double TopStartHookLength { get; init; }

    /// <summary>Exterior end anchorage type for top bars.</summary>
    public EndAnchorageType TopEndAnchorage { get; init; } = EndAnchorageType.Hook90Down;

    /// <summary>Hook length at end exterior support (mm, 0 for auto h - 2*Cover).</summary>
    public double TopEndHookLength { get; init; }

    /// <summary>Revit RebarBarType name for top bars.</summary>
    public string TopBarTypeName { get; init; } = string.Empty;

    // --- Bottom Main Bars ---
    /// <summary>Number of bottom continuous bars (minimum 2 to engage stirrup bottom corners).</summary>
    public int BottomCount { get; init; } = 2;

    /// <summary>Bottom bar diameter in millimetres (e.g. 18, 20, 22).</summary>
    public double BottomDiameter { get; init; } = 20.0;

    /// <summary>Bottom concrete cover in millimetres.</summary>
    public double BottomCover { get; init; } = 25.0;

    /// <summary>Exterior start anchorage type for bottom bars.</summary>
    public EndAnchorageType BottomStartAnchorage { get; init; } = EndAnchorageType.Hook90Up;

    /// <summary>Hook length at start exterior support (mm, 0 for auto h - 2*Cover).</summary>
    public double BottomStartHookLength { get; init; }

    /// <summary>Exterior end anchorage type for bottom bars.</summary>
    public EndAnchorageType BottomEndAnchorage { get; init; } = EndAnchorageType.Hook90Up;

    /// <summary>Hook length at end exterior support (mm, 0 for auto h - 2*Cover).</summary>
    public double BottomEndHookLength { get; init; }

    /// <summary>Revit RebarBarType name for bottom bars.</summary>
    public string BottomBarTypeName { get; init; } = string.Empty;

    // --- Splicing & Division Rules ---
    /// <summary>Maximum stock/commercial bar length (mm, default: 11700 mm = 11.7 m).</summary>
    public double MaxStockLength { get; init; } = 11700.0;

    /// <summary>Lap splice length multiplier in bar diameters (e.g. 40 -> 40 * d).</summary>
    public double LapFactor { get; init; } = 40.0;

    /// <summary>True to stagger lap splices by 50% between adjacent bar lines.</summary>
    public bool EnableStagger { get; init; } = true;

    /// <summary>Stagger offset multiplier (default: 1.3 * LapLength).</summary>
    public double StaggerOffsetRatio { get; init; } = 1.3;

    // --- Property Aliases ---
    public double LeftHookLengthMm => TopStartHookLength;
    public double RightHookLengthMm => TopEndHookLength;
    public double MaxStockLengthMm => MaxStockLength;
    public double LapLengthMultiplier => LapFactor;
    public bool StaggerSplice => EnableStagger;
}
