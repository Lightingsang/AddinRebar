using System;
using HPDcePro.Core.Calculations.Steel;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public sealed class SteelMomentConnectionTests
    {
        [Fact]
        public void Calculate_StandardExtendedEndPlate_EvaluatesYieldLineThickness()
        {
            var input = new SteelMomentConnectionInput
            {
                BeamDepthMm = 500.0,
                BeamFlangeWidthMm = 200.0,
                BeamFlangeThicknessMm = 16.0,
                BendingMomentKnm = 160.0,
                EndPlateThicknessMm = 25.0,
                YieldStrengthMpa = 345.0
            };

            var result = SteelMomentConnectionCalculator.Calculate(input);

            Assert.True(result.RequiredPlateThickness > 10.0);
            Assert.True(result.PlateThicknessRatio <= 1.0);
            Assert.True(result.FlangeTensionKn > 0);
        }

        [Fact]
        public void Calculate_BoltTensionAndPrying_ComputesPryingForceWithin30PercentCap()
        {
            var input = new SteelMomentConnectionInput
            {
                BeamDepthMm = 600.0,
                BendingMomentKnm = 200.0,
                EndPlateThicknessMm = 20.0,
                BoltDiameterMm = 24.0,
                TensionBoltRowCount = 2
            };

            var result = SteelMomentConnectionCalculator.Calculate(input);

            Assert.True(result.PryingForceKn >= 0.0);
            Assert.True(result.PryingForceKn <= (0.30 * result.BoltTensionKn) + 1e-4);
            Assert.Equal(result.BoltTensionKn + result.PryingForceKn, result.TotalBoltTensionKn, precision: 2);
            Assert.True(result.BoltTensileCapacityKn > 0);
        }

        [Fact]
        public void Calculate_BoltShearAndTensionInteraction_ComputesQuadraticSum()
        {
            var input = new SteelMomentConnectionInput
            {
                BendingMomentKnm = 150.0,
                ShearForceKn = 100.0,
                BoltDiameterMm = 24.0,
                Grade = BoltGrade.Grade8_8
            };

            var result = SteelMomentConnectionCalculator.Calculate(input);

            double expectedInteraction = (result.BoltTensionRatio * result.BoltTensionRatio) +
                                         (result.BoltShearRatio * result.BoltShearRatio);
            Assert.Equal(expectedInteraction, result.BoltInteractionRatio, precision: 3);
            Assert.True(result.BoltInteractionRatio <= 1.0);
        }

        [Fact]
        public void Calculate_FlangeAndWebWelds_ChecksStressRatios()
        {
            var input = new SteelMomentConnectionInput
            {
                BendingMomentKnm = 180.0,
                ShearForceKn = 120.0,
                FlangeWeldLegMm = 10.0,
                WebWeldLegMm = 8.0,
                WeldDesignStrengthMpa = 180.0
            };

            var result = SteelMomentConnectionCalculator.Calculate(input);

            Assert.True(result.FlangeWeldStressMpa > 0);
            Assert.True(result.WebWeldStressMpa > 0);
            Assert.True(result.WeldStressRatio > 0);
            Assert.True(result.WeldStressRatio <= 1.0);
        }

        [Fact]
        public void Calculate_ColumnWebAndFlange_DeterminesStiffenerRequirement()
        {
            // High moment connection on a thin column web -> stiffeners required
            var input = new SteelMomentConnectionInput
            {
                ColumnDepthMm = 400.0,
                ColumnWebThicknessMm = 8.0,
                ColumnFlangeThicknessMm = 12.0,
                BendingMomentKnm = 280.0,
                ColumnYieldStrengthMpa = 245.0
            };

            var result = SteelMomentConnectionCalculator.Calculate(input);

            Assert.True(result.StiffenersRequired, "High moment should require column transverse stiffeners");
            Assert.True(result.StiffenerThickness >= input.BeamFlangeThicknessMm);
        }

        [Fact]
        public void Calculate_Grade10_9Bolts_ProvidesHigherTensileCapacityThanGrade8_8()
        {
            var input88 = new SteelMomentConnectionInput { Grade = BoltGrade.Grade8_8, BoltDiameterMm = 24.0 };
            var input109 = new SteelMomentConnectionInput { Grade = BoltGrade.Grade10_9, BoltDiameterMm = 24.0 };

            var res88 = SteelMomentConnectionCalculator.Calculate(input88);
            var res109 = SteelMomentConnectionCalculator.Calculate(input109);

            Assert.True(res109.BoltTensileCapacityKn > res88.BoltTensileCapacityKn);
            Assert.True(res109.BoltShearCapacityKn > res88.BoltShearCapacityKn);
        }

        [Fact]
        public void Calculate_InvalidInput_ThrowsArgumentOutOfRangeException()
        {
            var badDepth = new SteelMomentConnectionInput { BeamDepthMm = 0 };
            Assert.Throws<ArgumentOutOfRangeException>(() => SteelMomentConnectionCalculator.Calculate(badDepth));

            var badPlate = new SteelMomentConnectionInput { EndPlateThicknessMm = -5 };
            Assert.Throws<ArgumentOutOfRangeException>(() => SteelMomentConnectionCalculator.Calculate(badPlate));

            var badBolt = new SteelMomentConnectionInput { BoltDiameterMm = 0 };
            Assert.Throws<ArgumentOutOfRangeException>(() => SteelMomentConnectionCalculator.Calculate(badBolt));
        }

        [Fact]
        public void Calculate_HighMoment_ReportsFailedComplianceWhenOverstressed()
        {
            var input = new SteelMomentConnectionInput
            {
                BendingMomentKnm = 900.0, // excessive moment
                ShearForceKn = 500.0,
                EndPlateThicknessMm = 12.0,
                BoltDiameterMm = 16.0,
                FlangeWeldLegMm = 4.0
            };

            var result = SteelMomentConnectionCalculator.Calculate(input);

            Assert.False(result.IsCompliant);
            Assert.True(result.PlateThicknessRatio > 1.0 || result.BoltInteractionRatio > 1.0 || result.WeldStressRatio > 1.0);
        }
    }
}
