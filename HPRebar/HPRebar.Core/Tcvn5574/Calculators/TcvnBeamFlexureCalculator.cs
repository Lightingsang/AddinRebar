using System;
using HPRebar.Core.Tcvn5574.Materials;
using HPRebar.Core.Tcvn5574.Models;

namespace HPRebar.Core.Tcvn5574.Calculators;

/// <summary>
/// Flexural reinforcement design calculator for rectangular reinforced concrete beams per TCVN 5574:2018 Clause 8.1.2.3.
/// </summary>
public static class TcvnBeamFlexureCalculator
{
    private const double MinMu = 0.001; // 0.1% minimum reinforcement ratio per Clause 10.3.1.1

    /// <summary>
    /// Calculates required tension and compression reinforcement for a rectangular section (b x h).
    /// </summary>
    /// <param name="b">Cross-section width in millimetres.</param>
    /// <param name="h">Cross-section total height in millimetres.</param>
    /// <param name="momentKnm">Design bending moment |M| in kN.m.</param>
    /// <param name="concrete">Concrete material properties (Rb, Rbt, Eb).</param>
    /// <param name="steel">Reinforcing steel properties (Rs, Rsc, xi_R, alpha_R).</param>
    /// <param name="a">Distance from tension face to tension steel centroid in mm (default 40 mm).</param>
    /// <param name="aPrime">Distance from compression face to compression steel centroid in mm (default 40 mm).</param>
    public static FlexureDesignResult Calculate(
        double b,
        double h,
        double momentKnm,
        ConcreteMaterial concrete,
        SteelMaterial steel,
        double a = 40.0,
        double aPrime = 40.0)
    {
        ValidateInputs(b, h, a, aPrime, concrete, steel);

        double h0 = h - a;
        double momentNmm = Math.Abs(momentKnm) * 1e6;
        double muMaxPercent = steel.XiR * (concrete.Rb / steel.Rs) * 100.0;

        if (momentNmm < 1e-4)
        {
            return CreateZeroMomentResult(b, h0, momentKnm, muMaxPercent);
        }

        double alphaM = momentNmm / (concrete.Rb * b * h0 * h0);

        if (alphaM <= steel.AlphaR)
        {
            return CalculateSinglyReinforced(b, h0, momentKnm, momentNmm, alphaM, concrete, steel, muMaxPercent);
        }

        return CalculateDoublyReinforced(b, h0, momentKnm, momentNmm, alphaM, aPrime, concrete, steel, muMaxPercent);
    }

    private static void ValidateInputs(
        double b, double h, double a, double aPrime, ConcreteMaterial concrete, SteelMaterial steel)
    {
        if (b <= 0.0) throw new ArgumentOutOfRangeException(nameof(b), "Beam width must be greater than zero.");
        if (h <= 0.0) throw new ArgumentOutOfRangeException(nameof(h), "Beam height must be greater than zero.");
        if (a <= 0.0 || a >= h) throw new ArgumentOutOfRangeException(nameof(a), "Centroid distance a must be within (0, h).");
        if (aPrime <= 0.0 || aPrime >= h) throw new ArgumentOutOfRangeException(nameof(aPrime), "Centroid distance aPrime must be within (0, h).");
        if (h <= a + aPrime)
        {
            throw new ArgumentOutOfRangeException(nameof(h), "Total height h must be greater than sum of centroid distances (a + aPrime).");
        }
        if (concrete == null) throw new ArgumentNullException(nameof(concrete));
        if (steel == null) throw new ArgumentNullException(nameof(steel));
    }

    private static FlexureDesignResult CreateZeroMomentResult(
        double b, double h0, double momentKnm, double muMaxPercent)
    {
        double asMin = MinMu * b * h0;
        double muPercent = (asMin / (b * h0)) * 100.0;

        return new FlexureDesignResult
        {
            MomentKnm = momentKnm,
            EffectiveDepthH0 = h0,
            AlphaM = 0.0,
            Xi = 0.0,
            Zeta = 1.0,
            NeutralAxisDepthX = 0.0,
            AsRequired = asMin,
            AsCompressionRequired = 0.0,
            IsDoublyReinforced = false,
            MuPercent = muPercent,
            MuMinPercent = MinMu * 100.0,
            MuMaxPercent = muMaxPercent,
            IsOverReinforced = false,
            IsCompliant = true,
            Message = "Zero moment: minimum constructive reinforcement applied."
        };
    }

    private static FlexureDesignResult CalculateSinglyReinforced(
        double b,
        double h0,
        double momentKnm,
        double momentNmm,
        double alphaM,
        ConcreteMaterial concrete,
        SteelMaterial steel,
        double muMaxPercent)
    {
        double xi = 1.0 - Math.Sqrt(Math.Max(0.0, 1.0 - (2.0 * alphaM)));
        double zeta = 1.0 - (0.5 * xi);
        double asCalc = momentNmm / (steel.Rs * zeta * h0);
        double asMin = MinMu * b * h0;
        double asRequired = Math.Max(asCalc, asMin);

        double muPercent = (asRequired / (b * h0)) * 100.0;
        bool isOverReinforced = muPercent > muMaxPercent;

        return new FlexureDesignResult
        {
            MomentKnm = momentKnm,
            EffectiveDepthH0 = h0,
            AlphaM = alphaM,
            Xi = xi,
            Zeta = zeta,
            NeutralAxisDepthX = xi * h0,
            AsRequired = asRequired,
            AsCompressionRequired = 0.0,
            IsDoublyReinforced = false,
            MuPercent = muPercent,
            MuMinPercent = MinMu * 100.0,
            MuMaxPercent = muMaxPercent,
            IsOverReinforced = isOverReinforced,
            IsCompliant = !isOverReinforced,
            Message = isOverReinforced
                ? "Warning: Reinforcement ratio exceeds recommended maximum."
                : "Singly reinforced section design compliant (cốt đơn)."
        };
    }

    private static FlexureDesignResult CalculateDoublyReinforced(
        double b,
        double h0,
        double momentKnm,
        double momentNmm,
        double alphaM,
        double aPrime,
        ConcreteMaterial concrete,
        SteelMaterial steel,
        double muMaxPercent)
    {
        double mb = steel.AlphaR * concrete.Rb * b * h0 * h0;
        double deltaM = momentNmm - mb;

        double asPrime = deltaM / (steel.Rsc * (h0 - aPrime));
        double asRequired = ((steel.XiR * concrete.Rb * b * h0) / steel.Rs) + ((steel.Rsc / steel.Rs) * asPrime);

        double xi = steel.XiR;
        double zeta = 1.0 - (0.5 * xi);
        double muPercent = (asRequired / (b * h0)) * 100.0;
        bool isOverReinforced = muPercent > 3.0; // Over 3% is excessively congested

        return new FlexureDesignResult
        {
            MomentKnm = momentKnm,
            EffectiveDepthH0 = h0,
            AlphaM = alphaM,
            Xi = xi,
            Zeta = zeta,
            NeutralAxisDepthX = xi * h0,
            AsRequired = asRequired,
            AsCompressionRequired = asPrime,
            IsDoublyReinforced = true,
            MuPercent = muPercent,
            MuMinPercent = MinMu * 100.0,
            MuMaxPercent = muMaxPercent,
            IsOverReinforced = isOverReinforced,
            IsCompliant = !isOverReinforced,
            Message = isOverReinforced
                ? "Section overloaded: steel ratio > 3.0%, enlarge section dimensions."
                : "Doubly reinforced section design compliant (cốt kép: As and As' calculated)."
        };
    }
}
