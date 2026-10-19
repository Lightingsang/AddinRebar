namespace HPKata.Core.Walls.Models
{
    /// <summary>
    /// Represents distributed double-curtain web reinforcement (thép thân vách),
    /// cross-ties, and U-bar edge closures.
    /// </summary>
    public sealed class WallWebReinforcementModel
    {
        public bool IsDoubleCurtain { get; set; } = true;

        public int VerticalBarDiameterMm { get; set; } = 12;
        public double VerticalBarSpacingMm { get; set; } = 200.0;

        public int HorizontalBarDiameterMm { get; set; } = 12;
        public double HorizontalBarSpacingMm { get; set; } = 200.0;

        public int CrossTieDiameterMm { get; set; } = 8;
        public double CrossTiesPerSquareMeter { get; set; } = 4.0;

        public bool HasUBarEdgeCaps { get; set; } = true;
        public int UBarDiameterMm { get; set; } = 12;
        public double UBarLapLengthMm { get; set; } = 400.0;
    }
}
