using System;
using System.Collections.Generic;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Column;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public sealed class ColumnPmmTests
    {
        private static ColumnPmmInput CreateStandardInput(
            double b = 400.0, double h = 400.0, double l0 = 3600.0,
            double n = 1200.0, double mx = 80.0, double my = 60.0,
            ColumnSectionShape shape = ColumnSectionShape.Rectangular)
        {
            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            var rebar = MaterialLibrary.GetRebar(RebarGrade.CB400V);

            double cover = 30.0;
            double halfB = b * 0.5 - cover;
            double halfH = h * 0.5 - cover;

            var bars = new List<ColumnBarPlacement>
            {
                new ColumnBarPlacement(-halfB, -halfH, 20),
                new ColumnBarPlacement(halfB, -halfH, 20),
                new ColumnBarPlacement(halfB, halfH, 20),
                new ColumnBarPlacement(-halfB, halfH, 20),
                new ColumnBarPlacement(0, -halfH, 18),
                new ColumnBarPlacement(0, halfH, 18),
                new ColumnBarPlacement(-halfB, 0, 18),
                new ColumnBarPlacement(halfB, 0, 18)
            };

            return new ColumnPmmInput("C1", shape, b, h, l0, concrete, rebar, bars, n, mx, my);
        }

        [Fact]
        public void PureAxialCompression_CalculatesPureCapacityCorrectly()
        {
            var input = CreateStandardInput(400.0, 400.0, 3000.0, n: 1000.0, mx: 0.0, my: 0.0);
            var result = ColumnPmmInteractionCalculator.Calculate(input);

            // Theoretical N0 = (Rb * Ac + Rsc * As) * 1e-3
            // Ac = 160000, As = 4*314.16 + 4*254.47 = 2274.5 mm2
            // N0 = (14.5 * 160000 + 350 * 2274.5) * 1e-3 = 2320 + 796 = ~3116 kN
            Assert.True(result.AxialCapacityN0Kn > 2800.0 && result.AxialCapacityN0Kn < 3400.0);
            Assert.True(result.IsSafe);
        }

        [Fact]
        public void ShortColumn_SlendernessEtaRemainsOne()
        {
            // Short column L0 = 1000 mm, lambda < 14
            var input = CreateStandardInput(400.0, 400.0, l0: 1000.0, n: 1200.0, mx: 80.0, my: 60.0);
            var result = ColumnPmmInteractionCalculator.Calculate(input);

            Assert.Equal(1.0, result.SlendernessEtaX);
            Assert.Equal(1.0, result.SlendernessEtaY);
            Assert.Equal(80.0, result.MagnifiedMomentMxdKnm);
            Assert.Equal(60.0, result.MagnifiedMomentMydKnm);
        }

        [Fact]
        public void SlenderColumn_IncreasesBucklingEtaFactor()
        {
            // Slender column L0 = 6500 mm
            var input = CreateStandardInput(400.0, 400.0, l0: 6500.0, n: 1200.0, mx: 80.0, my: 60.0);
            var result = ColumnPmmInteractionCalculator.Calculate(input);

            Assert.True(result.SlendernessEtaX > 1.0, "EtaX should exceed 1.0 for slender column");
            Assert.True(result.SlendernessEtaY > 1.0, "EtaY should exceed 1.0 for slender column");
            Assert.True(result.MagnifiedMomentMxdKnm > 80.0);
            Assert.True(result.MagnifiedMomentMydKnm > 60.0);
        }

        [Fact]
        public void UniaxialBending_CalculatesMxCapacity()
        {
            var input = CreateStandardInput(400.0, 400.0, 3000.0, n: 800.0, mx: 90.0, my: 0.0);
            var result = ColumnPmmInteractionCalculator.Calculate(input);

            Assert.True(result.MomentCapacityMx0Knm > 90.0, "Capacity should exceed design moment");
            Assert.True(result.UtilizationRatio < 1.0);
            Assert.True(result.IsSafe);
        }

        [Fact]
        public void BiaxialBending_EvaluatesBreslerContour()
        {
            var input = CreateStandardInput(400.0, 400.0, 3600.0, n: 1000.0, mx: 80.0, my: 80.0);
            var result = ColumnPmmInteractionCalculator.Calculate(input);

            double uniaxialRatioX = result.MagnifiedMomentMxdKnm / result.MomentCapacityMx0Knm;
            double uniaxialRatioY = result.MagnifiedMomentMydKnm / result.MomentCapacityMy0Knm;

            // Biaxial combination should be greater than or equal to single axis ratio
            Assert.True(result.UtilizationRatio >= uniaxialRatioX);
            Assert.True(result.UtilizationRatio >= uniaxialRatioY);
            Assert.NotNull(result.StatusSummary);
        }

        [Fact]
        public void CircularColumn_CalculatesPmmInteractionAndCapacity()
        {
            var input = CreateStandardInput(500.0, 500.0, 3600.0, n: 1200.0, mx: 70.0, my: 70.0, shape: ColumnSectionShape.Circular);
            var result = ColumnPmmInteractionCalculator.Calculate(input);

            Assert.True(result.AxialCapacityN0Kn > 2500.0);
            Assert.True(result.MomentCapacityMx0Knm > 50.0);
            Assert.NotEmpty(result.InteractionCurve);
            Assert.True(result.InteractionCurve.Count >= 10);
        }

        [Fact]
        public void ExtremeAxialOverload_FailsUtilizationAndSafety()
        {
            // Axial load far exceeding N0
            var input = CreateStandardInput(400.0, 400.0, 3600.0, n: 5000.0, mx: 150.0, my: 120.0);
            var result = ColumnPmmInteractionCalculator.Calculate(input);

            Assert.True(result.UtilizationRatio > 1.0);
            Assert.False(result.IsSafe);
            Assert.Contains("FAIL", result.StatusSummary);
        }

        [Fact]
        public void InteractionCurve_EnvelopePointsSpanFromZeroToN0()
        {
            var input = CreateStandardInput(400.0, 400.0, 3000.0, n: 1000.0, mx: 50.0, my: 50.0);
            var result = ColumnPmmInteractionCalculator.Calculate(input);

            Assert.NotEmpty(result.InteractionCurve);
            Assert.Equal(0.0, result.InteractionCurve[0].AxialKn, 1);
            Assert.True(result.InteractionCurve[^1].AxialKn > 2000.0);
            Assert.All(result.InteractionCurve, pt => Assert.True(pt.MomentXkNm >= 0.0));
        }
    }
}
