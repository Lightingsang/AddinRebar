using System;
using System.Collections.Generic;
using HPKata.Core.Stairs.Calculators;
using HPKata.Core.Stairs.Models;
using Xunit;

namespace HPKata.Core.Tests.Stairs
{
    public class StairGeometryCalculatorTests
    {
        [Fact]
        public void CheckErgonomics_BlondelFormula_ValidRanges()
        {
            // Standard: 2 * 160 + 280 = 600 mm (within 580..660)
            bool ok = StairGeometryCalculator.CheckErgonomics(160.0, 280.0, out double blondel);
            Assert.True(ok);
            Assert.Equal(600.0, blondel);

            // Too steep / small: 2 * 140 + 200 = 480 mm (< 580)
            bool okSmall = StairGeometryCalculator.CheckErgonomics(140.0, 200.0, out double blondelSmall);
            Assert.False(okSmall);
            Assert.Equal(480.0, blondelSmall);

            // Too large: 2 * 200 + 350 = 750 mm (> 660)
            bool okLarge = StairGeometryCalculator.CheckErgonomics(200.0, 350.0, out double blondelLarge);
            Assert.False(okLarge);
            Assert.Equal(750.0, blondelLarge);
        }

        [Fact]
        public void CalculateConcreteVolume_ValidFlight_ReturnsAccurateVolume()
        {
            var flight = new StairFlightModel
            {
                FlightMark = "ST-01",
                RiserCount = 11,
                RiserHeightMm = 160.0,
                TreadWidthMm = 280.0,
                FlightWidthMm = 1200.0,
                ThroatThicknessMm = 120.0,
                BottomLandingLengthMm = 1200.0,
                TopLandingLengthMm = 1200.0,
                BottomLandingThkMm = 120.0,
                TopLandingThkMm = 120.0,
                MemberCount = 1
            };

            double vol = StairGeometryCalculator.CalculateConcreteVolume(flight);

            // Expected components:
            // Waist: 1.2m * 0.12m * SlopeLength
            // Run = 10 * 280 = 2800 mm. Rise = 11 * 160 = 1760 mm.
            // SlopeLen = sqrt(2800^2 + 1760^2) = 3308.11 mm
            // WaistVol = 1.2 * 0.12 * 3.30811 = 0.4764 m3
            // StepsVol = 10 * (0.5 * 0.28 * 0.16) * 1.2 = 0.2688 m3
            // Landings = 2 * (1.2 * 1.2 * 0.12) = 0.3456 m3
            // Total approx = 1.0908 m3
            Assert.InRange(vol, 1.0, 1.2);
        }

        [Fact]
        public void CalculateFormworkArea_ValidFlight_ReturnsPositiveArea()
        {
            var flight = new StairFlightModel
            {
                FlightMark = "ST-02",
                RiserCount = 10,
                RiserHeightMm = 150.0,
                TreadWidthMm = 300.0,
                FlightWidthMm = 1000.0,
                ThroatThicknessMm = 100.0,
                BottomLandingLengthMm = 1000.0,
                TopLandingLengthMm = 1000.0,
                MemberCount = 2
            };

            double area = StairGeometryCalculator.CalculateFormworkArea(flight);
            Assert.True(area > 0);
            Assert.InRange(area, 10.0, 25.0);
        }

        [Fact]
        public void GenerateSectionProfile_ValidFlight_ReturnsClosedLoopPoints()
        {
            var flight = new StairFlightModel
            {
                RiserCount = 3,
                RiserHeightMm = 150.0,
                TreadWidthMm = 300.0,
                FlightWidthMm = 1000.0,
                BottomLandingLengthMm = 500.0,
                TopLandingLengthMm = 500.0,
                BottomLandingThkMm = 100.0,
                TopLandingThkMm = 100.0
            };

            List<(double X, double Y)> profile = StairGeometryCalculator.GenerateSectionProfile(flight);

            Assert.NotNull(profile);
            Assert.True(profile.Count >= 8);
            Assert.Equal((0.0, 0.0), profile[0]);
            Assert.Equal(500.0, profile[1].X);
            Assert.Equal(0.0, profile[1].Y);
        }

        [Fact]
        public void NullFlight_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => StairGeometryCalculator.CalculateConcreteVolume(null!));
            Assert.Throws<ArgumentNullException>(() => StairGeometryCalculator.CalculateFormworkArea(null!));
            Assert.Throws<ArgumentNullException>(() => StairGeometryCalculator.GenerateSectionProfile(null!));
        }
    }
}
