namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// Detailing specification for longitudinal side (skin) reinforcement and transverse cross-ties in deep beams.
/// Complies with TCVN 5574:2018 §10.3.2 and ACI 318 §9.7.2.3.
/// </summary>
public sealed record BeamSideBarSpec
{
    public BeamSideBarSpec()
    {
    }

    public BeamSideBarSpec(
        bool autoSkinBars = true,
        double depthThreshold = 700.0,
        double diameter = 12.0,
        double maxVerticalSpacing = 300.0,
        double cover = 25.0,
        bool includeCrossTies = true,
        double crossTieDiameter = 8.0,
        double crossTieSpacing = 400.0,
        CrossTieHookType crossTieHook = CrossTieHookType.Hook90And135,
        string sideBarTypeName = "",
        string crossTieBarTypeName = "")
    {
        AutoSkinBars = autoSkinBars;
        DepthThreshold = depthThreshold;
        Diameter = diameter;
        MaxVerticalSpacing = maxVerticalSpacing;
        Cover = cover;
        IncludeCrossTies = includeCrossTies;
        CrossTieDiameter = crossTieDiameter;
        CrossTieSpacing = crossTieSpacing;
        CrossTieHook = crossTieHook;
        SideBarTypeName = sideBarTypeName;
        CrossTieBarTypeName = crossTieBarTypeName;
    }

    /// <summary>True to automatically generate side bars when beam height h >= DepthThreshold.</summary>
    public bool AutoSkinBars { get; init; } = true;

    /// <summary>Beam height threshold triggering skin reinforcement (mm, default: 700 mm).</summary>
    public double DepthThreshold { get; init; } = 700.0;

    /// <summary>Diameter of longitudinal side bars (mm, e.g. 12 or 14).</summary>
    public double Diameter { get; init; } = 12.0;

    /// <summary>Maximum vertical center-to-center spacing between side bar pairs (mm, default: 300 mm).</summary>
    public double MaxVerticalSpacing { get; init; } = 300.0;

    /// <summary>Concrete cover from beam lateral vertical faces to the outer edge of side bars (mm).</summary>
    public double Cover { get; init; } = 25.0;

    /// <summary>True to generate transverse anti-buckling cross-ties connecting opposite side bars.</summary>
    public bool IncludeCrossTies { get; init; } = true;

    /// <summary>Diameter of cross-ties (mm, e.g. 6 or 8).</summary>
    public double CrossTieDiameter { get; init; } = 8.0;

    /// <summary>Longitudinal spacing along beam axis between cross-ties (mm, default: 400 mm).</summary>
    public double CrossTieSpacing { get; init; } = 400.0;

    /// <summary>Hook configuration for cross-ties.</summary>
    public CrossTieHookType CrossTieHook { get; init; } = CrossTieHookType.Hook90And135;

    /// <summary>Revit RebarBarType name for longitudinal side bars.</summary>
    public string SideBarTypeName { get; init; } = string.Empty;

    /// <summary>Revit RebarBarType name for transverse cross-ties.</summary>
    public string CrossTieBarTypeName { get; init; } = string.Empty;

    // --- Property Aliases ---
    public double DiameterMm => Diameter;
    public double CoverMm => Cover;
    public double TieSpacing => CrossTieSpacing;
    public double TieDiameter => CrossTieDiameter;
    public bool GenerateCrossTies => IncludeCrossTies;
}
