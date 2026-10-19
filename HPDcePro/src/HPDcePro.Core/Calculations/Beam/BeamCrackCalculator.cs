using System;

namespace HPDcePro.Core.Calculations.Beam
{
    /// <summary>
    /// Input parameters for beam crack width calculation.
    /// </summary>
    public sealed class BeamCrackInput
    {
        public double WidthMm { get; set; }
        public double HeightMm { get; set; }
        public double CoverTensionMm { get; set; } = 40.0;
        public double ServiceMomentKnm { get; set; }
        public double TensionSteelAreaMm2 { get; set; }
        public double BarDiameterMm { get; set; } = 20.0;
        public bool IsDeformedBar { get; set; } = true;
        public bool IsLongTerm { get; set; } = true;
        public double CustomAllowableWidthMm { get; set; } = 0.0;
        public ConcreteProperties Concrete { get; set; }
        public RebarProperties Steel { get; set; }

        public BeamCrackInput(
            double widthMm,
            double heightMm,
            double serviceMomentKnm,
            double tensionSteelAreaMm2,
            ConcreteProperties concrete,
            RebarProperties steel)
        {
            WidthMm = widthMm;
            HeightMm = heightMm;
            ServiceMomentKnm = serviceMomentKnm;
            TensionSteelAreaMm2 = tensionSteelAreaMm2;
            Concrete = concrete;
            Steel = steel;
        }
    }

    /// <summary>
    /// Output result of crack width evaluation per TCVN 5574:2018 Clause 8.2.1.2.
    /// </summary>
    public sealed class BeamCrackResult
    {
        public double CrackWidthMm { get; set; }
        public double AllowableWidthMm { get; set; }
        public double SteelStressMpa { get; set; }
        public double ReinforcementRatio { get; set; }
        public double Phi1 { get; set; }
        public double Phi2 { get; set; }
        public double Phi3 { get; set; }
        public double PsiS { get; set; }
        public bool IsCompliant { get; set; }
        public string Note { get; set; } = string.Empty;
    }

    /// <summary>
    /// Evaluates short-term and long-term crack width acrc for beams per TCVN 5574:2018 Clause 8.2.1.2.
    /// </summary>
    public static class BeamCrackCalculator
    {
        public static BeamCrackResult Calculate(BeamCrackInput input)
        {
            ValidateInput(input);

            double allowable = GetAllowableWidth(input);
            double mSerNmm = Math.Abs(input.ServiceMomentKnm) * 1e6;
            if (mSerNmm < 1e-4)
            {
                return CreateZeroMomentResult(allowable);
            }

            double h0 = input.HeightMm - input.CoverTensionMm;
            double sigmaS = mSerNmm / (input.TensionSteelAreaMm2 * (0.85 * h0));
            double mu = input.TensionSteelAreaMm2 / (input.WidthMm * h0);

            var (phi1, phi2) = GetCoefficients(input);
            double acrc = ComputeCrackWidth(sigmaS, mu, input.BarDiameterMm, input.Steel.Es, phi1, phi2);
            bool compliant = acrc <= allowable;

            return new BeamCrackResult
            {
                CrackWidthMm = acrc,
                AllowableWidthMm = allowable,
                SteelStressMpa = sigmaS,
                ReinforcementRatio = mu,
                Phi1 = phi1,
                Phi2 = phi2,
                Phi3 = 1.0,
                PsiS = 1.0,
                IsCompliant = compliant,
                Note = compliant
                    ? $"Crack width acrc = {acrc:F3} mm <= [acrc] = {allowable:F2} mm: compliant."
                    : $"Crack width acrc = {acrc:F3} mm > [acrc] = {allowable:F2} mm: excessive cracking."
            };
        }

        private static double GetAllowableWidth(BeamCrackInput input)
        {
            if (input.CustomAllowableWidthMm > 0.0) return input.CustomAllowableWidthMm;
            return input.IsLongTerm ? 0.30 : 0.40;
        }

        private static (double Phi1, double Phi2) GetCoefficients(BeamCrackInput input)
        {
            double phi1 = input.IsLongTerm ? 1.4 : 1.0;
            double phi2 = input.IsDeformedBar ? 0.5 : 0.8;
            return (phi1, phi2);
        }

        private static double ComputeCrackWidth(
            double sigmaS, double mu, double d, double es, double phi1, double phi2)
        {
            double clampedFactor = Math.Max(1.0, Math.Min(3.4, 3.5 - (100.0 * mu)));
            double cbrtD = Math.Pow(d, 1.0 / 3.0);
            return phi1 * phi2 * 1.0 * 1.0 * (sigmaS / es) * 20.0 * clampedFactor * cbrtD;
        }

        private static void ValidateInput(BeamCrackInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.WidthMm <= 0) throw new ArgumentOutOfRangeException(nameof(input.WidthMm), "Width must be positive.");
            if (input.HeightMm <= 0) throw new ArgumentOutOfRangeException(nameof(input.HeightMm), "Height must be positive.");
            if (input.CoverTensionMm <= 0 || input.CoverTensionMm >= input.HeightMm)
            {
                throw new ArgumentOutOfRangeException(nameof(input.CoverTensionMm), "Cover must be within (0, h).");
            }
            if (input.TensionSteelAreaMm2 <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(input.TensionSteelAreaMm2), "Steel area must be positive.");
            }
            if (input.BarDiameterMm <= 0) throw new ArgumentOutOfRangeException(nameof(input.BarDiameterMm), "Bar diameter must be positive.");
            if (input.Concrete == null) throw new ArgumentNullException(nameof(input.Concrete));
            if (input.Steel == null) throw new ArgumentNullException(nameof(input.Steel));
        }

        private static BeamCrackResult CreateZeroMomentResult(double allowable)
        {
            return new BeamCrackResult
            {
                CrackWidthMm = 0.0,
                AllowableWidthMm = allowable,
                SteelStressMpa = 0.0,
                ReinforcementRatio = 0.0,
                Phi1 = 1.0,
                Phi2 = 0.5,
                Phi3 = 1.0,
                PsiS = 1.0,
                IsCompliant = true,
                Note = "No moment applied: crack width is zero."
            };
        }
    }
}
