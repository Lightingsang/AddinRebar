using System;
using HPKata.Core.Walls.Models;
using Xunit;

namespace HPKata.Core.Tests.Walls
{
    public class ShearWallModelTests
    {
        [Fact]
        public void WallStorySegment_Validation_ThrowsOnInvalidDimensions()
        {
            var story = new WallStorySegment
            {
                StoryHeightMm = -100.0,
                ThicknessMm = 300.0,
                LengthMm = 3000.0
            };
            Assert.Throws<ArgumentException>(() => story.Validate());

            story.StoryHeightMm = 3600.0;
            story.ThicknessMm = 0.0;
            Assert.Throws<ArgumentException>(() => story.Validate());

            story.ThicknessMm = 300.0;
            story.LengthMm = -500.0;
            Assert.Throws<ArgumentException>(() => story.Validate());
        }

        [Fact]
        public void WallBoundaryElementModel_AreaAndRatio_CalculatesCorrectly()
        {
            var be = new WallBoundaryElementModel
            {
                Mark = "BE1",
                LengthBbMm = 500.0,
                ThicknessTbMm = 300.0,
                VerticalBarCount = 8,
                VerticalBarDiameterMm = 20
            };

            double expectedBarArea = 8.0 * (Math.PI * 20.0 * 20.0 / 4.0);
            Assert.Equal(expectedBarArea, be.TotalSteelAreaMm2, 2);

            double expectedRatio = (expectedBarArea / (500.0 * 300.0)) * 100.0;
            Assert.Equal(expectedRatio, be.ReinforcementRatioPercent, 2);
        }

        [Fact]
        public void ShearWallModel_TotalHeight_CalculatesSumOfStories()
        {
            var wall = new ShearWallModel { WallMark = "V1" };
            wall.Stories.Add(new WallStorySegment { StoryIndex = 0, BottomElevationMm = 0.0, StoryHeightMm = 3600.0 });
            wall.Stories.Add(new WallStorySegment { StoryIndex = 1, BottomElevationMm = 3600.0, StoryHeightMm = 3300.0 });

            Assert.Equal(6900.0, wall.TotalHeightMm);
        }
    }
}
