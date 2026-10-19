using System.Collections.Generic;

namespace HPKata.Core.DrawingLayout.Models
{
    public enum TitleBlockPlacement
    {
        BottomRight,
        BottomLeft
    }

    /// <summary>
    /// Metadata and boundary geometry for a drawing title block (Khung tên).
    /// Standard corner dimensions in millimeters: Width 160 mm, Height 60 mm (or 180x80).
    /// </summary>
    public class TitleBlockModel
    {
        public double WidthMm { get; set; } = 160.0;
        public double HeightMm { get; set; } = 60.0;
        public TitleBlockPlacement Placement { get; set; } = TitleBlockPlacement.BottomRight;

        public string ProjectName { get; set; } = string.Empty;
        public string SheetName { get; set; } = string.Empty;
        public string SheetNumber { get; set; } = "KC-01";
        public string Scale { get; set; } = "1:100";
        public string Date { get; set; } = string.Empty;
        public string Designer { get; set; } = string.Empty;
        public string Checker { get; set; } = string.Empty;

        public Dictionary<string, string> CustomAttributes { get; set; } = new Dictionary<string, string>();
    }
}
