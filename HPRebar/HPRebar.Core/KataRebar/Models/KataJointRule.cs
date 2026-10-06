namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// Joint stirrups and hanger bars round one kind of load resting on a span, as the rules apply them (from
/// <see cref="KataJointRebarSettings"/>). A stirrup diameter of 0 means the beam's own stirrup diameter (G6).
/// </summary>
public sealed record KataJointRule(
    int StirrupCount,
    double StirrupDiameter,
    double StirrupSpacing,
    int HangerCount,
    double HangerDiameter,
    double HangerTopLength,
    double HangerAngleDegrees)
{
    /// <summary>What Kata drew round both loads of B01: 5 joint stirrups of the beam's diameter at a50, 2Ø16 hangers.</summary>
    public static readonly KataJointRule Kata = new(5, 0.0, 50.0, 2, 16.0, 150.0, 45.0);
}
