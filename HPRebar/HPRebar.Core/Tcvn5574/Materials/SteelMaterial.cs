using System;

namespace HPRebar.Core.Tcvn5574.Materials;

/// <summary>
/// Reinforcing steel design material properties per TCVN 5574:2018 (Tables 13, 14, 15 and Clause 8.1.2.3.1).
/// </summary>
public sealed record SteelMaterial
{
    public SteelGrade Grade { get; init; }

    /// <summary>Design tensile yield strength Rs (MPa) per Table 13.</summary>
    public double Rs { get; init; }

    /// <summary>Design compressive yield strength Rsc (MPa) per Table 14.</summary>
    public double Rsc { get; init; }

    /// <summary>Design transverse stirrup strength Rsw (MPa) per Table 15.</summary>
    public double Rsw { get; init; }

    /// <summary>Modulus of elasticity Es (MPa) per Table 15 (200,000 MPa).</summary>
    public double Es { get; init; }

    /// <summary>
    /// Relative boundary depth of compression zone xi_R per Clause 8.1.2.3.1.
    /// xi_R = 0.8 / (1 + Rs / 700).
    /// </summary>
    public double XiR { get; init; }

    /// <summary>
    /// Boundary flexural resistance coefficient alpha_R per Clause 8.1.2.3.1.
    /// alpha_R = xi_R * (1 - 0.5 * xi_R).
    /// </summary>
    public double AlphaR { get; init; }

    /// <summary>Material designation name (e.g. "CB400-V").</summary>
    public string Name => MaterialGradeParser.FormatSteel(Grade);

    public SteelMaterial(
        SteelGrade grade,
        double rs,
        double rsc,
        double rsw,
        double es = 200000.0)
    {
        Grade = grade;
        Rs = rs;
        Rsc = rsc;
        Rsw = rsw;
        Es = es;

        // TCVN 5574:2018 Clause 8.1.2.3.1: xi_R = 0.8 / (1 + (Rs / Es) / 0.0035) = 0.8 / (1 + Rs / 700)
        XiR = 0.8 / (1.0 + (rs / 700.0));
        AlphaR = XiR * (1.0 - 0.5 * XiR);
    }

    /// <summary>
    /// Creates a steel material instance for the specified standard grade.
    /// </summary>
    public static SteelMaterial Create(SteelGrade grade)
    {
        return grade switch
        {
            SteelGrade.CB240T => new SteelMaterial(SteelGrade.CB240T, 210.0, 210.0, 170.0),
            SteelGrade.CB300V => new SteelMaterial(SteelGrade.CB300V, 260.0, 260.0, 210.0),
            SteelGrade.CB400V => new SteelMaterial(SteelGrade.CB400V, 350.0, 350.0, 280.0),
            // CB500-V: Rsc is capped at 400 MPa per Table 14 / Clause 8.1.2.3.1
            SteelGrade.CB500V => new SteelMaterial(SteelGrade.CB500V, 435.0, 400.0, 300.0),
            _ => throw new ArgumentOutOfRangeException(nameof(grade), $"Unsupported steel grade: {grade}")
        };
    }

    /// <summary>
    /// Default standard longitudinal steel: CB400-V.
    /// </summary>
    public static SteelMaterial DefaultLongitudinal => Create(SteelGrade.CB400V);

    /// <summary>
    /// Default standard stirrup steel: CB240-T.
    /// </summary>
    public static SteelMaterial DefaultStirrup => Create(SteelGrade.CB240T);
}
