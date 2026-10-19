using System;
using System.Collections.Generic;
using HPKata.Core.Models;
using HPKata.Core.Stairs.Calculators;
using HPKata.Core.Stairs.Models;
using Xunit;

namespace HPKata.Core.Tests.Stairs
{
    public class StairRebarCalculatorTests
    {
        [Fact]
        public void CalculateStairRebar_StandardFlight_GeneratesExpectedItems()
        {
            var flight = CreateStandardFlight();
            List<RebarItem> items = StairRebarCalculator.CalculateStairRebar(flight);

            Assert.NotNull(items);
            Assert.Equal(6, items.Count);

            // Item 1: Bottom Flight Tension Bar with Scissor Crank Extension
            var botFlight = items[0];
            Assert.Equal("1", botFlight.BarMark);
            Assert.Equal("02", botFlight.ShapeCode);
            Assert.Equal(12, botFlight.DiameterMm);
            // Scissor extension = max(500, 35 * 12) = 500 mm
            Assert.Equal(500.0, flight.ScissorCrankExtensionMm);
            Assert.True(botFlight.CuttingLengthMm > flight.SlopeLengthMm + 500.0);

            // Item 2: Landing bottom bar crossing
            var botLand = items[1];
            Assert.Equal("2", botLand.BarMark);
            Assert.Equal(1200.0 + 500.0, botLand.CuttingLengthMm);

            // Item 3: Top negative support bar
            var topNeg = items[2];
            Assert.Equal("3", topNeg.BarMark);
            Assert.Equal(10, topNeg.DiameterMm);

            // Item 5: Step nosing bars
            var noseBar = items[4];
            Assert.Equal("5", noseBar.BarMark);
            Assert.Equal(6, noseBar.DiameterMm);
            Assert.Equal(11, noseBar.CountPerMember); // 11 risers = 11 nosing bars
        }

        private static StairFlightModel CreateStandardFlight()
        {
            return new StairFlightModel
            {
                FlightMark = "ST1",
                RiserCount = 11,
                RiserHeightMm = 160.0,
                TreadWidthMm = 280.0,
                FlightWidthMm = 1200.0,
                ThroatThicknessMm = 120.0,
                BottomLandingLengthMm = 1200.0,
                TopLandingLengthMm = 1200.0,
                ConcreteCoverMm = 15.0,
                BottomTensionDiaMm = 12,
                BottomTensionSpacingMm = 150.0,
                TopNegativeDiaMm = 10,
                TopNegativeSpacingMm = 150.0,
                StepNoseDiaMm = 6,
                StepNoseSpacingMm = 200.0,
                MemberCount = 1
            };
        }

        [Fact]
        public void ScissorCrankExtension_EnforcesAtLeast35d()
        {
            var flightLarge = new StairFlightModel
            {
                BottomTensionDiaMm = 16 // 35 * 16 = 560 mm > 500 mm
            };

            Assert.Equal(560.0, flightLarge.ScissorCrankExtensionMm);

            var flightSmall = new StairFlightModel
            {
                BottomTensionDiaMm = 10 // 35 * 10 = 350 mm < 500 mm -> clamps to 500 mm
            };

            Assert.Equal(500.0, flightSmall.ScissorCrankExtensionMm);
        }

        [Fact]
        public void CalculateStairRebar_NullFlight_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => StairRebarCalculator.CalculateStairRebar(null!));
        }

        [Fact]
        public void CalculateStairRebar_MultiMember_ScalesTotalQuantities()
        {
            var flight = new StairFlightModel
            {
                FlightMark = "ST3",
                MemberCount = 4
            };

            var items = StairRebarCalculator.CalculateStairRebar(flight);
            foreach (var item in items)
            {
                Assert.Equal(4, item.MemberCount);
                Assert.Equal(item.CountPerMember * 4, item.TotalCount);
                Assert.True(item.TotalWeightKg > 0);
            }
        }
    }
}
