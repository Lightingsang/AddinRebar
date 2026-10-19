using HPKata.Core.Beams.Calculators;
using Xunit;

namespace HPKata.Core.Tests.Beams
{
    public class BeamSkinBarCalculatorTests
    {
        [Theory]
        [InlineData(600.0, false)]
        [InlineData(699.0, false)]
        [InlineData(700.0, true)]
        [InlineData(900.0, true)]
        public void IsSkinReinforcementRequired_Threshold700_ValidatesCorrectly(double height, bool expected)
        {
            Assert.Equal(expected, BeamSkinBarCalculator.IsSkinReinforcementRequired(height));
        }

        [Theory]
        [InlineData(600.0, 0)]
        [InlineData(750.0, 1)]
        [InlineData(900.0, 1)]
        [InlineData(1200.0, 2)]
        public void CalculateRequiredSideBarPairs_VariousHeights_CalculatesPairs(double height, int expectedPairs)
        {
            int pairs = BeamSkinBarCalculator.CalculateRequiredSideBarPairs(height);
            Assert.Equal(expectedPairs, pairs);
        }

        [Fact]
        public void CalculateSkinBarElevations_DeepBeam_DistributesElevations()
        {
            var elevations = BeamSkinBarCalculator.CalculateSkinBarElevations(800.0, coverMm: 30.0);
            Assert.Single(elevations);
            // webBottom = 80, webTop = 720, mid = 400
            Assert.Equal(400.0, elevations[0]);
        }

        [Fact]
        public void GenerateSkinBarRuns_DeepBeam_GeneratesPairedRuns()
        {
            var runs = BeamSkinBarCalculator.GenerateSkinBarRuns(6000.0, 800.0, coverMm: 30.0, diameterMm: 12);
            Assert.Single(runs);
            Assert.Equal(2, runs[0].BarCount); // 2 bars per layer (left & right face)
            Assert.Equal(12, runs[0].DiameterMm);
            Assert.Equal(6000.0, runs[0].LengthMm);
        }
    }
}
