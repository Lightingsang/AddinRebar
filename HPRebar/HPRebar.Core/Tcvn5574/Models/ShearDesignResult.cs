namespace HPRebar.Core.Tcvn5574.Models;

/// <summary>
/// Result of shear reinforcement design for a beam cross-section per TCVN 5574:2018 Clause 8.1.3 and 10.3.2.
/// </summary>
public sealed record ShearDesignResult
{
    /// <summary>Design shear force |Q| in kN.</summary>
    public double ShearKn { get; init; }

    /// <summary>Effective section depth h0 (mm).</summary>
    public double EffectiveDepthH0 { get; init; }

    /// <summary>Web diagonal crushing limit Qb,max = 0.3 * Rb * b * h0 (kN) per Clause 8.1.3.2.1.</summary>
    public double QbMaxKn { get; init; }

    /// <summary>True if shear force exceeds concrete web crushing capacity (section is undersized).</summary>
    public bool IsWebCrushExceeded { get; init; }

    /// <summary>Minimum concrete tensile shear resistance Qb,min = 0.5 * Rbt * b * h0 (kN).</summary>
    public double QbMinKn { get; init; }

    /// <summary>True if Q > Qb,min, requiring stirrups by structural calculation.</summary>
    public bool RequiresStirrupByCalculation { get; init; }

    /// <summary>Concrete shear resistance contribution Qb along the critical inclined section (kN).</summary>
    public double QbKn { get; init; }

    /// <summary>Stirrup shear resistance contribution Qsw across the critical inclined section (kN).</summary>
    public double QswKn { get; init; }

    /// <summary>Stirrup bar diameter d_sw (mm).</summary>
    public double StirrupDiameter { get; init; }

    /// <summary>Number of stirrup legs (branches) nw.</summary>
    public int LegCount { get; init; } = 2;

    /// <summary>Total cross-sectional area of stirrup legs Asw = nw * pi * d_sw^2 / 4 (mm2).</summary>
    public double StirrupAreaAsw { get; init; }

    /// <summary>Required stirrup spacing from equilibrium calculation s_calc (mm).</summary>
    public double CalculatedSpacing { get; init; }

    /// <summary>Maximum stirrup spacing s_max = 1.5 * Rbt * b * h0^2 / Q (mm) per Clause 10.3.2.1.</summary>
    public double MaxSpacing { get; init; }

    /// <summary>Constructive detailing spacing limit s_det per Clause 10.3.2.1.</summary>
    public double ConstructiveSpacing { get; init; }

    /// <summary>Final recommended practical spacing s (mm) rounded to standard intervals.</summary>
    public double RecommendedSpacing { get; init; }

    /// <summary>True if shear check is fully satisfied.</summary>
    public bool IsCompliant { get; init; }

    /// <summary>Descriptive message or warning.</summary>
    public string Message { get; init; } = string.Empty;
}
