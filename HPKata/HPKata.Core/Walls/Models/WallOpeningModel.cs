namespace HPKata.Core.Walls.Models
{
    /// <summary>
    /// Represents an opening in a shear wall and its stress-concentration trimmer reinforcement.
    /// </summary>
    public sealed class WallOpeningModel
    {
        public string OpeningMark { get; set; } = "OP1";

        public double XOffsetMm { get; set; } = 1000.0;
        public double YOffsetMm { get; set; } = 0.0;
        public double WidthMm { get; set; } = 1000.0;
        public double HeightMm { get; set; } = 2100.0;

        public bool HasDiagonalBars { get; set; } = true;
        public int DiagonalBarDiameterMm { get; set; } = 16;
        public int DiagonalBarsPerCorner { get; set; } = 2;
        public double DiagonalBarLengthMm { get; set; } = 0.0;

        public int JambExtraBarDiameterMm { get; set; } = 16;
        public int JambExtraBarCountPerSide { get; set; } = 2;

        public int HeaderExtraBarDiameterMm { get; set; } = 16;
        public int HeaderExtraBarCountPerEdge { get; set; } = 2;
    }
}
