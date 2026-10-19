using System;

namespace HPRebar.Core.Tcvn5574.Materials;

/// <summary>
/// Heavy concrete design material properties per TCVN 5574:2018 (Tables 6, 7, 10).
/// Evaluated for Limit State 1 (ULS) with short-term static loading condition (gamma_b1 = 1.0).
/// </summary>
public sealed record ConcreteMaterial
{
    public ConcreteGrade Grade { get; init; }

    /// <summary>Design compressive strength Rb (MPa) per Table 6.</summary>
    public double Rb { get; init; }

    /// <summary>Design tensile strength Rbt (MPa) per Table 7.</summary>
    public double Rbt { get; init; }

    /// <summary>Initial modulus of elasticity Eb (MPa) per Table 10.</summary>
    public double Eb { get; init; }

    /// <summary>Material designation name (e.g. "B25").</summary>
    public string Name => MaterialGradeParser.FormatConcrete(Grade);

    public ConcreteMaterial(ConcreteGrade grade, double rb, double rbt, double eb)
    {
        Grade = grade;
        Rb = rb;
        Rbt = rbt;
        Eb = eb;
    }

    /// <summary>
    /// Creates a concrete material instance for the specified standard grade.
    /// </summary>
    public static ConcreteMaterial Create(ConcreteGrade grade)
    {
        return grade switch
        {
            ConcreteGrade.B15 => new ConcreteMaterial(ConcreteGrade.B15, 8.5, 0.75, 24000.0),
            ConcreteGrade.B20 => new ConcreteMaterial(ConcreteGrade.B20, 11.5, 0.90, 27500.0),
            ConcreteGrade.B25 => new ConcreteMaterial(ConcreteGrade.B25, 14.5, 1.05, 30000.0),
            ConcreteGrade.B30 => new ConcreteMaterial(ConcreteGrade.B30, 17.0, 1.15, 32500.0),
            ConcreteGrade.B35 => new ConcreteMaterial(ConcreteGrade.B35, 19.5, 1.30, 34500.0),
            ConcreteGrade.B40 => new ConcreteMaterial(ConcreteGrade.B40, 22.0, 1.40, 36000.0),
            _ => throw new ArgumentOutOfRangeException(nameof(grade), $"Unsupported concrete grade: {grade}")
        };
    }

    /// <summary>
    /// Default standard concrete for typical building structures: B25 (M350).
    /// </summary>
    public static ConcreteMaterial DefaultB25 => Create(ConcreteGrade.B25);
}
