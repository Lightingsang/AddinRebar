using System;

namespace HPKata.Core.Walls.Models
{
    /// <summary>
    /// Represents an individual story tier within a multi-story shear wall.
    /// </summary>
    public sealed class WallStorySegment
    {
        public int StoryIndex { get; set; }
        public string StoryName { get; set; } = string.Empty;
        public double BottomElevationMm { get; set; }
        public double StoryHeightMm { get; set; } = 3600.0;
        public double ThicknessMm { get; set; } = 300.0;
        public double LengthMm { get; set; } = 3000.0;

        public double TopElevationMm => BottomElevationMm + StoryHeightMm;

        public void Validate()
        {
            if (StoryHeightMm <= 0.0) throw new ArgumentException("Story height must be positive.");
            if (ThicknessMm <= 0.0) throw new ArgumentException("Wall thickness must be positive.");
            if (LengthMm <= 0.0) throw new ArgumentException("Wall length must be positive.");
        }
    }
}
