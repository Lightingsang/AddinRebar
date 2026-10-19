using System;
using System.Collections.Generic;
using HPKata.Core.Models;
using HPKata.Core.Stairs.Calculators;
using HPKata.Core.Stairs.Models;
using Xunit;

namespace HPKata.Core.Tests.Stairs
{
    public class LintelRebarCalculatorTests
    {
        [Fact]
        public void CalculateLintelRebar_StandardLintel_GeneratesThreeItems()
        {
            var lintel = new LintelModel
            {
                LintelMark = "LT-01",
                ClearSpanMm = 1200.0,
                BearingLengthMm = 250.0,
                WidthMm = 220.0,
                HeightMm = 150.0,
                ConcreteCoverMm = 20.0,
                TopHangerCount = 2,
                TopHangerDiaMm = 10,
                BottomTensionCount = 2,
                BottomTensionDiaMm = 12,
                StirrupDiaMm = 6,
                StirrupSpacingEndMm = 100.0,
                StirrupSpacingMidMm = 150.0,
                MemberCount = 1
            };

            List<RebarItem> items = LintelRebarCalculator.CalculateLintelRebar(lintel);

            Assert.NotNull(items);
            Assert.Equal(3, items.Count);

            // Item 1: Top Hangers
            var top = items[0];
            Assert.Equal("1", top.BarMark);
            Assert.Equal("02", top.ShapeCode);
            Assert.Equal(10, top.DiameterMm);
            Assert.Equal(2, top.CountPerMember);
            Assert.True(top.CuttingLengthMm > 1200.0);

            // Item 2: Bottom Tension Bars
            var bot = items[1];
            Assert.Equal("2", bot.BarMark);
            Assert.Equal("02", bot.ShapeCode);
            Assert.Equal(12, bot.DiameterMm);
            Assert.Equal(2, bot.CountPerMember);

            // Item 3: Stirrups
            var stirrups = items[2];
            Assert.Equal("3", stirrups.BarMark);
            Assert.Equal("51", stirrups.ShapeCode);
            Assert.Equal(6, stirrups.DiameterMm);
            Assert.True(stirrups.CountPerMember >= 8);
        }

        [Fact]
        public void LintelModel_Validation_RejectsInvalidDimensions()
        {
            var badSpan = new LintelModel { ClearSpanMm = -100.0 };
            Assert.Throws<ArgumentException>(() => badSpan.Validate());

            var shortBearing = new LintelModel { BearingLengthMm = 100.0 };
            Assert.Throws<ArgumentException>(() => shortBearing.Validate());

            var zeroWidth = new LintelModel { WidthMm = 0.0 };
            Assert.Throws<ArgumentException>(() => zeroWidth.Validate());
        }

        [Fact]
        public void CalculateLintelRebar_NullLintel_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => LintelRebarCalculator.CalculateLintelRebar(null!));
        }
    }
}
