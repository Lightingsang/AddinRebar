using System;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Foundation;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public sealed class PadFootingTests
    {
        private static PadFootingInput CreateStandardInput(
            double nKn = 800.0, double mxKnm = 60.0, double myKnm = 40.0, double rtcKpa = 250.0)
        {
            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            var rebar = MaterialLibrary.GetRebar(RebarGrade.CB400V);
            return new PadFootingInput(
                "MD-1", 2000.0, 2400.0, 600.0, 400.0, 400.0, 1.5,
                concrete, rebar, nKn, mxKnm, myKnm, rtcKpa);
        }

        [Fact]
        public void Calculate_ConcentricAxialLoad_UniformSoilPressure()
        {
            var input = CreateStandardInput(nKn: 600.0, mxKnm: 0.0, myKnm: 0.0);
            var result = PadFootingCalculator.Calculate(input);

            Assert.Equal(0.0, result.EccentricityXMm, 2);
            Assert.Equal(0.0, result.EccentricityYMm, 2);
            Assert.Equal(result.BasePressureMaxKpa, result.BasePressureMinKpa, 1);
            Assert.Equal(result.BasePressureAvgKpa, result.BasePressureMaxKpa, 1);
            Assert.False(result.HasUplift);
            Assert.True(result.IsOverallSafe);
        }

        [Fact]
        public void Calculate_EccentricBending_CalculatesMaxMinPressure()
        {
            var input = CreateStandardInput(nKn: 800.0, mxKnm: 60.0, myKnm: 40.0);
            var result = PadFootingCalculator.Calculate(input);

            Assert.True(result.BasePressureMaxKpa > result.BasePressureAvgKpa);
            Assert.True(result.BasePressureMinKpa < result.BasePressureAvgKpa);
            Assert.True(result.BasePressureMinKpa > 0.0);
            Assert.False(result.HasUplift);
            Assert.True(result.SoilCapacityRatio <= 1.0);
        }

        [Fact]
        public void Calculate_LargeEccentricity_DetectsUpliftAndRecomputesCompression()
        {
            // Moment so large that e > L/6 (2400/6 = 400mm)
            var input = CreateStandardInput(nKn: 400.0, mxKnm: 200.0, myKnm: 180.0);
            var result = PadFootingCalculator.Calculate(input);

            Assert.True(result.HasUplift);
            Assert.Equal(0.0, result.BasePressureMinKpa);
            Assert.True(result.BasePressureMaxKpa > result.BasePressureAvgKpa);
        }

        [Fact]
        public void Calculate_PunchingShear_EvaluatesBasePyramid()
        {
            var input = CreateStandardInput(nKn: 900.0, mxKnm: 30.0, myKnm: 20.0);
            var result = PadFootingCalculator.Calculate(input);

            Assert.True(result.PunchingCapacityKn > 500.0);
            Assert.True(result.PunchingRatio <= 1.0);
            Assert.True(result.IsPunchingSafe);
        }

        [Fact]
        public void Calculate_FlexuralRebar_ComputesCantileverMoments()
        {
            var input = CreateStandardInput(nKn: 800.0, mxKnm: 40.0, myKnm: 30.0);
            var result = PadFootingCalculator.Calculate(input);

            Assert.True(result.MomentAtColumnFaceXkNm > 0.0);
            Assert.True(result.MomentAtColumnFaceYkNm > 0.0);
            Assert.True(result.RequiredAsXMm2 > 500.0);
            Assert.True(result.RequiredAsYMm2 > 500.0);
        }

        [Fact]
        public void Calculate_ExcessiveSoilPressure_FailsSafety()
        {
            // Extreme load exceeding soil capacity
            var input = CreateStandardInput(nKn: 3000.0, mxKnm: 200.0, myKnm: 150.0, rtcKpa: 150.0);
            var result = PadFootingCalculator.Calculate(input);

            Assert.False(result.IsOverallSafe);
            Assert.True(result.SoilCapacityRatio > 1.0);
            Assert.Contains("FAIL", result.StatusSummary);
        }

        [Fact]
        public void GeneratePadFootingDrawing_ProducesValidCadPrimitives()
        {
            var input = CreateStandardInput();
            var result = PadFootingCalculator.Calculate(input);
            var gen = new FoundationDetailingGenerator();

            var buffer = gen.GeneratePadFootingDrawing(result, input, new Point2D(0, 0));
            Assert.True(buffer.Count >= 4);
        }
    }
}
