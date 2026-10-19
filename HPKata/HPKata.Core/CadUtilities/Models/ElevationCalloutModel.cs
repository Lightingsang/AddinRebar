namespace HPKata.Core.CadUtilities.Models
{
    public enum ElevationArrowDirection
    {
        Up,
        Down
    }

    /// <summary>
    /// Geometric and text representation of a section level datum callout (cao độ mặt cắt).
    /// </summary>
    public class ElevationCalloutModel
    {
        public Point2D InsertionPoint { get; set; }
        public double ElevationM { get; set; }
        public string FormattedText { get; set; } = "±0.000";
        public ElevationArrowDirection Direction { get; set; } = ElevationArrowDirection.Up;
        public double SymbolSizeMm { get; set; } = 350.0;
        public double TextHeightMm { get; set; } = 250.0;

        public ElevationCalloutModel()
        {
        }

        public ElevationCalloutModel(Point2D pt, double elevationM, string formattedText)
        {
            InsertionPoint = pt;
            ElevationM = elevationM;
            FormattedText = formattedText;
        }
    }
}
