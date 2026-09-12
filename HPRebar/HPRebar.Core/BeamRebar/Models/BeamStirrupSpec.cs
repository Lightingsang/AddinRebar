namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// Specification parameters for beam stirrup (shear tie) distribution.
/// All lengths and spacings are in millimetres.
/// </summary>
public sealed record BeamStirrupSpec
{
    public BeamStirrupSpec()
    {
    }

    public BeamStirrupSpec(
        StirrupLayout layout = StirrupLayout.ThreeZoneL4,
        double diameter = 8.0,
        double cover = 25.0,
        double spacingDense = 100.0,
        double spacingSparse = 200.0,
        double startOffset = 50.0,
        bool includeStirrupsInNodes = false,
        double nodeSpacing = 150.0,
        HookAngle hookAngle = HookAngle.Hook135,
        string rebarShapeName = "M_T1",
        string barTypeName = "",
        double? S1 = null,
        double? S2 = null,
        double? StartOffsetMm = null,
        double? CoverMm = null,
        double? DiameterMm = null,
        bool? IsStirrupInNode = null)
    {
        Layout = layout;
        Diameter = DiameterMm ?? diameter;
        Cover = CoverMm ?? cover;
        SpacingDense = S1 ?? spacingDense;
        SpacingSparse = S2 ?? spacingSparse;
        StartOffset = StartOffsetMm ?? startOffset;
        IncludeStirrupsInNodes = IsStirrupInNode ?? includeStirrupsInNodes;
        NodeSpacing = nodeSpacing;
        HookAngle = hookAngle;
        RebarShapeName = rebarShapeName;
        BarTypeName = barTypeName;
    }

    /// <summary>Distribution layout algorithm.</summary>
    public StirrupLayout Layout { get; init; } = StirrupLayout.ThreeZoneL4;

    /// <summary>Stirrup bar diameter in millimetres (e.g. 8, 10).</summary>
    public double Diameter { get; init; } = 8.0;

    /// <summary>Concrete cover thickness to the outer edge of the stirrup (mm).</summary>
    public double Cover { get; init; } = 25.0;

    /// <summary>Dense spacing S1 in support zones (mm, e.g. 100).</summary>
    public double SpacingDense { get; init; } = 100.0;

    /// <summary>Sparse spacing S2 in midspan zone (mm, e.g. 200).</summary>
    public double SpacingSparse { get; init; } = 200.0;

    /// <summary>Offset from support inside face to the first stirrup (mm, default: 50).</summary>
    public double StartOffset { get; init; } = 50.0;

    /// <summary>True to carry stirrups through interior support column/wall nodes.</summary>
    public bool IncludeStirrupsInNodes { get; init; }

    /// <summary>Stirrup spacing across support node widths (mm, default: 150).</summary>
    public double NodeSpacing { get; init; } = 150.0;

    /// <summary>Hook bend angle on stirrup hoop (default: 135° seismic hook).</summary>
    public HookAngle HookAngle { get; init; } = HookAngle.Hook135;

    /// <summary>Rebar shape family name (e.g. "M_T1" or "T1").</summary>
    public string RebarShapeName { get; init; } = "M_T1";

    /// <summary>Revit RebarBarType name matched in project document.</summary>
    public string BarTypeName { get; init; } = string.Empty;

    // --- Property Aliases ---
    public double S1 => SpacingDense;
    public double S2 => SpacingSparse;
    public double Spacing => SpacingDense;
    public double StartOffsetMm => StartOffset;
    public double CoverMm => Cover;
    public double DiameterMm => Diameter;
    public bool IsStirrupInNode => IncludeStirrupsInNodes;
    public StirrupDistributionType TypeDis => (StirrupDistributionType)(int)Layout;
}
