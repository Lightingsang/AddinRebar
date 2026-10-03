namespace HPRebar.Core.FoundationRebar.Models;

/// <summary>
/// Reinforcement specification for pad/mat foundation rebar generation.
/// All lengths, diameters, and spacings are in millimetres.
/// </summary>
public sealed record FoundationRebarSpec
{
    public FoundationRebarSpec()
    {
    }

    public FoundationRebarSpec(
        double diameterBottomX = 16.0,
        double diameterBottomY = 16.0,
        double diameterTopX = 12.0,
        double diameterTopY = 12.0,
        double spacingBottomX = 150.0,
        double spacingBottomY = 150.0,
        double spacingTopX = 200.0,
        double spacingTopY = 200.0,
        double coverTop = 50.0,
        double coverBottom = 50.0,
        double coverSide = 50.0,
        bool isTopMatEnabled = true,
        FoundationHookType hookType = FoundationHookType.None,
        double hookLength = 0.0)
    {
        DiameterBottomX = diameterBottomX;
        DiameterBottomY = diameterBottomY;
        DiameterTopX = diameterTopX;
        DiameterTopY = diameterTopY;
        SpacingBottomX = spacingBottomX;
        SpacingBottomY = spacingBottomY;
        SpacingTopX = spacingTopX;
        SpacingTopY = spacingTopY;
        CoverTop = coverTop;
        CoverBottom = coverBottom;
        CoverSide = coverSide;
        IsTopMatEnabled = isTopMatEnabled;
        HookType = hookType;
        HookLength = hookLength;
    }

    /// <summary>Nominal bar diameter for Bottom Mat Direction X (mm).</summary>
    public double DiameterBottomX { get; init; } = 16.0;

    /// <summary>Nominal bar diameter for Bottom Mat Direction Y (mm).</summary>
    public double DiameterBottomY { get; init; } = 16.0;

    /// <summary>Nominal bar diameter for Top Mat Direction X (mm).</summary>
    public double DiameterTopX { get; init; } = 12.0;

    /// <summary>Nominal bar diameter for Top Mat Direction Y (mm).</summary>
    public double DiameterTopY { get; init; } = 12.0;

    /// <summary>Bar spacing for Bottom Mat Direction X (mm).</summary>
    public double SpacingBottomX { get; init; } = 150.0;

    /// <summary>Bar spacing for Bottom Mat Direction Y (mm).</summary>
    public double SpacingBottomY { get; init; } = 150.0;

    /// <summary>Bar spacing for Top Mat Direction X (mm).</summary>
    public double SpacingTopX { get; init; } = 200.0;

    /// <summary>Bar spacing for Top Mat Direction Y (mm).</summary>
    public double SpacingTopY { get; init; } = 200.0;

    /// <summary>Clear concrete cover at top face (mm).</summary>
    public double CoverTop { get; init; } = 50.0;

    /// <summary>Clear concrete cover at bottom face (mm).</summary>
    public double CoverBottom { get; init; } = 50.0;

    /// <summary>Clear concrete cover at side faces (mm).</summary>
    public double CoverSide { get; init; } = 50.0;

    /// <summary>Indicates whether the top reinforcement mat is enabled.</summary>
    public bool IsTopMatEnabled { get; init; } = true;

    /// <summary>Anchorage hook type for rebar ends.</summary>
    public FoundationHookType HookType { get; init; } = FoundationHookType.None;

    /// <summary>Default hook length in millimetres. 0.0 defaults to 15 * bar diameter.</summary>
    public double HookLength { get; init; } = 0.0;

    /// <summary>Hook length to request for a bar of <paramref name="diameter"/> mm: <see cref="HookLength"/>, or 15 × diameter when it is 0.</summary>
    public double GetHookLength(double diameter) => HookLength > 0 ? HookLength : 15.0 * diameter;
}
