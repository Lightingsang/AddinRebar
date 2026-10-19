namespace HPRebar.Core.Tcvn5574.Models;

/// <summary>
/// Result of flexural reinforcement design for a rectangular cross-section per TCVN 5574:2018 Clause 8.1.2.3.
/// </summary>
public sealed record FlexureDesignResult
{
    /// <summary>Design bending moment |M| in kN·m.</summary>
    public double MomentKnm { get; init; }

    /// <summary>Effective section depth h0 = h - a (mm).</summary>
    public double EffectiveDepthH0 { get; init; }

    /// <summary>Dimensionless moment ratio alpha_m = |M| / (Rb * b * h0^2).</summary>
    public double AlphaM { get; init; }

    /// <summary>Relative depth of compression zone xi.</summary>
    public double Xi { get; init; }

    /// <summary>Internal lever arm coefficient zeta = 1 - 0.5 * xi.</summary>
    public double Zeta { get; init; }

    /// <summary>Depth of concrete compression zone x = xi * h0 (mm).</summary>
    public double NeutralAxisDepthX { get; init; }

    /// <summary>Required tension reinforcement area As (mm2).</summary>
    public double AsRequired { get; init; }

    /// <summary>Required compression reinforcement area As' (mm2). Zero for singly reinforced.</summary>
    public double AsCompressionRequired { get; init; }

    /// <summary>True if alpha_m > alpha_R, requiring compression steel (cốt kép).</summary>
    public bool IsDoublyReinforced { get; init; }

    /// <summary>Actual tension reinforcement ratio mu = As / (b * h0) * 100%.</summary>
    public double MuPercent { get; init; }

    /// <summary>Minimum reinforcement ratio mu_min = 0.1% per Clause 10.3.1.1.</summary>
    public double MuMinPercent { get; init; } = 0.1;

    /// <summary>Maximum recommended ratio mu_max = xi_R * (Rb / Rs) * 100%.</summary>
    public double MuMaxPercent { get; init; }

    /// <summary>True if mu exceeds mu_max, indicating section is over-reinforced.</summary>
    public bool IsOverReinforced { get; init; }

    /// <summary>True if design is structurally verified and compliant.</summary>
    public bool IsCompliant { get; init; }

    /// <summary>Descriptive message or warning.</summary>
    public string Message { get; init; } = string.Empty;
}
