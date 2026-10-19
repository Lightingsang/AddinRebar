namespace HPKata.Core.DrawingLayout.Models
{
    public enum SheetFormat
    {
        A0,
        A1,
        A2,
        A3,
        A4
    }

    /// <summary>
    /// ISO 5457 / TCVN standard drawing sheet dimensions and margins in millimeters.
    /// </summary>
    public class SheetSizeDefinition
    {
        public SheetFormat Format { get; set; } = SheetFormat.A1;
        public double WidthMm { get; set; } = 841.0;
        public double HeightMm { get; set; } = 594.0;
        public bool IsLandscape { get; set; } = true;

        public double MarginLeftMm { get; set; } = 25.0;
        public double MarginTopMm { get; set; } = 10.0;
        public double MarginRightMm { get; set; } = 10.0;
        public double MarginBottomMm { get; set; } = 10.0;

        public double PrintableWidthMm => WidthMm - MarginLeftMm - MarginRightMm;
        public double PrintableHeightMm => HeightMm - MarginTopMm - MarginBottomMm;

        public static SheetSizeDefinition Create(SheetFormat format, bool landscape = true)
        {
            var (w, h) = format switch
            {
                SheetFormat.A0 => (1189.0, 841.0),
                SheetFormat.A1 => (841.0, 594.0),
                SheetFormat.A2 => (594.0, 420.0),
                SheetFormat.A3 => (420.0, 297.0),
                SheetFormat.A4 => (297.0, 210.0),
                _ => (841.0, 594.0)
            };

            return new SheetSizeDefinition
            {
                Format = format,
                WidthMm = landscape ? w : h,
                HeightMm = landscape ? h : w,
                IsLandscape = landscape
            };
        }
    }
}
