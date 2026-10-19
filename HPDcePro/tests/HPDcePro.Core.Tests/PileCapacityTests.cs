using System;
using System.Collections.Generic;
using HPDcePro.Core.Calculations.Pile;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public class PileCapacityTests
    {
        [Fact]
        public void CalculateUnitTipResistance_DrivenVsBored_AppliesCorrectBounds()
        {
            // For N60 = 25 in sand (cohesionless)
            double qbDrivenSand = PileCapacityCalculator.CalculateUnitTipResistance(
                PileInstallationType.DrivenPile, SoilConsistency.Cohesionless, 25.0);
            double qbBoredSand = PileCapacityCalculator.CalculateUnitTipResistance(
                PileInstallationType.BoredPile, SoilConsistency.Cohesionless, 25.0);

            Assert.Equal(350.0 * 25.0, qbDrivenSand);
            Assert.Equal(150.0 * 25.0, qbBoredSand);
            Assert.True(qbDrivenSand > qbBoredSand);

            // Capping check for very high SPT
            double qbCapped = PileCapacityCalculator.CalculateUnitTipResistance(
                PileInstallationType.DrivenPile, SoilConsistency.Cohesionless, 50.0);
            Assert.Equal(10000.0, qbCapped); // Capped at 10,000 kPa
        }

        [Fact]
        public void CalculateUnitShaftFriction_CohesiveVsCohesionless()
        {
            // Cohesive: fi = min(10 * N^0.6, 100)
            double fiClay = PileCapacityCalculator.CalculateUnitShaftFriction(
                PileInstallationType.DrivenPile, SoilConsistency.Cohesive, 16.0);
            double expectedClay = 10.0 * Math.Pow(16.0, 0.6);
            Assert.Equal(expectedClay, fiClay, 3);

            // Cohesionless: fi = min(Ks * N, 120) with Ks = 2.0 for driven
            double fiSand = PileCapacityCalculator.CalculateUnitShaftFriction(
                PileInstallationType.DrivenPile, SoilConsistency.Cohesionless, 20.0);
            Assert.Equal(40.0, fiSand, 3);
        }

        [Fact]
        public void Calculate_DrivenPile_IntegratesLayersAndTipCapacity()
        {
            var pile = new PileGeometry
            {
                IsCircular = true,
                DiameterM = 0.40, // D400
                TopDepthM = 1.0,
                TipDepthM = 21.0  // Length = 20 m
            };

            var layers = new List<SoilLayer>
            {
                new("Lop 1 - Bun set", SoilConsistency.Cohesive, 0.0, 6.0, 3.0),
                new("Lop 2 - Set deo mem", SoilConsistency.Cohesive, 6.0, 14.0, 8.0),
                new("Lop 3 - Cat min chat vua", SoilConsistency.Cohesionless, 14.0, 25.0, 22.0)
            };

            var result = PileCapacityCalculator.Calculate(
                pile, layers, PileInstallationType.DrivenPile, tipSoilN60: 25.0, tipSoilType: SoilConsistency.Cohesionless);

            Assert.True(result.UnitTipResistanceQbKpa > 0.0);
            Assert.True(result.TipCapacityRbKn > 0.0);
            Assert.True(result.ShaftCapacityRsKn > 0.0);
            Assert.Equal(result.TipCapacityRbKn + result.ShaftCapacityRsKn, result.UltimateCapacityRuKn, 2);

            double expectedRc = result.UltimateCapacityRuKn / (1.4 * 1.15);
            Assert.Equal(expectedRc, result.DesignCapacityRcKn, 2);

            // Layer contributions: all 3 layers intersected
            Assert.Equal(3, result.LayerContributions.Count);
        }

        [Fact]
        public void Calculate_InvalidDepths_ThrowsArgumentException()
        {
            var invalidPile = new PileGeometry
            {
                TopDepthM = 10.0,
                TipDepthM = 5.0 // Tip above top
            };

            Assert.Throws<ArgumentException>(() =>
            {
                PileCapacityCalculator.Calculate(
                    invalidPile, new List<SoilLayer>(), PileInstallationType.DrivenPile, 20.0, SoilConsistency.Cohesionless);
            });
        }

        [Fact]
        public void Calculate_RectangularPileGeometry_CalculatesAreaAndPerimeter()
        {
            var squarePile = new PileGeometry
            {
                IsCircular = false,
                WidthM = 0.35,
                HeightM = 0.35,
                TopDepthM = 0.0,
                TipDepthM = 15.0
            };

            Assert.Equal(0.35 * 0.35, squarePile.AreaM2, 4);
            Assert.Equal(4.0 * 0.35, squarePile.PerimeterM, 4);

            var layers = new List<SoilLayer>
            {
                new("Lop 1", SoilConsistency.Cohesive, 0.0, 15.0, 10.0)
            };

            var res = PileCapacityCalculator.Calculate(
                squarePile, layers, PileInstallationType.DrivenPile, 15.0, SoilConsistency.Cohesive);

            Assert.True(res.TipCapacityRbKn > 0.0);
            Assert.True(res.ShaftCapacityRsKn > 0.0);
        }

        [Fact]
        public void Calculate_ZeroSpt_ReturnsZeroCapacitiesSafely()
        {
            double qbZero = PileCapacityCalculator.CalculateUnitTipResistance(
                PileInstallationType.DrivenPile, SoilConsistency.Cohesionless, 0.0);
            double fiZero = PileCapacityCalculator.CalculateUnitShaftFriction(
                PileInstallationType.DrivenPile, SoilConsistency.Cohesionless, 0.0);

            Assert.Equal(0.0, qbZero);
            Assert.Equal(0.0, fiZero);
        }
    }
}
