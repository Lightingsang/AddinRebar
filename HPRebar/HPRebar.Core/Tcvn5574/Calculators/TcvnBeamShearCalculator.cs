using System;
using HPRebar.Core.Tcvn5574.Materials;
using HPRebar.Core.Tcvn5574.Models;

namespace HPRebar.Core.Tcvn5574.Calculators;

/// <summary>
/// Shear reinforcement design calculator for reinforced concrete beams per TCVN 5574:2018 (Clauses 8.1.3 and 10.3.2).
/// Evaluates concrete web crushing, tensile shear capacity on inclined cracks, and stirrup spacing.
/// </summary>
public static class TcvnBeamShearCalculator
{
    private const double PhiB1 = 0.3; // Web diagonal crushing coefficient (Clause 8.1.3.2.1)
    private const double PhiB2 = 1.5; // Concrete inclined crack coefficient (Clause 8.1.3.3.2)
    private const double PhiB3 = 0.5; // Minimum concrete shear capacity coefficient (Clause 8.1.3.3.2)
    private const double PhiB4 = 1.5; // Maximum stirrup spacing coefficient (Clause 10.3.2.1)

    /// <summary>
    /// Calculates shear capacity and required stirrup spacing for a rectangular beam section.
    /// </summary>
    /// <param name="b">Beam width in millimetres.</param>
    /// <param name="h">Beam total height in millimetres.</param>
    /// <param name="shearKn">Design shear force |Q| in kN.</param>
    /// <param name="concrete">Concrete material properties.</param>
    /// <param name="stirrupSteel">Stirrup steel material properties.</param>
    /// <param name="stirrupDiameter">Stirrup bar diameter in mm (default 8 mm).</param>
    /// <param name="legs">Number of stirrup legs/branches (default 2).</param>
    /// <param name="a">Distance to tensile reinforcement centroid in mm (default 40 mm).</param>
    /// <param name="isSupportZone">True for near-support dense zone (L/4), false for midspan sparse zone.</param>
    public static ShearDesignResult Calculate(
        double b,
        double h,
        double shearKn,
        ConcreteMaterial concrete,
        SteelMaterial stirrupSteel,
        double stirrupDiameter = 8.0,
        int legs = 2,
        double a = 40.0,
        bool isSupportZone = true)
    {
        ValidateInputs(b, h, a, stirrupDiameter, legs, concrete, stirrupSteel);

        double h0 = h - a;
        double qNewton = Math.Abs(shearKn) * 1e3;
        double asw = legs * (Math.PI * stirrupDiameter * stirrupDiameter / 4.0);

        // 1. Diagonal web crushing check per Clause 8.1.3.2.1: Q <= 0.3 * Rb * b * h0
        double qbMaxN = PhiB1 * concrete.Rb * b * h0;
        double qbMaxKn = qbMaxN / 1e3;
        if (qNewton > qbMaxN)
        {
            return CreateCrushExceededResult(shearKn, h0, qbMaxKn, stirrupDiameter, legs, asw);
        }

        // 2. Minimum concrete shear capacity per Clause 8.1.3.3.2: Qb,min = 0.5 * Rbt * b * h0
        double qbMinN = PhiB3 * concrete.Rbt * b * h0;
        double qbMinKn = qbMinN / 1e3;

        // 3. Spacing limits per Clause 10.3.2.1
        double sMax = qNewton > 1e-4
            ? (PhiB4 * concrete.Rbt * b * h0 * h0) / qNewton
            : 500.0;
        double sDet = CalculateConstructiveSpacing(h, isSupportZone);

        // 4. Equilibrium calculation
        if (qNewton <= qbMinN)
        {
            return CreateConcreteOnlyResult(
                shearKn, h0, qbMaxKn, qbMinKn, stirrupDiameter, legs, asw, sMax, sDet);
        }

        return CalculateStirrupsRequired(
            shearKn, qNewton, b, h0, concrete, stirrupSteel, stirrupDiameter, legs, asw, qbMaxKn, qbMinKn, sMax, sDet);
    }

    private static void ValidateInputs(
        double b, double h, double a, double d, int legs, ConcreteMaterial concrete, SteelMaterial steel)
    {
        if (b <= 0.0) throw new ArgumentOutOfRangeException(nameof(b), "Width must be positive.");
        if (h <= 0.0) throw new ArgumentOutOfRangeException(nameof(h), "Height must be positive.");
        if (a <= 0.0 || a >= h) throw new ArgumentOutOfRangeException(nameof(a), "Centroid distance must be within (0, h).");
        if (d <= 0.0) throw new ArgumentOutOfRangeException(nameof(d), "Stirrup diameter must be positive.");
        if (legs < 1) throw new ArgumentOutOfRangeException(nameof(legs), "Leg count must be at least 1.");
        if (concrete == null) throw new ArgumentNullException(nameof(concrete));
        if (steel == null) throw new ArgumentNullException(nameof(steel));
    }

    private static ShearDesignResult CreateCrushExceededResult(
        double shearKn, double h0, double qbMaxKn, double stirrupDiameter, int legs, double asw)
    {
        return new ShearDesignResult
        {
            ShearKn = Math.Abs(shearKn),
            EffectiveDepthH0 = h0,
            QbMaxKn = qbMaxKn,
            IsWebCrushExceeded = true,
            QbMinKn = 0.0,
            RequiresStirrupByCalculation = true,
            QbKn = 0.0,
            QswKn = 0.0,
            StirrupDiameter = stirrupDiameter,
            LegCount = legs,
            StirrupAreaAsw = asw,
            CalculatedSpacing = 0.0,
            MaxSpacing = 0.0,
            ConstructiveSpacing = 0.0,
            RecommendedSpacing = 0.0,
            IsCompliant = false,
            Message = "Concrete diagonal web crushing exceeded (Q > 0.3 * Rb * b * h0). Enlarge cross-section."
        };
    }

    private static ShearDesignResult CreateConcreteOnlyResult(
        double shearKn,
        double h0,
        double qbMaxKn,
        double qbMinKn,
        double stirrupDiameter,
        int legs,
        double asw,
        double sMax,
        double sDet)
    {
        double recommendedSpacing = RoundToPracticalSpacing(Math.Min(sMax, sDet));

        return new ShearDesignResult
        {
            ShearKn = Math.Abs(shearKn),
            EffectiveDepthH0 = h0,
            QbMaxKn = qbMaxKn,
            IsWebCrushExceeded = false,
            QbMinKn = qbMinKn,
            RequiresStirrupByCalculation = false,
            QbKn = qbMinKn,
            QswKn = 0.0,
            StirrupDiameter = stirrupDiameter,
            LegCount = legs,
            StirrupAreaAsw = asw,
            CalculatedSpacing = sDet,
            MaxSpacing = sMax,
            ConstructiveSpacing = sDet,
            RecommendedSpacing = recommendedSpacing,
            IsCompliant = true,
            Message = "Concrete alone resists shear (Q <= Qb,min). Constructive stirrup spacing applied."
        };
    }

    private static ShearDesignResult CalculateStirrupsRequired(
        double shearKn,
        double qNewton,
        double b,
        double h0,
        ConcreteMaterial concrete,
        SteelMaterial stirrupSteel,
        double stirrupDiameter,
        int legs,
        double asw,
        double qbMaxKn,
        double qbMinKn,
        double sMax,
        double sDet)
    {
        // q_sw from inclined crack equilibrium
        double mb1 = PhiB2 * concrete.Rbt * b * h0 * h0;
        double qsw1 = (qNewton * qNewton) / (4.0 * mb1);
        double qsw2 = (qNewton - (0.75 * concrete.Rbt * b * h0)) / (2.0 * h0);
        double qswMin = 0.25 * concrete.Rbt * b;

        double qswReq = Math.Max(Math.Max(qsw1, qsw2), qswMin);
        double sCalc = (stirrupSteel.Rsw * asw) / qswReq;

        // Critical inclined crack length clamped to [h0, 2h0]
        double c0 = Math.Sqrt(mb1 / qswReq);
        double c = Math.Max(h0, Math.Min(2.0 * h0, c0));
        double qb = mb1 / c;
        double qsw = qswReq * c;

        double upperLimit = Math.Min(sMax, sDet);
        double recommendedSpacing = RoundToPracticalSpacing(Math.Min(sCalc, upperLimit));

        bool isSpacingAdequate = sCalc >= recommendedSpacing && sCalc >= 50.0;

        return new ShearDesignResult
        {
            ShearKn = Math.Abs(shearKn),
            EffectiveDepthH0 = h0,
            QbMaxKn = qbMaxKn,
            IsWebCrushExceeded = false,
            QbMinKn = qbMinKn,
            RequiresStirrupByCalculation = true,
            QbKn = qb / 1e3,
            QswKn = qsw / 1e3,
            StirrupDiameter = stirrupDiameter,
            LegCount = legs,
            StirrupAreaAsw = asw,
            CalculatedSpacing = sCalc,
            MaxSpacing = sMax,
            ConstructiveSpacing = sDet,
            RecommendedSpacing = recommendedSpacing,
            IsCompliant = isSpacingAdequate,
            Message = isSpacingAdequate
                ? "Calculated shear reinforcement satisfies TCVN 5574:2018 inclined section equilibrium."
                : $"Stirrups inadequate: required spacing s_calc = {sCalc:F1}mm < 50mm minimum. Increase stirrup legs or diameter."
        };
    }

    private static double CalculateConstructiveSpacing(double h, bool isSupportZone)
    {
        if (isSupportZone)
        {
            // Support zone (L/4): h <= 450 mm => min(h/2, 150); h > 450 mm => min(h/3, 500, 150) = 150
            return h <= 450.0 ? Math.Min(h / 2.0, 150.0) : 150.0;
        }

        // Midspan zone (L/2): h > 150 mm => min(3h/4, 500)
        return h > 150.0 ? Math.Min(0.75 * h, 500.0) : 500.0;
    }

    private static double RoundToPracticalSpacing(double rawSpacing)
    {
        if (rawSpacing <= 50.0) return 50.0;
        if (rawSpacing < 100.0) return Math.Floor(rawSpacing / 25.0) * 25.0;
        if (rawSpacing <= 300.0) return Math.Floor(rawSpacing / 50.0) * 50.0;
        return Math.Min(300.0, Math.Floor(rawSpacing / 50.0) * 50.0);
    }
}
