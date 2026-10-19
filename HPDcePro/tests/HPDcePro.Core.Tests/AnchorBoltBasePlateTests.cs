using System;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Steel;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public sealed class AnchorBoltBasePlateTests
    {
        [Fact]
        public void Calculate_SmallEccentricity_ComputesTrapezoidalConcreteBearingPressure()
        {
            var input = new AnchorBoltBasePlateInput
            {
                PlateLengthLMm = 600.0,
                PlateWidthBMm = 400.0,
                AxialForceKn = 800.0,
                BendingMomentKnm = 30.0, // e = 37.5mm <= 600/6 = 100mm
                PedestalGrade = ConcreteGrade.B25
            };

            var result = AnchorBoltBasePlateCalculator.Calculate(input);

            Assert.True(result.BearingStress > 0);
            Assert.True(result.MinBearingStress >= 0);
            Assert.Equal(0.0, result.AnchorTensionForceKn);
            Assert.True(result.IsBearingSafe);
        }

        [Fact]
        public void Calculate_LargeEccentricity_EngagesAnchorBoltTension()
        {
            var input = new AnchorBoltBasePlateInput
            {
                PlateLengthLMm = 600.0,
                PlateWidthBMm = 400.0,
                AxialForceKn = 300.0,
                BendingMomentKnm = 120.0, // large moment -> uplift/tension
                AnchorBoltCount = 4,
                AnchorBoltDiameterMm = 24.0
            };

            var result = AnchorBoltBasePlateCalculator.Calculate(input);

            Assert.True(result.AnchorTensionForceKn > 0, "Large eccentricity must engage anchor bolt tension");
            Assert.True(result.AnchorTensileCapacityKn > 0);
            Assert.True(result.AnchorTensionRatio > 0);
        }

        [Fact]
        public void Calculate_CantileverPlateThickness_VerifiesSufficientThickness()
        {
            var input = new AnchorBoltBasePlateInput
            {
                ColumnDepthMm = 400.0,
                ColumnWidthMm = 250.0,
                PlateLengthLMm = 650.0,
                PlateWidthBMm = 450.0,
                PlateThicknessMm = 45.0,
                AxialForceKn = 600.0,
                BendingMomentKnm = 80.0
            };

            var result = AnchorBoltBasePlateCalculator.Calculate(input);

            Assert.True(result.RequiredPlateThickness > 10.0);
            Assert.True(result.PlateThicknessRatio <= 1.0);
            Assert.True(result.IsPlateThicknessSafe);
        }

        [Fact]
        public void Calculate_EmbedmentLength_EnforcesMinimum25DbRequirement()
        {
            double db = 24.0;
            var input = new AnchorBoltBasePlateInput
            {
                AnchorBoltDiameterMm = db,
                AxialForceKn = 200.0,
                BendingMomentKnm = 80.0
            };

            var result = AnchorBoltBasePlateCalculator.Calculate(input);

            double expectedMinEmbedment = 25.0 * db; // 600 mm
            Assert.True(result.EmbedmentLength >= expectedMinEmbedment,
                $"Embedment {result.EmbedmentLength} must be at least 25 * db = {expectedMinEmbedment}");
        }

        [Fact]
        public void Calculate_BaseShearResistance_CombinesFrictionAndBoltShear()
        {
            var input = new AnchorBoltBasePlateInput
            {
                AxialForceKn = 500.0,
                ShearForceKn = 80.0,
                AnchorBoltCount = 4,
                AnchorBoltDiameterMm = 24.0
            };

            var result = AnchorBoltBasePlateCalculator.Calculate(input);

            Assert.True(result.BaseShearCapacityKn > 100.0);
            Assert.True(result.BaseShearRatio <= 1.0);
            Assert.True(result.IsShearSafe);
        }

        [Fact]
        public void Calculate_PedestalConfinement_LimitsPsiFactorTo2()
        {
            // Very large pedestal relative to base plate -> sqrt(A_ped / A_plate) > 2 -> capped at 2.0
            var input = new AnchorBoltBasePlateInput
            {
                PlateLengthLMm = 400.0,
                PlateWidthBMm = 400.0,
                PedestalLengthMm = 2000.0,
                PedestalWidthMm = 2000.0,
                PedestalGrade = ConcreteGrade.B20
            };

            var result = AnchorBoltBasePlateCalculator.Calculate(input);

            // B20 has Rb = 11.5 MPa. With psi capped at 2.0, allowable = 1.5 * 2.0 * 11.5 = 34.5 MPa
            Assert.Equal(34.5, result.AllowableBearingStress, precision: 1);
        }

        [Fact]
        public void Calculate_InvalidInput_ThrowsArgumentOutOfRangeException()
        {
            var badLength = new AnchorBoltBasePlateInput { PlateLengthLMm = 0 };
            Assert.Throws<ArgumentOutOfRangeException>(() => AnchorBoltBasePlateCalculator.Calculate(badLength));

            var badCol = new AnchorBoltBasePlateInput { ColumnDepthMm = -50 };
            Assert.Throws<ArgumentOutOfRangeException>(() => AnchorBoltBasePlateCalculator.Calculate(badCol));

            var badBolt = new AnchorBoltBasePlateInput { AnchorBoltDiameterMm = 0 };
            Assert.Throws<ArgumentOutOfRangeException>(() => AnchorBoltBasePlateCalculator.Calculate(badBolt));
        }
    }
}
