using HPKata.Core.Walls.Calculators;
using HPKata.Core.Walls.Models;
using Xunit;

namespace HPKata.Core.Tests.Walls
{
    public class WallBoundaryCalculatorTests
    {
        [Theory]
        [InlineData(2000.0, 200.0, 400.0)] // 0.15*2000=300, 1.5*200=300, min=400 => 400
        [InlineData(4000.0, 200.0, 600.0)] // 0.15*4000=600, 1.5*200=300, min=400 => 600
        [InlineData(2000.0, 400.0, 600.0)] // 0.15*2000=300, 1.5*400=600, min=400 => 600
        public void CalculateBoundaryLength_ReturnsMaxCondition(
            double wallLen, double wallThick, double expectedBbz)
        {
            double bbz = WallBoundaryCalculator.CalculateBoundaryLength(wallLen, wallThick);
            Assert.Equal(expectedBbz, bbz);
        }

        [Fact]
        public void CalculateBoundaryTieSpacing_ClampsTo100Mm()
        {
            // tw = 300, db = 20 => min(300/2=150, 6*20=120, 100) = 100
            double s1 = WallBoundaryCalculator.CalculateBoundaryTieSpacing(300.0, 20);
            Assert.Equal(100.0, s1);

            // tw = 160, db = 12 => min(160/2=80, 6*12=72, 100) = 72
            double s2 = WallBoundaryCalculator.CalculateBoundaryTieSpacing(160.0, 12);
            Assert.Equal(72.0, s2);
        }

        [Fact]
        public void CreateBoundaryElement_EnforcesMinimum1PercentSteel()
        {
            var be = WallBoundaryCalculator.CreateBoundaryElement(
                "BE1", WallBoundaryPosition.Left, wallLengthMm: 3000.0, wallThicknessMm: 300.0);

            Assert.Equal(450.0, be.LengthBbMm);
            Assert.Equal(300.0, be.ThicknessTbMm);
            Assert.True(be.ReinforcementRatioPercent >= 1.0);
            Assert.True(be.VerticalBarCount >= 6);
        }
    }
}
