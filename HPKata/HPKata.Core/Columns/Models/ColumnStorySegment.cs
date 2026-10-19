using System;

namespace HPKata.Core.Columns.Models
{
    /// <summary>
    /// Represents an individual story tier within a multi-story column run.
    /// </summary>
    public sealed class ColumnStorySegment
    {
        public int StoryIndex { get; set; }
        public string StoryName { get; set; } = string.Empty;
        public double BottomElevationMm { get; set; }
        public double StoryHeightMm { get; set; }
        public double BeamDepthMm { get; set; }
        public double ClearHeightMm => Math.Max(0.0, StoryHeightMm - BeamDepthMm);
        public double TopElevationMm => BottomElevationMm + StoryHeightMm;

        public double WidthMm { get; set; }
        public double DepthMm { get; set; }

        public double OffsetXFromLowerMm { get; set; }
        public double OffsetYFromLowerMm { get; set; }

        public string ConcreteGrade { get; set; } = "B25";
        public double ConcreteCoverMm { get; set; } = 30.0;

        public ColumnSectionModel Section { get; set; } = new();
        public ColumnTieDistributionModel TieDistribution { get; set; } = new();

        public void Validate()
        {
            if (StoryHeightMm <= 0.0)
                throw new ArgumentException("Story height must be positive.");
            if (WidthMm <= 0.0 || DepthMm <= 0.0)
                throw new ArgumentException("Column dimensions must be positive.");
            if (BeamDepthMm < 0.0 || BeamDepthMm >= StoryHeightMm)
                throw new ArgumentException("Invalid beam depth.");
        }
    }
}
