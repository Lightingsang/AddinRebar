using System;
using HPDcePro.Core.Calculations.Beam;

namespace HPDcePro.Core.Calculations.Steel
{
    /// <summary>
    /// Column anchor bolt and base plate calculator per TCVN 5575:2024 & TCVN 5574:2018.
    /// </summary>
    public static class AnchorBoltBasePlateCalculator
    {
        public static BasePlateResult Calculate(AnchorBoltBasePlateInput input)
        {
            ValidateInput(input);

            var concrete = MaterialLibrary.GetConcrete(input.PedestalGrade);
            double psi = ComputeConfinementFactor(input);
            double allowableBearing = 1.5 * psi * concrete.Rb;

            var (sigmaMax, sigmaMin, anchorTensionTotalKn) = ComputeBearingStresses(input);

            double cMax = ComputeCantileverOverhang(input);
            double reqPlateThickness = cMax * Math.Sqrt((3.0 * sigmaMax) / Math.Max(100.0, input.PlateYieldStrengthMpa));
            double plateRatio = reqPlateThickness / Math.Max(1.0, input.PlateThicknessMm);

            var (anchorTensionKn, anchorCapKn, tensionRatio, embedmentMm) =
                ComputeAnchorBoltMetrics(input, anchorTensionTotalKn);

            var (shearCapKn, shearRatio) = ComputeBaseShear(input);

            return new BasePlateResult
            {
                BearingStress = sigmaMax,
                MinBearingStress = sigmaMin,
                AllowableBearingStress = allowableBearing,
                BearingStressRatio = sigmaMax / Math.Max(0.1, allowableBearing),
                RequiredPlateThickness = reqPlateThickness,
                PlateThicknessRatio = plateRatio,
                AnchorBoltCount = input.AnchorBoltCount,
                AnchorBoltDiameter = input.AnchorBoltDiameterMm,
                AnchorTensionForceKn = anchorTensionKn,
                AnchorTensileCapacityKn = anchorCapKn,
                AnchorTensionRatio = tensionRatio,
                EmbedmentLength = embedmentMm,
                BaseShearCapacityKn = shearCapKn,
                BaseShearRatio = shearRatio
            };
        }

        private static double ComputeConfinementFactor(AnchorBoltBasePlateInput input)
        {
            double aPed = input.PedestalLengthMm * input.PedestalWidthMm;
            double aPlate = input.PlateLengthLMm * input.PlateWidthBMm;
            double ratio = aPlate > 0 ? aPed / aPlate : 1.0;
            return Math.Min(2.0, Math.Max(1.0, Math.Sqrt(ratio)));
        }

        private static (double, double, double) ComputeBearingStresses(AnchorBoltBasePlateInput input)
        {
            double l = input.PlateLengthLMm;
            double b = input.PlateWidthBMm;
            double n = Math.Max(0.0, input.AxialForceKn) * 1e3;
            double m = Math.Abs(input.BendingMomentKnm) * 1e6;

            double area = l * b;
            double wx = (b * l * l) / 6.0;
            double e = n > 1.0 ? m / n : 1e9;

            if (e <= l / 6.0)
            {
                double sMax = (n / area) + (m / wx);
                double sMin = (n / area) - (m / wx);
                return (sMax, Math.Max(0.0, sMin), 0.0);
            }

            double dRows = Math.Max(100.0, input.BoltDistanceBetweenRowsMm);
            double y = Math.Max(50.0, Math.Min(l * 0.8, (l * 0.5) - (e * 0.2) + 100.0));
            double leverArm = (l * 0.5) + (dRows * 0.5) - (y / 3.0);
            double nAnchorN = Math.Max(0.0, (m - (n * ((l * 0.5) - (y / 3.0)))) / Math.Max(50.0, leverArm));
            double sigmaMax = (2.0 * (n + nAnchorN)) / (b * y);

            return (sigmaMax, 0.0, nAnchorN * 1e-3);
        }

        private static double ComputeCantileverOverhang(AnchorBoltBasePlateInput input)
        {
            double m = (input.PlateLengthLMm - (0.95 * input.ColumnDepthMm)) * 0.5;
            double n = (input.PlateWidthBMm - (0.80 * input.ColumnWidthMm)) * 0.5;
            return Math.Max(15.0, Math.Max(m, n));
        }

        private static (double, double, double, double) ComputeAnchorBoltMetrics(
            AnchorBoltBasePlateInput input, double totalAnchorTensionKn)
        {
            int tensionBolts = Math.Max(1, input.AnchorBoltCount / 2);
            double tPerBolt = totalAnchorTensionKn / tensionBolts;

            double db = input.AnchorBoltDiameterMm;
            double asMm2 = 0.78 * (Math.PI * db * db * 0.25);
            double rbt = 0.55 * input.AnchorYieldStrengthMpa;
            double btCapKn = (rbt * asMm2) * 1e-3;
            double tensionRatio = btCapKn > 0 ? tPerBolt / btCapKn : 0.0;

            double minEmbedment = 25.0 * db;
            double bondEmbedment = (tPerBolt * 1e3) / (Math.PI * db * 1.5);
            double embedmentMm = Math.Max(minEmbedment, bondEmbedment);

            return (tPerBolt, btCapKn, tensionRatio, embedmentMm);
        }

        private static (double, double) ComputeBaseShear(AnchorBoltBasePlateInput input)
        {
            double db = input.AnchorBoltDiameterMm;
            double asMm2 = 0.78 * (Math.PI * db * db * 0.25);
            double nComp = Math.Max(0.0, input.AxialForceKn);
            double vFric = 0.35 * nComp;

            double rvb = 0.40 * input.AnchorYieldStrengthMpa;
            double vBoltCap = (input.AnchorBoltCount * rvb * asMm2) * 1e-3;
            double vTotalCap = vFric + (0.5 * vBoltCap);

            double vApplied = Math.Abs(input.ShearForceKn);
            double shearRatio = vTotalCap > 0 ? vApplied / vTotalCap : 1.0;

            return (vTotalCap, shearRatio);
        }

        private static void ValidateInput(AnchorBoltBasePlateInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.PlateLengthLMm <= 0 || input.PlateWidthBMm <= 0)
                throw new ArgumentOutOfRangeException(nameof(input.PlateLengthLMm), "Plate dimensions must be positive.");
            if (input.ColumnDepthMm <= 0 || input.ColumnWidthMm <= 0)
                throw new ArgumentOutOfRangeException(nameof(input.ColumnDepthMm), "Column dimensions must be positive.");
            if (input.AnchorBoltDiameterMm <= 0)
                throw new ArgumentOutOfRangeException(nameof(input.AnchorBoltDiameterMm), "Anchor bolt diameter must be positive.");
        }
    }
}
