using System;
using System.Linq;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Slab;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public sealed class TwoWaySlabTests
    {
        private static TwoWaySlabInput CreateStandardInput(
            double lxMm = 4000.0, double lyMm = 5000.0, double thicknessH = 120.0,
            SlabBoundaryCondition boundary = SlabBoundaryCondition.FourEdgesFixed)
        {
            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            var rebar = MaterialLibrary.GetRebar(RebarGrade.CB400V);
            return new TwoWaySlabInput("S1", lxMm, lyMm, thicknessH, 3.5, 2.4, boundary, concrete, rebar);
        }

        [Fact]
        public void Calculate_TwoWaySlab_CalculatesMarcusMoments()
        {
            var input = CreateStandardInput(4000.0, 5000.0, 120.0);
            var result = TwoWaySlabCalculator.Calculate(input);

            Assert.True(result.IsTwoWay);
            Assert.Equal(1.25, result.AspectRatioLambda, 2);
            Assert.True(result.SpanPositiveMomentMxKnm > 0.0);
            Assert.True(result.SpanPositiveMomentMyKnm > 0.0);
            Assert.True(result.SupportNegativeMomentMxKnm > 0.0);
            Assert.True(result.RequiredAsBotXMm2 > 100.0);
            Assert.True(result.IsSafe);
        }

        [Fact]
        public void Calculate_OneWaySlab_WhenAspectRatioExceedsTwo()
        {
            var input = CreateStandardInput(2500.0, 6000.0, 100.0);
            var result = TwoWaySlabCalculator.Calculate(input);

            Assert.False(result.IsTwoWay);
            Assert.Equal(2.4, result.AspectRatioLambda, 1);
            Assert.True(result.SpanPositiveMomentMxKnm > result.SpanPositiveMomentMyKnm);
            Assert.True(result.RequiredAsBotXMm2 > result.RequiredAsBotYMm2);
        }

        [Fact]
        public void Calculate_BoundaryConditions_AltersMomentDistribution()
        {
            var inputSimple = CreateStandardInput(4000.0, 4000.0, 120.0, SlabBoundaryCondition.FourEdgesSimplySupported);
            var resultSimple = TwoWaySlabCalculator.Calculate(inputSimple);

            var inputFixed = CreateStandardInput(4000.0, 4000.0, 120.0, SlabBoundaryCondition.FourEdgesFixed);
            var resultFixed = TwoWaySlabCalculator.Calculate(inputFixed);

            Assert.Equal(0.0, resultSimple.SupportNegativeMomentMxKnm);
            Assert.True(resultFixed.SupportNegativeMomentMxKnm > 0.0);
            Assert.True(resultSimple.SpanPositiveMomentMxKnm > resultFixed.SpanPositiveMomentMxKnm);
        }

        [Fact]
        public void Calculate_Reinforcement_GeneratesProperLayersAndCutoffs()
        {
            var input = CreateStandardInput(4000.0, 5000.0, 120.0);
            var result = TwoWaySlabCalculator.Calculate(input);

            Assert.Equal(4, result.Layers.Count);
            Assert.Equal(1000.0, result.TopBarCutoffLengthXMm); // 4000 * 0.25
            Assert.Equal(1250.0, result.TopBarCutoffLengthYMm); // 5000 * 0.25
            Assert.All(result.Layers, l => Assert.True(l.ProvidedAsMm2PerM >= l.RequiredAsMm2PerM));
        }

        [Fact]
        public void Calculate_SpacerChair_CalculatesCorrectHeight()
        {
            var input = CreateStandardInput(4000.0, 5000.0, 120.0);
            var result = TwoWaySlabCalculator.Calculate(input);

            // Chair height = 120 - 2*15 - 10 - 10 = 70mm
            Assert.Equal(70.0, result.SpacerChairHeightMm);
            Assert.Equal(1.0, result.SpacerChairCountPerM2);
        }

        [Fact]
        public void Calculate_SlenderSlab_FlagsDeflectionRisk()
        {
            // Very thin slab for large span L = 6000, H = 80 -> L/H = 75 > 35
            var input = CreateStandardInput(6000.0, 6000.0, 80.0);
            var result = TwoWaySlabCalculator.Calculate(input);

            Assert.False(result.IsDeflectionSafe);
            Assert.False(result.IsSafe);
            Assert.Contains("FAIL", result.StatusSummary);
        }

        [Fact]
        public void GenerateSlabDrawing_GeneratesCadPrimitives()
        {
            var input = CreateStandardInput();
            var result = TwoWaySlabCalculator.Calculate(input);
            var gen = new FoundationDetailingGenerator();

            var buffer = gen.GenerateSlabDrawing(result, input, new Point2D(0, 0));
            Assert.True(buffer.Count >= 5);
        }
    }
}
