using System.Collections.Generic;

namespace HPKata.Core.DrawingLayout.Models
{
    /// <summary>
    /// Represents a complete grid system (X & Y axes, dimension offset, drawing scale).
    /// </summary>
    public class GridSystemModel
    {
        public string Name { get; set; } = "Main Grid System";
        public double Scale { get; set; } = 100.0;
        public double ExtensionLengthMm { get; set; } = 1500.0;
        public double DimensionOffsetMm { get; set; } = 1000.0;
        public double OverallDimensionOffsetMm { get; set; } = 800.0;
        public double BubbleRadiusMm { get; set; } = 450.0;
        public double TextHeightMm { get; set; } = 350.0;

        public List<GridLineModel> XAxes { get; set; } = new List<GridLineModel>();
        public List<GridLineModel> YAxes { get; set; } = new List<GridLineModel>();
        public List<GridLineModel> SkewedAxes { get; set; } = new List<GridLineModel>();
    }
}
