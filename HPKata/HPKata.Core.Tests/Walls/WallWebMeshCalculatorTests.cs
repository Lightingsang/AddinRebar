using HPKata.Core.Walls.Calculators;
using Xunit;

namespace HPKata.Core.Tests.Walls
{
    public class WallWebMeshCalculatorTests
    {
        [Fact]
        public void CalculateVerticalBarCount_DoubleCurtain_MultipliesByTwo()
        {
            // Lw = 3000, Left = 500, Right = 500 => Web = 2000mm
            // Spacing = 200mm => floor(2000/200) + 1 = 11 bars per curtain
            // Double curtain => 22 bars
            int count = WallWebMeshCalculator.CalculateVerticalBarCount(
                3000.0, 500.0, 500.0, 200.0, isDoubleCurtain: true);
            Assert.Equal(22, count);
        }

        [Fact]
        public void CalculateHorizontalBarCount_DoubleCurtain_MultipliesByTwo()
        {
            // Height = 3600mm, Spacing = 200mm => floor(3600/200) + 1 = 19 bars per curtain
            // Double curtain => 38 bars
            int count = WallWebMeshCalculator.CalculateHorizontalBarCount(
                3600.0, 200.0, isDoubleCurtain: true);
            Assert.Equal(38, count);
        }

        [Fact]
        public void CalculateCrossTieCount_ReturnsAtLeastFourPerSquareMeter()
        {
            // Web = 2000mm (2m), Height = 3000mm (3m) => Area = 6 m2
            // 6 * 4 = 24 ties
            int ties = WallWebMeshCalculator.CalculateCrossTieCount(2000.0, 3000.0, 4.0);
            Assert.Equal(24, ties);
        }

        [Fact]
        public void CalculateUBarDimensions_CalculatesWidthAndLegs()
        {
            // tw = 300, cover = 25, dia = 12 => width = 250, leg = max(30*12=360, 300) = 360
            // total = 250 + 2*360 = 970
            var (w, leg, total) = WallWebMeshCalculator.CalculateUBarDimensions(300.0, 25.0, 12);
            Assert.Equal(250.0, w);
            Assert.Equal(360.0, leg);
            Assert.Equal(970.0, total);
        }
    }
}
