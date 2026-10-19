using System;
using HPDcePro.Core.Calculations.Beam;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public class BeamCalculationsTests
    {
        [Fact]
        public void MaterialLibrary_ReturnsStandardConcreteGrades()
        {
            var b20 = MaterialLibrary.GetConcrete(ConcreteGrade.B20);
            Assert.Equal(11.5, b20.Rb);
            Assert.Equal(0.90, b20.Rbt);
            Assert.Equal(27500.0, b20.Eb);

            var b25 = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            Assert.Equal(14.5, b25.Rb);
            Assert.Equal(1.05, b25.Rbt);
            Assert.Equal(30000.0, b25.Eb);

            var b30 = MaterialLibrary.GetConcrete(ConcreteGrade.B30);
            Assert.Equal(17.0, b30.Rb);
            Assert.Equal(1.15, b30.Rbt);
            Assert.Equal(32500.0, b30.Eb);
        }

        [Fact]
        public void MaterialLibrary_ReturnsStandardRebarGrades()
        {
            var cb240 = MaterialLibrary.GetRebar(RebarGrade.CB240T);
            Assert.Equal(210.0, cb240.Rs);
            Assert.Equal(170.0, cb240.Rsw);

            var cb400 = MaterialLibrary.GetRebar(RebarGrade.CB400V);
            Assert.Equal(350.0, cb400.Rs);
            Assert.Equal(350.0, cb400.Rsc);
            Assert.Equal(280.0, cb400.Rsw);
            Assert.True(cb400.XiR > 0.5 && cb400.XiR < 0.6);

            var cb500 = MaterialLibrary.GetRebar(RebarGrade.CB500V);
            Assert.Equal(435.0, cb500.Rs);
            Assert.Equal(400.0, cb500.Rsc); // Capped at 400 MPa
            Assert.Equal(300.0, cb500.Rsw);
        }

        [Fact]
        public void MaterialLibrary_TryGetByName_ResolvesCorrectly()
        {
            Assert.True(MaterialLibrary.TryGetConcrete("B25", out var c25));
            Assert.NotNull(c25);
            Assert.Equal(14.5, c25!.Rb);

            Assert.True(MaterialLibrary.TryGetRebar("CB400-V", out var s400));
            Assert.NotNull(s400);
            Assert.Equal(350.0, s400!.Rs);

            Assert.False(MaterialLibrary.TryGetConcrete("Unknown", out _));
            Assert.False(MaterialLibrary.TryGetRebar("Unknown", out _));
        }

        [Fact]
        public void BeamFlexureCalculator_SinglyReinforced_ComputesCorrectAs()
        {
            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25); // Rb = 14.5 MPa
            var steel = MaterialLibrary.GetRebar(RebarGrade.CB400V);      // Rs = 350 MPa
            var input = new BeamFlexureInput(250.0, 500.0, 120.0, concrete, steel);

            var result = BeamFlexureCalculator.Calculate(input);

            Assert.Equal(460.0, result.EffectiveDepthH0);
            Assert.False(result.IsDoublyReinforced);
            Assert.True(result.AsRequiredMm2 > 700.0 && result.AsRequiredMm2 < 1200.0);
            Assert.True(result.ReinforcementRatioPercent >= 0.10);
            Assert.False(result.IsOverReinforced);
            Assert.True(result.IsCompliant);
        }

        [Fact]
        public void BeamFlexureCalculator_DoublyReinforced_WhenMomentExceedsAlphaR()
        {
            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B20); // Rb = 11.5 MPa
            var steel = MaterialLibrary.GetRebar(RebarGrade.CB300V);      // Rs = 260 MPa
            // Large moment to force doubly reinforced section
            var input = new BeamFlexureInput(200.0, 400.0, 250.0, concrete, steel);

            var result = BeamFlexureCalculator.Calculate(input);

            Assert.True(result.IsDoublyReinforced);
            Assert.True(result.AsPrimeRequiredMm2 > 0.0);
            Assert.True(result.AsRequiredMm2 > result.AsPrimeRequiredMm2);
        }

        [Fact]
        public void BeamFlexureCalculator_ZeroMoment_AppliesMinimumReinforcement()
        {
            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            var steel = MaterialLibrary.GetRebar(RebarGrade.CB400V);
            var input = new BeamFlexureInput(250.0, 500.0, 0.0, concrete, steel);

            var result = BeamFlexureCalculator.Calculate(input);

            double expectedMinAs = 0.001 * 250.0 * 460.0;
            Assert.Equal(expectedMinAs, result.AsRequiredMm2, 2);
            Assert.Equal(0.10, result.ReinforcementRatioPercent, 2);
            Assert.True(result.IsCompliant);
        }

        [Fact]
        public void BeamShearCalculator_ConcreteAlone_WhenShearIsSmall()
        {
            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25); // Rbt = 1.05 MPa
            var stirrupSteel = MaterialLibrary.GetRebar(RebarGrade.CB240T);
            var input = new BeamShearInput(250.0, 500.0, 30.0, concrete, stirrupSteel);

            var result = BeamShearCalculator.Calculate(input);

            Assert.True(result.ConcreteResistsShearAlone);
            Assert.False(result.IsWebCrushed);
            Assert.True(result.RecommendedSpacingMm <= 150.0);
            Assert.True(result.IsCompliant);
        }

        [Fact]
        public void BeamShearCalculator_StirrupsCalculated_UnderModerateShear()
        {
            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            var stirrupSteel = MaterialLibrary.GetRebar(RebarGrade.CB240T);
            var input = new BeamShearInput(250.0, 500.0, 150.0, concrete, stirrupSteel);

            var result = BeamShearCalculator.Calculate(input);

            Assert.False(result.ConcreteResistsShearAlone);
            Assert.False(result.IsWebCrushed);
            Assert.True(result.CalculatedSpacingMm > 0.0);
            Assert.True(result.RecommendedSpacingMm >= 50.0);
            Assert.True(result.TotalShearCapacityVuKn >= 150.0);
            Assert.True(result.IsCompliant);
        }

        [Fact]
        public void BeamShearCalculator_WebCrushing_UnderExcessiveShear()
        {
            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B20);
            var stirrupSteel = MaterialLibrary.GetRebar(RebarGrade.CB240T);
            // Excessive shear force
            var input = new BeamShearInput(200.0, 300.0, 800.0, concrete, stirrupSteel);

            var result = BeamShearCalculator.Calculate(input);

            Assert.True(result.IsWebCrushed);
            Assert.False(result.IsCompliant);
        }

        [Fact]
        public void BeamCrackCalculator_CalculatesShortAndLongTermWidths()
        {
            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            var steel = MaterialLibrary.GetRebar(RebarGrade.CB400V);

            // Short-term crack check
            var shortInput = new BeamCrackInput(250.0, 500.0, 80.0, 1000.0, concrete, steel)
            {
                IsLongTerm = false
            };
            var shortRes = BeamCrackCalculator.Calculate(shortInput);

            // Long-term crack check
            var longInput = new BeamCrackInput(250.0, 500.0, 80.0, 1000.0, concrete, steel)
            {
                IsLongTerm = true
            };
            var longRes = BeamCrackCalculator.Calculate(longInput);

            Assert.True(shortRes.CrackWidthMm > 0.0);
            Assert.True(longRes.CrackWidthMm > shortRes.CrackWidthMm); // Long-term crack is wider (phi1 = 1.4 vs 1.0)
            Assert.Equal(0.40, shortRes.AllowableWidthMm);
            Assert.Equal(0.30, longRes.AllowableWidthMm);
        }

        [Fact]
        public void BeamFlexureCalculator_NegativeMoment_HandledSymmetrically()
        {
            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            var steel = MaterialLibrary.GetRebar(RebarGrade.CB400V);

            var positiveInput = new BeamFlexureInput(250.0, 500.0, 100.0, concrete, steel);
            var negativeInput = new BeamFlexureInput(250.0, 500.0, -100.0, concrete, steel);

            var posRes = BeamFlexureCalculator.Calculate(positiveInput);
            var negRes = BeamFlexureCalculator.Calculate(negativeInput);

            Assert.Equal(posRes.AsRequiredMm2, negRes.AsRequiredMm2, 3);
            Assert.Equal(posRes.AlphaM, negRes.AlphaM, 4);
        }

        [Fact]
        public void BeamCalculators_InvalidInputs_ThrowExpectedExceptions()
        {
            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            var steel = MaterialLibrary.GetRebar(RebarGrade.CB400V);

            // Width <= 0
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                BeamFlexureCalculator.Calculate(new BeamFlexureInput(-200.0, 500.0, 50.0, concrete, steel)));

            // Cover >= Height
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                BeamFlexureCalculator.Calculate(new BeamFlexureInput(200.0, 100.0, 50.0, concrete, steel)
                {
                    CoverTensionMm = 120.0
                }));

            // Null concrete
            Assert.Throws<ArgumentNullException>(() =>
                BeamFlexureCalculator.Calculate(new BeamFlexureInput(200.0, 400.0, 50.0, null!, steel)));

            // Shear invalid legs
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                BeamShearCalculator.Calculate(new BeamShearInput(200.0, 400.0, 50.0, concrete, steel)
                {
                    StirrupLegs = 0
                }));
        }
    }
}
