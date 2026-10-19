namespace HPKata.Core.CadUtilities.Models
{
    /// <summary>
    /// Annotation model for slope / gradient indication with arrow direction and readable text angle.
    /// </summary>
    public class SlopeAnnotationModel
    {
        public Point2D StartPoint { get; set; }
        public Point2D EndPoint { get; set; }
        public double GradientPercent { get; set; }
        public string RatioText { get; set; } = string.Empty;
        public string FormattedText { get; set; } = string.Empty;
        public double TextAngleRad { get; set; }
        public bool IsVertical { get; set; }

        public SlopeAnnotationModel()
        {
        }
    }
}
