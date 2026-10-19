using HPKata.Core.Walls.Calculators;
using HPKata.Core.Walls.Models;
using Xunit;

namespace HPKata.Core.Tests.Walls
{
    public class WallOpeningTrimmerTests
    {
        [Fact]
        public void CalculateTrimmers_StandardOpening_CalculatesAllReinforcement()
        {
            var op = new WallOpeningModel
            {
                OpeningMark = "DOOR1",
                WidthMm = 1000.0,
                HeightMm = 2100.0,
                HasDiagonalBars = true,
                DiagonalBarsPerCorner = 2,
                DiagonalBarDiameterMm = 16,
                DiagonalBarLengthMm = 900.0,
                JambExtraBarCountPerSide = 2,
                JambExtraBarDiameterMm = 16,
                HeaderExtraBarCountPerEdge = 2,
                HeaderExtraBarDiameterMm = 16
            };

            var trimmers = WallOpeningTrimmerCalculator.CalculateTrimmers(op, anchorageLengthMm: 500.0);

            // Diagonal bars: 4 corners * 2 = 8 bars, length 900mm
            Assert.Equal(8, trimmers.DiagonalBarCount);
            Assert.Equal(900.0, trimmers.DiagonalBarLengthMm);

            // Jamb bars: 2 sides * 2 = 4 bars, length = 2100 + 2*500 = 3100mm
            Assert.Equal(4, trimmers.JambExtraBarCount);
            Assert.Equal(3100.0, trimmers.JambExtraBarLengthMm);

            // Header/sill bars: 2 edges * 2 = 4 bars, length = 1000 + 2*500 = 2000mm
            Assert.Equal(4, trimmers.HeaderSillExtraBarCount);
            Assert.Equal(2000.0, trimmers.HeaderSillExtraBarLengthMm);
        }

        [Fact]
        public void CalculateTrimmers_NoDiagonalBars_DiagonalCountIsZero()
        {
            var op = new WallOpeningModel
            {
                OpeningMark = "W1",
                WidthMm = 800.0,
                HeightMm = 1200.0,
                HasDiagonalBars = false
            };

            var trimmers = WallOpeningTrimmerCalculator.CalculateTrimmers(op);
            Assert.Equal(0, trimmers.DiagonalBarCount);
        }

        [Fact]
        public void CalculateTrimmers_DefaultConstructor_DynamicallyCalculatesTwoTimesAnchorageLength()
        {
            var op = new WallOpeningModel(); // Default DiagonalBarLengthMm = 0.0
            Assert.Equal(0.0, op.DiagonalBarLengthMm);

            var trimmers = WallOpeningTrimmerCalculator.CalculateTrimmers(op, anchorageLengthMm: 500.0);
            Assert.Equal(1000.0, trimmers.DiagonalBarLengthMm);
        }
    }
}
