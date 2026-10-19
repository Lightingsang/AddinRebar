using System;

namespace HPDcePro.Core.Calculations.Steel
{
    public sealed class SteelStabilityInput
    {
        public double AxialForceKn { get; set; }
        public double BendingMomentXknm { get; set; }
        public SteelSectionProperties Section { get; set; }
        public double EffectiveLengthXm { get; set; }
        public double EffectiveLengthYm { get; set; }
        public double YieldStrengthMpa { get; set; } = 235.0;
        public double ElasticModulusMpa { get; set; } = 206000.0;
        public double GammaC { get; set; } = 1.0;
        public double TotalDepthMm { get; set; }
        public double FlangeThicknessMm { get; set; }

        public SteelStabilityInput(
            double axialForceKn,
            double bendingMomentXknm,
            SteelSectionProperties section,
            double effectiveLengthXm,
            double effectiveLengthYm,
            double yieldStrengthMpa = 235.0,
            double gammaC = 1.0)
        {
            AxialForceKn = axialForceKn;
            BendingMomentXknm = bendingMomentXknm;
            Section = section;
            EffectiveLengthXm = effectiveLengthXm;
            EffectiveLengthYm = effectiveLengthYm;
            YieldStrengthMpa = yieldStrengthMpa;
            GammaC = gammaC;
        }
    }

    public sealed class SteelStabilityResult
    {
        public double InPlaneRatio { get; set; }
        public bool IsInPlaneStable => InPlaneRatio <= 1.0;
        public double OutOfPlaneRatio { get; set; }
        public bool IsOutOfPlaneStable => OutOfPlaneRatio <= 1.0;
        public double PhiE { get; set; }
        public double PhiY { get; set; }
        public double PhiB { get; set; }
        public double RelativeEccentricityM { get; set; }
        public double CoefficientC { get; set; }
        public bool IsCompliant => IsInPlaneStable && IsOutOfPlaneStable;
        public string Note { get; set; } = string.Empty;
    }

    /// <summary>
    /// Beam-Column stability calculator per TCVN 5575:2024 Chapter 7 (Clauses 7.2.1 and 7.2.4).
    /// </summary>
    public static class SteelStabilityCalculator
    {
        public static SteelStabilityResult Calculate(SteelStabilityInput input)
        {
            ValidateInput(input);

            double nNewton = Math.Abs(input.AxialForceKn) * 1e3;
            double mxNmm = Math.Abs(input.BendingMomentXknm) * 1e6;
            double fy = input.YieldStrengthMpa;
            double a = input.Section.AreaMm2;
            double wx = input.Section.WxMm3;
            double gammaC = input.GammaC;

            double m = CalculateRelativeEccentricity(nNewton, mxNmm, a, wx);
            double phiE = CalculatePhiE(input, m, a, fy);
            double inPlaneRatio = nNewton / (phiE * a * fy * gammaC);

            double phiY = CalculatePhiY(input, fy);
            double phiB = CalculatePhiB(input, fy);
            double c = CalculateCoefficientC(m, input);
            double outOfPlaneRatio = CalculateOutOfPlaneRatio(nNewton, mxNmm, a, wx, fy, gammaC, phiY, phiB, c, m);

            return new SteelStabilityResult
            {
                InPlaneRatio = inPlaneRatio,
                OutOfPlaneRatio = outOfPlaneRatio,
                PhiE = phiE,
                PhiY = phiY,
                PhiB = phiB,
                RelativeEccentricityM = m,
                CoefficientC = c,
                Note = (inPlaneRatio <= 1.0 && outOfPlaneRatio <= 1.0)
                    ? "Beam-column overall in-plane and out-of-plane stability verified."
                    : "Stability check failed: stress ratio exceeds 1.0."
            };
        }

        private static void ValidateInput(SteelStabilityInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.Section == null) throw new ArgumentNullException(nameof(input.Section));
            if (input.Section.AreaMm2 <= 0) throw new ArgumentOutOfRangeException(nameof(input.Section.AreaMm2), "Area must be positive.");
            if (input.Section.WxMm3 <= 0) throw new ArgumentOutOfRangeException(nameof(input.Section.WxMm3), "Wx must be positive.");
            if (input.EffectiveLengthXm <= 0 || input.EffectiveLengthYm <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(input.EffectiveLengthXm), "Effective lengths must be positive.");
            }
            if (input.YieldStrengthMpa <= 0) throw new ArgumentOutOfRangeException(nameof(input.YieldStrengthMpa), "Yield strength must be positive.");
        }

        private static double CalculateRelativeEccentricity(double nN, double mxNmm, double a, double wx)
        {
            if (nN < 1e-4) return 0.0;
            double e = mxNmm / nN;
            return (e * a) / wx;
        }

        private static double CalculateBucklingPhi(double slenderness, double fy, double eModulus)
        {
            double lambdaBar = (slenderness / Math.PI) * Math.Sqrt(fy / eModulus);
            if (lambdaBar <= 0.2) return 1.0;

            double alpha = 0.34;
            double phiParam = 0.5 * (1.0 + alpha * (lambdaBar - 0.2) + (lambdaBar * lambdaBar));
            double denom = phiParam + Math.Sqrt(Math.Max(0.0, (phiParam * phiParam) - (lambdaBar * lambdaBar)));
            return Math.Min(1.0, Math.Max(0.05, 1.0 / denom));
        }

        private static double CalculatePhiE(SteelStabilityInput input, double m, double a, double fy)
        {
            double ix = input.Section.RadiusGyrationXmm;
            double lambdaX = (input.EffectiveLengthXm * 1000.0) / (ix > 0 ? ix : 1.0);
            double phiX = CalculateBucklingPhi(lambdaX, fy, input.ElasticModulusMpa);

            if (m <= 1e-4) return phiX;
            double phiE = phiX / (1.0 + (m * Math.Sqrt(phiX)));
            return Math.Min(phiX, Math.Max(0.05, phiE));
        }

        private static double CalculatePhiY(SteelStabilityInput input, double fy)
        {
            double iy = input.Section.RadiusGyrationYmm;
            double lambdaY = (input.EffectiveLengthYm * 1000.0) / (iy > 0 ? iy : 1.0);
            return CalculateBucklingPhi(lambdaY, fy, input.ElasticModulusMpa);
        }

        private static double CalculatePhiB(SteelStabilityInput input, double fy)
        {
            double l0y = input.EffectiveLengthYm * 1000.0;
            double h = input.TotalDepthMm > 0 ? input.TotalDepthMm : 300.0;
            double tf = input.FlangeThicknessMm > 0 ? input.FlangeThicknessMm : 10.0;
            double ix = input.Section.IxMm4;
            double iy = input.Section.IyMm4;
            double it = input.Section.TorsionalConstantItMm4;

            if (ix <= 0 || iy <= 0 || l0y <= 0) return 1.0;

            double term1 = (iy / ix) * Math.Pow(h / l0y, 2) * (input.ElasticModulusMpa / fy);
            double term2 = Math.Sqrt(1.0 + (0.1 * l0y * l0y * it) / (Math.Pow(h - tf, 2) * iy));
            double phiB0 = Math.Max(0.1, term1 * term2);

            if (phiB0 > 0.85)
            {
                return Math.Min(1.0, 0.68 + (0.21 * phiB0));
            }
            return Math.Min(1.0, phiB0);
        }

        private static double CalculateCoefficientC(double m, SteelStabilityInput input)
        {
            if (m <= 1e-4) return 1.0;
            if (m > 5.0) return 1.0;

            double iy = input.Section.RadiusGyrationYmm;
            double lambdaY = (input.EffectiveLengthYm * 1000.0) / (iy > 0 ? iy : 1.0);
            double alpha = Math.Max(0.1, Math.Min(0.5, 0.7 - (0.002 * lambdaY)));
            return Math.Min(1.0, 1.0 / (1.0 + (alpha * m)));
        }

        private static double CalculateOutOfPlaneRatio(
            double nN, double mxNmm, double a, double wx, double fy, double gammaC,
            double phiY, double phiB, double c, double m)
        {
            if (m > 5.0)
            {
                return mxNmm / (phiB * wx * fy * gammaC);
            }

            double axialPart = nN / (c * phiY * a * fy * gammaC);
            double bendingPart = mxNmm / (phiB * wx * fy * gammaC);
            return axialPart + bendingPart;
        }
    }
}
