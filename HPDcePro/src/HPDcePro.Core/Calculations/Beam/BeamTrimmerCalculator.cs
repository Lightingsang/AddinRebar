using System;

namespace HPDcePro.Core.Calculations.Beam
{
    public sealed class BeamTrimmerInput
    {
        public double PrimaryBeamWidthMm { get; set; } = 300.0;
        public double PrimaryBeamHeightMm { get; set; } = 600.0;
        public double SecondaryBeamWidthMm { get; set; } = 220.0;
        public double SecondaryBeamHeightMm { get; set; } = 500.0;
        public double SecondaryShearReactionKn { get; set; } = 80.0;
        public double ConcreteRbtMpa { get; set; } = 1.05; // e.g. B20
        public double StirrupRswMpa { get; set; } = 225.0; // e.g. CB240-T
        public double StirrupDiameterMm { get; set; } = 8.0;
        public int StirrupLegCount { get; set; } = 2;
        public double PrimaryCoverMm { get; set; } = 30.0;
    }

    public sealed class HangerReinforcementResult
    {
        public double RequiredHangerAreaAshMm2 { get; set; }
        public bool IsHangerRequired { get; set; }
        public int RecommendedStirrupPairs { get; set; }
        public double RecommendedStirrupDiameterMm { get; set; } = 8.0;
        public double ZoneWidthMm { get; set; }
        public double ZoneLeftOffsetMm { get; set; }
        public double ZoneRightOffsetMm { get; set; }
        public string Note { get; set; } = string.Empty;
    }

    public static class BeamTrimmerCalculator
    {
        public static HangerReinforcementResult Calculate(BeamTrimmerInput input)
        {
            Validate(input);

            double hm = input.PrimaryBeamHeightMm;
            double hs = input.SecondaryBeamHeightMm;
            double bm = input.PrimaryBeamWidthMm;
            double bs = input.SecondaryBeamWidthMm;
            double h0 = hm - input.PrimaryCoverMm;

            double thresholdKn = 0.5 * input.ConcreteRbtMpa * bm * h0 * 1e-3;
            if (input.SecondaryShearReactionKn <= thresholdKn)
            {
                return new HangerReinforcementResult
                {
                    RequiredHangerAreaAshMm2 = 0.0,
                    IsHangerRequired = false,
                    RecommendedStirrupPairs = 0,
                    ZoneWidthMm = bs + 2.0 * h0,
                    ZoneLeftOffsetMm = hm * 0.5,
                    ZoneRightOffsetMm = hm * 0.5,
                    Note = "Secondary beam shear load is below threshold; no additional hanger rebar required."
                };
            }

            double factor = (hs >= hm) ? 1.0 : Math.Max(0.0, 1.0 - (hs / hm));
            double fvN = input.SecondaryShearReactionKn * 1000.0;
            double ash = (fvN * factor) / input.StirrupRswMpa;

            double singleLegArea = 0.25 * Math.PI * input.StirrupDiameterMm * input.StirrupDiameterMm;
            double pairArea = singleLegArea * input.StirrupLegCount * 2.0; // pair on both sides
            int pairs = Math.Max(1, (int)Math.Ceiling(ash / pairArea));

            return new HangerReinforcementResult
            {
                RequiredHangerAreaAshMm2 = ash,
                IsHangerRequired = true,
                RecommendedStirrupPairs = pairs,
                RecommendedStirrupDiameterMm = input.StirrupDiameterMm,
                ZoneWidthMm = bs + 2.0 * h0,
                ZoneLeftOffsetMm = hm * 0.5,
                ZoneRightOffsetMm = hm * 0.5,
                Note = $"Concentrated hanger rebar {pairs} pairs of {input.StirrupLegCount}-leg d{input.StirrupDiameterMm} stirrups across joint."
            };
        }

        private static void Validate(BeamTrimmerInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.PrimaryBeamWidthMm <= 0 || input.PrimaryBeamHeightMm <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(input.PrimaryBeamWidthMm), "Primary beam dimensions must be positive.");
            }
            if (input.SecondaryBeamWidthMm <= 0 || input.SecondaryBeamHeightMm <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(input.SecondaryBeamWidthMm), "Secondary beam dimensions must be positive.");
            }
            if (input.StirrupRswMpa <= 0) throw new ArgumentOutOfRangeException(nameof(input.StirrupRswMpa), "Stirrup yield strength must be positive.");
        }
    }
}
