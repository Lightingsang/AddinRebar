using System;
using HPDcePro.Core.Calculations.Beam;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public class BeamCalculationExtendedTests
    {
        [Fact]
        public void BeamFlexureCalculator_HighStrengthB40_CB500V_ComputesCompliantDesign()
        {
            var b40 = MaterialLibrary.GetConcrete(ConcreteGrade.B40);
            var cb500 = MaterialLibrary.GetRebar(RebarGrade.CB500V);

            // 300 x 600 mm beam, cover = 40 mm, M = 280 kNm
            var input = new BeamFlexureInput(300.0, 600.0, 280.0, b40, cb500);
            var result = BeamFlexureCalculator.Calculate(input);

            Assert.Equal(560.0, result.EffectiveDepthH0);
            Assert.False(result.IsDoublyReinforced);
            Assert.True(result.AsRequiredMm2 > 1000.0 && result.AsRequiredMm2 < 1800.0);
            Assert.True(result.ReinforcementRatioPercent >= 0.10);
            Assert.False(result.IsOverReinforced);
            Assert.True(result.IsCompliant);
        }

        [Fact]
        public void BeamCrackCalculator_MinimalBendingMoment_UnderMcrc_ReturnsZeroOrCompliantCrack()
        {
            var b30 = MaterialLibrary.GetConcrete(ConcreteGrade.B30);
            var cb400 = MaterialLibrary.GetRebar(RebarGrade.CB400V);

            // M = 0 -> zero crack width
            var zeroInput = new BeamCrackInput(250.0, 500.0, 0.0, 800.0, b30, cb400);
            var zeroRes = BeamCrackCalculator.Calculate(zeroInput);
            Assert.Equal(0.0, zeroRes.CrackWidthMm);
            Assert.True(zeroRes.IsCompliant);

            // Small moment M = 5.0 kNm -> tiny compliant crack
            var minInput = new BeamCrackInput(250.0, 500.0, 5.0, 800.0, b30, cb400);
            var minRes = BeamCrackCalculator.Calculate(minInput);
            Assert.True(minRes.CrackWidthMm >= 0.0);
            Assert.True(minRes.CrackWidthMm < 0.05);
            Assert.True(minRes.IsCompliant);
        }

        [Fact]
        public void BeamCrackCalculator_HighServiceMoment_ExceedsAllowableCrackWidth()
        {
            var b20 = MaterialLibrary.GetConcrete(ConcreteGrade.B20);
            var cb400 = MaterialLibrary.GetRebar(RebarGrade.CB400V);

            // High service moment on small beam with light reinforcement (2d18 = ~509 mm2)
            var input = new BeamCrackInput(200.0, 400.0, 160.0, 509.0, b20, cb400)
            {
                IsLongTerm = true
            };

            var res = BeamCrackCalculator.Calculate(input);

            Assert.True(res.CrackWidthMm > res.AllowableWidthMm);
            Assert.False(res.IsCompliant);
            Assert.Contains("excessive cracking", res.Note);
        }

        [Fact]
        public void BeamShearCalculator_WebCrushingCriteria_GovernsUnderExcessiveShear()
        {
            var b20 = MaterialLibrary.GetConcrete(ConcreteGrade.B20);
            var cb240 = MaterialLibrary.GetRebar(RebarGrade.CB240T);

            // 200 x 350 mm beam subjected to massive 1000 kN shear
            var input = new BeamShearInput(200.0, 350.0, 1000.0, b20, cb240);
            var res = BeamShearCalculator.Calculate(input);

            Assert.True(res.IsWebCrushed);
            Assert.False(res.IsCompliant);
            Assert.Contains("crushing limit exceeded", res.Note);
        }

        [Fact]
        public void BeamShearCalculator_ConcreteAlone_GovernsUnderMinimalShear()
        {
            var b40 = MaterialLibrary.GetConcrete(ConcreteGrade.B40);
            var cb240 = MaterialLibrary.GetRebar(RebarGrade.CB240T);

            // 300 x 600 mm beam with small 25 kN shear
            var input = new BeamShearInput(300.0, 600.0, 25.0, b40, cb240);
            var res = BeamShearCalculator.Calculate(input);

            Assert.False(res.IsWebCrushed);
            Assert.True(res.ConcreteResistsShearAlone);
            Assert.True(res.IsCompliant);
            Assert.Equal(res.ConstructiveSpacingMm, res.CalculatedSpacingMm);
        }

        [Fact]
        public void BeamShearCalculator_HighStrengthB40_CB500V_DesignStirrups()
        {
            var b40 = MaterialLibrary.GetConcrete(ConcreteGrade.B40);
            var stirrupSteel = MaterialLibrary.GetRebar(RebarGrade.CB240T);

            // 250 x 500 mm beam, shear = 160 kN
            var input = new BeamShearInput(250.0, 500.0, 160.0, b40, stirrupSteel)
            {
                StirrupDiameterMm = 8.0,
                StirrupLegs = 2
            };

            var res = BeamShearCalculator.Calculate(input);

            Assert.False(res.IsWebCrushed);
            Assert.False(res.ConcreteResistsShearAlone);
            Assert.True(res.CalculatedSpacingMm > 0.0);
            Assert.True(res.RecommendedSpacingMm >= 50.0);
            Assert.True(res.TotalShearCapacityVuKn >= 160.0);
            Assert.True(res.IsCompliant);
        }

        [Fact]
        public void BeamCalculations_MaterialCatalog_B40_And_CB500V_PropertiesExact()
        {
            var b40 = MaterialLibrary.GetConcrete(ConcreteGrade.B40);
            Assert.Equal(22.0, b40.Rb);
            Assert.Equal(1.40, b40.Rbt);
            Assert.Equal(36000.0, b40.Eb);

            var cb500 = MaterialLibrary.GetRebar(RebarGrade.CB500V);
            Assert.Equal(435.0, cb500.Rs);
            Assert.Equal(400.0, cb500.Rsc);
            Assert.Equal(300.0, cb500.Rsw);

            Assert.True(MaterialLibrary.TryGetConcrete("B40", out var resolvedB40));
            Assert.NotNull(resolvedB40);
            Assert.Equal(22.0, resolvedB40!.Rb);

            Assert.True(MaterialLibrary.TryGetRebar("CB500-V", out var resolvedCb500));
            Assert.NotNull(resolvedCb500);
            Assert.Equal(435.0, resolvedCb500!.Rs);
        }
    }
}
