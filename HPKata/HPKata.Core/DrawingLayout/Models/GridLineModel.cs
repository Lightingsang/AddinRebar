using System;

namespace HPKata.Core.DrawingLayout.Models
{
    /// <summary>
    /// Geometric representation of a single grid line axis with bubble endpoints and label.
    /// Coordinates are in millimeters (mm).
    /// </summary>
    public class GridLineModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Label { get; set; } = string.Empty;
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }
        public GridBubbleOrientation BubblePlacement { get; set; } = GridBubbleOrientation.BothEnds;
        public double BubbleRadiusMm { get; set; } = 450.0;
        public double TextHeightMm { get; set; } = 350.0;
        public bool IsCustomAngle { get; set; }
        public double AngleRad { get; set; }

        public GridLineModel()
        {
        }

        public GridLineModel(string label, double x1, double y1, double x2, double y2)
        {
            Label = label;
            StartX = x1;
            StartY = y1;
            EndX = x2;
            EndY = y2;
            AngleRad = Math.Atan2(y2 - y1, x2 - x1);
        }
    }
}
