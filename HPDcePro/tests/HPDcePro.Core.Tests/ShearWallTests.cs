using System;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Wall;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public sealed class ShearWallTests
    {
        private static ShearWallInput CreateStandardInput(
            double lw = 3000.0, double tw = 250.0, double hw = 3600.0,
            double n = 2500.0, double vu = 400.0, double mu = 900.0,
            int vertDia = 12, double vertSpacing = 200.0,
            int horizDia = 10, double horizSpacing = 150.0,
            int crossTies = 4)
        {
            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            var rebar = MaterialLibrary.GetRebar(RebarGrade.CB400V);

            return new ShearWallInput(
                "W1", lw, tw, hw, concrete, rebar, n, vu, mu,
                vertDia, vertSpacing, horizDia, horizSpacing, crossTies);
        }

        [Fact]
        public void BoundaryElement_SizedCorrectly_WhenCompressiveStressExceedsThreshold()
        {
            // High compression and moment creates sigmaC > 0.20 * Rb
            var input = CreateStandardInput(lw: 3000.0, tw: 250.0, n: 3000.0, vu: 300.0, mu: 1200.0);
            var result = ShearWallCalculator.Calculate(input);

            Assert.True(result.BoundaryElementsRequired);
            Assert.True(result.MaxCompressiveStressMpa > result.StressThresholdMpa);

            // lc >= max(0.15*3000=450, 1.5*250=375, 300) = 450 mm
            Assert.True(result.BoundaryElement.LengthLcMm >= 450.0);
            Assert.Equal(250.0, result.BoundaryElement.WidthBcMm);
            Assert.True(result.BoundaryElement.RebarRatioPercent >= 0.50, "Boundary rebar ratio must be >= 0.5%");
            Assert.True(result.BoundaryElement.TieSpacingMm <= 100.0, "Confinement tie spacing must be <= 100 mm");
        }

        [Fact]
        public void BoundaryElement_NotRequired_WhenCompressiveStressLow()
        {
            // Low compression and negligible moment
            var input = CreateStandardInput(lw: 4000.0, tw: 300.0, n: 500.0, vu: 200.0, mu: 50.0);
            var result = ShearWallCalculator.Calculate(input);

            Assert.False(result.BoundaryElementsRequired);
            Assert.True(result.MaxCompressiveStressMpa <= result.StressThresholdMpa);
        }

        [Fact]
        public void WebReinforcement_ValidatesMinimumRatio()
        {
            // phi12a200 (muV ~ 0.45%), phi10a150 (muH ~ 0.42%), crossTies = 4
            var input = CreateStandardInput(vertDia: 12, vertSpacing: 200.0, horizDia: 10, horizSpacing: 150.0, crossTies: 4);
            var result = ShearWallCalculator.Calculate(input);

            Assert.True(result.WebReinforcement.VerticalRatioPercent >= 0.25);
            Assert.True(result.WebReinforcement.HorizontalRatioPercent >= 0.25);
            Assert.Equal(2, result.WebReinforcement.CurtainCount);
            Assert.True(result.WebReinforcement.CrossTieDensityPerM2 >= 4);
            Assert.True(result.WebReinforcement.IsSufficient);
        }

        [Fact]
        public void WebReinforcement_FailsWhenRatioTooLow()
        {
            // Sparse rebar phi8a300 yields ratio ~0.13% < 0.25%
            var input = CreateStandardInput(vertDia: 8, vertSpacing: 300.0, horizDia: 8, horizSpacing: 300.0, crossTies: 2);
            var result = ShearWallCalculator.Calculate(input);

            Assert.False(result.WebReinforcement.IsSufficient);
            Assert.False(result.IsOverallSafe);
        }

        [Fact]
        public void ShearCapacity_CalculatesConcreteAndStirrupContributions()
        {
            var input = CreateStandardInput(lw: 3000.0, tw: 250.0, vu: 400.0);
            var result = ShearWallCalculator.Calculate(input);

            Assert.True(result.ConcreteShearCapacityVbKn > 100.0, "Concrete shear contribution Vb should be > 100 kN");
            Assert.True(result.WebShearCapacityVswKn > 200.0, "Horizontal web shear contribution Vsw should be > 200 kN");
            Assert.True(result.TotalShearCapacityVuMaxKn >= result.ConcreteShearCapacityVbKn + result.WebShearCapacityVswKn - 1.0);
            Assert.True(result.IsShearSafe);
            Assert.True(result.ShearUtilizationRatio < 1.0);
        }

        [Fact]
        public void ShearOverload_TriggersFailure()
        {
            // Overloaded shear Vu = 3000 kN
            var input = CreateStandardInput(lw: 3000.0, tw: 250.0, vu: 3000.0);
            var result = ShearWallCalculator.Calculate(input);

            Assert.False(result.IsShearSafe);
            Assert.True(result.ShearUtilizationRatio > 1.0);
            Assert.False(result.IsOverallSafe);
            Assert.Contains("FAIL", result.Summary);
        }

        [Fact]
        public void InvalidThickness_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                CreateStandardInput(tw: 120.0); // Under minimum 150 mm
            });
        }
    }
}
