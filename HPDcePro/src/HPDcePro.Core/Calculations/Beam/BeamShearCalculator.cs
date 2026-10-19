using System;

namespace HPDcePro.Core.Calculations.Beam
{
    /// <summary>
    /// Input parameters for beam shear design.
    /// </summary>
    public sealed class BeamShearInput
    {
        public double WidthMm { get; set; }
        public double HeightMm { get; set; }
        public double CoverTensionMm { get; set; } = 40.0;
        public double ShearForceKn { get; set; }
        public double StirrupDiameterMm { get; set; } = 8.0;
        public int StirrupLegs { get; set; } = 2;
        public bool IsNearSupport { get; set; } = true;
        public ConcreteProperties Concrete { get; set; }
        public RebarProperties StirrupSteel { get; set; }

        public BeamShearInput(
            double widthMm,
            double heightMm,
            double shearForceKn,
            ConcreteProperties concrete,
            RebarProperties stirrupSteel)
        {
            WidthMm = widthMm;
            HeightMm = heightMm;
            ShearForceKn = shearForceKn;
            Concrete = concrete;
            StirrupSteel = stirrupSteel;
        }
    }

    /// <summary>
    /// Output result of beam shear design per TCVN 5574:2018.
    /// </summary>
    public sealed class BeamShearResult
    {
        public double EffectiveDepthH0 { get; set; }
        public double VbMaxKn { get; set; }
        public bool IsWebCrushed { get; set; }
        public double VbMinKn { get; set; }
        public bool ConcreteResistsShearAlone { get; set; }
        public double AswMm2 { get; set; }
        public double CalculatedSpacingMm { get; set; }
        public double ConstructiveSpacingMm { get; set; }
        public double RecommendedSpacingMm { get; set; }
        public double ConcreteShearCapacityVbKn { get; set; }
        public double StirrupShearCapacityVswKn { get; set; }
        public double TotalShearCapacityVuKn { get; set; }
        public bool IsCompliant { get; set; }
        public string Note { get; set; } = string.Empty;
    }

    /// <summary>
    /// Beam shear design calculator per TCVN 5574:2018 Clauses 8.1.3 and 10.3.2.
    /// </summary>
    public static class BeamShearCalculator
    {
        private const double PhiB2 = 1.5;
        private const double PhiB3 = 0.5;

        public static BeamShearResult Calculate(BeamShearInput input)
        {
            ValidateInput(input);

            double b = input.WidthMm;
            double h0 = input.HeightMm - input.CoverTensionMm;
            double vN = Math.Abs(input.ShearForceKn) * 1e3;
            double asw = input.StirrupLegs * (Math.PI * input.StirrupDiameterMm * input.StirrupDiameterMm / 4.0);

            // 1. Concrete crushing limit V <= 0.3 * phi_w1 * phi_b1 * Rb * b * h0
            double phiW1 = 1.0;
            double phiB1 = 1.0 - 0.01 * input.Concrete.Rb;
            double vbMaxN = 0.3 * phiW1 * phiB1 * input.Concrete.Rb * b * h0;
            double vbMaxKn = vbMaxN / 1e3;

            if (vN > vbMaxN)
            {
                return CreateWebCrushedResult(h0, vbMaxKn, asw);
            }

            // 2. Minimum concrete shear capacity Vb,min = 0.5 * Rbt * b * h0
            double vbMinN = PhiB3 * input.Concrete.Rbt * b * h0;
            double vbMinKn = vbMinN / 1e3;
            double sConstructive = CalculateConstructiveSpacing(input.HeightMm, input.IsNearSupport);

            if (vN <= vbMinN)
            {
                return CreateConcreteAloneResult(h0, vbMaxKn, vbMinKn, asw, sConstructive);
            }

            return CalculateRequiredStirrups(input, b, h0, vN, asw, vbMaxKn, vbMinKn, sConstructive);
        }

        private static void ValidateInput(BeamShearInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.WidthMm <= 0) throw new ArgumentOutOfRangeException(nameof(input.WidthMm), "Width must be positive.");
            if (input.HeightMm <= 0) throw new ArgumentOutOfRangeException(nameof(input.HeightMm), "Height must be positive.");
            if (input.CoverTensionMm <= 0 || input.CoverTensionMm >= input.HeightMm)
            {
                throw new ArgumentOutOfRangeException(nameof(input.CoverTensionMm), "Cover must be within (0, h).");
            }
            if (input.StirrupDiameterMm <= 0) throw new ArgumentOutOfRangeException(nameof(input.StirrupDiameterMm), "Stirrup diameter must be positive.");
            if (input.StirrupLegs < 1) throw new ArgumentOutOfRangeException(nameof(input.StirrupLegs), "Stirrup legs must be >= 1.");
            if (input.Concrete == null) throw new ArgumentNullException(nameof(input.Concrete));
            if (input.StirrupSteel == null) throw new ArgumentNullException(nameof(input.StirrupSteel));
        }

        private static BeamShearResult CreateWebCrushedResult(double h0, double vbMaxKn, double asw)
        {
            return new BeamShearResult
            {
                EffectiveDepthH0 = h0,
                VbMaxKn = vbMaxKn,
                IsWebCrushed = true,
                VbMinKn = 0.0,
                ConcreteResistsShearAlone = false,
                AswMm2 = asw,
                CalculatedSpacingMm = 0.0,
                ConstructiveSpacingMm = 0.0,
                RecommendedSpacingMm = 0.0,
                ConcreteShearCapacityVbKn = 0.0,
                StirrupShearCapacityVswKn = 0.0,
                TotalShearCapacityVuKn = 0.0,
                IsCompliant = false,
                Note = "Concrete crushing limit exceeded: V > 0.3 * phi_w1 * phi_b1 * Rb * b * h0. Enlarge section."
            };
        }

        private static BeamShearResult CreateConcreteAloneResult(
            double h0, double vbMaxKn, double vbMinKn, double asw, double sConstructive)
        {
            double sRec = RoundSpacing(sConstructive);
            return new BeamShearResult
            {
                EffectiveDepthH0 = h0,
                VbMaxKn = vbMaxKn,
                IsWebCrushed = false,
                VbMinKn = vbMinKn,
                ConcreteResistsShearAlone = true,
                AswMm2 = asw,
                CalculatedSpacingMm = sConstructive,
                ConstructiveSpacingMm = sConstructive,
                RecommendedSpacingMm = sRec,
                ConcreteShearCapacityVbKn = vbMinKn,
                StirrupShearCapacityVswKn = 0.0,
                TotalShearCapacityVuKn = vbMinKn,
                IsCompliant = true,
                Note = "Concrete resists shear alone (V <= Vb,min). Constructive stirrups applied."
            };
        }

        private static BeamShearResult CalculateRequiredStirrups(
            BeamShearInput input,
            double b,
            double h0,
            double vN,
            double asw,
            double vbMaxKn,
            double vbMinKn,
            double sConstructive)
        {
            double mb1 = PhiB2 * input.Concrete.Rbt * b * h0 * h0;
            double qsw1 = (vN * vN) / (4.0 * mb1);
            double qsw2 = (vN - (0.75 * input.Concrete.Rbt * b * h0)) / (1.5 * h0);
            double qswMin = 0.25 * input.Concrete.Rbt * b;
            double qswReq = Math.Max(Math.Max(qsw1, qsw2), qswMin);

            double sCalc = (input.StirrupSteel.Rsw * asw) / qswReq;
            double sRec = RoundSpacing(Math.Min(sCalc, sConstructive));
            double qswProvided = sRec > 0 ? (input.StirrupSteel.Rsw * asw) / sRec : qswReq;

            double c0 = Math.Sqrt(mb1 / qswProvided);
            double c = Math.Max(h0, Math.Min(2.0 * h0, c0));
            double vb = mb1 / c;
            double vsw = 0.75 * qswProvided * c;

            bool compliant = sCalc >= 50.0;

            return new BeamShearResult
            {
                EffectiveDepthH0 = h0,
                VbMaxKn = vbMaxKn,
                IsWebCrushed = false,
                VbMinKn = vbMinKn,
                ConcreteResistsShearAlone = false,
                AswMm2 = asw,
                CalculatedSpacingMm = sCalc,
                ConstructiveSpacingMm = sConstructive,
                RecommendedSpacingMm = sRec,
                ConcreteShearCapacityVbKn = vb / 1e3,
                StirrupShearCapacityVswKn = vsw / 1e3,
                TotalShearCapacityVuKn = (vb + vsw) / 1e3,
                IsCompliant = compliant,
                Note = compliant
                    ? "Shear reinforcement satisfies diagonal tension and concrete crushing conditions."
                    : "Required stirrup spacing s < 50mm. Increase diameter or number of legs."
            };
        }

        private static double CalculateConstructiveSpacing(double h, bool isNearSupport)
        {
            if (isNearSupport)
            {
                return h <= 450.0 ? Math.Min(h / 2.0, 150.0) : 150.0;
            }
            return h > 150.0 ? Math.Min(0.75 * h, 300.0) : 300.0;
        }

        private static double RoundSpacing(double raw)
        {
            if (raw <= 50.0) return 50.0;
            if (raw < 100.0) return Math.Floor(raw / 25.0) * 25.0;
            if (raw <= 300.0) return Math.Floor(raw / 50.0) * 50.0;
            return 300.0;
        }
    }
}
