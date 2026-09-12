namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// Detailing specification for secondary framing beam intersection reinforcement.
/// Handles concentrated shear hanging stirrups ("cốt treo") and diagonal bent ties ("thép vai bò").
/// </summary>
public sealed record BeamSpecialBarSpec
{
    public BeamSpecialBarSpec()
    {
    }

    public BeamSpecialBarSpec(
        bool enableHangingStirrups = true,
        int hangingStirrupsPerSide = 3,
        double hangingStirrupDiameter = 8.0,
        double hangingStirrupSpacing = 50.0,
        bool enableDiagonalTies = false,
        int diagonalTieCount = 2,
        double diagonalTieDiameter = 14.0,
        double diagonalAngleDegrees = 45.0,
        string hangingStirrupTypeName = "",
        string diagonalTieTypeName = "")
    {
        EnableHangingStirrups = enableHangingStirrups;
        HangingStirrupsPerSide = hangingStirrupsPerSide;
        HangingStirrupDiameter = hangingStirrupDiameter;
        HangingStirrupSpacing = hangingStirrupSpacing;
        EnableDiagonalTies = enableDiagonalTies;
        DiagonalTieCount = diagonalTieCount;
        DiagonalTieDiameter = diagonalTieDiameter;
        DiagonalAngleDegrees = diagonalAngleDegrees;
        HangingStirrupTypeName = hangingStirrupTypeName;
        DiagonalTieTypeName = diagonalTieTypeName;
    }

    // --- Hanging Stirrups (Cốt treo) ---
    /// <summary>True to generate concentrated hanging stirrups flanking secondary beam joints.</summary>
    public bool EnableHangingStirrups { get; init; } = true;

    /// <summary>Number of stirrup pairs on EACH side of the incoming secondary beam (default: 3 pairs).</summary>
    public int HangingStirrupsPerSide { get; init; } = 3;

    /// <summary>Diameter of hanging stirrups (mm, e.g. 8 or 10).</summary>
    public double HangingStirrupDiameter { get; init; } = 8.0;

    /// <summary>Close spacing between hanging stirrups (mm, default: 50 mm).</summary>
    public double HangingStirrupSpacing { get; init; } = 50.0;

    // --- Diagonal Ties (Thép vai bò) ---
    /// <summary>True to generate 45° diagonal bent bars under the secondary beam soffit.</summary>
    public bool EnableDiagonalTies { get; init; }

    /// <summary>Number of diagonal bars across the beam width (default: 2).</summary>
    public int DiagonalTieCount { get; init; } = 2;

    /// <summary>Diameter of diagonal bent bars (mm, e.g. 14 or 16).</summary>
    public double DiagonalTieDiameter { get; init; } = 14.0;

    /// <summary>Angle of diagonal inclination in degrees (default: 45.0°).</summary>
    public double DiagonalAngleDegrees { get; init; } = 45.0;

    // --- Revit Types ---
    /// <summary>Revit RebarBarType name for hanging stirrups.</summary>
    public string HangingStirrupTypeName { get; init; } = string.Empty;

    /// <summary>Revit RebarBarType name for diagonal ties.</summary>
    public string DiagonalTieTypeName { get; init; } = string.Empty;

    // --- Property Aliases ---
    public int CountPerSide => HangingStirrupsPerSide;
    public double Spacing => HangingStirrupSpacing;
}
