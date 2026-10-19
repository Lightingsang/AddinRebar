using HPKata.Core.Columns.Calculators;
using Xunit;

namespace HPKata.Core.Tests.Columns
{
    public class ColumnConfinementCalculatorTests
    {
        [Theory]
        [InlineData(400.0, 400.0, 3000.0, 500.0)] // 3000/6 = 500, max(400, 500, 500) = 500
        [InlineData(600.0, 400.0, 3000.0, 600.0)] // max(600, 500, 500) = 600
        [InlineData(400.0, 400.0, 4200.0, 700.0)] // 4200/6 = 700, max(400, 700, 500) = 700
        public void CalculateConfinementHeight_ReturnsExpectedL0(
            double width, double depth, double clearHeight, double expectedL0)
        {
            double l0 = ColumnConfinementCalculator.CalculateConfinementHeight(width, depth, clearHeight);
            Assert.Equal(expectedL0, l0);
        }

        [Fact]
        public void CalculateDenseSpacing_ClampsTo100MmOrSixDb()
        {
            // Dim 400, dia 20: min(400/4=100, 6*20=120, 100) = 100
            double a1 = ColumnConfinementCalculator.CalculateDenseSpacing(400.0, 20);
            Assert.Equal(100.0, a1);

            // Dim 300, dia 14: min(300/4=75, 6*14=84, 100) = 75
            double a1Small = ColumnConfinementCalculator.CalculateDenseSpacing(300.0, 14);
            Assert.Equal(75.0, a1Small);
        }

        [Fact]
        public void CalculateSparseSpacing_ClampsTo200MmOrFifteenDb()
        {
            // Dim 400, dia 20: min(15*20=300, 400, 200) = 200
            double a2 = ColumnConfinementCalculator.CalculateSparseSpacing(400.0, 20);
            Assert.Equal(200.0, a2);

            // Dim 400, dia 12: min(15*12=180, 400, 200) = 180
            double a2Small = ColumnConfinementCalculator.CalculateSparseSpacing(400.0, 12);
            Assert.Equal(180.0, a2Small);
        }

        [Fact]
        public void CalculateConfinement_FullRun_ReturnsCompleteResult()
        {
            var res = ColumnConfinementCalculator.CalculateConfinement(
                widthMm: 500.0,
                depthMm: 500.0,
                clearHeightMm: 3000.0,
                beamDepthMm: 600.0,
                minLongitudinalDiaMm: 20);

            Assert.Equal(500.0, res.ConfinementHeightL0Mm);
            Assert.Equal(100.0, res.DenseSpacingA1Mm);
            Assert.Equal(200.0, res.SparseSpacingA2Mm);
            Assert.Equal(150.0, res.JointSpacingAJointMm);

            Assert.Equal(5, res.BottomDenseSets); // 500 / 100 = 5
            Assert.Equal(5, res.TopDenseSets);    // 500 / 100 = 5
            Assert.Equal(10, res.MidSparseSets);  // 2000 / 200 = 10
            Assert.Equal(4, res.JointSets);       // 600 / 150 = 4
            Assert.Equal(24, res.TotalSets);
        }
    }
}
