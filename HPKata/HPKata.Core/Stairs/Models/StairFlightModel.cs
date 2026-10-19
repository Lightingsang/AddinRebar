using System;

namespace HPKata.Core.Stairs.Models
{
    /// <summary>
    /// Represents a reinforced concrete staircase flight connecting landings,
    /// with geometric parameters, step nosings, and reinforcement specifications.
    /// </summary>
    public class StairFlightModel
    {
        public string FlightMark { get; set; } = "ST1";
        public int RiserCount { get; set; } = 11;
        public double RiserHeightMm { get; set; } = 160.0;
        public int TreadCount => Math.Max(1, RiserCount - 1);
        public double TreadWidthMm { get; set; } = 280.0;

        public double FlightWidthMm { get; set; } = 1100.0;
        public double ThroatThicknessMm { get; set; } = 120.0;

        public double BottomLandingLengthMm { get; set; } = 1200.0;
        public double TopLandingLengthMm { get; set; } = 1200.0;
        public double BottomLandingThkMm { get; set; } = 120.0;
        public double TopLandingThkMm { get; set; } = 120.0;

        public double ConcreteCoverMm { get; set; } = 15.0;

        public int BottomTensionDiaMm { get; set; } = 12;
        public double BottomTensionSpacingMm { get; set; } = 150.0;
        public double ScissorCrankExtensionMm => Math.Max(500.0, 35.0 * BottomTensionDiaMm);

        public int TopNegativeDiaMm { get; set; } = 10;
        public double TopNegativeSpacingMm { get; set; } = 150.0;

        public int StepNoseDiaMm { get; set; } = 6;
        public double StepNoseSpacingMm { get; set; } = 200.0;

        public int MemberCount { get; set; } = 1;
        public string ConcreteGrade { get; set; } = "B25";
        public string SteelGrade { get; set; } = "CB300-T";

        public double FlightRunLengthMm => TreadCount * TreadWidthMm;
        public double FlightTotalHeightMm => RiserCount * RiserHeightMm;
        public double SlopeAngleRad => Math.Atan2(RiserHeightMm, TreadWidthMm);
        public double SlopeAngleDeg => SlopeAngleRad * (180.0 / Math.PI);
        public double SlopeLengthMm => Math.Sqrt(Math.Pow(FlightRunLengthMm, 2) + Math.Pow(FlightTotalHeightMm, 2));

        public void Validate()
        {
            if (RiserCount < 2) throw new ArgumentException("Riser count must be at least 2.");
            if (RiserHeightMm <= 0 || TreadWidthMm <= 0) throw new ArgumentException("Step dimensions must be positive.");
            if (ThroatThicknessMm <= 0) throw new ArgumentException("Waist thickness must be positive.");
            if (MemberCount < 1) MemberCount = 1;
        }
    }
}
