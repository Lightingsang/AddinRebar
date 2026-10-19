using System;
using System.Linq;
using HPKata.Core.Foundations.Calculators;
using Xunit;

namespace HPKata.Core.Tests.Foundations
{
    public class PileCoordinateGeneratorTests
    {
        [Fact]
        public void GenerateLayout_SinglePile_IsAtOrigin()
        {
            var (piles, recL, recB) = PileCoordinateGenerator.GenerateLayout(1, 400.0);
            Assert.Single(piles);
            Assert.Equal(0.0, piles[0].X);
            Assert.Equal(0.0, piles[0].Y);
            Assert.True(recL > 400.0);
            Assert.True(recB > 400.0);
        }

        [Fact]
        public void GenerateLayout_TwoPiles_AreSymmetricOnXAxis()
        {
            var (piles, recL, recB) = PileCoordinateGenerator.GenerateLayout(2, 400.0, customSpacingMm: 1200.0);
            Assert.Equal(2, piles.Count);
            Assert.Equal(-600.0, piles[0].X);
            Assert.Equal(600.0, piles[1].X);
            Assert.Equal(0.0, piles[0].Y);
            Assert.Equal(0.0, piles[1].Y);
            Assert.Equal(0.0, piles.Average(p => p.X), 3);
        }

        [Fact]
        public void GenerateLayout_ThreePiles_EquilateralTriangleCentroidIsZero()
        {
            var (piles, _, _) = PileCoordinateGenerator.GenerateLayout(3, 400.0, customSpacingMm: 1200.0);
            Assert.Equal(3, piles.Count);
            double avgX = piles.Average(p => p.X);
            double avgY = piles.Average(p => p.Y);
            Assert.True(Math.Abs(avgX) < 1e-3, $"Centroid X {avgX} is not 0");
            Assert.True(Math.Abs(avgY) < 1e-3, $"Centroid Y {avgY} is not 0");
        }

        [Fact]
        public void GenerateLayout_FourPiles_Grid2x2Symmetry()
        {
            var (piles, recL, recB) = PileCoordinateGenerator.GenerateLayout(4, 400.0, customSpacingMm: 1200.0);
            Assert.Equal(4, piles.Count);
            Assert.Equal(0.0, piles.Average(p => p.X), 3);
            Assert.Equal(0.0, piles.Average(p => p.Y), 3);
            Assert.Equal(recL, recB);
        }

        [Fact]
        public void GenerateLayout_FivePiles_QuincunxCenterAtOrigin()
        {
            var (piles, _, _) = PileCoordinateGenerator.GenerateLayout(5, 400.0, customSpacingMm: 1200.0);
            Assert.Equal(5, piles.Count);
            Assert.Contains(piles, p => Math.Abs(p.X) < 1e-3 && Math.Abs(p.Y) < 1e-3);
            Assert.Equal(0.0, piles.Average(p => p.X), 3);
            Assert.Equal(0.0, piles.Average(p => p.Y), 3);
        }

        [Theory]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(9)]
        [InlineData(12)]
        public void GenerateLayout_HigherCounts_ProducesCorrectCountAndNonZeroDimensions(int count)
        {
            var (piles, recL, recB) = PileCoordinateGenerator.GenerateLayout(count, 400.0);
            Assert.Equal(count, piles.Count);
            Assert.True(recL >= 1200.0);
            Assert.True(recB >= 1200.0);
        }
    }
}
