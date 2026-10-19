using System;

namespace HPDcePro.Core.Calculations.Steel
{
    /// <summary>
    /// Bolted extended end-plate moment connection calculator per TCVN 5575:2024.
    /// </summary>
    public static class SteelMomentConnectionCalculator
    {
        public static SteelConnectionResult Calculate(SteelMomentConnectionInput input)
        {
            ValidateInput(input);

            double leverArmMm = Math.Max(100.0, input.BeamDepthMm - input.BeamFlangeThicknessMm);
            double flangeTensionKn = (Math.Abs(input.BendingMomentKnm) * 1e3 / leverArmMm) + (input.AxialForceKn * 0.5);

            int tensionBolts = Math.Max(2, input.TensionBoltRowCount * 2);
            int totalBolts = tensionBolts + (Math.Max(1, input.CompressionBoltRowCount) * 2);

            var (boltTensionKn, pryingKn, totalBoltTensionKn, btKn, tensionRatio) =
                ComputeBoltTension(input, flangeTensionKn, tensionBolts);

            var (boltShearKn, vbKn, shearRatio, interactionRatio) =
                ComputeBoltShearAndInteraction(input, totalBolts, totalBoltTensionKn, btKn);

            var (reqPlateThickness, plateRatio) = ComputeYieldLineEndPlate(input, flangeTensionKn);
            var (flangeWeldStress, webWeldStress, weldRatio) = ComputeWelds(input, flangeTensionKn);
            var (panelShearKn, panelCapKn, stiffenersReq, stiffenerThick) = CheckColumnStiffeners(input, flangeTensionKn);

            return new SteelConnectionResult
            {
                FlangeTensionKn = flangeTensionKn,
                BoltTensionKn = boltTensionKn,
                PryingForceKn = pryingKn,
                TotalBoltTensionKn = totalBoltTensionKn,
                BoltTensileCapacityKn = btKn,
                BoltTensionRatio = tensionRatio,
                BoltShearKn = boltShearKn,
                BoltShearCapacityKn = vbKn,
                BoltShearRatio = shearRatio,
                BoltInteractionRatio = interactionRatio,
                RequiredPlateThickness = reqPlateThickness,
                PlateThicknessRatio = plateRatio,
                FlangeWeldStressMpa = flangeWeldStress,
                WebWeldStressMpa = webWeldStress,
                WeldStressRatio = weldRatio,
                ColumnWebPanelShearKn = panelShearKn,
                ColumnWebShearCapacityKn = panelCapKn,
                StiffenersRequired = stiffenersReq,
                StiffenerThickness = stiffenerThick
            };
        }

        private static (double, double, double, double, double) ComputeBoltTension(
            SteelMomentConnectionInput input, double flangeTensionKn, int tensionBolts)
        {
            double tBolt = flangeTensionKn / tensionBolts;
            double db = input.BoltDiameterMm;
            double bpPrime = input.BoltPitchTensionMm - (db * 0.5);
            double apPrime = Math.Min(input.EndPlateExtensionMm, 1.25 * bpPrime);
            double tp = input.EndPlateThicknessMm;

            double pryingRatio = (apPrime > 0 && db > 0)
                ? Math.Max(0.0, Math.Min(0.30, ((3.0 * bpPrime) / (8.0 * apPrime)) - (Math.Pow(tp, 3) / (328000.0 * db))))
                : 0.0;
            double qKn = tBolt * pryingRatio;
            double totalBoltTension = tBolt + qKn;

            double asMm2 = 0.78 * (Math.PI * db * db * 0.25);
            double rbt = input.Grade == BoltGrade.Grade10_9 ? 500.0 : 400.0;
            double btKn = (rbt * asMm2) * 1e-3;
            double tensionRatio = btKn > 0 ? totalBoltTension / btKn : 1.0;

            return (tBolt, qKn, totalBoltTension, btKn, tensionRatio);
        }

        private static (double, double, double, double) ComputeBoltShearAndInteraction(
            SteelMomentConnectionInput input, int totalBolts, double totalBoltTensionKn, double btKn)
        {
            double db = input.BoltDiameterMm;
            double asMm2 = 0.78 * (Math.PI * db * db * 0.25);
            double rvb = input.Grade == BoltGrade.Grade10_9 ? 380.0 : 300.0;
            double vbKn = (rvb * asMm2) * 1e-3;

            double vBolt = Math.Abs(input.ShearForceKn) / totalBolts;
            double shearRatio = vbKn > 0 ? vBolt / vbKn : 0.0;

            double tRatio = btKn > 0 ? totalBoltTensionKn / btKn : 0.0;
            double interaction = (tRatio * tRatio) + (shearRatio * shearRatio);

            return (vBolt, vbKn, shearRatio, interaction);
        }

        private static (double, double) ComputeYieldLineEndPlate(SteelMomentConnectionInput input, double flangeTensionKn)
        {
            double mx = Math.Max(15.0, input.BoltPitchTensionMm - (0.707 * input.FlangeWeldLegMm));
            double bEff = Math.Min(input.EndPlateWidthMm, input.BeamFlangeWidthMm + (2.0 * input.EndPlateExtensionMm));
            double mPl = (flangeTensionKn * 1e3 * mx) / (2.0 * Math.Max(50.0, bEff));

            double reqThickness = Math.Sqrt((4.0 * mPl) / input.YieldStrengthMpa);
            double ratio = reqThickness / Math.Max(1.0, input.EndPlateThicknessMm);
            return (reqThickness, ratio);
        }

        private static (double, double, double) ComputeWelds(SteelMomentConnectionInput input, double flangeTensionKn)
        {
            double lWf = (2.0 * input.BeamFlangeWidthMm) + (2.0 * Math.Max(0.0, input.BeamFlangeWidthMm - input.BeamWebThicknessMm));
            double af = 0.707 * Math.Max(3.0, input.FlangeWeldLegMm);
            double stressWf = (flangeTensionKn * 1e3) / (af * Math.Max(10.0, lWf));

            double lWw = 2.0 * Math.Max(20.0, input.BeamDepthMm - (2.0 * input.BeamFlangeThicknessMm));
            double aw = 0.707 * Math.Max(3.0, input.WebWeldLegMm);
            double stressWw = (Math.Abs(input.ShearForceKn) * 1e3) / (aw * lWw);

            double maxRatio = Math.Max(stressWf, stressWw) / input.WeldDesignStrengthMpa;
            return (stressWf, stressWw, maxRatio);
        }

        private static (double, double, bool, double) CheckColumnStiffeners(
            SteelMomentConnectionInput input, double flangeTensionKn)
        {
            double panelShearKn = flangeTensionKn * 0.85;
            double panelCapKn = (0.58 * input.ColumnYieldStrengthMpa * input.ColumnDepthMm * input.ColumnWebThicknessMm) * 1e-3;
            double flangeCapKn = (0.625 * Math.Pow(input.ColumnFlangeThicknessMm, 2) * input.ColumnYieldStrengthMpa) * 1e-3;

            bool stiffenersReq = (flangeTensionKn > flangeCapKn) || (panelShearKn > panelCapKn);
            double stiffenerThickness = Math.Max(input.BeamFlangeThicknessMm, 10.0);

            return (panelShearKn, panelCapKn, stiffenersReq, stiffenerThickness);
        }

        private static void ValidateInput(SteelMomentConnectionInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.BeamDepthMm <= 0 || input.BeamFlangeWidthMm <= 0)
                throw new ArgumentOutOfRangeException(nameof(input.BeamDepthMm), "Beam dimensions must be positive.");
            if (input.EndPlateThicknessMm <= 0)
                throw new ArgumentOutOfRangeException(nameof(input.EndPlateThicknessMm), "End plate thickness must be positive.");
            if (input.BoltDiameterMm <= 0)
                throw new ArgumentOutOfRangeException(nameof(input.BoltDiameterMm), "Bolt diameter must be positive.");
        }
    }
}
