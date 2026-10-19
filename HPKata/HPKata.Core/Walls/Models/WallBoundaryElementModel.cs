using System;

namespace HPKata.Core.Walls.Models
{
    public enum WallBoundaryPosition
    {
        Left,
        Right,
        WebCorner,
        FlangeEnd
    }

    /// <summary>
    /// Represents a concentrated boundary element (cột biên / vùng biên vách) resisting
    /// wall boundary flexural tension and compression.
    /// </summary>
    public sealed class WallBoundaryElementModel
    {
        public string Mark { get; set; } = "BE1";
        public WallBoundaryPosition Position { get; set; } = WallBoundaryPosition.Left;

        public double LengthBbMm { get; set; } = 500.0;
        public double ThicknessTbMm { get; set; } = 300.0;

        public int VerticalBarCount { get; set; } = 8;
        public int VerticalBarDiameterMm { get; set; } = 20;

        public int TieDiameterMm { get; set; } = 8;
        public double TieSpacingMm { get; set; } = 100.0;

        public int CrossTieDiameterMm { get; set; } = 8;
        public int CrossTieCountPerSet { get; set; } = 2;

        public double TotalSteelAreaMm2 =>
            VerticalBarCount * (Math.PI * VerticalBarDiameterMm * VerticalBarDiameterMm / 4.0);

        public double ReinforcementRatioPercent =>
            (LengthBbMm > 0.0 && ThicknessTbMm > 0.0)
                ? (TotalSteelAreaMm2 / (LengthBbMm * ThicknessTbMm) * 100.0)
                : 0.0;
    }
}
