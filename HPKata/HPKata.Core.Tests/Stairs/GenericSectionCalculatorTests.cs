using System;
using System.Collections.Generic;
using HPKata.Core.Models;
using HPKata.Core.Stairs.Calculators;
using HPKata.Core.Stairs.Models;
using Xunit;

namespace HPKata.Core.Tests.Stairs
{
    public class GenericSectionCalculatorTests
    {
        [Fact]
        public void CreateRectangularSection_MultipleBars_PlacesCoordinatesAccurately()
        {
            var sec = GenericSectionCalculator.CreateRectangularSection(
                compMark: "D1",
                widthMm: 300.0,
                heightMm: 600.0,
                coverMm: 25.0,
                topBarCount: 3,
                topBarDia: 18,
                botBarCount: 4,
                botBarDia: 20,
                stirrupDia: 8);

            Assert.NotNull(sec);
            Assert.Equal("D1", sec.ComponentMark);
            Assert.Equal(300.0, sec.WidthMm);
            Assert.Equal(600.0, sec.HeightMm);

            // Longitudinal bars count = 3 + 4 = 7
            Assert.Equal(7, sec.LongitudinalBars.Count);

            // First top bar X should be innerMinX = 25 + 8 + 9 = 42
            Assert.Equal(42.0, sec.LongitudinalBars[0].X);
            Assert.Equal(600.0 - 25.0 - 8.0 - 9.0, sec.LongitudinalBars[0].Y);

            // Stirrup hoop check
            Assert.Single(sec.StirrupHoops);
            var hoop = sec.StirrupHoops[0];
            Assert.Equal(4, hoop.Vertices.Count);
            Assert.Equal((25.0, 25.0), hoop.Vertices[0]);
            Assert.Equal((275.0, 25.0), hoop.Vertices[1]);
        }

        [Fact]
        public void CreateRectangularSection_SingleBar_CentersBar()
        {
            var sec = GenericSectionCalculator.CreateRectangularSection(
                compMark: "COL1",
                widthMm: 200.0,
                heightMm: 200.0,
                coverMm: 20.0,
                topBarCount: 1,
                topBarDia: 16,
                botBarCount: 1,
                botBarDia: 16,
                stirrupDia: 6);

            Assert.Equal(2, sec.LongitudinalBars.Count);
            Assert.Equal(100.0, sec.LongitudinalBars[0].X); // Centered
            Assert.Equal(100.0, sec.LongitudinalBars[1].X); // Centered
        }

        [Fact]
        public void SectionToRebarItems_GeneratesValidItems()
        {
            var sec = GenericSectionCalculator.CreateRectangularSection(
                compMark: "D2",
                widthMm: 250.0,
                heightMm: 500.0,
                coverMm: 25.0,
                topBarCount: 2,
                topBarDia: 16,
                botBarCount: 2,
                botBarDia: 16,
                stirrupDia: 8);

            List<RebarItem> items = GenericSectionCalculator.SectionToRebarItems(sec, lengthMm: 3000.0, memberCount: 2);

            Assert.NotNull(items);
            // 4 longitudinal + 1 stirrup item = 5 items
            Assert.Equal(5, items.Count);

            foreach (var item in items)
            {
                Assert.Equal("D2", item.ComponentMark);
                Assert.Equal(2, item.MemberCount);
                Assert.True(item.TotalWeightKg > 0);
            }
        }

        [Fact]
        public void NullSection_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => GenericSectionCalculator.SectionToRebarItems(null!));
        }
    }
}
