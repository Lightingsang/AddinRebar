using System;
using System.Linq;
using HPKata.Core.Foundations.Calculators;
using HPKata.Core.Foundations.Models;
using Xunit;

namespace HPKata.Core.Tests.Foundations
{
    /// <summary>
    /// Empirical challenge and stress tests for Foundation calculators (Milestone M3 Phase 4).
    /// Author: challenger_m3_1 (Empirical Challenger).
    /// </summary>
    public sealed class FoundationEmpiricalChallengeTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(9)]
        public void PileCentroidInvariance_Counts1To9_CenterOfMassIsOrigin(int pileCount)
        {
            var (piles, recL, recB) = PileCoordinateGenerator.GenerateLayout(pileCount, pileDiaMm: 400.0, customSpacingMm: 1200.0);
            Assert.Equal(pileCount, piles.Count);

            double xBar = piles.Average(p => p.X);
            double yBar = piles.Average(p => p.Y);

            Assert.True(Math.Abs(xBar) < 1e-4, $"Pile count {pileCount}: X_bar {xBar} exceeds tolerance 1e-4");
            Assert.True(Math.Abs(yBar) < 1e-4, $"Pile count {pileCount}: Y_bar {yBar} exceeds tolerance 1e-4");
            Assert.True(recL > 0 && recB > 0, "Recommended dimensions must be positive");
        }

        [Theory]
        [InlineData(12, 4, 3)]
        [InlineData(16, 4, 4)]
        [InlineData(20, 5, 4)]
        [InlineData(25, 5, 5)]
        [InlineData(36, 6, 6)]
        [InlineData(100, 10, 10)]
        public void PileCentroidInvariance_FullGrids_CenterOfMassIsOrigin(int pileCount, int nx, int ny)
        {
            Assert.Equal(pileCount, nx * ny);
            var (piles, recL, recB) = PileCoordinateGenerator.GenerateLayout(pileCount, pileDiaMm: 350.0, customSpacingMm: 1050.0);
            Assert.Equal(pileCount, piles.Count);

            double xBar = piles.Average(p => p.X);
            double yBar = piles.Average(p => p.Y);

            Assert.True(Math.Abs(xBar) < 1e-4, $"Grid {nx}x{ny}: X_bar {xBar} exceeds tolerance 1e-4");
            Assert.True(Math.Abs(yBar) < 1e-4, $"Grid {nx}x{ny}: Y_bar {yBar} exceeds tolerance 1e-4");
        }

        [Theory]
        [InlineData(200.0, 600.0, 250.0)]
        [InlineData(250.0, 750.0, 250.0)]
        [InlineData(300.0, 900.0, 300.0)]
        [InlineData(400.0, 1200.0, 400.0)]
        [InlineData(500.0, 1500.0, 500.0)]
        [InlineData(600.0, 1800.0, 600.0)]
        [InlineData(800.0, 2400.0, 800.0)]
        public void PileSpacingAndEdgeDistances_BoundarySweeps_Enforces3DAndMin250mm(
            double dia, double expectedDefaultSpacing, double expectedMinEdgeDist)
        {
            var (piles, recL, recB) = PileCoordinateGenerator.GenerateLayout(4, dia);
            double actualSpacing = Math.Abs(piles[1].X - piles[0].X);
            Assert.Equal(expectedDefaultSpacing, actualSpacing);

            double minEdgeCe = Math.Max(250.0, 1.0 * dia);
            Assert.Equal(expectedMinEdgeDist, minEdgeCe);

            double halfL = recL / 2.0;
            double halfB = recB / 2.0;

            foreach (var p in piles)
            {
                double distLeft = (p.X - p.DiameterMm / 2.0) - (-halfL);
                double distRight = halfL - (p.X + p.DiameterMm / 2.0);
                double distBottom = (p.Y - p.DiameterMm / 2.0) - (-halfB);
                double distTop = halfB - (p.Y + p.DiameterMm / 2.0);

                Assert.True(distLeft >= minEdgeCe - 1e-4, $"Left clearance {distLeft} < {minEdgeCe}");
                Assert.True(distRight >= minEdgeCe - 1e-4, $"Right clearance {distRight} < {minEdgeCe}");
                Assert.True(distBottom >= minEdgeCe - 1e-4, $"Bottom clearance {distBottom} < {minEdgeCe}");
                Assert.True(distTop >= minEdgeCe - 1e-4, $"Top clearance {distTop} < {minEdgeCe}");
            }
        }

        [Fact]
        public void PileCapVolume_NetVolumeDeduction_SubtractsExactPileVolumes()
        {
            var (piles, _, _) = PileCoordinateGenerator.GenerateLayout(5, pileDiaMm: 400.0);
            foreach (var p in piles) p.EmbedmentDepthMm = 150.0;

            var cap = new PileCapModel
            {
                LengthMm = 2400.0,
                WidthMm = 2400.0,
                TotalHeightMm = 1200.0,
                Piles = piles,
                PileDiameterMm = 400.0,
                PileEmbedmentDepthMm = 100.0,
                MemberCount = 2
            };

            double vol = FootingGeometryCalculator.CalculatePileCapVolume(cap);
            double grossMm3 = 2400.0 * 2400.0 * 1200.0;
            double singlePileDedMm3 = Math.PI * 200.0 * 200.0 * 150.0;
            double totalPileDedMm3 = 5.0 * singlePileDedMm3;
            double expectedNetM3 = (grossMm3 - totalPileDedMm3) / 1e9 * 2;

            Assert.Equal(expectedNetM3, vol, 4);

            // Test fallback when pile EmbedmentDepthMm is 0
            foreach (var p in piles) p.EmbedmentDepthMm = 0.0;
            cap.PileEmbedmentDepthMm = 120.0;
            double volFallback = FootingGeometryCalculator.CalculatePileCapVolume(cap);
            double fallbackDedMm3 = 5.0 * (Math.PI * 200.0 * 200.0 * 120.0);
            double expectedFallbackM3 = (grossMm3 - fallbackDedMm3) / 1e9 * 2;
            Assert.Equal(expectedFallbackM3, volFallback, 4);
        }

        [Theory]
        [InlineData(1600.0, 1600.0, 500.0, 500.0, 450.0)]
        [InlineData(2000.0, 1600.0, 500.0, 400.0, 600.0)]
        [InlineData(2400.0, 1800.0, 600.0, 450.0, 500.0)]
        [InlineData(3000.0, 2000.0, 600.0, 400.0, 800.0)]
        public void FootingFrustumVolume_PrismoidalVsPyramidFormula_IdenticalForSimilarBases(
            double l, double b, double lc, double bc, double h2)
        {
            // Verify bases are similar: L / B == Lc / Bc
            Assert.Equal(l / b, lc / bc, 5);

            var model = new IsolatedFootingModel
            {
                LengthMm = l,
                WidthMm = b,
                BaseSlabHeightMm = 300.0,
                TotalHeightMm = 300.0 + h2,
                PedestalLengthMm = lc,
                PedestalWidthMm = bc,
                PedestalStyle = PedestalType.Sloped,
                MemberCount = 1
            };

            double totalVol = FootingGeometryCalculator.CalculateFootingVolume(model);
            double baseVol = (l * b * 300.0) / 1e9;
            double actualPedestalVol = totalVol - baseVol;

            double a1 = l * b;
            double a2 = lc * bc;
            double classicalPyramidVol = (h2 / 3.0) * (a1 + a2 + Math.Sqrt(a1 * a2)) / 1e9;

            Assert.Equal(classicalPyramidVol, actualPedestalVol, 6);
        }

        [Fact]
        public void FootingFrustumVolume_NonSimilarBases_CalculatesExactPrismoidalVolume()
        {
            // Non-similar bases: Rectangular footing 2000x1500 with square pedestal 500x500
            double l = 2000.0, b = 1500.0, lc = 500.0, bc = 500.0, h2 = 400.0;
            var model = new IsolatedFootingModel
            {
                LengthMm = l,
                WidthMm = b,
                BaseSlabHeightMm = 350.0,
                TotalHeightMm = 750.0,
                PedestalLengthMm = lc,
                PedestalWidthMm = bc,
                PedestalStyle = PedestalType.Sloped,
                MemberCount = 1
            };

            double totalVol = FootingGeometryCalculator.CalculateFootingVolume(model);
            double baseVol = (l * b * 350.0) / 1e9;
            double actualPedestalVol = totalVol - baseVol;

            // Simpson's prismoidal formula: h2/6 * (A1 + 4*Am + A2)
            double a1 = l * b;
            double a2 = lc * bc;
            double am = ((l + lc) / 2.0) * ((b + bc) / 2.0);
            double exactPrismoidalVol = (h2 / 6.0) * (a1 + 4.0 * am + a2) / 1e9;

            Assert.Equal(exactPrismoidalVol, actualPedestalVol, 6);
        }

        [Fact]
        public void FootingFormworkAndLeanConcrete_CalculatesCorrectQuantities()
        {
            var model = new IsolatedFootingModel
            {
                LengthMm = 1800.0,
                WidthMm = 1800.0,
                BaseSlabHeightMm = 400.0,
                TotalHeightMm = 900.0,
                PedestalLengthMm = 600.0,
                PedestalWidthMm = 600.0,
                PedestalStyle = PedestalType.Vertical,
                LeanConcreteThicknessMm = 100.0,
                LeanConcreteOverhangMm = 100.0,
                MemberCount = 1
            };

            double fw = FootingGeometryCalculator.CalculateFootingFormwork(model);
            double expectedFw = (2.0 * (1.8 + 1.8) * 0.4) + (2.0 * (0.6 + 0.6) * 0.5); // 2.88 + 1.2 = 4.08 m2
            Assert.Equal(expectedFw, fw, 3);

            double leanVol = FootingGeometryCalculator.CalculateLeanConcreteVolume(model);
            double expectedLean = (2.0 * 2.0 * 0.1); // 0.4 m3
            Assert.Equal(expectedLean, leanVol, 4);
        }
    }
}
