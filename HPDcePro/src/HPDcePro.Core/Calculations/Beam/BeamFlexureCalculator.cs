using System;

namespace HPDcePro.Core.Calculations.Beam
{
    /// <summary>
    /// Input parameters for beam flexural design.
    /// </summary>
    public sealed class BeamFlexureInput
    {
        public double WidthMm { get; set; }
        public double HeightMm { get; set; }
        public double CoverTensionMm { get; set; } = 40.0;
        public double CoverCompressionMm { get; set; } = 40.0;
        public double MomentKnm { get; set; }
        public ConcreteProperties Concrete { get; set; }
        public RebarProperties Steel { get; set; }

        public BeamFlexureInput(
            double widthMm,
            double heightMm,
            double momentKnm,
            ConcreteProperties concrete,
            RebarProperties steel)
        {
            WidthMm = widthMm;
            HeightMm = heightMm;
            MomentKnm = momentKnm;
            Concrete = concrete;
            Steel = steel;
        }
    }

    /// <summary>
    /// Output result of beam flexural design per TCVN 5574:2018.
    /// </summary>
    public sealed class BeamFlexureResult
    {
        public double EffectiveDepthH0 { get; set; }
        public double AlphaM { get; set; }
        public double Xi { get; set; }
        public double AsRequiredMm2 { get; set; }
        public double AsPrimeRequiredMm2 { get; set; }
        public bool IsDoublyReinforced { get; set; }
        public double ReinforcementRatioPercent { get; set; }
        public double MinRatioPercent { get; set; } = 0.10;
        public double MaxRatioPercent { get; set; } = 3.00;
        public bool IsOverReinforced { get; set; }
        public bool IsCompliant { get; set; }
        public string Note { get; set; } = string.Empty;
    }

    /// <summary>
    /// Beam flexural design calculator for rectangular sections per TCVN 5574:2018 Clause 8.1.2.3.
    /// </summary>
    public static class BeamFlexureCalculator
    {
        private const double MinMu = 0.001; // 0.10%

        public static BeamFlexureResult Calculate(BeamFlexureInput input)
        {
            ValidateInput(input);

            double b = input.WidthMm;
            double h0 = input.HeightMm - input.CoverTensionMm;
            double aPrime = input.CoverCompressionMm;
            double mNmm = Math.Abs(input.MomentKnm) * 1e6;

            if (mNmm < 1e-4)
            {
                return CreateZeroMomentResult(b, h0);
            }

            double alphaM = mNmm / (input.Concrete.Rb * b * h0 * h0);
            if (alphaM <= input.Steel.AlphaR)
            {
                return CalculateSinglyReinforced(b, h0, mNmm, alphaM, input.Concrete, input.Steel);
            }

            return CalculateDoublyReinforced(b, h0, aPrime, mNmm, alphaM, input.Concrete, input.Steel);
        }

        private static void ValidateInput(BeamFlexureInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.WidthMm <= 0) throw new ArgumentOutOfRangeException(nameof(input.WidthMm), "Width must be positive.");
            if (input.HeightMm <= 0) throw new ArgumentOutOfRangeException(nameof(input.HeightMm), "Height must be positive.");
            if (input.CoverTensionMm <= 0 || input.CoverTensionMm >= input.HeightMm)
            {
                throw new ArgumentOutOfRangeException(nameof(input.CoverTensionMm), "Tension cover must be within (0, h).");
            }
            if (input.HeightMm <= input.CoverTensionMm + input.CoverCompressionMm)
            {
                throw new ArgumentOutOfRangeException(nameof(input.HeightMm), "Height must exceed sum of covers.");
            }
            if (input.Concrete == null) throw new ArgumentNullException(nameof(input.Concrete));
            if (input.Steel == null) throw new ArgumentNullException(nameof(input.Steel));
        }

        private static BeamFlexureResult CreateZeroMomentResult(double b, double h0)
        {
            double asMin = MinMu * b * h0;
            return new BeamFlexureResult
            {
                EffectiveDepthH0 = h0,
                AlphaM = 0.0,
                Xi = 0.0,
                AsRequiredMm2 = asMin,
                AsPrimeRequiredMm2 = 0.0,
                IsDoublyReinforced = false,
                ReinforcementRatioPercent = (asMin / (b * h0)) * 100.0,
                IsOverReinforced = false,
                IsCompliant = true,
                Note = "Zero moment: minimum constructive steel applied."
            };
        }

        private static BeamFlexureResult CalculateSinglyReinforced(
            double b, double h0, double mNmm, double alphaM, ConcreteProperties concrete, RebarProperties steel)
        {
            double xi = 1.0 - Math.Sqrt(Math.Max(0.0, 1.0 - (2.0 * alphaM)));
            double zeta = 1.0 - (0.5 * xi);
            double asCalc = mNmm / (steel.Rs * zeta * h0);
            double asMin = MinMu * b * h0;
            double asReq = Math.Max(asCalc, asMin);
            double muPercent = (asReq / (b * h0)) * 100.0;
            bool overReinforced = muPercent > 3.0;

            return new BeamFlexureResult
            {
                EffectiveDepthH0 = h0,
                AlphaM = alphaM,
                Xi = xi,
                AsRequiredMm2 = asReq,
                AsPrimeRequiredMm2 = 0.0,
                IsDoublyReinforced = false,
                ReinforcementRatioPercent = muPercent,
                IsOverReinforced = overReinforced,
                IsCompliant = !overReinforced,
                Note = overReinforced
                    ? "Warning: Single reinforcement ratio exceeds 3.0%."
                    : "Singly reinforced section compliant (cốt đơn)."
            };
        }

        private static BeamFlexureResult CalculateDoublyReinforced(
            double b, double h0, double aPrime, double mNmm, double alphaM, ConcreteProperties concrete, RebarProperties steel)
        {
            double mb = steel.AlphaR * concrete.Rb * b * h0 * h0;
            double deltaM = mNmm - mb;
            double asPrime = deltaM / (steel.Rsc * (h0 - aPrime));
            double asReq = ((steel.XiR * concrete.Rb * b * h0) / steel.Rs) + ((steel.Rsc / steel.Rs) * asPrime);

            double muPercent = (asReq / (b * h0)) * 100.0;
            bool overReinforced = muPercent > 3.0;

            return new BeamFlexureResult
            {
                EffectiveDepthH0 = h0,
                AlphaM = alphaM,
                Xi = steel.XiR,
                AsRequiredMm2 = asReq,
                AsPrimeRequiredMm2 = asPrime,
                IsDoublyReinforced = true,
                ReinforcementRatioPercent = muPercent,
                IsOverReinforced = overReinforced,
                IsCompliant = !overReinforced,
                Note = overReinforced
                    ? "Warning: Section overloaded with mu > 3.0%, increase dimensions."
                    : "Doubly reinforced section compliant (cốt kép: As and As' calculated)."
            };
        }
    }
}
