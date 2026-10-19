using HPKata.Core.Beams.Calculators;
using Xunit;

namespace HPKata.Core.Tests.Beams
{
    public class BeamStirrupZoneCalculatorTests
    {
        [Fact]
        public void CalculateZones_StandardBeam_CalculatesDenseAndSparseZones()
        {
            // Clear span 6000, h=600: Ln/4 = 1500, 2h = 1200 => dense = 1500
            var zones = BeamStirrupZoneCalculator.CalculateZones(0, 6000.0, 300.0, 600.0);

            Assert.Equal(1500.0, zones.LeftDenseZoneLengthMm);
            Assert.Equal(1500.0, zones.RightDenseZoneLengthMm);
            Assert.Equal(3000.0, zones.MidspanSparseZoneLengthMm);
            Assert.Equal(2, zones.TotalLegCount);
        }

        [Fact]
        public void CalculateZones_DeepShortBeam_Uses2hAndCapsAtHalfSpan()
        {
            // Clear span 3000, h=800: Ln/4 = 750, 2h = 1600. Capped at Ln/2 = 1500
            var zones = BeamStirrupZoneCalculator.CalculateZones(0, 3000.0, 300.0, 800.0);

            Assert.Equal(1500.0, zones.LeftDenseZoneLengthMm);
            Assert.Equal(0.0, zones.MidspanSparseZoneLengthMm);
        }

        [Theory]
        [InlineData(250.0, 2)]
        [InlineData(350.0, 2)]
        [InlineData(400.0, 4)]
        [InlineData(600.0, 4)]
        [InlineData(700.0, 6)]
        public void ResolveLegCount_VariousBeamWidths_SelectsAppropriateLegs(double width, int expectedLegs)
        {
            int legs = BeamStirrupZoneCalculator.ResolveLegCount(width);
            Assert.Equal(expectedLegs, legs);
        }

        [Fact]
        public void GenerateStirrupStations_Span_GeneratesDenseAndSparseStations()
        {
            var zones = BeamStirrupZoneCalculator.CalculateZones(
                0, 6000.0, 300.0, 600.0, denseSpacingMm: 100.0, sparseSpacingMm: 200.0);

            var stations = BeamStirrupZoneCalculator.GenerateStirrupStations(6000.0, zones, firstOffsetMm: 50.0);

            Assert.NotEmpty(stations);
            Assert.Equal(50.0, stations[0]); // First stirrup at 50mm
            Assert.True(stations[stations.Count - 1] <= 5950.0);
        }
    }
}
