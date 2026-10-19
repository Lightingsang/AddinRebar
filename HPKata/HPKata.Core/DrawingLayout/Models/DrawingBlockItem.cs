using System;

namespace HPKata.Core.DrawingLayout.Models
{
    /// <summary>
    /// Represents an individual drawing detail block to be packed on a sheet (e.g. beam elevation, section cut, schedule table).
    /// </summary>
    public class DrawingBlockItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = string.Empty;
        public double WidthMm { get; set; }
        public double HeightMm { get; set; }
        public double Scale { get; set; } = 100.0;

        // Assigned placement coordinates on sheet (relative to paper origin)
        public double X { get; set; }
        public double Y { get; set; }
        public int SheetIndex { get; set; } = -1;
        public bool IsPlaced => SheetIndex >= 0;

        public DrawingBlockItem()
        {
        }

        public DrawingBlockItem(string title, double widthMm, double heightMm, double scale = 100.0)
        {
            Title = title;
            WidthMm = widthMm;
            HeightMm = heightMm;
            Scale = scale;
        }
    }
}
