using System;

namespace HPKata.Core.Stairs.Models
{
    /// <summary>
    /// Represents a reinforced concrete lintel beam spanning a masonry wall opening
    /// with masonry wall bearings, top hanger bars, bottom tension bars, and stirrups.
    /// </summary>
    public class LintelModel
    {
        public string LintelMark { get; set; } = "LT1";
        public double ClearSpanMm { get; set; } = 1200.0;
        public double BearingLengthMm { get; set; } = 250.0;
        public double WidthMm { get; set; } = 220.0;
        public double HeightMm { get; set; } = 150.0;
        public double ConcreteCoverMm { get; set; } = 20.0;

        public int TopHangerCount { get; set; } = 2;
        public int TopHangerDiaMm { get; set; } = 10;

        public int BottomTensionCount { get; set; } = 2;
        public int BottomTensionDiaMm { get; set; } = 12;

        public int StirrupDiaMm { get; set; } = 6;
        public double StirrupSpacingEndMm { get; set; } = 100.0;
        public double StirrupSpacingMidMm { get; set; } = 150.0;

        public int MemberCount { get; set; } = 1;
        public string ConcreteGrade { get; set; } = "B20";
        public string SteelGrade { get; set; } = "CB240-T";

        public double TotalLengthMm => ClearSpanMm + 2.0 * BearingLengthMm;

        public void Validate()
        {
            if (ClearSpanMm <= 0) throw new ArgumentException("Clear span must be positive.");
            if (BearingLengthMm < 150.0) throw new ArgumentException("Bearing length must be at least 150mm.");
            if (WidthMm <= 0 || HeightMm <= 0) throw new ArgumentException("Dimensions must be positive.");
            if (MemberCount < 1) MemberCount = 1;
        }
    }
}
