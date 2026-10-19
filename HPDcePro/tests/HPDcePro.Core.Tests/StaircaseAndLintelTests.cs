using System;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Secondary;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public sealed class StaircaseAndLintelTests
    {
        private static StaircaseInput CreateStandardStair(
            StaircaseType type = StaircaseType.WaistSlab, int steps = 11, double flightL = 3000.0, double totalH = 1760.0)
        {
            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            var rebar = MaterialLibrary.GetRebar(RebarGrade.CB400V);
            // 11 risers -> hr = 1760/11 = 160mm. bt = 3000/10 = 300mm. 2*160+300 = 620mm (perfect ergonomic)
            return new StaircaseInput("THANG-1", type, flightL, totalH, 120.0, steps, 1200.0, concrete, rebar);
        }

        private static LintelInput CreateStandardLintel(
            double clearSpanL0 = 1200.0, double bearing = 250.0, double depthH = 200.0)
        {
            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            var rebar = MaterialLibrary.GetRebar(RebarGrade.CB400V);
            return new LintelInput("LT-1", clearSpanL0, bearing, 220.0, depthH, 1500.0, concrete, rebar);
        }

        [Fact]
        public void Staircase_WaistSlab_ErgonomicCheckAndLoad()
        {
            var input = CreateStandardStair(StaircaseType.WaistSlab);
            var result = StaircaseCalculator.Calculate(input);

            Assert.Equal(160.0, result.RiserHeightMm, 1);
            Assert.Equal(300.0, result.TreadWidthMm, 1);
            Assert.Equal(620.0, result.BlondelFormulaValue, 1);
            Assert.True(result.IsErgonomicSafe);
            Assert.True(result.MaxDesignMomentKnm > 0.0);
            Assert.True(result.RequiredMainAsMm2 > 300.0);
            Assert.True(result.IsSafe);
        }

        [Fact]
        public void Staircase_FoldedPlateZigZag_AppliesHigherSelfWeightFactor()
        {
            var inputWaist = CreateStandardStair(StaircaseType.WaistSlab);
            var resultWaist = StaircaseCalculator.Calculate(inputWaist);

            var inputZigZag = CreateStandardStair(StaircaseType.FoldedPlateZigZag);
            var resultZigZag = StaircaseCalculator.Calculate(inputZigZag);

            Assert.True(resultZigZag.TotalDesignLoadQKnM > resultWaist.TotalDesignLoadQKnM);
            Assert.Contains("ziczac", resultZigZag.DetailingNotes, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Staircase_CrankedBarLapLength_MeetsThirtyFiveDiameters()
        {
            var input = CreateStandardStair();
            input.MainBarDiameterMm = 14;
            var result = StaircaseCalculator.Calculate(input);

            // 35 * 14 = 490 mm
            Assert.Equal(490.0, result.CrankedBarLapLengthMm);
        }

        [Fact]
        public void Lintel_StandardOpening_CalculatesMomentAndMainRebar()
        {
            var input = CreateStandardLintel(1200.0, 250.0, 200.0);
            var result = LintelCalculator.Calculate(input);

            Assert.Equal(1700.0, result.TotalSpanLMm); // 1200 + 2*250
            Assert.True(result.IsBearingAdequate);
            Assert.True(result.EquivalentArchingHeightMm <= 1200.0 / 3.0);
            Assert.True(result.DesignMomentKnm > 0.0);
            Assert.True(result.RequiredBottomAsMm2 > 30.0);
            Assert.Equal(2, result.RecommendedMainBarCount);
            Assert.True(result.RecommendedMainBarDiameter >= 10);
            Assert.True(result.IsSafe);
        }

        [Fact]
        public void Lintel_InadequateBearing_FlagsSafetyDefect()
        {
            // Bearing 150mm < 200mm standard requirement
            var input = CreateStandardLintel(1200.0, bearing: 150.0, 200.0);
            var result = LintelCalculator.Calculate(input);

            Assert.False(result.IsBearingAdequate);
            Assert.False(result.IsSafe);
            Assert.Contains("FAIL", result.StatusSummary);
        }

        [Fact]
        public void Lintel_ShearCapacity_IncludesConcreteAndStirrups()
        {
            var input = CreateStandardLintel(1500.0, 250.0, 250.0);
            var result = LintelCalculator.Calculate(input);

            Assert.True(result.ShearCapacityKn > result.DesignShearKn);
            Assert.True(result.IsShearSafe);
        }

        [Fact]
        public void GenerateStaircaseAndLintelDrawings_GeneratesCadBuffers()
        {
            var stairInput = CreateStandardStair();
            var stairResult = StaircaseCalculator.Calculate(stairInput);

            var lintelInput = CreateStandardLintel();
            var lintelResult = LintelCalculator.Calculate(lintelInput);

            var gen = new FoundationDetailingGenerator();
            var stairBuf = gen.GenerateStaircaseDrawing(stairResult, stairInput, new Point2D(0, 0));
            var lintelBuf = gen.GenerateLintelDrawing(lintelResult, lintelInput, new Point2D(0, 0));

            Assert.True(stairBuf.Count >= 3);
            Assert.True(lintelBuf.Count >= 5);
        }
    }
}
