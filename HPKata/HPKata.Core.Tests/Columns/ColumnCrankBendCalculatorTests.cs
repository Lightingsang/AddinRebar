using System;
using HPKata.Core.Columns.Calculators;
using Xunit;

namespace HPKata.Core.Tests.Columns
{
    public class ColumnCrankBendCalculatorTests
    {
        [Theory]
        [InlineData(0.0, ColumnTransitionStrategy.ContinuousStraight)]
        [InlineData(25.0, ColumnTransitionStrategy.CrankBend)]
        [InlineData(50.0, ColumnTransitionStrategy.CrankBend)]
        [InlineData(50.1, ColumnTransitionStrategy.CutoffAndStarterDowel)]
        [InlineData(100.0, ColumnTransitionStrategy.CutoffAndStarterDowel)]
        public void DetermineStrategy_VariousStepDowns_ReturnsExpectedStrategy(
            double stepDown, ColumnTransitionStrategy expected)
        {
            var strategy = ColumnCrankBendCalculator.DetermineStrategy(stepDown);
            Assert.Equal(expected, strategy);
        }

        [Fact]
        public void DetermineStrategy_NegativeStepDown_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ColumnCrankBendCalculator.DetermineStrategy(-10.0));
        }

        [Theory]
        [InlineData(25.0, 150.0)]
        [InlineData(50.0, 300.0)]
        [InlineData(0.0, 0.0)]
        public void CalculateCrankLength_Calculates1To6Slope(double stepDown, double expectedLength)
        {
            double len = ColumnCrankBendCalculator.CalculateCrankLength(stepDown);
            Assert.Equal(expectedLength, len);
        }

        [Fact]
        public void CalculateStepDown_ConcentricVsFlush_ReturnsExpectedStep()
        {
            // Lower 500, Upper 400
            // Concentric: (500 - 400) / 2 = 50mm
            double concentric = ColumnCrankBendCalculator.CalculateStepDown(500.0, 400.0, isConcentric: true);
            Assert.Equal(50.0, concentric);

            // Flush: 500 - 400 = 100mm
            double flush = ColumnCrankBendCalculator.CalculateStepDown(500.0, 400.0, isConcentric: false);
            Assert.Equal(100.0, flush);
        }

        [Fact]
        public void CalculateCrankElevation_ReturnsValidZCoordinates()
        {
            double floorZ = 3600.0;
            double beamDepth = 600.0;
            double stepDown = 25.0; // Crank length = 150mm

            var (lowerZ, crankTopZ, upperStartZ) = ColumnCrankBendCalculator.CalculateCrankElevation(floorZ, beamDepth, stepDown);
            Assert.Equal(3600.0, lowerZ);
            Assert.Equal(3750.0, crankTopZ); // inside beam depth: 3600 + 150
            Assert.Equal(4200.0, upperStartZ);
        }

        [Fact]
        public void CalculateCrankElevation_ShallowBeamDepth_ClampsCrankVerticalRiseToBeamDepth()
        {
            // stepDown = 50mm => crankLength = 6 * 50 = 300mm
            // shallow beamDepth = 200mm (< 300mm)
            double floorZ = 3600.0;
            double shallowBeamDepth = 200.0;
            double stepDown = 50.0;

            var (lowerZ, crankTopZ, upperStartZ) = ColumnCrankBendCalculator.CalculateCrankElevation(
                floorZ, shallowBeamDepth, stepDown);

            Assert.Equal(3600.0, lowerZ);
            // crank vertical rise is clamped to beamDepth = 200mm instead of 300mm
            Assert.Equal(3800.0, crankTopZ);
            Assert.Equal(3800.0, upperStartZ);
        }
    }
}
