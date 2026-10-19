using System.Collections.Generic;

namespace HPKata.Core.Columns.Models
{
    /// <summary>
    /// Represents the detailing layout of a multi-story column, containing stories,
    /// longitudinal bar paths, dowels, and tie distributions.
    /// </summary>
    public sealed class ColumnRebarLayout
    {
        public string ColumnMark { get; set; } = "C1";
        public double FoundationBottomElevationMm { get; set; } = -2000.0;
        public double BasePlateElevationMm { get; set; } = 0.0;

        public List<ColumnStorySegment> Stories { get; set; } = new();
        public List<ColumnBarRunModel> LongitudinalBars { get; set; } = new();
        public List<ColumnStarterDowelModel> FoundationDowels { get; set; } = new();
        public Dictionary<int, ColumnTieDistributionModel> StoryTies { get; set; } = new();

        public double TotalHeightMm
        {
            get
            {
                if (Stories.Count == 0) return 0.0;
                return Stories[^1].TopElevationMm - Stories[0].BottomElevationMm;
            }
        }
    }
}
